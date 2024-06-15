using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipManager : NetworkBehaviour
{
    public static ShipManager instance { get; private set; } //we do not want this to be publically setable on getable
    [SerializeField] private Vector2 playerShipStartPosition;
    [SerializeField] private shipTemplate[] shipsPlayersCanChoose;
    [SerializeField] private shipTemplate[] enemyShipTemplates;
    [SerializeField] private GameObject repairHullPrefab;
    private static Ship playersShip;
    private static List<Ship> enemyShips = new List<Ship>();
    public delegate void newEnemyShip(Ship newEnemy);
    private static newEnemyShip newEnemyShipCallback;
    public delegate void removedEnemyShip(Ship removedEnemy);
    private static newEnemyShip removedEnemyShipCallback;
    private enum ShipAllegiance { player, enemy, ally }
    const float HULL_REPAIR_RANDOM_DISTANCE = 2.5F;
    const int MAX_HULL_REPAIRS_PER_SPAWN = 3;

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
            bool destroyShipOnSceneChanged = true;

            switch (allegiance)
            {
                case ShipAllegiance.player: playersShip = shipCreated; destroyShipOnSceneChanged = false; break;
                case ShipAllegiance.enemy: enemyShipCreated(shipCreated); break;
            }

            shipCreated.GetComponent<NetworkObject>().Spawn(destroyShipOnSceneChanged);//spawn it as a network object
            return shipCreated;
        }
    }

    public void shipJumped(Ship ship)
    {
        if (ship == playersShip)
        {
            EncounterManager.instance.createNextEncounter();
        }
        else
        {
            //todo: allow ai ships to jump
        }
    }

    public void freshEncounter()
    {
        enemyShips.Clear();
    }

    public void informAllShipOfEncounterChange(bool peaceful)
    {
        playersShip.newEncounterBegun(peaceful);

        foreach (Ship ship in enemyShips)
        {
            ship.newEncounterBegun(peaceful);
        }
    }

    public void resetPlayerShipPosition()
    {
        playersShip.transform.position = new Vector2(0, 0);
    }

    public void shipDestroyed(Ship ship)
    {
        if (ship == playersShip)
        {
            //gameover
        }
        else if (enemyShips.Contains(ship))
        {
            enemyShipRemoved(ship, true);
            spawnNewRepairHullPrefab(ship.transform.position);
        }
        else
        {
            Debug.LogError("Ship not registered as enemy or player ship!");
        }
    }

    public void spawnNewRepairHullPrefab(Vector2 around)
    {
        int amountToSpawn = UnityEngine.Random.Range(1, MAX_HULL_REPAIRS_PER_SPAWN + 1);

        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject newRepairHullObject = Instantiate(repairHullPrefab);
            newRepairHullObject.transform.position = around + 
            new Vector2(UnityEngine.Random.Range(-HULL_REPAIR_RANDOM_DISTANCE, HULL_REPAIR_RANDOM_DISTANCE),
            UnityEngine.Random.Range(-HULL_REPAIR_RANDOM_DISTANCE, HULL_REPAIR_RANDOM_DISTANCE));
            newRepairHullObject.GetComponent<NetworkObject>().Spawn(true);
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

    private void enemyShipRemoved(Ship enemy, bool destroyed)
    {
        enemyShips.Remove(enemy);

        if (enemyShips.Count == 0)
        {
            playersShip.setJumpChargeToMax();
        }

        if (removedEnemyShipCallback != null)
        {
            removedEnemyShipCallback(enemy);
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
        PlayersManager.instance.playerShipSpawned(playersShip);
    }

    public Ship spawnEnemyShip(int shipId, Vector2 atPosition)
    {
        return enemyShipTemplates[shipId].createShipInGameWorldServerRpc(ShipAllegiance.enemy, atPosition);
    }
}
