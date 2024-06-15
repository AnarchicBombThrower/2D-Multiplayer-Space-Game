using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : NetworkBehaviour
{
    const string DONT_DESTROY_TAG = "DontDestroy";
    public static EncounterManager instance { get; private set; } //we do not want this to be publically setable on getable
    [SerializeField]
    private GameObject[] planetPrefabs;
    [SerializeField]
    private Encounter baseEncounter;
    private bool inTransition = false;
    private int encounterNumberOn = 1;
    const int LOGARITHMIC_GROWTH_ENEMY_SHIPS = 8;
    const int ROLL_PEACFUL_MIN = 2;
    const int ROLL_PEACFUL_MAX = 5;
    const int MAX_AMOUNT_OF_PLANETS = 3;
    const int MIN_AMOUNT_OF_PLANETS = 1;
    const int MAX_AMOUNT_OF_ENEMIES_ON_SHIP = 4;
    const int MIN_AMOUNT_OF_ENEMIES_ON_SHIP = 3;
    const float PLANET_POSITION_RANGE = 20;
    private int timesSincePeacefulStage = 0;

    public override void OnNetworkSpawn()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Actors manager");
            return;
        }

        instance = this;

        GameObject[] dontDestroyObjects = GameObject.FindGameObjectsWithTag(DONT_DESTROY_TAG);

        foreach (GameObject dontDestroy in dontDestroyObjects)
        {
            DontDestroyOnLoad(dontDestroy);
        }
    }

    public void setInTransitionStatus(bool to)
    {
        inTransition = to;
    }

    public bool areWeInTransition()
    {
        return inTransition;
    }

    [Serializable]
    private class Encounter
    {
        [SerializeField]
        private ShipSpawn[] shipsToSpawn;
        [SerializeField]
        private string sceneToLoad;
        [SerializeField]
        private bool peaceful;
        [SerializeField]
        private bool noRepair;
        [SerializeField]
        private int canBeSpawnedByLevel;
        private Vector2[] planetPositions;
        private GameObject[] planets;
        const float RANDOM_SPAWN_DISTANCE_FOR_HULL_REPAIRS = 10;

        public Encounter(ShipSpawn[] ships, string scene, bool isPeaceful, Vector2[] placePlanetPositions, GameObject[] planetPrefabs)
        {
            shipsToSpawn = ships;
            sceneToLoad = scene;
            peaceful = isPeaceful;
            planetPositions = placePlanetPositions;
            planets = planetPrefabs;
            noRepair = false;
        }

        public void spawnEncounter()
        {
            if (EncounterManager.instance.areWeInTransition())
            {
                return;
            }

            EncounterManager.instance.setInTransitionStatus(true);
            ShipManager.instance.freshEncounter();
            NetworkManager.Singleton.SceneManager.OnLoadComplete += sceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);  
        }

        private void sceneLoadCompleted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            ShipManager.instance.resetPlayerShipPosition();

            if (shipsToSpawn != null)
            {
                foreach (ShipSpawn spawnData in shipsToSpawn)
                {
                    Ship newShip = ShipManager.instance.spawnEnemyShip(spawnData.shipIdToSpawn, spawnData.spawnpoint);

                    for (int i = 0; i < spawnData.enemiesOnShip; i++)
                    {
                        Actor newActor = ActorsManager.instance.createActor(ActorsManager.actorType.nonPlayer, new Vector2(0, 0), spawnAiActor);
                        newActor.getOnShip(newShip);
                        newShip.GetComponent<ShipAIControl>().addAIActor(newActor.GetComponent<AIControl>());
                    }

                    void spawnAiActor(Actor toSpawn)
                    {
                        toSpawn.GetComponent<NetworkObject>().Spawn(true);
                    }
                }
            }

            ShipManager.instance.informAllShipOfEncounterChange(peaceful);
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= sceneLoadCompleted;
            EncounterManager.instance.setInTransitionStatus(false);
            if (planetPositions != null) { EncounterManager.instance.placePlanets(planetPositions, planets); }
            if (peaceful && noRepair == false) 
            {
                Vector2 randomSpawn = new Vector2(
                UnityEngine.Random.Range(-RANDOM_SPAWN_DISTANCE_FOR_HULL_REPAIRS, RANDOM_SPAWN_DISTANCE_FOR_HULL_REPAIRS),
                UnityEngine.Random.Range(-RANDOM_SPAWN_DISTANCE_FOR_HULL_REPAIRS, RANDOM_SPAWN_DISTANCE_FOR_HULL_REPAIRS));
                ShipManager.instance.spawnNewRepairHullPrefab(randomSpawn); 
            }
        }

        public int getSpawnedByLevel()
        {
            return canBeSpawnedByLevel;
        }

        public bool getIsPeaceful()
        {
            return peaceful;
        }
    }

    [Serializable]
    struct ShipSpawn
    {
        [SerializeField]
        public int shipIdToSpawn;
        [SerializeField]
        public Vector2 spawnpoint;
        public int enemiesOnShip;
    }

    //call from server

    public void startFirstEncounter()
    {
        newEncounter(baseEncounter);
    }

    Vector2[] spawnpoints = new Vector2[] { new Vector2(10, 10), new Vector2(0, 10), new Vector2(-10, 10), new Vector2(-10, 0), new Vector2(10, 0), new Vector2(-10, -10), new Vector2(-10, 0), new Vector2(10, -10) };

    public void createNextEncounter()
    {
        int peacefulRoll = UnityEngine.Random.Range(ROLL_PEACFUL_MIN, ROLL_PEACFUL_MAX);
        bool peace = peacefulRoll <= timesSincePeacefulStage;

        ShipSpawn[] spawn = null;

        if (peace == false)
        {
            spawn = createShips();
        }

        int numberOfPlanets = UnityEngine.Random.Range(MIN_AMOUNT_OF_PLANETS, MAX_AMOUNT_OF_PLANETS);
        Vector2[] placeAt = new Vector2[numberOfPlanets];
        GameObject[] prefabs = new GameObject[numberOfPlanets];

        for (int i = 0; i < numberOfPlanets; i++)
        {
            placeAt[i] = new Vector2(
            UnityEngine.Random.Range(-PLANET_POSITION_RANGE, PLANET_POSITION_RANGE), 
            UnityEngine.Random.Range(-PLANET_POSITION_RANGE, PLANET_POSITION_RANGE));
            int index = UnityEngine.Random.Range(0, planetPrefabs.Length);
            prefabs[i] = planetPrefabs[index];
        }

        Encounter randomEncounter = new Encounter(spawn, "BlankScene", peace, placeAt, prefabs);
        newEncounter(randomEncounter);
    }

    private void placePlanets(Vector2[] placeAt, GameObject[] prefabs)
    {
        for (int i = 0; i < placeAt.Length; i++)
        {
            GameObject newPlanet = Instantiate(prefabs[i]);
            newPlanet.transform.position = placeAt[i];
            newPlanet.GetComponent<NetworkObject>().Spawn(true);
        }   
    }

    private ShipSpawn[] createShips()
    {
        int maxShips = Mathf.CeilToInt(Mathf.Log(encounterNumberOn, LOGARITHMIC_GROWTH_ENEMY_SHIPS));
        int shipsToSpawn = UnityEngine.Random.Range(1, maxShips);
        ShipSpawn[] shipsToSpawnArray = new ShipSpawn[shipsToSpawn];

        for (int i = 0; i < shipsToSpawn; i++)
        {
            shipsToSpawnArray[i].spawnpoint = spawnpoints[i];
            shipsToSpawnArray[i].enemiesOnShip = UnityEngine.Random.Range(MIN_AMOUNT_OF_ENEMIES_ON_SHIP, MAX_AMOUNT_OF_ENEMIES_ON_SHIP + 1); //update this adds consts
        }

        return shipsToSpawnArray;
    }

    private void newEncounter(Encounter newEncounter)
    {
        newEncounter.spawnEncounter();
        encounterNumberOn++;
        if (newEncounter.getIsPeaceful()) 
        { timesSincePeacefulStage = 0; } 
        else { timesSincePeacefulStage++; }
    }
}
