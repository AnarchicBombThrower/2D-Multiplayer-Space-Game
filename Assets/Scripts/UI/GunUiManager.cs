using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunUiManager : MonoBehaviour
{
    const int ROUND_TO_DIGITS = 2;
    const string DISTANCE_UNIT = "m";
    [SerializeField]
    private Text distanceText;
    private GunComponent gunPlayerIsOn = null;

    private void Update()
    {
        if (gunPlayerIsOn == null)
        {
            return;
        }

        distanceText.transform.position = gunPlayerIsOn.transform.position;
    }

    public void showGunUi(GunComponent gun)
    {
        distanceText.gameObject.SetActive(true);
        gunPlayerIsOn = gun;
        gunPlayerIsOn.subscribeToOnShootDistanceChangedCallback(onMissileShootDistanceChanged);
        onMissileShootDistanceChanged(0, gunPlayerIsOn.getMissileShootDistance());
    }

    public void unshowGunUi()
    {
        distanceText.gameObject.SetActive(false);
        gunPlayerIsOn.unsubscribeToOnShootDistanceChangedCallback(onMissileShootDistanceChanged);
        gunPlayerIsOn = null;
    }

    public void onMissileShootDistanceChanged(float prev, float to)
    {
        distanceText.text = Math.Round(to, ROUND_TO_DIGITS) + DISTANCE_UNIT;
    }
}
