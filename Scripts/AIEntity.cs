using System;
using System.Collections;
using Cinemachine;
using Photon.Pun;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class AIEntity : MonoBehaviourPunCallbacks, IDamageable, IStunable, IPunObservable
{
    [TabGroup("Info")] 
    public string nickname;
    [TabGroup("Info")] 
    [Range(1, 30)] public int level;
    [TabGroup("Info")] 
    [Range(1, 1000)]public int expWorth;
    [TabGroup("Info")] 
    [SerializeField] private OverheadUI overheadUI;
    
    [TabGroup("Movement")] public float moveSpeed = 300, patrolSpeed = 1;
    [TabGroup("Stats")] public int health, damage;
    [TabGroup("Stats")] public float attackRate = 3f;
    [TabGroup("Stats")] public float attackRange = 5f;
    [HideInInspector] public Vector3 dirToTarget;
    [HideInInspector] public Vector3 lookDirection;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public LineOfSight lineOfSight;
    [HideInInspector] public AggroTarget aggroTarget;
    private bool hasDied;
    [HideInInspector] public bool canAttack;

    [TabGroup("Movement")] [SerializeField]
    private float rotationSpeed = 9f;

    [HideInInspector] public Animator anim;
    [HideInInspector] public AudioEntity audio;

    [TabGroup("Debugging")]
    [SerializeField]private float rayRadiusSide = 0.8f;
    [TabGroup("Debugging")]
    [SerializeField]private float rayRadiusFor = 0.5f;
    [TabGroup("Debugging")]
    [SerializeField]private float rayDstSide = 0.5f;
    [TabGroup("Debugging")]
    [SerializeField]private float rayDstFor = 1.2f;

    protected bool isStunned;
    public bool flying;
    public float patrolArea;
    public Vector3 monsterAreaT;
    public bool startPoint;
    private int maxHealth;

    private AIBaseState currentState;

    public AIBaseState CurrentState
    {
        get { return currentState; }
    }

    public readonly AIPatrolState PatrolState = new AIPatrolState();
    public readonly AIChaseState ChaseState = new AIChaseState();
    public readonly AIAttackState AttackState = new AIAttackState();

    private void Awake()
    {
        lineOfSight = GetComponent<LineOfSight>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        aggroTarget = GetComponent<AggroTarget>();
        audio = GetComponent<AudioEntity>();
    }

    private void Start()
    {
        TransitionToState(PatrolState);
        anim.SetBool("isDead", false);
        canAttack = true;

        maxHealth = health;
        
        if (overheadUI != null)
        {
            overheadUI.SetName(nickname);
            UpdateMyUI();
        }
    }

    private void FixedUpdate()
    {
        if (hasDied || isStunned) return;

        currentState.Update(this);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.gameObject.layer == 9)
        {
            currentState.OnCollisionEnter(this);
        }
    }

    //Might need to call this thru the server with an rpc. Dunno how tho maybe with a temp component?.
    public void TransitionToState(AIBaseState state)
    {
        currentState = state;
        currentState.EnterState(this);
    }
    
    public void TakeDamage(int damage, GameObject attacker)
    {
        if (!hasDied)
        {
            GameManager.Instance.UIManager.CreatePopupText(damage.ToString(), transform.position, false, false);
         
            if(audio.InAudioRange()) AudioManager.instance.PlaySFX(audio.hit, 1f);
            photonView.RPC("RPCTakeDamage", RpcTarget.All, damage);

            hasDied = isDead(health);
            Debug.Log("I'm dead: " + hasDied);
            
            if (hasDied)
            {
                attacker.GetComponent<PlayerController>().GiveExperience(expWorth, nickname);
            }
        }
    }

    //Calls the takeDamage function thru the server and updates all clients.
    [PunRPC]
    private void RPCTakeDamage(int dmg)
    {
        health -= dmg;
        anim.SetTrigger("TakeDamage");
        UpdateMyUI();
    }

    public bool isDead(int hp)
    {
        if (hp < 1)
        {
            anim.SetBool("isDead", true);
            if(audio.InAudioRange()) AudioManager.instance.PlaySFX(audio.death, 1f);
            Invoke(nameof(DestroyAI), 5f);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void DestroyAI()
    {
        PhotonNetwork.Destroy(gameObject);
    }

    private void UpdateMyUI()
    {
        if (overheadUI != null)
        {
            float percentHp = (float)health / maxHealth;
            overheadUI.UpdateUI(level, percentHp);
        }
    }
    
    /// <summary>
    /// Rotation of the AI with a lerp smoothing.
    /// </summary>
    public void RotateAI(Vector3 target)
    {
        //Rotation
        if (Vector3.Distance(target, transform.position) > 1)
        {
            lookDirection = target - transform.position;
            var newRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
        }
    }

    //Cast three spherecasts to check for obstacles. Used for the AIs pathfinding.
    public bool ForwardRaycast()
    {
        Vector3 dirToTarget = (aggroTarget.currentTarget.player.transform.position - transform.position).normalized;
        //Debug.DrawLine(transform.position + Vector3.up, aggroTarget.currentTarget.player.transform.position, Color.yellow);
        //if(Physics.Raycast(transform.position + Vector3.up, dirToTarget, 2f, lineOfSight.obstacleMask))
        if(Physics.SphereCast(transform.position + Vector3.up, rayRadiusSide, transform.forward, out RaycastHit hit,rayDstFor, lineOfSight.obstacleMask))
        {
            return true;
        }

        if(Physics.SphereCast(transform.position + Vector3.up, rayRadiusSide, transform.right, out RaycastHit hit2,rayDstSide, lineOfSight.obstacleMask))
        {
            return true;
        }

        if(Physics.SphereCast(transform.position + Vector3.up, rayRadiusSide, -transform.right, out RaycastHit hit3,rayDstSide, lineOfSight.obstacleMask))
        {
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
        /*if (aggroTarget.currentTarget == null) return;
        Vector3 dirToTarget = (aggroTarget.currentTarget.player.transform.position - transform.position).normalized;
        Gizmos.DrawRay(transform.position + Vector3.up, dirToTarget);*/
        Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.forward * rayDstFor, rayRadiusFor);
        Gizmos.DrawWireSphere(transform.position + Vector3.up + transform.right * rayRadiusSide, rayRadiusSide);
        Gizmos.DrawWireSphere(transform.position + Vector3.up + -transform.right * rayDstSide, rayRadiusSide);

    }

    public void Stun(float duration)
    {
        StartCoroutine(Stunned(duration));
    }

    private IEnumerator Stunned(float duration)
    {
        isStunned = true;
        anim.SetBool("Stunned", isStunned);
        Debug.Log("Iam stunned");
        if(audio.InAudioRange()) AudioManager.instance.PlaySFX(audio.stunned, 1f);
        yield return new WaitForSeconds(duration);
        Debug.Log("Iam not stunned");
        isStunned = false;
        anim.SetBool("Stunned", isStunned);
    }

    /*[PunRPC]
    private void RPCSetPatrolPoint(Vector3 patrolPoint)
    {
        currentState.MiscFunction(this, patrolPoint);
    }*/

    public void ServerStart(float monsterArea, Vector3 areaT)
    {
        patrolArea = monsterArea;
        monsterAreaT = areaT;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }
}
