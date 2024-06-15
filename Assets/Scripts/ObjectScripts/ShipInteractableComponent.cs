using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class ShipInteractableComponent : NetworkBehaviour
{
    //our component's attributes
    private BoxCollider2D ourCollider;
    private SpriteRenderer ourSpriteRenderer;
    private NetworkVariable<int> health = new NetworkVariable<int>();
    private NetworkVariable<bool> broken = new NetworkVariable<bool>();
    const int MAX_HEALTH = 100;

    public override void OnNetworkSpawn()
    {
        ourCollider = GetComponent<BoxCollider2D>();
        ourSpriteRenderer = GetComponent<SpriteRenderer>();

        if (IsHost == false) { return; }

        health.Value = MAX_HEALTH;
        broken.Value = false;
        onSpawnedAsHost();
    }

    public virtual void onSpawnedAsHost() { }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        handleCollision(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (GetComponentInParent<NetworkObject>() == null || GetComponentInParent<NetworkObject>().IsSpawned == false) //messy but has to be done
        {
            return;
        }

        handleCollision(collision, false);
    }

    private void handleCollision(Collider2D collision, bool enteringUs)
    {
        if (collision.gameObject == null)
        {
            return;
        }

        Actor actorWithinCollision = collision.gameObject.GetComponent<Actor>(); //does this collider have an actor attached?

        if (actorWithinCollision != null)
        {
            if (enteringUs)
            {
                actorWithinCollision.inRangeOfInteractableComponent(this);
                actorInRangeServerRpc(actorWithinCollision.getActorId());
            }
            else
            {
                actorWithinCollision.noLongerInRangeOfInteractableComponent(this);
                actorOutOfRangeServerRpc(actorWithinCollision.getActorId());
            }
        }
    }

    public SpriteRenderer getOurSpriteRenderer() { return ourSpriteRenderer; }

    [ServerRpc(RequireOwnership = false)]
    public virtual void interactWithUsServerRpc(int actorId) { }

    [ServerRpc(RequireOwnership=false)]
    public virtual void actorInRangeServerRpc(int actorId) { }

    [ServerRpc(RequireOwnership = false)]
    public virtual void actorOutOfRangeServerRpc(int actorId) { }

    public void dealDamange(int toDeal)
    {
        if (health.Value - toDeal <= 0)
        {
            setHealth(0);
            breakComponent();
        }
        else
        {
            setHealth(health.Value - toDeal);
        }      
    }

    public void repairDamage(int toRepair)
    {
        if (health.Value + toRepair >= MAX_HEALTH)
        {
            setHealth(MAX_HEALTH);
            repairComponent();
        }
        else
        {
            setHealth(health.Value + toRepair);
        }
    }

    public void subscribeToHealthSetCallback(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        health.OnValueChanged += callback;
    }

    public void unsubscribeToHealthSetCallback(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        health.OnValueChanged -= callback;
    }

    public void subscribeToBrokenStatusSetCallback(NetworkVariable<bool>.OnValueChangedDelegate callback)
    {
        broken.OnValueChanged += callback;
    }

    public void unsubscribeToBrokenStatusSetCallback(NetworkVariable<bool>.OnValueChangedDelegate callback)
    {
        broken.OnValueChanged -= callback;
    }

    private void setHealth(int setTo)
    {
        health.Value = setTo;
    }

    public float getHealthAsFraction()
    {
        return (float)health.Value / (float)MAX_HEALTH;
    }

    private void breakComponent()
    {
        broken.Value = true;
        componentBroken();
    }

    public virtual void componentBroken() { }

    private void repairComponent()
    {
        broken.Value = false;
    }

    protected bool isComponentFunctioning()
    {
        return broken.Value == false;
    }
}
