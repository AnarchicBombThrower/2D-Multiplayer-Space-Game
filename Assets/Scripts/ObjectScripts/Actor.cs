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
    //ship interaction
    private List<ShipInteractableComponent> interactableComponentsWeCanInteractWith = new List<ShipInteractableComponent>();
    private List<Ship> shipsWeSee = new List<Ship>();
    private Ship shipOn = null;
    private ShipMountableComponent mountedOn = null;
    //attributes
    private Vector2 localPositionToKeep;
    private bool locked = false;
    private float repairTimer = 0;
   //constants 
    const float force = 100;
    const int repairAmount = 20;

    public override void OnNetworkSpawn()
    {
        ourController = GetComponent<Controller>();
        ourRigidbody = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        repairTimer = Mathf.Max(0, repairTimer - Time.deltaTime);

        GetComponent<Collider2D>().enabled = !locked; //temp

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

    public void addForceDown()
    {
        ourRigidbody.AddForce(new Vector2(0, -1) * Time.deltaTime * force);
    }

    public ShipMountableComponent whatAreWeMountedOn()
    {
        return mountedOn;
    }

    [ServerRpc]
    public void unmountFromSteeringServerRpc()
    {
        unlockPosition();
        ourController.unmounted();
        mountedOn = null;
    }

    //interactable component interaction
    public void mountOntoSteering(SteeringComponent steeringMountedOn, Vector2 offset)
    {
        mountedOn = steeringMountedOn;
        ourController.mountedOnSteering(steeringMountedOn);
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
    }

    public void unlockPosition()
    {
        //ourRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        locked = false;
    } 
}
