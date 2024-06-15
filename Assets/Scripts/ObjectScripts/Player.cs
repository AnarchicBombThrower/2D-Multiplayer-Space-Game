using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static PlayersManager.playerServerSideActionShip;

public class Player : Controller
{
    public enum controllerTypes { standard, steering, gun }
    public Camera playerCamera;
    private controllerType controllingManager = null;

    protected override void onStart()
    {
        if (IsOwner == false) { return; }

        playerCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
        PlayersManager.instance.setPlayerControllerToStandard(this);
    }

    private void Update()
    {
        if (IsOwner == false) { return; } //if we do not own this then we cannot control      

        if (Input.GetKey("a"))
        {
            controllingManager.leftPressed();
        }

        if (Input.GetKey("d"))
        {
            controllingManager.rightPressed();
        }

        if (Input.GetKey("w"))
        {
            controllingManager.upPressed();
        }

        if (Input.GetKey("s"))
        {
            controllingManager.downPressed();
        }

        if(Input.GetKeyDown("f"))
        {
            controllingManager.interactionPressed();
        }

        if (Input.GetKey("r"))
        {
            controllingManager.repairPressed();
        }

        if (Input.GetKey("space"))
        {
            controllingManager.mainActionPressed();
        }

        if (Input.GetMouseButtonDown(1))
        {
            PlayersManager.instance.placePingAtServerRpc(playerCamera.ScreenToWorldPoint(Input.mousePosition));
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            PlayersManager.instance.disconnectLocalPlayer();
        }

        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
        controllingManager.endOfUpdate();
    }

    public override void unmounted()
    {
        PlayersManager.instance.setPlayerControllerToStandard(this);
    }

    public override void mountedOnSteering(SteeringComponent mountedOn)
    {
        PlayersManager.instance.setPlayerControllerToSteering(this, mountedOn);
    }

    public override void mountedOnGun(GunComponent mountedOn)
    {
        PlayersManager.instance.setPlayerControllerToGun(this, mountedOn);
    }

    public override void onShip(Ship nowOn)
    {
        PlayersManager.instance.playerOnShip(this, nowOn);
    }

    public override void sightOfShip(Ship nowSeen)
    {
        PlayersManager.instance.showShipToPlayer(nowSeen, this);
    }

    public override void noSightOfShip(Ship nowUnseen)
    {
        PlayersManager.instance.unshowShipToPlayer(nowUnseen, this);
    }

    public void setToStandardController()
    {
        if (IsOwner == false) { return; }
        newControllerSet();
        controllingManager = new standardController(this);
    }

    public void setToSteeringController(SteeringComponent steering)
    {
        if (IsOwner == false) { return; }
        newControllerSet();
        controllingManager = new steeringController(steering);
    }

    public void setToGunController(GunComponent gun)
    {
        if (IsOwner == false) { return; }
        newControllerSet();
        controllingManager = new gunController(ourActor, gun);
    }

    private void newControllerSet()
    {
        if (controllingManager == null)
        {
            return;
        }

        controllingManager.controllerDestroyed();
    }

    public Camera getPlayerCamera()
    {
        return playerCamera;
    }

    interface controllerType
    {
        public abstract void leftPressed();

        public abstract void rightPressed();

        public abstract void downPressed();

        public abstract void upPressed();

        public abstract void interactionPressed();

        public abstract void repairPressed();

        public virtual void mainActionPressed() { }

        public abstract void endOfUpdate();

        public virtual void controllerDestroyed() { }
    }

    class standardController : controllerType
    {
        private List<uint> playerActions = new List<uint>();
        Player controlling;
        Actor actorControlling;

        public standardController(Player player)
        {
            controlling = player;
            actorControlling = player.getActor();
        }

        void controllerType.leftPressed()
        {
            playerActions.Add((uint)PlayersManager.playerClientSideActionStandard.playerStandardAction.moveLeft);
        }

        void controllerType.rightPressed()
        {
            playerActions.Add((uint)PlayersManager.playerClientSideActionStandard.playerStandardAction.moveRight);
        }

        void controllerType.downPressed()
        {
            playerActions.Add((uint)PlayersManager.playerClientSideActionStandard.playerStandardAction.moveDown);
        }

        void controllerType.upPressed()
        {
            playerActions.Add((uint)PlayersManager.playerClientSideActionStandard.playerStandardAction.moveUp);
        }

        void controllerType.interactionPressed()
        {
            actorControlling.interact();
        }

        void controllerType.repairPressed()
        {
            actorControlling.repair();
        }

        void controllerType.endOfUpdate()
        {
            PlayersManager.instance.registerClientAction(PlayersManager.playerClientSideAction.clientActionType.standard, playerActions.ToArray(), controlling);
            playerActions.Clear();
        }
    }

    class steeringController : controllerType 
    {
        private List<uint> shipActions;
        private SteeringComponent ourSteeringComponent;

        public steeringController(SteeringComponent steering)
        {
            ourSteeringComponent = steering;
            shipActions = new List<uint>();
        }

        //calls to server rpcs as we need data stored server side
        void controllerType.leftPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionShip.shipAction.rotateLeft);
        }

        void controllerType.rightPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionShip.shipAction.rotateRight);
        }     

        void controllerType.downPressed()
        {
            throw new NotImplementedException();
        }

        void controllerType.upPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionShip.shipAction.thrust);
        }

        void controllerType.interactionPressed()
        {
            PlayersManager.instance.unmountLocalPlayerFromComponent(ourSteeringComponent);
        }

        void controllerType.repairPressed()
        {
            return;
        }     

        void controllerType.mainActionPressed()
        {
            ourSteeringComponent.jumpServerRpc();
        }

        void controllerType.endOfUpdate()
        {
            PlayersManager.instance.registerServerAction(PlayersManager.playerServerSideAction.serverActionType.ship, shipActions.ToArray());
            shipActions.Clear();
        }
    }

    class gunController : controllerType
    {
        private List<uint> shipActions;
        private Actor actorControlling;
        private GunComponent gunControlling;

        public gunController(Actor actorToControl, GunComponent gun)
        {
            actorControlling = actorToControl;
            gunControlling = gun;
            shipActions = new List<uint>();
            PlayerUiManager.playerOnGun(gun);
        }

        void controllerType.leftPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionGun.gunAction.rotateLeft);
        }

        void controllerType.rightPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionGun.gunAction.rotateRight);
        }

        void controllerType.downPressed()
        {
            throw new NotImplementedException();
        }

        void controllerType.upPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionGun.gunAction.extendDistance);
        }

        void controllerType.interactionPressed()
        {
            PlayersManager.instance.unmountLocalPlayerFromComponent(gunControlling);
        }

        void controllerType.repairPressed()
        {
            return;
        }

        void controllerType.mainActionPressed()
        {
            shipActions.Add((uint)PlayersManager.playerServerSideActionGun.gunAction.shoot);
        }

        void controllerType.endOfUpdate()
        {
            PlayersManager.instance.registerServerAction(PlayersManager.playerServerSideAction.serverActionType.gun, shipActions.ToArray());
            shipActions.Clear();
        }

        void controllerType.controllerDestroyed()
        {
            PlayerUiManager.playerNoLongerOnGun(gunControlling);
        }
    }
}
