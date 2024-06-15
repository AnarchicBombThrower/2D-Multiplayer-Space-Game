using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Networking;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager instance { get; private set; } //we do not want this to be publically setable on getable
    private UnityTransport transport = null;
    //call back for entering game HOST OR JOIN THEN IN LOBBY
    public delegate void gameEntered();
    private gameEntered gameEnteredCallback;
    private string joinCode = "";

    private async void Start()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Connection manager");
            return;
        }

        instance = this;
        transport = GetComponent<UnityTransport>();
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void subscribeToGameEnteredCallback(gameEntered subscribe)
    {
        gameEnteredCallback += subscribe;
    }

    public void unsubscribeToGameEnteredCallback(gameEntered unsubscribe)
    {
        gameEnteredCallback -= unsubscribe;
    }

    public async void hostGame()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(PlayersManager.instance.getPlayerLimit() - 1);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
            return;
        }

        if (NetworkManager.Singleton.StartHost())
        {
            gameEnteredCallback();
        }
    }

    public async void joinGame(string codeTry)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(codeTry);
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            transport.SetRelayServerData(relayServerData);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            return;
        }

        joinCode = codeTry;

        if (NetworkManager.Singleton.StartClient())
        {
            gameEnteredCallback();
        }
    }

    public void disconnectLocalClient()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public string getJoinCode()
    {
        return joinCode;
    }
}
