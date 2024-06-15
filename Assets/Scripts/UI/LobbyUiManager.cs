using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUiManager : MonoBehaviour
{
    [SerializeField]
    private GameObject lobbyParent;
    [SerializeField]
    private Text joinCodeText;

    private void Start()
    {
        ConnectionManager.instance.subscribeToGameEnteredCallback(openLobby);
        PlayersManager.instance.subscribeToGameStartedCallback(closeLobby);
    }

    private void openLobby()
    {
        lobbyParent.SetActive(true);
        joinCodeText.text = "Join code:" + ConnectionManager.instance.getJoinCode();
    }

    private void closeLobby()
    {
        lobbyParent.SetActive(false);
    }

    public void readyUp()
    {
        PlayersManager.instance.localClientReadyUp();
    }
}
