using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [TabGroup("Gameplay UI")] 
    [SerializeField] private GameObject[] gameplayUIPanel;
    [TabGroup("Gameplay UI")] 
    [SerializeField] private HUD classHUD;
    [TabGroup("Gameplay UI")] 
    [SerializeField] private GameObject mapPrefab;

    [Space]
    
    [TabGroup("Gameplay UI")] 
    [SerializeField] private GameObject damagePopupContainer;
    [TabGroup("Gameplay UI")]
    [SerializeField] private GameObject damageTemplate;

    [Space]
    
    [TabGroup("Gameplay UI")]
    [SerializeField] private GameObject menu;
    
    [Space]
    
    [TabGroup("Feed")] 
    [SerializeField] private GameObject container;
    [TabGroup("Feed")] 
    [SerializeField] private GameObject feedReportTemplate;
    [TabGroup("Feed")] 
    [SerializeField] private Sprite goldIcon;
    [TabGroup("Feed")] 
    [SerializeField] private Sprite expIcon;
    
    [SerializeField] private PlayerController clientPlayer;
    
    public enum feedType
    {
        Gold,
        Experience
    }

    [TabGroup("Respawn")]
    [SerializeField][Range(1, 100)] private float respawnTimer;
    [TabGroup("Respawn")]
    [SerializeField] private GameObject respawnPanel;
    [TabGroup("Respawn")] 
    [SerializeField] private TextMeshProUGUI respawnClock;
    
    public void SetHUD(PlayerController playerClass)
    {
        respawnPanel.SetActive(false);
        foreach (GameObject hud in gameplayUIPanel)
        {
            hud.SetActive(false);
        }
        
        switch (playerClass)
        {
            case Archer archer:
                gameplayUIPanel[0].SetActive(true);
                classHUD = gameplayUIPanel[0].GetComponent<HUD>();
                break;
            case Warrior warrior:
                gameplayUIPanel[1].SetActive(true);
                classHUD = gameplayUIPanel[1].GetComponent<HUD>();
                break;
            case Wizard wizard:
                gameplayUIPanel[2].SetActive(true);
                classHUD = gameplayUIPanel[2].GetComponent<HUD>();
                
                break;
        }
        
        classHUD.expBar.fillAmount = 0;
        classHUD.levelText.text = "1";
    }

    public IEnumerator Respawn(PlayerController downedPlayer, bool isLocalPlayer)
    {
        if (isLocalPlayer)
        {
            respawnPanel.SetActive(true);
        }

        float currentRespawnTimer = respawnTimer;
        respawnClock.text = currentRespawnTimer.ToString();
        
        while (currentRespawnTimer > 0)
        {
            currentRespawnTimer--;
            respawnClock.text = currentRespawnTimer.ToString();
            Debug.Log(currentRespawnTimer);
            yield return new WaitForSecondsRealtime(1);
        }
        
        downedPlayer.Revive(GameManager.Instance.respawnPoint);

        respawnPanel.SetActive(false);
    }

    public void UpdateUI(int health, int maxHealth)
    {
        float percentHp = (float)health / maxHealth;

        classHUD.healthBar.fillAmount = percentHp;
        classHUD.healthCount.text = health + "/" + maxHealth;
    }

    public void CreatePopupText(string text, Vector3 location, bool isLocalPlayer, bool isHealing)
    {
        GameObject inst = Instantiate(damageTemplate, location, Quaternion.identity);
        inst.transform.position = location;
        
        if (isLocalPlayer && isHealing)
        {
            inst.GetComponent<PopupText>().Initialize(text, Color.green);
        }
        else if (isLocalPlayer)
        {
            inst.GetComponent<PopupText>().Initialize(text, Color.red);
        }
        else
        {
            inst.GetComponent<PopupText>().Initialize(text, Color.white);
        }
    }

    public void ToggleMap()
    {
        switch (mapPrefab.activeSelf)
        {
            case true:
                mapPrefab.SetActive(false);
                break;
            case false:
                mapPrefab.SetActive(true);
                break;
        }
    }

    public void ToggleMenu(SphericCamera cameraScript)
    {
        switch (menu.activeSelf)
        {
            case true:
                menu.SetActive(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                cameraScript.enabled = true;
                break;
            
            case false:
                menu.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
                cameraScript.enabled = false;
                break;
        }
    }
    
    public void ShowExpProgress(float level, float experience, float multiplier)
    {
        float displayExp = experience / (100f * level * multiplier);
        Debug.Log("Updating UI: " + experience + "Exp/" + (100 * level * multiplier) + " | Display Exp is " + displayExp);
        
        classHUD.expBar.fillAmount = displayExp;
        classHUD.levelText.text = level.ToString();
    }

    public void ShowFeedReport(feedType type ,int amount, string victim)
    {
        GameObject feedReport = Instantiate(feedReportTemplate, classHUD.feedContainer.transform);
        
        switch (type)
        {
            case feedType.Gold:
                feedReport.GetComponentInChildren<TextMeshProUGUI>().text = amount + " Gold";
                feedReport.GetComponentInChildren<Image>().sprite = goldIcon;
                feedReport.GetComponentsInChildren<TextMeshProUGUI>()[1].text = "From " + victim;
                break;
            case feedType.Experience:
                feedReport.GetComponentInChildren<TextMeshProUGUI>().text = amount + " Exp";
                feedReport.GetComponentInChildren<Image>().sprite = expIcon;
                feedReport.GetComponentsInChildren<TextMeshProUGUI>()[1].text = "From " + victim;
                break;
        }
    }
}
