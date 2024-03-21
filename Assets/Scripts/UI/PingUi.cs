using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PingUi : MonoBehaviour
{
    [SerializeField]
    private Text distanceText;
    const int ROUND_TO_DIGITS = 2;
    const string DISTANCE_UNIT = "m";

    void Update()
    {
        distanceText.text = Math.Round(PlayersManager.instance.getDistanceToLocalPlayer(transform.position), ROUND_TO_DIGITS) + DISTANCE_UNIT;
    }
}
