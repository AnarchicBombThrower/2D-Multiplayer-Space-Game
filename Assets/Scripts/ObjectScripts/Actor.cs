using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using System;
using Unity.Mathematics;

public class Actor : NetworkBehaviour
{
    private NetworkVariable<int> actorId = new NetworkVariable<int>(-1);
    //helper components
    private Controller ourController;
    private Rigidbody2D ourRigidbody;
    private Collider2D ourCollider;
    //ship interaction
    private List<ShipInteractableComponent> interactableComponentsWeCanInteractWith = new List<ShipInteractableComponent>();
    private List<Ship> shipsWeSee = new List<Ship>();
    private Ship shipOn = null;
    private ShipMountableComponent mountedOn = null;
    //attributes
    private Vector2 localPositionToKeep;
    private bool locked = false;
    private float repairTimer = 0;
    private int health;
    private Vector2 localMovementVelocity;
    private bool movingLeft;
    private bool movingRight;
    private bool movingUp;
    private bool movingDown;
   //constants 
    const float SPEED = 5;
    const int REPAIR_AMOUNT = 20;
    const float REPAIR_TIMER = 2;
    const float RANDOM_FORCE = 200;
    //callbacks
    public delegate void canInteractWithComponent(ShipInteractableComponent canInteractWith);
    private canInteractWithComponent canInteractWithComponentCallback;

    public override void OnNetworkSpawn()
    {
        ourController = GetComponent<Controller>();
        ourRigidbody = GetComponent<Rigidbody2D>();
        ourCollider = GetComponent<Collider2D>();
        health = 100;
    }

    void Update()
    {
        if (IsOwner == false) { return; }

        repairTimer = Mathf.Max(0, repairTimer - Time.deltaTime);

        if (!locked)
        {
            return;
        }
  
        transform.localPosition = localPositionToKeep;
    }

    private void FixedUpdate()
    {
        if (IsOwner == false) { return; }

        localMovementVelocity = new Vector2(0, 0);

        if (movingLeft)
        {
            localMovementVelocity += new Vector2(-SPEED, 0);
        }

        if (movingRight)
        {
            localMovementVelocity += new Vector2(SPEED, 0);
        }

        if (movingUp)
        {
            localMovementVelocity += new Vector2(0, SPEED);
        }

        if (movingDown)
        {
            localMovementVelocity += new Vector2(0, -SPEED);
        }

        movingLeft = movingRight = movingUp = movingDown = false;
        ourRigidbody.velocity = localMovementVelocity;

        if (shipOn != null) { ourRigidbody.velocity += shipOn.getRigidbodyVelocity(); }
    }
    public void subscribeToCanInteractWithCallback(canInteractWithComponent subscribe)
    {
        canInteractWithComponentCallback += subscribe;
    }

    public void unsubscribeToCanInteractWithCallback(canInteractWithComponent subscribe)
    {
        canInteractWithComponentCallback -= subscribe;
    }

    //only should be called once on an actor (from server)
    public void setActorId(int to)
    {
        if (actorId.Value != -1)
        {
            Debug.LogError("Actor Id being set a second time! " + actorId.Value + " attempted to set to " + to);
            return;
        }

        actorId.Value = to;
    }

    public int getActorId()
    {
        return actorId.Value;
    }

    public void getOnShip(Ship getOn)
    {
        getOnShip(getOn, getOn.getDefaultBoardPositionGlobal());
    }

    public void getOnShip(Ship getOn, Vector2 whereWeBoardTo)
    {
        if (shipOn != null)
        {
            Debug.LogError("Trying to get on ship without getting off current ship!");
        }

        shipOn = getOn;
        shipOn.actorOnShip(this);
        addShipToSight(shipOn);
        transform.position = whereWeBoardTo;
        transform.SetParent(getOn.transform);
        ourController.onShip(getOn);
    }

    public void getOffShip()
    {
        if (shipOn == null)
        {
            Debug.LogError("Trying to get off ship without being on a ship!");
        }

        shipOn.actorLeftShip(this);
        removeShipFromSight(shipOn);
        transform.SetParent(null);
        shipOn = null;
    }

    public void addShipToSight(Ship toAdd) //we are recieving this on server
    {
        shipsWeSee.Add(toAdd);
        ourController.sightOfShip(toAdd);
    }

