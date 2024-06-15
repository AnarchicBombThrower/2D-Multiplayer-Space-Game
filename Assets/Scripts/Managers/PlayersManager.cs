using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayersManager : NetworkBehaviour
{
    public static PlayersManager instance { get; private set; } //we do not want this to be publically setable on getable
    private static Vector2[] playerStartingPositions = new Vector2[4] { new Vector2(11,7), new Vector2(12,7), new Vector2(11,6), new Vector2(12,6) };
    private static List<ulong> clientsReady = new List<ulong>();
    private static List<Player> playersInGame = new List<Player>();
    private Dictionary<Player, List<playerServerSideAction>> playerServerActions = new Dictionary<Player, List<playerServerSideAction>>();
    private List<playerClientSideAction> playerClientActions = new List<playerClientSideAction>();
    const int playerLimit = 4;
    //call back for game starting (CALLED ON EACH CLIENT)
    public delegate void gameStarted();
    private gameStarted gameStartedCallback;
    const string TO_LOAD_ON_DISCONNECT = "ManagerScene";
    const string DONT_DESTROY_TAG = "DontDestroy";

    public void Start()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Player(s) manager");
            return;
        }

        instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            NetworkManager.OnClientConnectedCallback += playerJoinedServerRpc;
        }

        NetworkManager.OnClientDisconnectCallback += playerDisconnected;
        NetworkManager.OnClientStopped += localClientStopped;
    }

    private void FixedUpdate()
    {
        foreach (playerClientSideAction action in playerClientActions)
        {
            action.execute();
        }

        if (IsHost == false)
        {
            return;
        }

        foreach (Player player in playersInGame)
        {
            List<playerServerSideAction> serverActions = playerServerActions[player];

            foreach (playerServerSideAction action in serverActions)
            {
                //for each player we execute all the actions we have been sent this is so they are executed at a constant rate (the fixed update of the server)
                action.execute();
            }
        }
    }

    public int getPlayerLimit()
    {
        return playerLimit;
    }

    public void subscribeToGameStartedCallback(gameStarted subscribe)
    {
        gameStartedCallback += subscribe;
    }

    public void unsubscribeToGameStartedCallback(gameStarted unsubscribe)
    {
        gameStartedCallback -= unsubscribe;
    }

    [ServerRpc(RequireOwnership = false)]
    public void playerJoinedServerRpc(ulong clientId)
    {
        if (NetworkManager.ConnectedClients.Count >= playerLimit)
        {
            Debug.LogError("Too many players! limit is: " + playerLimit);
            return;
        }

        playerJoinedClientRpc(clientId);
        Debug.Log("New player joined!");
    }

    public void disconnectLocalPlayer()
    {
        ConnectionManager.instance.disconnectLocalClient();
    }

    //called locally on all players!!
    private void playerDisconnected(ulong clientID)
    {
        PlayerUiManager.clientDisconnected(clientID == NetworkManager.LocalClientId);
    }

    private void localClientStopped(bool host)
    {
        PlayerUiManager.clientStopped();
    }

    [ClientRpc]
    public void playerJoinedClientRpc(ulong clientId)
    {
        
    }

    [ServerRpc]
    public void startGameServerRpc()
    {
        //spawn all players
        for (int i = 0; i < clientsReady.Count; i++)
        {
            Actor newPlayer = ActorsManager.instance.createActor(ActorsManager.actorType.player, playerStartingPositions[i], spawnActorAsPlayer);

            void spawnActorAsPlayer(Actor player){
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientsReady[i]);
                Player playerComponent = player.GetComponent<Player>();
                playerServerActions.Add(playerComponent, new List<playerServerSideAction>());
                playersInGame.Add(playerComponent);
            } 
        }

        //spawn ship and the first encounter (basically first scene)
        ShipManager.instance.spawnPlayerShip();
        EncounterManager.instance.startFirstEncounter();
        gameStartedClientRpc();
    }

    [ClientRpc]
    public void gameStartedClientRpc()
    {
        gameStartedCallback();
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
            startGameServerRpc(); //we start game
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

    public Camera getPlayerCamera()
    {
        return getLocalClientPlayer().getPlayerCamera();
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

    public void playerOnShip(Player player, Ship shipOn)
    {
        playerOnLocalClientRpc(shipOn, getClientRpcParams(player.OwnerClientId));
    }

    [ClientRpc]
    public void playerOnLocalClientRpc(NetworkBehaviourReference shipReference, ClientRpcParams clientRpcParams = default)
    {
        Ship ship = null;
        if (shipReference.TryGet(out ship) == false) { Debug.LogError("Player on argument is not a ship! " + shipReference.ToString()); return; }
        PlayerUiManager.playerNowOnShip(ship);
    }

    public void unmountLocalPlayerFromComponent(ShipMountableComponent component)
    {
        unmountPlayerFromComponentServerRpc(NetworkManager.Singleton.LocalClientId, component);
    }

    [ServerRpc(RequireOwnership=false)]
    private void unmountPlayerFromComponentServerRpc(ulong playerID, NetworkBehaviourReference componentReference)
    {
        ShipMountableComponent componentToDismount = null;
        if (componentReference.TryGet(out componentToDismount) == false) { Debug.LogError("Player is trying to dismount from component that is not mountable component " + componentReference.ToString()); return; }
        componentToDismount.unmount(getPlayerFromClientId(playerID).getActor());
    }

    public void setPlayerControllerToStandard(Player player)
    {
        //TODO: THIS PATTERN IS REPEATED BETWEEN THE THREE FUNCTIONS, FIND A WAY TO SIMPLFY THIS! (interfaces can't work because of client rpcs)
        if (IsHost) //if we are the host or the local player then we can do it...
        {
            setPlayerControllerToStandardClientRpc(getClientRpcParams(player.OwnerClientId));
        }
        else if (player.IsLocalPlayer)
        {
            setPlayerControllerToStandard();
        }
        else //if we are a client calling another client throw an error, this should not happen!
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    public void setPlayerControllerToSteering(Player player, SteeringComponent component)
    {
        if (IsHost)
        {
            setPlayerControllerToSteeringClientRpc(component, getClientRpcParams(player.OwnerClientId));
        }
        else if (player.IsLocalPlayer)
        {
            setPlayerControllerToSteering(component);
        }
        else
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    public void setPlayerControllerToGun(Player player, GunComponent component)
    {
        if (IsHost)
        {
            setPlayerControllerToGunClientRpc(component, getClientRpcParams(player.OwnerClientId));
        }
        else if (player.IsLocalPlayer)
        {
            setPlayerControllerToGun(component);
        }
        else
        {
            Debug.LogError("Client cannot change another clients controller type!");
        }
    }

    [ClientRpc]
    private void setPlayerControllerToStandardClientRpc(ClientRpcParams clientRpcParams = default)
    {
        setPlayerControllerToStandard();
    }

    private void setPlayerControllerToStandard()
    {
        Player playerToSet = getLocalClientPlayer();
        playerToSet.setToStandardController();
    }

    [ClientRpc]
    private void setPlayerControllerToSteeringClientRpc(NetworkBehaviourReference component, ClientRpcParams clientRpcParams = default)
    {
        SteeringComponent steeringComponent = null;
        if (component.TryGet(out steeringComponent) == false) { Debug.LogError("Component not of correct type for steering controller! " + component.ToString()); }
        setPlayerControllerToSteering(steeringComponent);
    }

    private void setPlayerControllerToSteering(SteeringComponent component)
    {
        Player playerToSet = getLocalClientPlayer();
        playerToSet.setToSteeringController(component);
    }

    [ClientRpc]
    private void setPlayerControllerToGunClientRpc(NetworkBehaviourReference component, ClientRpcParams clientRpcParams = default)
    {    
        GunComponent gunComponent = null;
        if (component.TryGet(out gunComponent) == false) { Debug.LogError("Component not of correct type for gun controller! " + component.ToString()); }
        setPlayerControllerToGun(gunComponent);
    }

    private void setPlayerControllerToGun(GunComponent component)
    {
        Player playerToSet = getLocalClientPlayer();
        playerToSet.setToGunController(component);
    }

    public void showShipToPlayer(Ship ship, Player playerShow)
    {
        if (ship.GetComponent<NetworkObject>().IsSpawned == false)
        {
            return;
        }

        ship.seenByPlayerClientRpc(getClientRpcParams(playerShow.OwnerClientId));
    }

    public void unshowShipToPlayer(Ship ship, Player playerUnShow)
    {
        ship.unseenByPlayerClientRpc(getClientRpcParams(playerUnShow.OwnerClientId));
    }

    public void playerShipSpawned(Ship ship)
    {
        foreach (Player player in playersInGame)
        {
            player.getActor().getOnShip(ship);
        }
    }

    [ServerRpc(RequireOwnership=false)]
    public void placePingAtServerRpc(Vector2 position)
    {
        placePingAtClientRpc(position);
    }

    //TODO: later introduce parameters to limit certain pings to certain players
    [ClientRpc]
    public void placePingAtClientRpc(Vector2 position)
    {
        PlayerPingUiManager.instance.createPingLocally(position);
    }

    public interface playerAction
    {   
        public abstract void execute();
    }

    public interface playerClientSideAction : playerAction //this is needed to call actions on the fixed update loop instead of the standard update loop 
    {
        public enum clientActionType { standard }
    }

    public class playerClientSideActionStandard : playerClientSideAction
    {
        public enum playerStandardAction { moveLeft, moveUp, moveRight, moveDown }
        private playerStandardAction ourAction;
        private Actor playerActingOn;

        public playerClientSideActionStandard(playerStandardAction action, Player player)
        {
            ourAction = action;
            playerActingOn = player.getActor();
        }

        public void execute()
        {
            switch (ourAction)
            {
                case playerStandardAction.moveLeft:
                    playerActingOn.addForceLeft();
                    return;
                case playerStandardAction.moveUp:
                    playerActingOn.addForceUp();
                    return;
                case playerStandardAction.moveRight:
                    playerActingOn.addForceRight();
                    return;
                case playerStandardAction.moveDown:
                    playerActingOn.addForceDown();
                    return;
            }
        }
    }

    public interface playerServerSideAction : playerAction
    {
        public enum serverActionType { ship, gun }
    }

    public class playerServerSideActionShip : playerServerSideAction
    {
        public enum shipAction { rotateLeft, rotateRight, thrust }
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
        public enum gunAction { rotateLeft, rotateRight, extendDistance, shoot }
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
                case gunAction.extendDistance:
                    gunActingOn.extendMissileShootDistance();
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

    public void registerServerAction(playerServerSideAction.serverActionType actionType, uint[] toRegister)
    {
        registerActionWithServerRpc(actionType, toRegister, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void registerActionWithServerRpc(playerServerSideAction.serverActionType actionType, uint[] toRegister, ulong playerClientId)
    {
        Player playerToRegisterAction = getPlayerFromClientId(playerClientId);
        playerServerActions[playerToRegisterAction].Clear();

        foreach (uint action in toRegister)
        {
            playerServerActions[playerToRegisterAction].Add(createServerAction(actionType, action, playerToRegisterAction));
        }
    }

    private playerServerSideAction createServerAction(playerServerSideAction.serverActionType actionType, uint action, Player playerToRegisterAction)
    {
        switch (actionType)
        {
            case playerServerSideAction.serverActionType.ship:
                return new playerServerSideActionShip((playerServerSideActionShip.shipAction)action, playerToRegisterAction);
            case playerServerSideAction.serverActionType.gun:
                return new playerServerSideActionGun((playerServerSideActionGun.gunAction)action, playerToRegisterAction);
        }

        return null;
    }

    public void registerClientAction(playerClientSideAction.clientActionType actionType, uint[] toRegister, Player player)
    {
        playerClientActions.Clear();

        foreach (uint action in toRegister)
        {
            playerClientActions.Add(createClientAction(actionType, action, player));
        }
    }

    private playerClientSideAction createClientAction(playerClientSideAction.clientActionType actionType, uint action, Player playerToRegisterAction)
    {
        switch (actionType)
        {
            case playerClientSideAction.clientActionType.standard:
                return new playerClientSideActionStandard((playerClientSideActionStandard.playerStandardAction)action, playerToRegisterAction);
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
