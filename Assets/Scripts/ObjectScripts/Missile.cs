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
    private int damage;
    [SerializeField]
    private float damageFalloffPerDistance;
    private List<Ship> shipsTouching = new List<Ship>();
    const float ROTATION_OFFSET = 90;
    const float TARGET_DISTANCE_LEEWAY = 0.01f;
    const string SHIP_HITBOX_TAG = "ShipTrigger";

    // Update is called once per frame
    void Update()
    {
        if (IsHost == false)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < TARGET_DISTANCE_LEEWAY)
        {
            explode();
        }
    }

    public void missileGoto(Vector2 gotoPosition, float angleAt)
    {
        target = gotoPosition;
        transform.rotation = Quaternion.Euler(0, 0, angleAt + ROTATION_OFFSET);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        handleCollision(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        handleCollision(collision, false);
    }

    private void handleCollision(Collider2D collision, bool entering)
    {
        if (collision.gameObject == null || collision.gameObject.tag != SHIP_HITBOX_TAG)
        {
            return;
        }

        Ship ship = collision.transform.parent.GetComponent<Ship>();

        if (ship != null)
        {
            if (entering)
            {
                shipsTouching.Add(ship);
            }
            else if (shipsTouching.Contains(ship))
            {
                shipsTouching.Remove(ship);
            }
        }
    }

    private void explode()
    {
        Ship[] shipArray = shipsTouching.ToArray(); //convert to array as list may get modified mid iteration

        foreach (Ship touch in shipArray)
        {
            attackShip(touch);
        }

        Destroy(gameObject);
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

        shipAttack.dealHullDamage(damage);
    }

    private int getDamage(GameObject objectToHandle)
    {
        float distanceToObject = Vector2.Distance(transform.position, objectToHandle.transform.position);
        //damage = initial damage - distance * falloff per unit of distance
        float damageToDealFloat = damage - distanceToObject * damageFalloffPerDistance; 
        int damageRoundedDown = (int)Math.Round(damageToDealFloat, 0);
        return damageRoundedDown;
    }

    public float getSpeed()
    {
        return speed;
    }
}
