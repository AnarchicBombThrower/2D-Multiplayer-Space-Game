using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPingUiManager : MonoBehaviour
{
    public static PlayerPingUiManager instance { get; private set; } //we do not want this to be publically setable on getable
    [SerializeField]
    private GameObject playerPingPrefab;
    [SerializeField]
    private Canvas worldSpaceCanvas;
    const float PING_LIFETIME_SECONDS = 9999999; //it is deleted after this amount of seconds

    void Start()
    {
        if (instance != null) //this is a singleton, we do not want a second instance of this
        {
            Debug.LogError("Two instances of Player(s) manager");
            return;
        }

        instance = this;
    }

    public void createPingLocally(Vector2 pos)
    {
        PingUi newPing = Instantiate(playerPingPrefab, worldSpaceCanvas.transform).GetComponent<PingUi>();
        newPing.setTimer(PING_LIFETIME_SECONDS);
        newPing.setPlacePoint(pos);
        newPing.setCamera(PlayersManager.instance.getPlayerCamera());
    }
}
