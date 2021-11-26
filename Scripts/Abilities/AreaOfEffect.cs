using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[CreateAssetMenu(fileName = "New AoE Ability", menuName = "Create New Ability/Area Of Effect")]
public class AreaOfEffect : Ability
{
    #region InspectorParameters

        [EnumToggleButtons]
        public Targeting targeting;
        
        public EffectType effectType;
        
        [BoxGroup("Ability Effect")][ShowIf("effectType", EffectType.AbilityEffect)]
        public Ability additionalEffect;
    
        [BoxGroup("Ability Effect")][ShowIf("effectType", EffectType.AbilityEffect)][EnumToggleButtons]
        public EffectBehaviour effectBehaviour;
    
        [BoxGroup("Ability Effect")][ShowIf("additionalEffect", typeof(Projectile))]
        public bool randomized;
        [BoxGroup("Ability Effect")][ShowIf("additionalEffect", typeof(Projectile))]
        public int spawnHeight;
    
        [BoxGroup("Behaviour")]
        public int amount;
        [BoxGroup("Behaviour")]
        [Range(0.1f, 2)] public float frequency;
        
        [Space]
        
        [BoxGroup("Stats")][HideIf("effectBehaviour", EffectBehaviour.AbilityBased)]
        public int power;
        [BoxGroup("Stats")]
        public int range;

        [BoxGroup("Visual")][EnumToggleButtons]
        public VisualBehaviour visualBehaviour;

        [Space] 
        
        [BoxGroup("Visual")][PreviewField(100)][LabelText("Body(Optional)")]
        public GameObject body;
        [BoxGroup("Visual")][PreviewField(100)]
        public ParticleSystem mainEffect;
        [BoxGroup("Visual")][PreviewField(100)]
        public Animation playerAnimation;
        
        public enum EffectType
        {
            Damage,
            Healing,
            AbilityEffect
        }
        
        public enum Targeting
        {
            SelfTargeting,
            Skillshot
        }
        
        public enum Shape
        {
            Sphere,
            Cube
        }
        
        public enum VisualBehaviour
        {
            Continuous,
            Waves
        }
        
        public enum EffectBehaviour
        {
            OnhitBased,
            AbilityBased
        }

    #endregion
}
