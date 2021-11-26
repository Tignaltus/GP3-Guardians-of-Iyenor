using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Photon.Pun;

public class Archer : PlayerController
{
    [SerializeField] private LineOfSight lineOfSight;
    
    [SerializeField] private Ability primaryAttack;
    [SerializeField] private Ability specialAttack;
    [SerializeField] private Ability ultimateAbility;

    private bool specialAttackCD;
    private bool evasivemanouverCD;

    private int jumpCounter;

    public override void PassiveAbility()
    {
        
    }
    
    public override void PrimaryAttack(Transform playerTransform)
    {
        comboAnims.StartCombo();
    }

    public void Attack()
    {
        rotateAttack = true;
        photonView.RPC("AttackRPC", RpcTarget.AllViaServer);
        AudioManager.instance.PlaySFX(audio.attack, GetRandomPitch());
    }

    [PunRPC]
    public void AttackRPC()
    {
        GameObject target = GetClosestCharacter();
        
        if (target != null)
        {
            GetComponent<AbilityFactory>().FireAbility(primaryAttack, this, target.transform.position);
        }
        else
        {
            GetComponent<AbilityFactory>().FireAbility(primaryAttack, this, transform.position + transform.forward);
        }
    }

    public override void SpecialAttack()
    {
        if (photonView.IsMine && !specialAttackCD)
        {
            StartCoroutine(CooldownSpecial(specialAttack.cooldown));
            photonView.RPC("SpecialAttackRPC", RpcTarget.AllViaServer);
            AudioManager.instance.PlaySFX(audio.specialAttack, 1f);
        }
        
    }
    
    [PunRPC]
    public void SpecialAttackRPC()
    {
        animator.SetTrigger("Shoot");

        GetComponent<AbilityFactory>().FireAbility(specialAttack, this, transform.position + transform.forward);
    }

    public override void EvasiveManeuver()
    {
        if (photonView.IsMine && !evasivemanouverCD)
        {
            StartCoroutine(CooldownUlt(ultimateAbility.cooldown));
            photonView.RPC("UltimateAttackRPC", RpcTarget.AllViaServer);
            AudioManager.instance.PlaySFX(audio.EvasiveManouver, 1f);
        }
    }

    [PunRPC]
    public void UltimateAttackRPC()
    {
        animator.SetTrigger("Shoot");
        
        GetComponent<AbilityFactory>().FireAbility(ultimateAbility, this, transform.position + transform.forward);
    }

    public override void Jump()
    {
        base.Jump();
    }

    private GameObject GetClosestCharacter()
    {
        if (lineOfSight.visibleTargets.Count > 0)
        {
            GameObject closestTarget = lineOfSight.visibleTargets[0];
    
            foreach (GameObject visibleTarget in lineOfSight.visibleTargets)
            {
                if (Vector3.Distance(visibleTarget.transform.position, transform.position) < Vector3.Distance(closestTarget.transform.position, transform.position))
                {
                    closestTarget = visibleTarget;
                }
            }
    
            return closestTarget;
        }

        return null;
    }

    private IEnumerator CooldownSpecial(float seconds)
    {
        specialAttackCD = true;
        yield return new WaitForSeconds(seconds);
        specialAttackCD = false;
    }
    
    private IEnumerator CooldownUlt(float seconds)
    {
        evasivemanouverCD = true;
        yield return new WaitForSeconds(seconds);
        evasivemanouverCD = false;
    }
}
