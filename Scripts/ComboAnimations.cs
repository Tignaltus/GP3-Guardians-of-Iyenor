using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAnimations : MonoBehaviour
{
    [SerializeField] private Animator assignedAnimator;
    [SerializeField] private string[] animations;
    
    private int comboStep;
    private bool comboPossible;

    public void StartCombo()
    {
        if (comboStep == 0)
        {
            comboStep++;
            assignedAnimator.Play(animations[comboStep - 1]);
        }

        if (comboStep > 0 && comboPossible && comboStep < animations.Length)
        {
            comboPossible = false;
            comboStep++;
        }
        
        //Debug.Log("Attack! Combo Step: " + comboStep);
    }

    public void ComboPossible()
    {
        comboPossible = true;
    }
    
    public void Combo()
    {
        //Debug.Log("Play " + (comboStep - 1));
        assignedAnimator.Play(animations[comboStep - 1]);
    }

    public void ResetCombo()
    {
        comboPossible = false;
        comboStep = 0;
    }
}
