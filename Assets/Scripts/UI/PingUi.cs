using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PingUi : MonoBehaviour
{
    [SerializeField]
    private Text distanceText;
    private float lifetimeSeconds;
    private Vector2 placedAt; //our real position may be different as we may be off screen but this is where the ping has been placed originally
    private Camera localCamera;
    private float cameraHeight;
    private float cameraWidth;
    const int ROUND_TO_DIGITS = 2;
    const string DISTANCE_UNIT = "m";
    const float TUCKIN_DISTANCE = 1f; //this is the distance from the edge of the screen the ping ui starts staying within bounds (i.e. higher means further from edge)

    void Update()
    {
        lifetimeSeconds -= Time.deltaTime;

        if (lifetimeSeconds < 0)
        {
            stopPing();
        }

        distanceText.text = Math.Round(getDistance(), ROUND_TO_DIGITS) + DISTANCE_UNIT;
        transform.position = getRealPosition();
    }

    private float getDistance()
    {
        return PlayersManager.instance.getDistanceToLocalPlayer(placedAt);
    }

    private Vector2 getRealPosition()
    {
        Vector2 cameraPos = localCamera.transform.position;

        /* first we find the gradient of the line between the camera position (centre of the screen) and where the ping has been placed
        at some point we will hit the edge of the camera's view and that is were we want this ping to be (i.e as close as possible)
        to where we have been placed therefore pointing toward the position we are placed */
        float gradientOfLineToPlaceLocation = (cameraPos.y - placedAt.y) / (cameraPos.x - placedAt.x);
        float leftX = cameraPos.x - cameraWidth / 2 + TUCKIN_DISTANCE;
        float rightX = cameraPos.x + cameraWidth / 2 - TUCKIN_DISTANCE;
        float bottomY = cameraPos.y - cameraHeight / 2 + TUCKIN_DISTANCE;
        float topY = cameraPos.y + cameraHeight / 2 - TUCKIN_DISTANCE;

        if (placedAt.x > leftX && placedAt.x < rightX && placedAt.y < topY && placedAt.y > bottomY)
        {
            return placedAt;
        }

        //check the left side of the rectangle
        if (placedAt.x <= cameraPos.x)
        {
            //find the y point that the line is ending up at on the left line
            float yAtLineCrossing = gradientOfLineToPlaceLocation * (leftX - placedAt.x) + placedAt.y;

            //are we between the top and bottom line?
            if (bottomY <= yAtLineCrossing && yAtLineCrossing <= topY)
            {
                //we return we are at leftx (because we are on the left line of the rectangle) and the y point we cross that line
                return new Vector2(leftX, yAtLineCrossing);
            }
        }

        //check the right side
        if (placedAt.x >= cameraPos.x)
        {
            //find y point at right
            float yAtLineCrossing = gradientOfLineToPlaceLocation * (rightX - placedAt.x) + placedAt.y;

            //same as above case
            if (bottomY <= yAtLineCrossing && yAtLineCrossing <= topY)
            {
                //same as above case but the right x
                return new Vector2(rightX, yAtLineCrossing);
            }
        }

        //check bottom side
        if (placedAt.y <= cameraPos.y)
        {
            //find the x point that the line is ending up at on the bottom line
            float xAtLineCrossing = (bottomY - placedAt.y) / gradientOfLineToPlaceLocation + placedAt.x;

            //are we between right and left line?
            if (leftX <= xAtLineCrossing && xAtLineCrossing <= rightX)
            {
                return new Vector2(xAtLineCrossing, bottomY);
            }
        }

        //check top side
        if (placedAt.y >= cameraPos.y)
        {
            //find the x point that the line is ending up at on the bottom line
            float xAtLineCrossing = (topY - placedAt.y) / gradientOfLineToPlaceLocation + placedAt.x;

            //are we between right and left line?
            if (leftX <= xAtLineCrossing && xAtLineCrossing <= rightX)
            {
                return new Vector2(xAtLineCrossing, topY);
            }
        }

        Debug.LogError("Should not get here? Fix ping ui position calculation function!");
        return new Vector2(0, 0);
    }

    public void setTimer(float to)
    {
        lifetimeSeconds = to;
    }

    public void setPlacePoint(Vector2 to)
    {
        placedAt = to;
    }

    public void setCamera(Camera to)
    {
        localCamera = to;
        cameraHeight = 2f * localCamera.orthographicSize; //camera height is 2 times size
        cameraWidth = cameraHeight * localCamera.aspect; //width is height times aspect (based on it being othogprahic if camera is no longer othographic change!)
    }

    private void stopPing()
    {
        Destroy(gameObject);
    }
}
