using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class ProjectileObject : MonoBehaviour
{
    public Projectile ability;

    private PlayerController casterP;
    private Transform origin;

    private float myLifetime;

    public void Initialize(Projectile assigned, PlayerController caster, Vector3 targetDirection)
    {
        casterP = caster;
        ability = assigned;
        origin = transform;
        myLifetime = assigned.lifetime;

        gameObject.tag = "Ability";

        //Debug.Log(assigned.myTarget.position);
        
        Vector3 toTarget = (targetDirection + Vector3.up) - transform.position;
        transform.rotation = Quaternion.LookRotation(toTarget.normalized);

        if (ability.travelEffect != null)
        {
            Instantiate(ability.travelEffect, transform);
        }
    }

    private void FixedUpdate()
    {
        Movement();
        UpdateLifetime();
    }

    private void Movement()
    {
        if (Vector3.Distance(origin.position, transform.position) > ability.range)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.position += transform.forward * ability.travelSpeed * Time.deltaTime;
        }
    }

    private void UpdateLifetime()
    {
        myLifetime -= Time.deltaTime;

        if (myLifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider hit)
    {
        if (!hit.gameObject.CompareTag("Ability") && hit.gameObject != casterP.gameObject)
        {
            if (ability.effectType == Projectile.EffectType.Damage)
            {
                if (hit.GetComponent<IDamageable>() != null )
                {
                    hit.GetComponent<IDamageable>().TakeDamage(ability.power + casterP.GetPower(), casterP.gameObject);
                    if (hit.GetComponent<AIEntity>() != null)
                    {
                        hit.GetComponent<AggroTarget>().IncreaseAggro(casterP.gameObject, 15f);
                    }
                }
            }
            else if (ability.effectType == Projectile.EffectType.Healing)
            {
                if (hit.GetComponent<PlayerController>() == casterP)
                {
                    hit.GetComponent<PlayerController>().TakeHealing(ability.power);
                }
            }
            else if (ability.effectType == Projectile.EffectType.AbilityEffect)
            { 
                switch (ability.additionalEffect)
                {
                   case Projectile projectile:
                       ProjectileObject projectileOb;
                       projectileOb = Instantiate(projectile.body, transform.position, Quaternion.identity).AddComponent<ProjectileObject>();
                       projectileOb.Initialize(projectile, casterP, hit.transform.position);
                       break;
                   case AreaOfEffect areaOfEffect:
                       AoEObject aoeOb;
                       aoeOb = Instantiate(areaOfEffect.body, transform.position, Quaternion.identity).AddComponent<AoEObject>();
                       aoeOb.Initialize(areaOfEffect ,casterP, transform.position);
                       break;
                }
            }
            
            if (ability.impactEffect != null)
            {
                Instantiate(ability.impactEffect, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
        
    }

    private void OnDestroy()
    {

    }
}
