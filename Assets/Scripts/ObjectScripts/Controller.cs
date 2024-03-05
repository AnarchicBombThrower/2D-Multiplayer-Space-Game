using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Controller : NetworkBehaviour
{
    protected Actor ourActor;

    public override void OnNetworkSpawn()
    {
        ourActor = GetComponent<Actor>();
        onStart();
    }

    public Actor getActor()
    {
        return ourActor;
    }

    protected abstract void onStart();

    public abstract void unmounted();

    public abstract void mountedOnSteering(SteeringComponent mountedOn);

    public abstract void sightOfShip(Ship nowSeen);

    public abstract void noSightOfShip(Ship nowUnseen);
}
