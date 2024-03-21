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
        Debug.LogError(OwnerClientId);
        PlayersManager.instance.setPlayerControllerToStandard(this);
    }

    private void FixedUpdate() //Movement in FIXED update because we are making use of the unity physics system. TODO: MAKE THIS NORMAL UPDATE AND HANDLE EXECUTING IT IN PLAYER MANAGER OR SOMETHING
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

        if(Input.GetKey("f"))
        {
            controllingManager.interactionPressed();
        }

        if (Input.GetKey("r"))
        {
            controllingManager.repairPressed();
        }

        if (Input.GetMouseButton(1))
        {
            PlayersManager.instance.placePingAtClientRpc(playerCamera.ScreenToWorldPoint(Input.mousePosition));
        }

        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
        controllingManager.endOfUpdate();
    }

    public override void unmounted()
    {
        //setToStandardControllerClientRpc(getOurClientRpcParams());
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
        controllingManager = new standardController(ourActor);
    }

    public void setToSteeringController(SteeringComponent steering)
    {
        if (IsOwner == false) { return; }
        controllingManager = new steeringController(steering);
    }

    public void setToGunController(GunComponent gun)
    {
        if (IsOwner == false) { return; }
        controllingManager = new gunController(ourActor);
    }

    interface controllerType
    {
        public abstract void leftPressed();

        public abstract void rightPressed();

        public abstract void downPressed();

        public abstract void upPressed();

        public abstract void interactionPressed();

        public abstract void repairPressed();

        public abstract void endOfUpdate();
    }

    class standardController : controllerType
    {
        Actor controlling;

        public standardController(Actor toControl)
        {
            controlling = toControl;
        }

        void controllerType.leftPressed()
        {
            controlling.addForceLeft();
        }

        void controllerType.rightPressed()
        {
            controlling.addForceRight();
        }

        void controllerType.downPressed()
        {
            controlling.addForceDown();
        }

        void controllerType.upPressed()
        {
            controlling.addForceUp();
        }

        void controllerType.interactionPressed()
        {
            controlling.interact();
        }

        void controllerType.repairPressed()
        {
            controlling.repair();
        }

        void controllerType.endOfUpdate()
        {

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
            shipActions.Add((uint)PlayersManager.playerServerSideActionShip.shipAction.interactionPressed);
        }

        void controllerType.repairPressed()
        {
            return;
        }     

        void controllerType.endOfUpdate()
        {
            PlayersManager.instance.registerAction(PlayersManager.playerServerSideAction.actionType.ship, shipActions.ToArray());
            shipActions.Clear();
        }
    }

    class gunController : controllerType
    {
        private List<uint> shipActions;
        private Actor actorControlling;

        public gunController(Actor actorToControl)
        {
            actorControlling = actorToControl;
            shipActions = new List<uint>();
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
            shipActions.Add((uint)PlayersManager.playerServerSideActionGun.gunAction.shoot);
        }

        void controllerType.interactionPressed()
        {
            //shipActions.Add(PlayersManager.playerServerSideActionShip.shipAction.interactionPressed);
        }

        void controllerType.repairPressed()
        {
            return;
        }

        void controllerType.endOfUpdate()
        {
            PlayersManager.instance.registerAction(PlayersManager.playerServerSideAction.actionType.gun, shipActions.ToArray());
            shipActions.Clear();
        }
    }
}
