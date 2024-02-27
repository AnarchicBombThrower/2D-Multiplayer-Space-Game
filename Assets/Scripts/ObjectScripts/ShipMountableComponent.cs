using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipMountableComponent : ShipInteractableComponent
{
    List<Actor> mountedOnUs = new List<Actor>();

    protected void mountOntoUs(Actor actor)
    {
        mountedOnUs.Add(actor);
    }

    protected bool unmount(Actor actor)
    {
        if (!mountedOnUs.Contains(actor)) { return false; }
        mountedOnUs.Remove(actor);
        return true;
    }
}
