using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform[] allPlayers;
    private float yOffset = 1.6f;
    private float minDistance = 3f;
    private float xMin, xMax, yMin, yMax;

    // Update is called once per frame
    void LateUpdate()
    {
        if(allPlayers.Length == 0)
        {
            return;
        }

        xMin = xMax = allPlayers[0].position.x;
        yMin = yMax = allPlayers[0].position.y;

        for (int i = 1; i < allPlayers.Length; i++)
        {
            if( allPlayers[i].position.x < xMin)
            {
                xMin = allPlayers[i].position.x;
            }

            if (allPlayers[i].position.x > xMax)
            {
                xMax = allPlayers[i].position.x;
            }

            if (allPlayers[i].position.y < yMin)
            {
                yMin = allPlayers[i].position.y;
            }

            if (allPlayers[i].position.y > yMax)
            {
                yMax = allPlayers[i].position.y;
            }
        }

        float xMiddle = (xMin + xMax) / 4;
        float yMiddle = (yMin + yMax) / 4;
        float distance = xMax - xMin;

        if (distance < minDistance)
        {
            distance = minDistance;
        }

        transform.position = new Vector3(xMiddle, yMiddle + yOffset, -distance);
    }

}
