using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GunComponent : ShipMountableComponent
{
    [SerializeField]
    private GameObject missilePrefab;
    [SerializeField]
    private float rotateSpeed;
    [SerializeField]
    private float missileMaxDistance;
    [SerializeField]
    private float missileDistanceGrowth;
    [SerializeField]
    private float reloadTime;
    private float distanceShoot = 40;
    private float currentReloadTime;
    const float Y_OFFSET = 0.55f;

    private void Update()
    {
        if (IsHost == false)
        {
            return;
        }

        currentReloadTime = Mathf.Max(currentReloadTime - Time.deltaTime, 0);
    }

    //server side methods
    public void shootMissile()
    {
        if (currentReloadTime != 0)
        {
            return;
        }

        Missile newMissile = Instantiate(missilePrefab, transform.position, Quaternion.identity).GetComponent<Missile>();
        newMissile.GetComponent<NetworkObject>().Spawn();
        //shoot at the direction we are pointed times the distance (added onto where we are intially)
        newMissile.missileGoto((Vector2)transform.position + (Vector2)transform.up * distanceShoot, transform.eulerAngles.z);
        currentReloadTime = reloadTime;
    }

    public void extendMissileShootDistance()
    {
        distanceShoot = getMissileDistanceWithIncrease(distanceShoot);
    }

    public void rotateGunLeft()
    {
        transform.localRotation = Quaternion.Euler(0, 0, transform.localRotation.eulerAngles.z + rotateSpeed * Time.deltaTime);
    }

    public void rotateGunRight()
    {
        transform.localRotation = Quaternion.Euler(0, 0, transform.localRotation.eulerAngles.z - rotateSpeed * Time.deltaTime);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        mountOntoUs(actor);
        actor.mountOntoGun(this, new Vector2(0, Y_OFFSET));
    }

    //other methods
    public float getMissileDistanceWithIncrease(float current)
    {
        return Mathf.Min(current + missileDistanceGrowth * Time.deltaTime, missileMaxDistance);
    }
}
