using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipMountableComponent : ShipInteractableComponent
{
    List<Actor> mountedOnUs = new List<Actor>();
    const int MOUNTED_ON_LIMIT = 1; //limit at 1 for now but future potential for objects with multiple mountees

    protected bool mountOntoUs(Actor actor)
    {
        if (isComponentFunctioning() == false || mountedOnUs.Count == MOUNTED_ON_LIMIT)
        {
            return false;
        }

        mountedOnUs.Add(actor);
        return true;
    }

    public bool unmount(Actor actor)
    {
        if (!mountedOnUs.Contains(actor)) { return false; }
        mountedOnUs.Remove(actor);
        actor.unmountFromMountedComponent();
        return true;
    }

    public override void componentBroken()
    {
        while (mountedOnUs.Count > 0)
        {
            unmount(mountedOnUs[0]);
        }
    }
}
