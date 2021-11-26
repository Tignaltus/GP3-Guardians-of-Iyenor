using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Image expBar;
    public TextMeshProUGUI levelText;
    
    [Space]
    
    public Image healthBar;
    public TextMeshProUGUI healthCount;

    [Space] 
    
    public GameObject feedContainer;

    [Space] 
    
    public GameObject[] abilityAccess;
}
