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
    private float missileMinDistance;
    [SerializeField]
    private float missileMaxDistance;
    [SerializeField]
    private float missileDistanceGrowth;
    [SerializeField]
    private float reloadTime;
    private NetworkVariable<float> distanceShoot = new NetworkVariable<float>();
    private NetworkVariable<float> currentReloadTime = new NetworkVariable<float>();
    const float Y_OFFSET = 0.55f;

    public override void onSpawnedAsHost()
    {
        distanceShoot.Value = missileMinDistance;
        currentReloadTime.Value = 0;
    }

    private void Update()
    {
        if (IsHost == false)
        {
            return;
        }

        currentReloadTime.Value = Mathf.Max(currentReloadTime.Value - Time.deltaTime, 0);
    }

    //server side methods
    public void shootMissile()
    {
        if (currentReloadTime.Value != 0)
        {
            return;
        }

        Missile newMissile = Instantiate(missilePrefab, transform.position, Quaternion.identity).GetComponent<Missile>();
        newMissile.GetComponent<NetworkObject>().Spawn(true);
        //shoot at the direction we are pointed times the distance (added onto where we are intially)
        newMissile.missileGoto((Vector2)transform.position + (Vector2)transform.up * distanceShoot.Value, transform.eulerAngles.z);
        currentReloadTime.Value = reloadTime;
        distanceShoot.Value = missileMinDistance;
    }

    public void extendMissileShootDistance()
    {
        distanceShoot.Value = getMissileDistanceWithIncrease(distanceShoot.Value);
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
    private float getMissileDistanceWithIncrease(float current)
    {
        return Mathf.Min(current + missileDistanceGrowth * Time.deltaTime, missileMaxDistance);
    }

    public float getMissileShootDistance()
    {
        return distanceShoot.Value;
    }

    public float getMissileSpeed()
    {
        return missilePrefab.GetComponent<Missile>().getSpeed();
    }

    public void subscribeToOnShootDistanceChangedCallback(NetworkVariable<float>.OnValueChangedDelegate callback)
    {
        distanceShoot.OnValueChanged += callback;
    }

    public void unsubscribeToOnShootDistanceChangedCallback(NetworkVariable<float>.OnValueChangedDelegate callback)
    {
        distanceShoot.OnValueChanged -= callback;
    }
}
