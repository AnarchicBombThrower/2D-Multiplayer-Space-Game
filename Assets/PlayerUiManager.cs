using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiManager : MonoBehaviour
{
    private static ShipUiManager shipUiManager;

    // Start is called before the first frame update
    void Start()
    {
        shipUiManager = GetComponent<ShipUiManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void playerGainedSightOfShip(Ship nowSeen)
    {
        shipUiManager.displayShip(nowSeen);
    }

    public static void playerLostSightOfShip(Ship nowSeen)
    {
        shipUiManager.hideShip(nowSeen);
    }
}
