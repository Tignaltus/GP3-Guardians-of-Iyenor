using System;
using System.Collections;
using System.Collections.Generic;
using System.Media;
using Photon.Pun;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Projectile Ability", menuName = "Create New Ability/Projectile")]
public class Projectile : Ability
{
    #region InspectorParameters

    public EffectType effectType;
    
    [BoxGroup("Ability Effect")][ShowIf("effectType", EffectType.AbilityEffect)]
    public Ability additionalEffect;
    
    [Space]
    
    [BoxGroup("Behaviour")][Range(1, 25)]
    public int amount;
    [BoxGroup("Behaviour")][Range(0, 1)]
    public float frequency;

    [Space] 
    
    [BoxGroup("Stats")]
    public float travelSpeed;
    [BoxGroup("Stats")]
    public int power;
    [BoxGroup("Stats")]
    public int range;
    [BoxGroup("Stats")] 
    [Range(5, 30)]public int lifetime;

    [Space]
        
    [BoxGroup("Visual")][PreviewField(100)]
    public GameObject body;
    [BoxGroup("Visual")][PreviewField(100)]
    public ParticleSystem travelEffect;
    [BoxGroup("Visual")][PreviewField(100)]
    public ParticleSystem impactEffect;
    [BoxGroup("Visual")][PreviewField(100)]
    public Animation playerAnimation;

    public enum EffectType
    {
        Damage,
        Healing,
        AbilityEffect
    }

    #endregion
}
