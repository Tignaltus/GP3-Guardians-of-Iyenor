using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class OverheadUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject container;
    
    [Space]
    
    [SerializeField] private TextMeshProUGUI playerNameDisplay;
    [SerializeField] private TextMeshProUGUI playerLevelDisplay;

    [SerializeField] private Image playerHealthDisplay;

    [SerializeField][Range(1, 50)] private float distance;

    private void Start()
    {
        container.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.parent.position + new Vector3(0, 1, 0), PlayerController.LocalPlayerInstance.transform.position) <= distance)
        {
            container.SetActive(true);
            transform.LookAt(FindObjectOfType<SphericCamera>().transform);
        }
        else
        {
            container.SetActive(false);
        }
    }

    public void SetName(string nickname)
    {
        playerNameDisplay.text = nickname;
    }

    public void UpdateUI(int playerLevel, float fillAmt)
    {
        playerLevelDisplay.text = playerLevel.ToString();
        playerHealthDisplay.fillAmount = fillAmt;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.parent.position + new Vector3(0, 1, 0), distance);
    }
}
