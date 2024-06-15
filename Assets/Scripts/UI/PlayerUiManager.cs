using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiManager : MonoBehaviour
{
    private static ShipUiManager shipUiManager;
    private static GunUiManager gunUiManager;
    private static ClientIssuesUiManager clientIssuesUiManager;

    // Start is called before the first frame update
    void Start()
    {
        shipUiManager = GetComponent<ShipUiManager>();
        gunUiManager = GetComponent<GunUiManager>();
        clientIssuesUiManager = GetComponent<ClientIssuesUiManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void playerNowOnShip(Ship shipOn)
    {
        shipUiManager.localPlayerOnShip(shipOn);
    }

    public static void playerGainedSightOfShip(Ship nowSeen)
    {
        shipUiManager.displayShip(nowSeen);
    }

    public static void playerLostSightOfShip(Ship nowSeen)
    {
        shipUiManager.hideShip(nowSeen);
    }

    public static void playerOnGun(GunComponent gun)
    {
        gunUiManager.showGunUi(gun);
    }

    public static void playerNoLongerOnGun(GunComponent gun)
    {
        gunUiManager.unshowGunUi();
    }

    public static void clientDisconnected(bool localClient)
    {
        clientIssuesUiManager.clientDisconnected(localClient);
    }

    public static void clientStopped()
    {
        shipUiManager.removeComponentsFromMemory();
    }
}
