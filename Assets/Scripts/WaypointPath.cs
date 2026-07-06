using UnityEngine;

// Container for an ordered list of waypoints (its children).
// Draws the route in the Scene view so it is easy to edit.
public class WaypointPath : MonoBehaviour
{
    [Tooltip("If on, the car returns to Waypoint 1 after the last one.")]
    [SerializeField] private bool loop = true;
    [SerializeField] private Color gizmoColor = Color.yellow;

    public int Count => transform.childCount;

    public Transform GetWaypoint(int index)
    {
        return transform.GetChild(index);
    }

    public int NextIndex(int current)
    {
        int next = current + 1;
        if (next >= Count) return loop ? 0 : Count - 1;
        return next;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform wp = transform.GetChild(i);
            Gizmos.DrawSphere(wp.position, 0.5f);
            if (i + 1 < transform.childCount)
                Gizmos.DrawLine(wp.position, transform.GetChild(i + 1).position);
            else if (loop && transform.childCount > 1)
                Gizmos.DrawLine(wp.position, transform.GetChild(0).position);
        }
    }
}
