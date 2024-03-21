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
    private Dictionary<Player, List<playerServerSideAction>> playerActions = new Dictionary<Player, List<playerServerSideAction>>();
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

    private void FixedUpdate()
    {
        if (IsHost == false)
        {
            return;
        }

        foreach (Player player in playersInGame)
        {
            List<playerServerSideAction> actions = playerActions[player];

            foreach (playerServerSideAction action in actions)
            {
                //for each player we execute all the actions we have been sent this is so they are executed at a constant rate (the fixed update of the server)
                action.execute();
            }
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
                Player playerComponent = player.GetComponent<Player>();
                playerActions.Add(playerComponent, new List<playerServerSideAction>());
                playersInGame.Add(playerComponent);
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

    public float getDistanceToLocalPlayer(Vector2 pos)
    {
        Vector2 playerPos = getLocalClientPlayer().transform.position;
        return Vector2.Distance(playerPos, pos);
    }

    //this is for calling it LOCALLY! then we send an rpc to the server
    public void localClientReadyUp()
    {
        playerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void setPlayerControllerToStandard(Player player)
    {
        if (areWeNotSendingBetweenNonhosts(player)) //if we are the host or the local player then we can do it...
        {
            setPlayerControllerToStandardClientRpc(getClientRpcParams(player.OwnerClientId));
        }
        else //if we are a client calling another client throw an error, this should not happen!
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    public void setPlayerControllerToSteering(Player player, SteeringComponent component)
    {
        if (areWeNotSendingBetweenNonhosts(player))
        {
            setPlayerControllerToSteeringClientRpc(component, getClientRpcParams(player.OwnerClientId));
        }
        else
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    public void setPlayerControllerToGun(Player player, GunComponent component)
    {
        if (areWeNotSendingBetweenNonhosts(player))
        {
            setPlayerControllerToGunClientRpc(component, getClientRpcParams(player.OwnerClientId));
        }
        else
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    [ClientRpc]
    private void setPlayerControllerToStandardClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Player playerToSet = getLocalClientPlayer();
        playerToSet.setToStandardController();
    }

    [ClientRpc]
    private void setPlayerControllerToSteeringClientRpc(NetworkBehaviourReference component, ClientRpcParams clientRpcParams = default)
    {
        Player playerToSet = getLocalClientPlayer();
        SteeringComponent steeringComponent = null;
        if (component.TryGet(out steeringComponent) == false) { Debug.LogError("Component not of correct type for steering controller! " + component.ToString()); }

        playerToSet.setToSteeringController(steeringComponent);
    }

    [ClientRpc]
    private void setPlayerControllerToGunClientRpc(NetworkBehaviourReference component, ClientRpcParams clientRpcParams = default)
    {
        Player playerToSet = getLocalClientPlayer();
        GunComponent gunComponent = null;
        if (component.TryGet(out gunComponent) == false) { Debug.LogError("Component not of correct type for gun controller! " + component.ToString()); }

        playerToSet.setToGunController(gunComponent);
    }

    public bool areWeNotSendingBetweenNonhosts(Player player)
    {
        return IsHost || player.IsLocalPlayer;
    }

    public void showShipToPlayer(Ship ship, Player playerShow)
    {
        ship.seenByPlayerClientRpc(getClientRpcParams(playerShow.OwnerClientId));
    }

    public void unshowShipToPlayer(Ship ship, Player playerUnShow)
    {
        ship.unseenByPlayerClientRpc(getClientRpcParams(playerUnShow.OwnerClientId));
    }

    //TODO: later introduce parameters to limit certain pings to certain players
    [ClientRpc]
    public void placePingAtClientRpc(Vector2 position)
    {
        PlayerPingUiManager.instance.createPingLocally(position);
    }

    public interface playerServerSideAction
    {
        public enum actionType { ship, gun }
        public abstract void execute();
    }

    public class playerServerSideActionShip : playerServerSideAction
    {
        public enum shipAction { rotateLeft, rotateRight, thrust, interactionPressed }
        private shipAction ourAction;
        private SteeringComponent steeringActingOn;
        private Player playerActingOn;

        public playerServerSideActionShip(shipAction actionType, Player player)
        {
            ourAction = actionType;
            steeringActingOn = getActorSteeringComponent(player.getActor());
            playerActingOn = player;
        } 

        public void execute()
        {
            switch (ourAction)
            {
                case shipAction.rotateLeft:
                    steeringActingOn.rotateLeft();
                    return;
                case shipAction.rotateRight:
                    steeringActingOn.rotateRight();
                    return;
                case shipAction.thrust:
                    steeringActingOn.thrust();
                    return;
                case shipAction.interactionPressed:
                    steeringActingOn.dismountFromSteering(playerActingOn.getActor());
                    return;
            }
        }

        private SteeringComponent getActorSteeringComponent(Actor actor) //we should probably pass through an ID that can get this with the action but for now we do this.
        {
            SteeringComponent steeringOn = (SteeringComponent)actor.whatAreWeMountedOn();
            return steeringOn;
        }
    }

    public class playerServerSideActionGun : playerServerSideAction
    {
        public enum gunAction { rotateLeft, rotateRight, shoot }
        private gunAction ourAction;
        private GunComponent gunActingOn;
        private Player playerActingOn;

        public playerServerSideActionGun(gunAction actionType, Player player)
        {
            ourAction = actionType;
            gunActingOn = getActorGunComponent(player.getActor());
            playerActingOn = player;
        }

        public void execute()
        {
            switch (ourAction)
            {
                case gunAction.rotateLeft:
                    gunActingOn.rotateGunLeft();
                    return;
                case gunAction.rotateRight:
                    gunActingOn.rotateGunRight();
                    return;
                case gunAction.shoot:
                    gunActingOn.shootMissile();
                    return;
            }
        }

        private GunComponent getActorGunComponent(Actor actor)
        {
            GunComponent steeringOn = (GunComponent)actor.whatAreWeMountedOn();
            return steeringOn;
        }
    }

    public void registerAction(playerServerSideAction.actionType actionType, uint[] toRegister)
    {
        registerActionWithServerRpc(actionType, toRegister, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void registerActionWithServerRpc(playerServerSideAction.actionType actionType, uint[] toRegister, ulong playerClientId)
    {
        Player playerToRegisterAction = getPlayerFromClientId(playerClientId);
        playerActions[playerToRegisterAction].Clear();

        foreach (uint action in toRegister)
        {
            playerActions[playerToRegisterAction].Add(createAction(actionType, action, playerToRegisterAction));
        }
    }

    private playerServerSideAction createAction(playerServerSideAction.actionType actionType, uint action, Player playerToRegisterAction)
    {
        switch (actionType)
        {
            case playerServerSideAction.actionType.ship:
                return new playerServerSideActionShip((playerServerSideActionShip.shipAction)action, playerToRegisterAction);
            case playerServerSideAction.actionType.gun:
                return new playerServerSideActionGun((playerServerSideActionGun.gunAction)action, playerToRegisterAction);
        }

        return null;
    }

    private ClientRpcParams getClientRpcParams(ulong id)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };

        return clientRpcParams;
    }
}
