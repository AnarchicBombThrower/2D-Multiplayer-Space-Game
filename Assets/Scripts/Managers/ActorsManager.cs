using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ActorsManager : NetworkBehaviour
{
    public static ActorsManager instance { get; private set; } //we do not want this to be publically setable on getable
    public GameObject AiActorPrefab;
    public GameObject playerActorPrefab;
    public enum actorType { player, nonPlayer }
    public delegate void actorCreatedCallback(Actor createdActor);
    const int MAX_ACTORS = 100;
    private Actor[] actorsInGame = new Actor[MAX_ACTORS];
    private int earliestFreeActorId = 0;

    public override void OnNetworkSpawn()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Actors manager");
            return;
        }

        instance = this;
    }

    //should only be called from server
    public Actor createActor(actorType ofType, Vector2 at, actorCreatedCallback callbackToWhenCreated)
    {
        if (IsHost == false) { Debug.LogError("Don't call createActor from client!"); }

        Actor thisActor = Instantiate(getPrefabOfTypeOfActor(ofType)).GetComponent<Actor>();
        actorsInGame[earliestFreeActorId] = thisActor;
        callbackToWhenCreated(thisActor);
        thisActor.setActorId(earliestFreeActorId);
        earliestFreeActorId = findNextFreeId(earliestFreeActorId + 1);

        switch (ofType)
        {
            case actorType.player:
                break;
            case actorType.nonPlayer:
                break;
        }

        thisActor.transform.position = at;
        return thisActor; //this gets returned and the network spawning gets handeled by whoever calls this
    }

    private GameObject getPrefabOfTypeOfActor(actorType type)
    {
        switch (type) 
        {
            case actorType.player: return playerActorPrefab;
            case actorType.nonPlayer: return AiActorPrefab;
        }

        return null;
    }

    //should only be called from server
    public Actor getActor(int actorId)
    {
        if (IsHost == false) { Debug.LogError("Don't call getActor from client!"); return null; }

        return actorsInGame[actorId];
    }

    [ServerRpc]
    public void removeActorServerRpc(int actorId) 
    {
        if (actorsInGame[actorId] == null)
        {
            Debug.LogError("No actor to remove with ID " + actorId);
            return;
        }

        Destroy(actorsInGame[actorId]);

        if (earliestFreeActorId > actorId)
        {
            earliestFreeActorId = actorId;
        }
    }

    private int findNextFreeId(int from)
    {
        for (int i = 0; i < MAX_ACTORS; i++)
        {
            if (actorsInGame[i] == null)
            {
                return i;
            }
        }

        return -1;
    }
}
