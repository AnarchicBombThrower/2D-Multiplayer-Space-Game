using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Missile : NetworkBehaviour
{
    private Vector2 target;
    [SerializeField]
    private float speed;
    [SerializeField]
    private float damage;
    [SerializeField]
    private float damageFalloffPerDistance;
    const float ROTATION_OFFSET = 90;

    // Update is called once per frame
    void Update()
    {
        if (IsHost == false)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    public void missileGoto(Vector2 gotoPosition, float angleAt)
    {
        target = gotoPosition;
        transform.rotation = Quaternion.Euler(0, 0, angleAt + ROTATION_OFFSET);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        handleCollision(collision);
    }

    private void handleCollision(Collider2D collision)
    {
        if (collision.gameObject == null)
        {
            return;
        }

        Ship ship = collision.gameObject.GetComponent<Ship>();

        if (ship != null)
        {
            attackShip(ship);
        }
    }

    private void attackShip(Ship shipAttack)
    {
        ShipInteractableComponent[] shipComponents = shipAttack.getShipComponents();
        List<Actor> shipActors = shipAttack.getActorsOnShip();

        foreach (ShipInteractableComponent component in shipComponents)
        {
            component.dealDamange(getDamage(component.gameObject));
        }

        foreach (Actor actor in shipActors)
        {
            actor.dealDamage(getDamage(actor.gameObject));
        }
    }

    private int getDamage(GameObject objectToHandle)
    {
        float distanceToObject = Vector2.Distance(transform.position, objectToHandle.transform.position);
        float damageToDealFloat = damage - distanceToObject * damageFalloffPerDistance; //damage = initial damage - distance * falloff per unit of distance
        int damageRoundedDown = (int)Math.Round(damageToDealFloat, 0);
        return damageRoundedDown;
    }
}
