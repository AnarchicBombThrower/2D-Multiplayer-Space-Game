using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GunComponent : ShipMountableComponent
{
    [SerializeField]
    private GameObject missilePrefab;
    const float Y_OFFSET = 0.55f;

    public void shootMissile(Vector2 at)
    {
        Missile newMissile = Instantiate(missilePrefab, transform.position, Quaternion.identity).GetComponent<Missile>();
        newMissile.missileGoto(at);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void interactWithUsServerRpc(int actorId)
    {
        Actor actor = ActorsManager.instance.getActor(actorId);
        mountOntoUs(actor);
        actor.mountOntoGun(this, new Vector2(0, Y_OFFSET));
    }
}
