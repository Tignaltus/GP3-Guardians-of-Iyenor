using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using Photon.Pun;
using Sirenix.OdinInspector;
using UnityEngine;

//[CreateAssetMenu(fileName = "New Ability", menuName = "Create New Ability")]
[InlineEditor]
public abstract class Ability : ScriptableObject
{
    public string abilityName;
    [TextArea]
    public string descryption;
    public float cooldown;
}
