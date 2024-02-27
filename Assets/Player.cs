using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : Controller
{
    public enum controllerTypes { standard, steering }
    public Camera playerCamera;
    private controllerType controllingManager = null;

    protected override void onStart()
    {
        if (IsOwner == false) { return; }

        playerCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
        Debug.LogError(OwnerClientId);
        PlayersManager.instance.setPlayerControllerTo(this, controllerTypes.standard);
    }

    private void FixedUpdate() //Movement in FIXED update because we are making use of the unity physics system.
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

        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    private void Update()
    {
        if (IsOwner == false) { return; } //if we do not own this then we cannot control

        if (Input.GetKeyDown("f"))
        {
            controllingManager.interactionPressed();
        }

        if (Input.GetKey("r"))
        {
            controllingManager.repairPressed();
        }
    }

    public override void unmounted()
    {
        //setToStandardControllerClientRpc(getOurClientRpcParams());
        PlayersManager.instance.setPlayerControllerTo(this, controllerTypes.standard);
    }

    public override void mountedOnSteering(SteeringComponent mountedOn)
    {
        PlayersManager.instance.setPlayerControllerTo(this, controllerTypes.steering);
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

    public void setToSteeringController()
    {
        if (IsOwner == false) { return; }
        controllingManager = new steeringController(ourActor);
    }

    interface controllerType
    {
        public abstract void leftPressed();

        public abstract void rightPressed();

        public abstract void downPressed();

        public abstract void upPressed();

        public abstract void interactionPressed();

        public abstract void repairPressed();
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
    }

    class steeringController : controllerType 
    {
        Actor actorControlling;

        public steeringController(Actor actorToControl)
        {
            actorControlling = actorToControl;
        }

        void controllerType.leftPressed()
        {
            leftPressedServerRpc(actorControlling.getActorId());
        }

        //unlike the standard controller, these are server rpcs as we need data stored server side
        [ServerRpc]
        public void leftPressedServerRpc(int actorId)
        {
            print("test");
            SteeringComponent steeringOn = getActorSteeringComponenet(actorId);
            steeringOn.rotateLeft();
        }

        void controllerType.rightPressed()
        {
            rightPressedServerRpc(actorControlling.getActorId());
        }

        [ServerRpc]
        public void rightPressedServerRpc(int actorId)
        {
            SteeringComponent steeringOn = getActorSteeringComponenet(actorId);
            steeringOn.rotateRight();
        }

        void controllerType.downPressed()
        {
            throw new NotImplementedException();
        }

        void controllerType.upPressed()
        {
            upPressedServerRpc(actorControlling.getActorId());
        }

        [ServerRpc]
        public void upPressedServerRpc(int actorId)
        {
            SteeringComponent steeringOn = getActorSteeringComponenet(actorId);
            steeringOn.thrust();
        }

        void controllerType.interactionPressed()
        {
            interactionPressedServerRpc(actorControlling.getActorId());
        }

        [ServerRpc]
        public void interactionPressedServerRpc(int actorId)
        {
            SteeringComponent steeringOn = getActorSteeringComponenet(actorId);
            steeringOn.dismountFromSteeringServerRpc(actorId);
        }

        void controllerType.repairPressed()
        {
            return;
        }

        private SteeringComponent getActorSteeringComponenet(int actorId)
        {
            Actor actorControlling = ActorsManager.instance.getActor(actorId);
            SteeringComponent steeringOn = (SteeringComponent)actorControlling.whatAreWeMountedOn();
            return steeringOn;
        }
    }
}
