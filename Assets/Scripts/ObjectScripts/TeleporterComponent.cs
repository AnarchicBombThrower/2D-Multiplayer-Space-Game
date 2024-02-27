using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeleporterComponent : ShipInteractableComponent
{
    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        actor.getOnShip(ShipManager.getPlayersShip());
    }
}
