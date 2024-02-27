using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SteeringComponent : ShipMountableComponent
{
    public Ship ourShip;

    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        mountOntoUs(actor);
        actor.mountOntoSteering(this, new Vector2(0, 0.55f));
    }

    public void thrust()
    {
        ourShip.applyThrust();
    }

    public void rotateLeft()
    {
        ourShip.rotateShip(false);
    }

    public void rotateRight()
    {
        ourShip.rotateShip(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void dismountFromSteeringServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        unmount(actor);
        actor.unmountFromSteeringServerRpc();
    }
}
