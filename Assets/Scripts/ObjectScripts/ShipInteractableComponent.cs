using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class ShipInteractableComponent : NetworkBehaviour
{
    public delegate void componentHealthSet(ShipInteractableComponent sender, int to);
    public delegate void componentFunctioningStateSet(ShipInteractableComponent sender, bool to);
    //we can subscribe to these if we want to recieve updates about what's going on
    private componentHealthSet healthSet;
    private componentFunctioningStateSet functioningStateSetCallback;
    //our component's attributes
    private BoxCollider2D ourCollider;
    private SpriteRenderer ourSpriteRenderer;
    private int health = 100;
    private bool broken = false;
    
    // Start is called before the first frame update
    void Start()
    {
        ourCollider = GetComponent<BoxCollider2D>();
        ourSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        handleCollision(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
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
        if (health - toDeal <= 0)
        {
            setHealth(0);
            breakComponent();
        }
        else
        {
            setHealth(health - toDeal);
        }      
    }

    public void repairDamamge(int toRepair)
    {
        if (health + toRepair >= 100)
        {
            setHealth(100);
            repairComponent();
        }
        else
        {
            setHealth(health + toRepair);
        }
    }

    public void subscribeToHealthSetEvent(componentHealthSet toSubscribe)
    {
        healthSet += toSubscribe;
    }

    private void setHealth(int setTo)
    {
        health = setTo;

        if (healthSet != null)
        {
            healthSet(this, health);
        }
    }

    private void breakComponent()
    {
        broken = true;

        if (functioningStateSetCallback != null)
        {
            functioningStateSetCallback(this, broken);
        }  
    }

    private void repairComponent()
    {
        broken = false;

        if (functioningStateSetCallback != null)
        {
            functioningStateSetCallback(this, broken);
        }   
    }

    protected bool isComponentFunctioning()
    {
        return broken == false;
    }
}
