using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class AoEObject : MonoBehaviour
{
    public AreaOfEffect ability;
    public PlayerController casterP;

    public void Initialize(AreaOfEffect aoe, PlayerController caster, Vector3 targetPos)
    {
        ability = aoe;
        casterP = caster;
        gameObject.tag = "Ability";
        transform.position += transform.up / 2;

        switch (ability.visualBehaviour)
        {
            case AreaOfEffect.VisualBehaviour.Waves:
                StartCoroutine(Waves(ability.amount, ability.frequency, GetComponent<Collider>()));
                break;
            case AreaOfEffect.VisualBehaviour.Continuous:
                break;
        }
    }

    IEnumerator Waves(int amt, float freq, Collider area)
    {
        for (int waves = 0; waves < amt; waves++)
        {
            if (ability.effectBehaviour == AreaOfEffect.EffectBehaviour.AbilityBased)
            {
                
            }
            else if (ability.effectBehaviour == AreaOfEffect.EffectBehaviour.OnhitBased)
            {
                area.enabled = true;
                Instantiate(ability.mainEffect, transform);
                yield return new WaitForSeconds(freq);
                area.enabled = false;
                yield return new WaitForSeconds(freq);
            }
        }
        
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider hit)
    {
        if (ability.effectType == AreaOfEffect.EffectType.Damage)
        {
            if (hit.gameObject != casterP.gameObject)
            {
                if(hit.TryGetComponent<AggroTarget>(out var aggroUnit))
                {
                    aggroUnit.IncreaseAggro(this.gameObject, 20);
                }
                if(hit.TryGetComponent<IDamageable>(out var iDamageable))
                {
                    iDamageable.TakeDamage(ability.power + casterP.GetPower(), casterP.gameObject);
                }
            }
        }
        else if (ability.effectType == AreaOfEffect.EffectType.Healing)
        {
            if (hit.gameObject == casterP.gameObject)
            {
                casterP.TakeHealing(ability.power);
            }
        }
        else if (ability.effectType == AreaOfEffect.EffectType.AbilityEffect)
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
                    aoeOb = Instantiate(areaOfEffect.body, hit.transform.position, Quaternion.identity).AddComponent<AoEObject>();
                    aoeOb.Initialize(areaOfEffect ,casterP, hit.transform.position);
                    break;
            }
        }
    }
}
