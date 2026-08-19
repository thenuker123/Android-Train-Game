using UnityEngine;

/// <summary>
/// Stores track waypoints and provides lightweight waypoint lookup helpers.
/// </summary>
public class TrackManager : MonoBehaviour
{
    [Tooltip("Ordered waypoints that define the train track path.")]
    public Transform[] waypoints;

    public int WaypointCount => waypoints == null ? 0 : waypoints.Length;

    public Transform GetWaypoint(int index)
    {
        if (WaypointCount == 0 || index < 0 || index >= WaypointCount)
        {
            return null;
        }

        return waypoints[index];
    }

    public int GetClosestWaypointIndex(Vector3 worldPosition)
    {
        if (WaypointCount == 0)
        {
            return -1;
        }

        int closestIndex = 0;
        float closestSqrDistance = (waypoints[0].position - worldPosition).sqrMagnitude;

        for (int i = 1; i < WaypointCount; i++)
        {
            float sqrDistance = (waypoints[i].position - worldPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public Transform GetClosestWaypoint(Vector3 worldPosition)
    {
        int index = GetClosestWaypointIndex(worldPosition);
        return index >= 0 ? waypoints[index] : null;
    }

    public int GetNextWaypointIndex(int currentIndex)
    {
        if (WaypointCount == 0)
        {
            return -1;
        }

        if (WaypointCount == 1)
        {
            return 0;
        }

        int clampedIndex = Mathf.Clamp(currentIndex, 0, WaypointCount - 1);
        int nextIndex = clampedIndex + 1;
        return nextIndex >= WaypointCount ? 0 : nextIndex;
    }
}
