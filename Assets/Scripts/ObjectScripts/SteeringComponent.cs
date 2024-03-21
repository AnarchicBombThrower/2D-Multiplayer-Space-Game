using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SteeringComponent : ShipMountableComponent
{
    public Ship ourShip;
    const float Y_OFFSET = 0.55f;

    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        return;
        mountOntoUs(actor);
        actor.mountOntoSteering(this, new Vector2(0, Y_OFFSET));
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

    public void dismountFromSteering(Actor actor)
    {
        unmount(actor);
        actor.unmountFromMountedComponent();
    }
}
