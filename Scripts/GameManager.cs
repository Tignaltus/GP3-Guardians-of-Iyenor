using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviourPunCallbacks
{

    public static GameManager Instance;
    [Tooltip("The prefab to use for representing the player")]
    public List<GameObject> playerPrefab;

    public List<GameObject> spawnedPlayers;
    
    public UIManager UIManager;
    
    public Transform spawnPoint;
    public Transform respawnPoint;

    public PlayerController clientPlayer;

    public BellTimer bellTimer;

    [SerializeField]private GameObject victoryCanvas;

    public bool arenaPhase;

    [SerializeField] private AudioClip BGMusic;
        
    private bool playerSpawned;
    private GameObject newPlayer;
    private int character;

    #region Photon Callbacks

    private void Awake()
    {
        Instance = this;
        UIManager = FindObjectOfType<UIManager>();
    }

    private void Start()
    {
        
        AudioManager.instance.PlayMusic(BGMusic, 0.33f);
        if (playerPrefab == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'",this);
        }
        else
        {
            character = PlayerPrefs.GetInt("Character");
            Debug.Log(character);
            Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
            // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
            if (PlayerController.LocalPlayerInstance == null)
            {
                var area = spawnPoint.GetComponent<MonsterArea>();
                Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                PhotonNetwork.Instantiate(this.playerPrefab[character].name, new Vector3(
                    spawnPoint.position.x + Random.Range(-area.AreaRadius, area.AreaRadius), spawnPoint.position.y, 
                    spawnPoint.position.z + Random.Range(-area.AreaRadius, area.AreaRadius)), Quaternion.identity, 0);
            }
            else
            {
                Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
            }
        }

        if(bellTimer != null) bellTimer.StartCounter();
    }

    //Called in playerController to add the player character to the playerlist thru the photonview ID.
    public void AddNewPlayer(PhotonView player, bool condition)
    {
        Debug.Log(PhotonView.Find(player.ViewID).gameObject);

        if (condition)
        {
            spawnedPlayers.Add(PhotonView.Find(player.ViewID).gameObject);
        }
        else
        {
            spawnedPlayers.Remove(PhotonView.Find(player.ViewID).gameObject);
        }
    }

    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }
    #endregion

    #region Public Methods

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void DiedInArena(GameObject player)
    {
        spawnedPlayers.Remove(player);
        if (spawnedPlayers.Count == 1)
        {
            victoryCanvas.GetComponent<VictoryScreen>().GetChampion(spawnedPlayers[0].GetComponent<PhotonView>().Owner.NickName);
            victoryCanvas.SetActive(true);
        }
    }
    
    public void ExitApplication()
    {
        Debug.Log("Quit");
        PhotonNetwork.Disconnect();
        Application.Quit();
    }

    #endregion

    private void LoadArena()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
        }
        Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
        //PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

     #region Photon Callbacks


    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting


        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            LoadArena();
        }
    }


    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects


        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            LoadArena();
        }
    }

    #endregion
}
