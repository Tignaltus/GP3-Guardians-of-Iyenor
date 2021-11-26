using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AbilityFactory : MonoBehaviour
{
    public void FireAbility(Ability theAbility, PlayerController caster, Vector3 targetPos)
    {
        switch (theAbility)
        {
            case Projectile projectile:
                StartCoroutine(FireProjectile(projectile, caster, targetPos, null));
                break;
            case AreaOfEffect areaOfEffect:
                FireAreaOfEffect(areaOfEffect, caster, targetPos);
                break;
        }
    }
    
    public void FireAbility(Ability theAbility, PlayerController caster, Vector3 targetPos, Transform origin)
    {
        switch (theAbility)
        {
            case Projectile projectile:
                StartCoroutine(FireProjectile(projectile, caster, targetPos, origin));
                break;
            case AreaOfEffect areaOfEffect:
                break;
        }
    }

    IEnumerator FireProjectile(Projectile projectile, PlayerController caster, Vector3 targetPos, Transform origin)
    {
        ProjectileObject proj;
        
        for (int rounds = 0; rounds < projectile.amount; rounds++)
        {
            if (origin != null)
            {
                proj = Instantiate(projectile.body, origin.position + Vector3.up, Quaternion.identity).AddComponent<ProjectileObject>();
                proj.Initialize(projectile, caster, targetPos);
            }
            else
            {
                proj = Instantiate(projectile.body, caster.transform.position + Vector3.up, Quaternion.identity).AddComponent<ProjectileObject>();
                proj.Initialize(projectile, caster, targetPos);
            }
            
            
            yield return new WaitForSeconds(projectile.frequency);
        }
    }

    private void FireAreaOfEffect(AreaOfEffect areaOfEffect, PlayerController caster, Vector3 targetPos)
    {
        AoEObject aoe;

        if (areaOfEffect.targeting == AreaOfEffect.Targeting.Skillshot)
        {
            aoe = Instantiate(areaOfEffect.body, targetPos, Quaternion.identity).AddComponent<AoEObject>();
            aoe.Initialize(areaOfEffect, caster, targetPos);
        }
        else if (areaOfEffect.targeting == AreaOfEffect.Targeting.SelfTargeting)
        {
            aoe = Instantiate(areaOfEffect.body, caster.transform.position, Quaternion.identity).AddComponent<AoEObject>();
            aoe.Initialize(areaOfEffect, caster, caster.transform.position);
        }
        
    }
}
