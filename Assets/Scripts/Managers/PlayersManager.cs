using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayersManager : NetworkBehaviour
{
    public static PlayersManager instance { get; private set; } //we do not want this to be publically setable on getable
    private static Vector2[] playerStartingPositions = new Vector2[4] { new Vector2(11,7), new Vector2(12,7), new Vector2(11,6), new Vector2(12,6) };
    private static List<ulong> clientsReady = new List<ulong>();
    private static List<Player> playersInGame = new List<Player>();
    const int playerLimit = 4;

    public override void OnNetworkSpawn()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Player(s) manager");
            return;
        }

        instance = this;

        if (IsHost)
        {
            NetworkManager.OnClientConnectedCallback += playerJoinedServerRpc;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void playerJoinedServerRpc(ulong clientId)
    {
        if (NetworkManager.ConnectedClients.Count >= playerLimit)
        {
            Debug.LogError("Too many players! limit is: " + playerLimit);
            return;
        }

        Debug.Log("New player joined!");
    }

    [ServerRpc]
    public void startPlayersServerRpc()
    {
        for (int i = 0; i < clientsReady.Count; i++)
        {
            Actor newPlayer = ActorsManager.instance.createActor(ActorsManager.actorType.player, playerStartingPositions[i], spawnActorAsPlayer);

            void spawnActorAsPlayer(Actor player){
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientsReady[i]);
            }
            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void playerReadyServerRpc(ulong clientId)
    {
        if (clientsReady.Contains(clientId)) //we've already readied up!
        {
            return;
        }

        clientsReady.Add(clientId);

        if (clientsReady.Count == NetworkManager.ConnectedClients.Count) //everyone is ready!
        {
            startPlayersServerRpc(); //we start game
        }
    }

    private Player getPlayerFromClientId(ulong id)
    {
        if (NetworkManager.ConnectedClients.ContainsKey(id) == false)
        {
            Debug.LogError("No client connected with ID " + id);
            return null;
        }

        Player thisPlayer = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>();

        if (thisPlayer == null) { Debug.LogError("This client does not have an existing player object in the game world!"); return null; }
        return NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Player>();
    }

    private Player getLocalClientPlayer()
    {
        return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
    }

    //this is for calling it LOCALLY! then we send an rpc to the server
    public void localClientReadyUp()
    {
        playerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void setPlayerControllerTo(Player player, Player.controllerTypes typeToSetTo)
    {
        if (player.IsLocalPlayer) //are we the local client? if so no client rpc needed
        {
            setPlayerControllerToStandardLocalCall(player, typeToSetTo);
        }
        else if (IsHost) //if not and we are the host, call a client rpc
        {
            setPlayerControllerToStandardClientRpc(typeToSetTo, getClientRpcParams(player.OwnerClientId));
        }
        else //if we are a client calling another client throw an error, this should not happen!
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    [ClientRpc]
    private void setPlayerControllerToStandardClientRpc(Player.controllerTypes typeToSetTo, ClientRpcParams clientRpcParams = default)
    {
        Player playerToSet = getLocalClientPlayer();
        setPlayerControllerToStandardLocalCall(playerToSet, typeToSetTo);
    }

    public void showShipToPlayer(Ship ship, Player playerShow)
    {
        ship.seenByPlayerClientRpc(getClientRpcParams(playerShow.OwnerClientId));
    }

    public void unshowShipToPlayer(Ship ship, Player playerUnShow)
    {
        ship.unseenByPlayerClientRpc(getClientRpcParams(playerUnShow.OwnerClientId));
    }

    private void setPlayerControllerToStandardLocalCall(Player player, Player.controllerTypes typeToSetTo)
    {
        switch (typeToSetTo)
        {
            case Player.controllerTypes.standard:
                player.setToStandardController();
                return;
            case Player.controllerTypes.steering:
                player.setToSteeringController();
                return;
        }
        
    }

    private ClientRpcParams getClientRpcParams(ulong id)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams //Somewhat messy may be best to find a way to put this code elsewhere
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };

        return clientRpcParams;
    }
}
