using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class ScannerComponent : ShipInteractableComponent
{
    [ServerRpc(RequireOwnership = false)]
    public override void actorInRangeServerRpc(int actorInRangeId)
    {
        List<Ship> shipsToAddToSight = new List<Ship>();
        shipsToAddToSight.AddRange(ShipManager.getEnemyShips());

        Actor inRange = ActorsManager.instance.getActor(actorInRangeId);

        foreach (Ship nowSee in shipsToAddToSight)
        {
            inRange.addShipToSight(nowSee);      
        }

        ShipManager.subscribeToNewEnemyCallback(inRange.addShipToSight);
        ShipManager.subscribeToEnemyRemovedCallback(inRange.removeShipFromSight);
    }

    [ServerRpc(RequireOwnership=false)]
    public override void actorOutOfRangeServerRpc(int actorOutOfRangeId)
    {
        List<Ship> shipsToRemoveFromSight = new List<Ship>();
        shipsToRemoveFromSight.AddRange(ShipManager.getEnemyShips());

        Actor outOfRange = ActorsManager.instance.getActor(actorOutOfRangeId);

        foreach (Ship nowUnSee in shipsToRemoveFromSight)
        {
            outOfRange.removeShipFromSight(nowUnSee);   
        }

        ShipManager.unsubscribeToEnemyRemovedCallback(outOfRange.addShipToSight);
        ShipManager.unsubscribeToEnemyRemovedCallback(outOfRange.removeShipFromSight);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        throw new System.NotImplementedException();
    }
}
