using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ShipInteractableComponent;

[RequireComponent(typeof(Rigidbody2D))]
public class Ship : NetworkBehaviour
{
    public Vector2 defaultBoardPosition;
    private ShipInteractableComponent[] shipInteractableComponents;
    private Rigidbody2D ourRigidbody;
    private float thrust = 500;
    private float rotateSpeed = 45;
    private bool applyingThrust = false;
    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        ourRigidbody = GetComponent<Rigidbody2D>();
        shipInteractableComponents = GetComponentsInChildren<ShipInteractableComponent>();
    }

    public ShipInteractableComponent[] getShipComponents()
    {
        return shipInteractableComponents;
    }

    public void subscribeToAllHealthSetEvents(componentHealthSet toSubscribe)
    {
        foreach (ShipInteractableComponent component in shipInteractableComponents)
        {
            component.subscribeToHealthSetEvent(toSubscribe);
        }
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

    public Vector2 getDefaultBoardPositionGlobal()
    {
        return new Vector2(transform.position.x, transform.position.y) + defaultBoardPosition;
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
