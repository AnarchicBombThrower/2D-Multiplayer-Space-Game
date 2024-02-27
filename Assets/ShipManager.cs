using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipManager : NetworkBehaviour
{
    public static ShipManager instance { get; private set; } //we do not want this to be publically setable on getable
    [SerializeField]private Vector2 playerShipStartPosition;
    [SerializeField]private shipTemplate[] shipsPlayersCanChoose;
    private static Ship playersShip;
    private static List<Ship> enemyShips = new List<Ship>();
    public delegate void newEnemyShip(Ship newEnemy);
    private static newEnemyShip newEnemyShipCallback;
    public delegate void removedEnemyShip(Ship removedEnemy);
    private static newEnemyShip removedEnemyShipCallback;
    private enum ShipAllegiance { player, enemy, ally }

    public override void OnNetworkSpawn()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Actors manager");
            return;
        }

        instance = this;
    }

    [Serializable]
    private class shipTemplate
    {
        [SerializeField] private GameObject shipPrefab;

        [ServerRpc]
        public Ship createShipInGameWorldServerRpc(ShipAllegiance allegiance, Vector2 positionOfShip)
        {
            Ship shipCreated = Instantiate(shipPrefab).GetComponent<Ship>();
            shipCreated.transform.position = positionOfShip;
            shipCreated.GetComponent<NetworkObject>().Spawn(); //spawn it as a network object

            switch (allegiance)
            {
                case ShipAllegiance.player: playersShip = shipCreated; break; 
                case ShipAllegiance.enemy: enemyShipCreated(shipCreated); break;
            }

            return shipCreated;
        }
    }

    private static void enemyShipCreated(Ship enemy)
    {
        enemyShips.Add(enemy);

        if (newEnemyShipCallback != null)
        {
            newEnemyShipCallback(enemy);
        }
    }

    public static Ship getPlayersShip()
    {
        return playersShip;
    }

    public static List<Ship> getEnemyShips()
    {
        return enemyShips;
    }

    public static void subscribeToNewEnemyCallback(newEnemyShip callback)
    {
        newEnemyShipCallback += callback;
    }

    public static void subscribeToEnemyRemovedCallback(newEnemyShip callback)
    {
        removedEnemyShipCallback += callback;
    }

    public static void unsubscribeToNewEnemyCallback(newEnemyShip callback)
    {
        newEnemyShipCallback -= callback;
    }

    public static void unsubscribeToEnemyRemovedCallback(newEnemyShip callback)
    {
        removedEnemyShipCallback -= callback;
    }

    public void spawnPlayerShip()
    {
        shipsPlayersCanChoose[0].createShipInGameWorldServerRpc(ShipAllegiance.player, playerShipStartPosition);
    }

    public void spawnEnemyShip(int shipId, Vector2 atPosition)
    {
        shipsPlayersCanChoose[shipId].createShipInGameWorldServerRpc(ShipAllegiance.enemy, atPosition);
    }
}