    public void removeShipFromSight(Ship toRemove)
    {
        shipsWeSee.Remove(toRemove);
        ourController.noSightOfShip(toRemove);
    }

    //actor basic controls
    public void addForceLeft()
    {
        movingLeft = true;
    }

    public void addForceRight()
    {
        movingRight = true;
    }

    public void addForceUp()
    {
        movingUp = true;
    }

    public void addForceDown()
    {
        movingDown = true;
    }

    public bool interact()
    {
        if (interactableComponentsWeCanInteractWith.Count == 0)
        {
            return false;
        }
      
        return interactWithSpecific(interactableComponentsWeCanInteractWith[0]);
    }

    public bool interactWithSpecific(ShipInteractableComponent interactWith)
    {
        if (interactableComponentsWeCanInteractWith.Contains(interactWith) == false)
        {
            return false;
        }

        interactWith.interactWithUsServerRpc(getActorId());
        
        return true;
    }

    public bool repair()
    {
        if (interactableComponentsWeCanInteractWith.Count == 0)
        {
            return false;
        }
  
        return repairSpecific(interactableComponentsWeCanInteractWith[0]);
    }

    public bool repairSpecific(ShipInteractableComponent repair)
    {
        if (interactableComponentsWeCanInteractWith.Contains(repair) == false)
        {
            return false;
        }

        if (repairTimer > 0)
        {
            return true;
        }

        repairTimer = REPAIR_TIMER;
        repair.repairDamage(REPAIR_AMOUNT);
        return true;
    }

    public bool canInteractWith(ShipInteractableComponent canInteract)
    {
        return interactableComponentsWeCanInteractWith.Contains(canInteract);
    }

    public void dealDamage(int damage)
    {
        health = Math.Max(0, health - damage); //health can only drop to zero

        if (damage == 0)
        {
            die();
        }
    }

    public void die()
    {
        ragdollMode();
        enabled = false;
        ourController.enabled = false;

        if (locked)
        {
            unlockPosition();
        }
    }

    public void ragdollMode()
    {
        ourRigidbody.constraints = RigidbodyConstraints2D.None; //allow the lifeless corpse to drift...
        ourRigidbody.mass = 1;
        ourRigidbody.drag = 1;
        ourRigidbody.AddForce(
        new Vector2(UnityEngine.Random.Range(-RANDOM_FORCE, RANDOM_FORCE), 
        UnityEngine.Random.Range(-RANDOM_FORCE, RANDOM_FORCE)));
    }

    public ShipMountableComponent whatAreWeMountedOn()
    {
        return mountedOn;
    }

    public void unmountFromMountedComponent()
    {
        unlockPosition();
        ourController.unmounted();
        mountedOn = null;
    }

    //interactable component interaction
    public void mountOntoSteering(SteeringComponent steeringMountedOn, Vector2 offset)
    {
        mountOntoComponent(steeringMountedOn, offset);
        ourController.mountedOnSteering(steeringMountedOn);
    }

    public void mountOntoGun(GunComponent gunMountedOn, Vector2 offset)
    {
        mountOntoComponent(gunMountedOn, offset);
        ourController.mountedOnGun(gunMountedOn);
    }

    private void mountOntoComponent(ShipMountableComponent mountOn, Vector2 offset)
    {
        mountedOn = mountOn;
        transform.position = mountedOn.transform.position + new Vector3(offset.x, offset.y, 0);
        localPositionToKeep = transform.localPosition;
        lockPosition(localPositionToKeep);
    }

    public void inRangeOfInteractableComponent(ShipInteractableComponent component)
    {
        interactableComponentsWeCanInteractWith.Add(component);

        if (canInteractWithComponentCallback != null)
        {
            canInteractWithComponentCallback(component);
        }
    }

    public void noLongerInRangeOfInteractableComponent(ShipInteractableComponent component)
    {
        interactableComponentsWeCanInteractWith.Remove(component);
    }

    public void lockPosition(Vector2 localPositionAt)
    {
        localPositionToKeep = localPositionAt;
        locked = true;
        ourCollider.enabled = false;
    }

    public void unlockPosition()
    {
        locked = false;
        ourCollider.enabled = true;
    }
    
    public Rigidbody2D getRigidbody()
    {
        return ourRigidbody;
    }
}
