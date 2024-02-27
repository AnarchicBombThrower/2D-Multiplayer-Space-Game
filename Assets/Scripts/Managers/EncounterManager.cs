using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EncounterManager : NetworkBehaviour
{
    public static EncounterManager instance { get; private set; } //we do not want this to be publically setable on getable
    [SerializeField]
    private Encounter[] encounters;

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
    private class Encounter
    {
        [SerializeField]
        private ShipSpawn[] shipsToSpawn;

        public void spawnEncounter()
        {
            foreach (ShipSpawn spawnData in shipsToSpawn)
            {
                ShipManager.instance.spawnEnemyShip(spawnData.shipIdToSpawn, spawnData.spawnpoint);
            }
        }
    }

    [Serializable]
    struct ShipSpawn
    {
        [SerializeField]
        public int shipIdToSpawn;
        [SerializeField]
        public Vector2 spawnpoint;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void createRandomEncounter()
    {
        int encounterRandomIndex = UnityEngine.Random.Range(0, encounters.Length);
        encounters[encounterRandomIndex].spawnEncounter();
    }
}
