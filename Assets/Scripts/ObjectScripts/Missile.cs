using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Missile : MonoBehaviour
{
    private Vector2 target;
    private float speed;
    private float damage;
    private float damageFalloffPerDistance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Input.GetKeyDown("j"))
        {
            testFunction();
        }
    }

    public void missileGoto(Vector2 gotoPosition)
    {
        target = gotoPosition;
    }

    private void testFunction()
    {
        damage = 50;
        damageFalloffPerDistance = 10;
        speed = 10;
        transform.position = new Vector2(10, 10);
        missileGoto(Vector2.zero);
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
