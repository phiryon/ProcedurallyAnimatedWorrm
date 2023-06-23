using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class WormAI : MonoBehaviour
{
    [Header("Pathing")]
    [SerializeField] CinemachineSmoothPath pathINT;
    [SerializeField] CinemachineSmoothPath pathEXT;
    [SerializeField] CinemachineDollyCart cart;
    [SerializeField] LayerMask terrainLayer;

    [SerializeField] float speed = 100f;
    [SerializeField] float offset = 1f;
    [SerializeField] Vector3 OvergroundArcShape = new Vector3(15f, 10f, 45f);
    [SerializeField] Vector3 UndergroundArcShape = new Vector3(5f, 30f, 0);

    [HideInInspector] public Vector3 startPosition, endPosition;
    RaycastHit hitInfo;
    bool isUnder = false;

    [SerializeField] Transform target;

    void Start()
    {
        AI();
    }

    void AI()
    {
        UpdatePath();
        StartCoroutine(FollowPath());
        IEnumerator FollowPath()
        {
            while (true)
            {
                yield return new WaitUntil(() => cart.m_Position >= 0.99f);

                //update which path to follow
                isUnder = !isUnder;
                if (isUnder) cart.m_Path = pathINT;
                else cart.m_Path = pathEXT;

                UpdatePath();

                yield return new WaitUntil(() => cart.m_Position <= 0.05f);
            }
        }
    }

    void UpdatePath()
    {
        if (isUnder) UpdatePathUnderground();
        else UpdatePathOverground();

    }

    void UpdatePathUnderground()
    {
        Vector3 playerPosition = target.position /*+ (playerShip.spaceshipRigidbody.velocity * 3)*/; //commenting out player position predicting
        playerPosition.y = Mathf.Min(-10, playerPosition.y);
        Vector3 randomRange = Random.insideUnitSphere * 10;
        randomRange.y = 0;
        startPosition = pathEXT.m_Waypoints[pathINT.m_Waypoints.Length - 1].position;
        endPosition = startPosition + randomRange;

        //if (Physics.Raycast(startPosition, Vector3.up, out hitInfo, terrainLayer.value))
        //{
        //    startPosition = hitInfo.point;

        //}

        if (Physics.Raycast(endPosition, Vector3.up, out hitInfo, 20f, terrainLayer.value))
        {
            endPosition = hitInfo.point;
        }

        Quaternion rot = pathEXT.EvaluateOrientationAtUnit(1f, CinemachinePathBase.PositionUnits.Normalized);
        Vector3 pos = pathEXT.EvaluatePositionAtUnit(1f, CinemachinePathBase.PositionUnits.Normalized);

        pathINT.m_Waypoints[0].position = startPosition;
        pathINT.m_Waypoints[1].position = startPosition + (rot * Vector3.forward * offset);
        pathINT.m_Waypoints[2].position = startPosition + (Vector3.down * UndergroundArcShape.y);
        pathINT.m_Waypoints[3].position = endPosition;

        pathINT.InvalidateDistanceCache();

        cart.m_Position = 0;

        //speed
        cart.m_Speed = speed / pathINT.PathLength;
    }

    void UpdatePathOverground()
    {
        Vector3 targetPosition = target.position /*+ (playerShip.spaceshipRigidbody.velocity * 3)*/; //commenting out player position predicting

        float distance = Vector3.Distance(targetPosition, transform.position);
        if (distance > 20f)
        {
            targetPosition = Vector3.Lerp(transform.parent.position, targetPosition, 20f / distance);
            // for noise
            targetPosition = targetPosition - Random.insideUnitSphere * 2;
        }

        Vector3 randomRange = Random.insideUnitSphere * 5;
        randomRange.y = 0;
        startPosition = pathINT.m_Waypoints[pathINT.m_Waypoints.Length - 1].position;
        endPosition = targetPosition + randomRange;

        //if (Physics.Raycast(startPosition, Vector3.down, out hitInfo, terrainLayer.value))
        //{
        //    startPosition = hitInfo.point;
        //}

        if (Physics.Raycast(endPosition, Vector3.down, out hitInfo, 20f, terrainLayer.value))
        {
            endPosition = hitInfo.point;
        }
        Quaternion rot = pathINT.EvaluateOrientationAtUnit(1f, CinemachinePathBase.PositionUnits.Normalized);
        Vector3 pos = pathINT.EvaluatePositionAtUnit(1f, CinemachinePathBase.PositionUnits.Normalized);

        pathEXT.m_Waypoints[0].position = startPosition;
        pathEXT.m_Waypoints[1].position = startPosition + (rot * Vector3.forward * offset);

        Vector3 arcPeak = targetPosition + (Vector3.up * OvergroundArcShape.y);

        if (Physics.Raycast(arcPeak, Vector3.down, out hitInfo, 5f, terrainLayer.value))
        {
            arcPeak = endPosition + (Vector3.up * 5f);
            endPosition = arcPeak + randomRange;
            endPosition.y = hitInfo.point.y;
        }

        pathEXT.m_Waypoints[2].position = arcPeak;
        pathEXT.m_Waypoints[3].position = endPosition + (Vector3.down * OvergroundArcShape.z);

        //pathEXT.m_Waypoints[3].position = startPosition + newEnd/*(Vector3.down * OvergroundArcShape.z)*/;

        pathEXT.InvalidateDistanceCache();

        cart.m_Position = 0;

        //speed
        cart.m_Speed = speed / pathEXT.PathLength;
    }
}
