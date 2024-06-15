using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ShipInteractableComponent;

[RequireComponent(typeof(Rigidbody2D))]
public class Ship : NetworkBehaviour
{
    public Vector2 defaultBoardPosition;
    [SerializeField]
    private GunComponent gunComponent;
    [SerializeField]
    private SteeringComponent steeringComponent;
    [SerializeField]
    private ScannerComponent scanningComponent;
    private ShipInteractableComponent[] shipInteractableComponents;
    private List<Actor> actorsOnShip;
    private Rigidbody2D ourRigidbody;
    private float thrust = 500;
    private float rotateSpeed = 45;
    [SerializeField]
    private int hullStrengthMax;
    private NetworkVariable<int> hullStrength = new NetworkVariable<int>();
    private NetworkVariable<float> jumpCharge = new NetworkVariable<float>();
    const float JUMP_CHARGE_RATE_SEC = 1.2f;
    const float JUMP_CHARGE_MAX = 100;
    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        ourRigidbody = GetComponent<Rigidbody2D>();
        shipInteractableComponents = GetComponentsInChildren<ShipInteractableComponent>();
        if (IsHost == false) { return; }
        actorsOnShip = new List<Actor>();
        hullStrength.Value = hullStrengthMax;
    }

    private void Update()
    {
        if (IsHost == false) { return; }
        jumpCharge.Value = Mathf.Min(jumpCharge.Value + JUMP_CHARGE_RATE_SEC * Time.deltaTime, JUMP_CHARGE_MAX);
    }

    public void subscribeToJumpChargeValueChangeCallback(NetworkVariable<float>.OnValueChangedDelegate callback)
    {
        jumpCharge.OnValueChanged += callback;
    }

    public void unsubscribeToJumpChargeValueChangeCallback(NetworkVariable<float>.OnValueChangedDelegate callback)
    {
        jumpCharge.OnValueChanged -= callback;
    }

    public void subscribeToHullStrengthValueChangeCallback(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        hullStrength.OnValueChanged += callback;
    }

    public void unsubscribeToHullStrengthValueChangeCallback(NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        hullStrength.OnValueChanged -= callback;
    }

    public void newEncounterBegun(bool peaceful)
    {
        if (peaceful)
        {
            setJumpChargeToMax();
        }
        else
        {
            jumpCharge.Value = 0;
        }    
    }

    public void setJumpChargeToMax()
    {
        jumpCharge.Value = JUMP_CHARGE_MAX;
    }

    public void dealHullDamage(int amount)
    {
        hullStrength.Value = Mathf.Max(0, hullStrength.Value - amount);

        if (hullStrength.Value == 0)
        {
            shipDestroyed();
        }
    }

    public void repairHullDamage(int amount)
    {
        hullStrength.Value = Mathf.Min(hullStrengthMax, hullStrength.Value + amount);
    }

    public void shipDestroyed()
    {
        while (actorsOnShip.Count > 0)
        {
            Actor currentActor = actorsOnShip[0];
            currentActor.getOffShip();
            currentActor.die();
        }

        ShipManager.instance.shipDestroyed(this);
        GetComponent<NetworkObject>().Despawn();
    }

    public ShipInteractableComponent[] getShipComponents()
    {
        return shipInteractableComponents;
    }

    public GunComponent getGunComponent()
    {
        return gunComponent;
    }

    public SteeringComponent getSteeringComponent()
    {
        return steeringComponent;
    }

    public ScannerComponent getScannerComponent()
    {
        return scanningComponent;
    }

    public Rigidbody2D getRigidbody()
    {
        return ourRigidbody;
    }

    public Vector2 getRigidbodyVelocity()
    {
        return ourRigidbody.velocity;
    }

    public List<Actor> getActorsOnShip()
    {
        return actorsOnShip;
    }

    public void subscribeToAllHealthSetEvents(NetworkVariable<int>.OnValueChangedDelegate toSubscribe)
    {
        foreach (ShipInteractableComponent component in shipInteractableComponents)
        {
            component.subscribeToHealthSetCallback(toSubscribe);
        }
    }

    public void actorOnShip(Actor actor)
    {
        actorsOnShip.Add(actor);
    }

    public void actorLeftShip(Actor actor)
    {
        if (actorsOnShip.Contains(actor) == false)
        {
            Debug.LogError(actor + " trying to leave ship we are not on!");
            return;
        }

        actorsOnShip.Remove(actor);
    }

    public void rotateShip(bool right)
    {
        float rotate = Time.fixedDeltaTime * rotateSpeed;
        if (right == true) { rotate *= -1; }
        ourRigidbody.MoveRotation(ourRigidbody.rotation + rotate);
    }

    public void applyThrust()
    {
        ourRigidbody.AddForce(transform.up * thrust * Time.fixedDeltaTime);
    }

    public bool jumpSector()
    {
        if (jumpCharge.Value < JUMP_CHARGE_MAX)
        {
            return false;
        }

        ShipManager.instance.shipJumped(this);
        return true;
    }

    public Vector2 getDefaultBoardPositionGlobal()
    {
        return new Vector2(transform.position.x, transform.position.y) + defaultBoardPosition;
    }

    public float getShipMaxCharge()
    {
        return JUMP_CHARGE_MAX;
    }

    public int getShipMaxHullStrength()
    {
        return hullStrengthMax;
    }

    public float getJumpCharge()
    {
        return jumpCharge.Value;
    }

    public int getHullStrength()
    {
        return hullStrength.Value;
    }

    [ClientRpc]
    public void seenByPlayerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        PlayerUiManager.playerGainedSightOfShip(this);
    }

    [ClientRpc]
    public void unseenByPlayerClientRpc(ClientRpcParams clientRpcParams = default) //as in we no longer see this
    {
        PlayerUiManager.playerLostSightOfShip(this);
    }
}
