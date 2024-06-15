using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RepairShip : MonoBehaviour
{
    const int REPAIR_AMOUNT = 50;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        handleCollision(collision);
        print("test");
    }

    private void handleCollision(Collider2D collision)
    {
        if (collision.gameObject == null || collision.GetComponent<Ship>() == null)
        {
            return;
        }

        Ship ship = collision.GetComponent<Ship>();

        if (ship != null)
        {
            ship.repairHullDamage(REPAIR_AMOUNT);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
