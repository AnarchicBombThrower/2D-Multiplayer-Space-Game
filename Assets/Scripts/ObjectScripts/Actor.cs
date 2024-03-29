using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using System;

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
   //constants 
    const float force = 100;
    const int repairAmount = 20;

    public override void OnNetworkSpawn()
    {
        ourController = GetComponent<Controller>();
        ourRigidbody = GetComponent<Rigidbody2D>();
        ourCollider = GetComponent<Collider2D>();
        health = 100;
    }

    void Update()
    {
        repairTimer = Mathf.Max(0, repairTimer - Time.deltaTime);

        if (!locked)
        {
            return;
        }
  
        transform.localPosition = localPositionToKeep;
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
            removeShipFromSight(shipOn);
        }

        shipOn = getOn;
        shipOn.actorOnShip(this);
        addShipToSight(shipOn);
        transform.position = whereWeBoardTo;
        transform.SetParent(getOn.transform);
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
        ourRigidbody.AddForce(new Vector2(-1, 0) * Time.deltaTime * force);
    }

    public void addForceRight()
    {
        ourRigidbody.AddForce(new Vector2(1, 0) * Time.deltaTime * force);
    }

    public void addForceUp()
    {
        ourRigidbody.AddForce(new Vector2(0, 1) * Time.deltaTime * force);
    }

    public void addForceDown()
    {
        ourRigidbody.AddForce(new Vector2(0, -1) * Time.deltaTime * force);
    }

    public bool interact()
    {
        if (interactableComponentsWeCanInteractWith.Count == 0)
        {
            return false;
        }

        interactableComponentsWeCanInteractWith[0].interactWithUsServerRpc(getActorId());
        return true;
    }

    public bool repair()
    {
        if (interactableComponentsWeCanInteractWith.Count == 0)
        {
            return false;
        }

        interactableComponentsWeCanInteractWith[0].repairDamamge(repairAmount);
        return true;
    }

    public void dealDamage(int damage)
    {
        health = Math.Max(0, health - damage); //health can only drop to zero

        if (damage == 0)
        {
            die();
        }
    }

    private void die()
    {
        throw new NotImplementedException();
        //decision to be made here what should we do when the player dies? for now nothing happens but something to be designed here
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
        print("test");
        mountOntoComponent(gunMountedOn, offset);
        ourController.mountedOnGun(gunMountedOn);
    }

    private void mountOntoComponent(ShipMountableComponent mountOn, Vector2 offset)
    {
        print("test");
        mountedOn = mountOn;
        transform.position = mountedOn.transform.position + new Vector3(offset.x, offset.y, 0);
        localPositionToKeep = transform.localPosition;
        lockPosition(localPositionToKeep);
    }

    public void inRangeOfInteractableComponent(ShipInteractableComponent component)
    {
        interactableComponentsWeCanInteractWith.Add(component);
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
}
