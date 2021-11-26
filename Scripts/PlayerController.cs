using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable, IDamageable, IStunable
{
    #region Variables
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;

    [TabGroup("Stats")]
    [SerializeField] protected int health, maxHealth = 100, power, defense;

    [TabGroup("Stats")]
    [SerializeField] private float stamina;

    [TabGroup("Stats")]
    [SerializeField] protected float attackDelay;

    [TabGroup("Levels")]
    [SerializeField] private int level = 1, experience, levelThreshholdMultiplier = 1;

    [TabGroup("Movement")] [SerializeField]
    protected float speedModifier = 10f, acceleration = 0.15f;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Transform modelTransform;
    [SerializeField] protected Collider myCollider;
    [SerializeField] private float attackRotSpeed = 8f;

    [TabGroup("Movement")]
    [SerializeField] protected float jumpHeight = 10;

    [Tooltip("Custom gravity which only this object uses")][TabGroup("Gravity")]
    [SerializeField] private float gravity = -9.82f;

    [Tooltip("The gravity power in which the player falls")][TabGroup("Gravity")]
    [SerializeField] private float fallMultiplier = 3f;

    [Tooltip("The gravity force in which the player goes up aka jumps")][TabGroup("Gravity")]
    [SerializeField] private float upMultiplier = 2.5f;

    [TabGroup("Gravity")]
    [SerializeField]private float gravityScale = 1f;

    [TabGroup("Gravity")]
    [SerializeField]protected bool isGrounded;

    [TabGroup("Gravity")]
    [SerializeField]private float groundCheckDistance = 0.13f;

    [TabGroup("Gravity")]
    [SerializeField] private float maxGroundAngle = 120f;

    [TabGroup("Gravity")]
    [SerializeField] private float slopeGlideSpeed = 90f;

    [TabGroup("Animations")]
    [SerializeField] private float animAcceleration = 0.01f, animDeceleration = 0.03f;

    [SerializeField] private OverheadUI overheadUI;

    public LayerMask groundLayer;
    public LayerMask targetLayers;
    public bool debugMode;
    public float footStepsIntervall = 0.5f;

    protected bool canJump = true;
    public bool isAlive = true;
    private Vector3 smoothMoveVelocity;
    protected Vector3 moveDir;
    [HideInInspector]public Vector3 moveAmount;
    private Renderer renderer;
    private float animatorFloat;
    private int isMoving;
    private float groundAngle, angle;
    private Vector3 forward;
    private RaycastHit hitInfo;
    public bool isStunned;
    protected bool rotateAttack;
    private bool usingSpecial;
    private bool usingManouver;
    private bool pvpEnabled;
    private bool footStep;
    private ModelRotation modelR;

    protected ComboAnimations comboAnims;

    //Used if an ability changes the inputs.
    public bool customInput;

    public UnityEvent OnTakeDamage;
    protected SphericCamera cameraScript;
    private ScreenShake shakeScript;
    [HideInInspector]public AudioEntity audio;

    //public UnityEvent OnScreenShake;
    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            LocalPlayerInstance = gameObject;
            photonView.RPC("StartBufferRPC", RpcTarget.AllBuffered);
        }

        renderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        comboAnims = GetComponent<ComboAnimations>();
        myCollider = GetComponent<Collider>();
        audio = GetComponent<AudioEntity>();
        modelR = modelTransform.GetComponent<ModelRotation>();

        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        //DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        //For custom gravity setting disable gravity
        rb.useGravity = false;
        PhotonNetwork.SendRate = 84;
        PhotonNetwork.SerializationRate = 84;
        if (photonView.IsMine)
        {
            //FindObjectOfType<SphericCamera>().startMoving = true;
            cameraScript = FindObjectOfType<SphericCamera>();
            cameraScript.target = transform;
            cameraScript.playerC = this;

            shakeScript = FindObjectOfType<ScreenShake>();
        }

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        if (photonView.IsMine)
        {
            GetComponentInChildren<OverheadUI>().gameObject.SetActive(false);
            GameManager.Instance.UIManager.SetHUD(this);
            GameManager.Instance.UIManager.UpdateUI(health, maxHealth);
        }
        else
        {
            overheadUI = GetComponentInChildren<OverheadUI>();
            UpdateMyUI();
            overheadUI.SetName(photonView.Owner.NickName);
        }

        /*if (PhotonNetwork.IsMasterClient)
        {
            //photonView.RPC("StartBellRPC", RpcTarget.AllBufferedViaServer);
        }*/

    }

    [PunRPC]
    public void StartBufferRPC()
    {
        GameManager.Instance.AddNewPlayer(photonView, true);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        EventManager.Instance.OnPvpStart += PvpToggler;
    }

    public override void OnDisable()
    {
        // Always call the base to remove callbacks
        base.OnDisable ();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        EventManager.Instance.OnPvpStart -= PvpToggler;
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            if (isAlive || !isStunned)
            {
                Movement();
                CheckGround();
                CalculateForward();
                CalculateGroundAngle();
                ClassUpdate();
            }

            AnimationUpdate();
            DrawDebugLines();

            if (rotateAttack && isAlive)
            {
                AttackingRotate();
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            GameManager.Instance.UIManager.ToggleMap();
        }

        if (!photonView.IsMine)
        {
            ServerSideCalls();
        }
    }

    //For custom gravity options can be moved to an invoke later for performance optimization.
    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            CustomGravity();

            if (groundAngle <= maxGroundAngle && moveDir.magnitude > 0)
            {
                //rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.deltaTime);
                //I multiply speedModifier here again to make the number smaller in the hierachy and easier to understand
                //for designers.

                if (!isAlive || isStunned)
                {
                    rb.AddRelativeForce(moveAmount * (0 * Time.deltaTime), ForceMode.Impulse);
                }
                else
                {
                    rb.AddRelativeForce(moveAmount * (speedModifier * Time.deltaTime), ForceMode.Impulse);
                    if (!footStep)
                    {
                        footStep = true;
                        Invoke("FootSteps", footStepsIntervall);
                    }
                }

            }

            animator.SetFloat("Velocity", animatorFloat);
        }
    }

    #endregion

    #region PhotonStuff

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
    {
        CalledOnLevelWasLoaded(scene.buildIndex);
    }

    void OnLevelWasLoaded(int level)
    {
        CalledOnLevelWasLoaded(level);
    }

    void CalledOnLevelWasLoaded(int level)
    {
        // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
        if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
        {
            transform.position = new Vector3(0f, 5f, 0f);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        GameManager.Instance.AddNewPlayer(photonView, false);
    }

    #endregion

    #region Client

    //Movement and inputs.
    private void Movement()
    {
        if (groundAngle >= maxGroundAngle) return;

        if(!customInput) moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (!isAlive)
        {
            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * 0, ref smoothMoveVelocity, acceleration);
        }
        else
        {
            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * speedModifier, ref smoothMoveVelocity, acceleration);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
            //StartCoroutine(JumpCd());
        }

        if (Input.GetMouseButtonDown(0))
        {
            PrimaryAttack(transform);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            usingSpecial = true;
            SpecialAttack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            usingManouver = true;
            EvasiveManeuver();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.UIManager.ToggleMenu(cameraScript);
        }

        //Needed to calculate all angles equally in the CalculateForward function makes the player not being able to strafe up hills.
        if (moveDir.magnitude > 0 && isMoving == 0)
        {
            isMoving = 1;
        }
        else if(moveDir.magnitude == 0 && isMoving == 1)
        {
            isMoving = 0;
        }
    }

    /// <summary>
    /// Updated parameters in the animator.
    /// </summary>
    private void AnimationUpdate()
    {
        if (!isAlive)
        {
            animator.SetBool("IsAlive", false);
        }
        else
        {
            if (moveDir.magnitude > 0 && animatorFloat <= 2)
            {
                animatorFloat += animAcceleration;
            }
            else if(animatorFloat >= 0)
            {
                animatorFloat -= animDeceleration;
            }
        }

    }

    /*Adds a custom gravity force pulling the player down. If the players velocity is under -0.5f (falling)
    the gravityscale changes to the fallMultiplier making the player fall faster based on the fallMultiplier
    If the players velocity is over 0.5f (going up for example jumping) the gravityScale changes based on upMultiplier
    if non of these are true the gravityScale is normal*/
    private void CustomGravity()
    {
        Vector3 customGravity = gravity * gravityScale * Vector3.up;
        rb.AddForce(customGravity, ForceMode.Acceleration);

        if (rb.velocity.y < -0.5f)
        {
            gravityScale = fallMultiplier;
        }else if (rb.velocity.y > 0.5f)
        {
            gravityScale = upMultiplier;
        }
        else
        {
            gravityScale = 1f;
        }

    }

    //To avoid spam jump abusers and avoids jittering in the checksphere isgrounded check. There's probably a better way to do this but works for now.
    private IEnumerator JumpCd()
    {
        canJump = false;
        isGrounded = false;
        yield return new WaitForSeconds(0.8f);
        canJump = true;
    }

    public virtual void Jump()
    {
        if (isGrounded && canJump)
        {
            StartCoroutine(JumpCd());
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            animator.SetTrigger("Jump");
            AudioManager.instance.PlaySFX(audio.jump, GetRandomPitch());
        }
    }

    #endregion

    #region RPCs and Server

    /// <summary>
    /// This function sends and receives data from the network.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="info"></param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Under the if statement write what you which to send to the server under the else what you want to recieve.
        if (stream.IsWriting)
        {
            stream.SendNext(health);
            //stream.SendNext(transform.position);
            stream.SendNext(rb.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(modelTransform.rotation);
            stream.SendNext(usingSpecial);
            //stream.SendNext(usingManouver);
        }
        else
        {
            health = (int) stream.ReceiveNext();
            //transform.position = (Vector3) stream.ReceiveNext();
            rb.position = (Vector3) stream.ReceiveNext();
            transform.rotation = (Quaternion) stream.ReceiveNext();
            modelTransform.rotation = (Quaternion) stream.ReceiveNext();
            usingSpecial = (bool) stream.ReceiveNext();
            //usingManouver = (bool) stream.ReceiveNext();
        }
    }

    public void ServerSideCalls()
    {
        if (usingSpecial)
        {
            usingSpecial = false;
            SpecialAttack();
        }

        if (usingManouver)
        {
            usingManouver = false;
            EvasiveManeuver();
        }
    }

    #endregion

    #region Gameplay Checks

        public void TakeDamage(int damage, GameObject attacker)
        {
            if (isAlive)
            {
               if (attacker.gameObject.CompareTag("Player") && !pvpEnabled) return;
               int damageSum = damage - defense;
               health -= damageSum;
               OnTakeDamage.Invoke();
   
               animator.SetTrigger("TakingDamage");
               AudioManager.instance.PlaySFX(audio.hit, GetRandomPitch());
               
               if(photonView.IsMine) shakeScript.ShakeCamera();
               UpdateMyUI();
               
               GameManager.Instance.UIManager.CreatePopupText(damage.ToString(), transform.position, photonView.IsMine, false);
   
               isAlive = isDead(health);
               if (!isAlive)
               {
                   Debug.Log("You Died");
                   
                   if (attacker.tag == "Player")
                   {
                        attacker.GetComponent<PlayerController>().GiveExperience(100, "Player");
                   }
                   AudioManager.instance.PlaySFX(audio.death, 3f);
                   if (GameManager.Instance.arenaPhase)
                   {
                       GameManager.Instance.DiedInArena(gameObject);
                   }
                   else
                   {
                       StartCoroutine(GameManager.Instance.UIManager.Respawn(this, photonView.IsMine));
                   }
               } 
            }
        }

        public void TakeHealing(int healing)
        {
            health += healing;
            if (health > maxHealth)
            {
                health = maxHealth;
            }
            else
            {
                GameManager.Instance.UIManager.CreatePopupText(healing.ToString(), transform.position, photonView.IsMine, true);
            }

            UpdateMyUI();
        }

        public bool isDead(int hp)
        {
            if (hp <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Revive(Transform respawnPoint)
        {
            health = maxHealth;
            isAlive = isDead(health);
            transform.position = respawnPoint.position;
            animator.SetBool("IsAlive", true);

            UpdateMyUI();
        }

        public void GiveExperience(int amount, string victimName)
        {
            experience += amount;

            while (experience >= 100 * level * levelThreshholdMultiplier)
            {
                experience -= 100 * level * levelThreshholdMultiplier;

                LevelUp();
            }

            if (photonView.IsMine)
            {
                GameManager.Instance.UIManager.ShowExpProgress(level, experience, levelThreshholdMultiplier);
                GameManager.Instance.UIManager.ShowFeedReport(UIManager.feedType.Experience, amount, victimName);
            }
        }
    

        private void LevelUp()
        {
            level++;
            maxHealth += 25;
            health = maxHealth;
            power += 4;
            defense += 0;

            UpdateMyUI();
        }

        public void UpdateMyUI()
        {
            if (!photonView.IsMine)
            {
                float percentHp = (float)health / maxHealth;
                overheadUI.UpdateUI(level, percentHp);
            }
            else
            {
                GameManager.Instance.UIManager.UpdateUI(health, maxHealth);
            }
        }

        #endregion

    #region ClassAbilities override

    public virtual void PassiveAbility()
    {

    }
    public virtual void PrimaryAttack(Transform playerTransform)
    {

    }

    public virtual void SpecialAttack()
    {

    }

    public virtual void EvasiveManeuver()
    {

    }

    public virtual void ClassUpdate()
    {

    }

    #endregion

    private void AttackingRotate()
    {
        if (!modelR.dontRotate)
        {
            StartCoroutine(RotateFalse());
        }
        modelR.dontRotate = true;
        modelTransform.forward = Vector3.Slerp(modelTransform.forward, transform.forward, attackRotSpeed * Time.deltaTime);
    }
    #region Ground/Slope Calculations

    /// <summary>
    /// Calculate the angle of the ground based on what the ground-raycast hits. Calculates the angle between the hitinfo.normal
    /// and transform.up and adds 90degrees for readability. Also adds a acceleration force along the angle of the normal
    /// if the groundangle is bigger then the maxgroundangle.
    /// </summary>
    private void CalculateGroundAngle()
    {
        if (!isGrounded)
        {
            groundAngle = 90;
            return;
        }

        //var slopeMove = Vector3.ProjectOnPlane(new Vector3(0, 0, isMoving), hitInfo.normal);
        groundAngle = Vector3.Angle(hitInfo.normal, transform.up) + 90;

        if (isGrounded && groundAngle >= maxGroundAngle)
        {
            rb.AddForce(new Vector3(hitInfo.normal.x, 0f, hitInfo.normal.z) * slopeGlideSpeed, ForceMode.Acceleration);
            Debug.DrawRay(hitInfo.point, new Vector3(hitInfo.normal.x, 0, hitInfo.normal.z), Color.yellow);
        }
    }

    /// <summary>
    /// Uses the crossprodukt to calculate the forward of the character.
    /// </summary>
    private void CalculateForward()
    {
        if (!isGrounded)
        {
            forward = transform.forward;
            return;
        }

        forward = Vector3.Cross(transform.right, hitInfo.normal);
    }

    /// <summary>
    /// Checks if the player is standing on the ground with a raycast pointing straight down
    /// </summary>
    private void CheckGround()
    {
        if (Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), -Vector3.up, out hitInfo, groundCheckDistance, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    #endregion

    #region Debugging

    private void DrawDebugLines()
    {
        if (!debugMode) return;
        Vector3 startTransform = transform.position + new Vector3(0, 0.5f, 0);
        Debug.DrawLine(startTransform, startTransform + forward * groundCheckDistance * 2, Color.blue);
        Debug.DrawLine(startTransform, startTransform - Vector3.up * groundCheckDistance, Color.red);
    }

    #endregion

    #region Accessors

        public bool IsPlayerAlive()
        {
            return isAlive;
        }

        #endregion

        public void Stun(float duration)
        {
            StartCoroutine(Stunned(duration));
        }

        private IEnumerator Stunned(float duration)
        {
            isStunned = true;
            AudioManager.instance.PlaySFX(audio.stunned, GetRandomPitch());
            Debug.Log("Iam stunned");
            yield return new WaitForSeconds(duration);
            Debug.Log("Iam not stunned");
            isStunned = false;
        }

        private void PvpToggler(bool statement)
        {
            pvpEnabled = statement;
        }

        private void FootSteps()
        {
            if (isGrounded)
            {
                var rand = Random.Range(0, 2);
                switch (rand)
                {
                    case 0:
                        AudioManager.instance.PlayFootSteps(audio.footsteps);
                        break;

                    case 1:
                        AudioManager.instance.PlayFootSteps(audio.footsteps2);
                        break;
                }
            }

            footStep = false;
        }

        protected float GetRandomPitch()
        {
            var pitch = Random.Range(0.8f, 2f);
            return pitch;
        }

        [PunRPC]
        private void StartBellRPC()
        {
            GameManager.Instance.bellTimer.StartCounter();
        }

        public int GetPower()
        {
            return power;
        }

        private IEnumerator RotateFalse()
        {
            yield return new WaitForSeconds(0.5f);
            rotateAttack = false;
            modelR.dontRotate = false;
        }
}
