using UnityEngine;

// Drives the car along a WaypointPath: steers toward the next waypoint,
// accelerates on straights and slows down for corners.
public class CarAI : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Drag the TaxiRoute (or any WaypointPath) object here.")]
    [SerializeField] private WaypointPath path;
    [SerializeField] private float waypointReachDistance = 3f;

    [Header("Driving")]
    [SerializeField] private float maxSpeed = 12f;            // metres per second
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float turnSpeed = 90f;           // degrees per second
    [SerializeField, Range(0.1f, 1f)]
    private float corneringSlowdown = 0.5f;  // fraction of max speed in sharp turns

    int currentIndex;
    float currentSpeed;

    void Start()
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"{name}: CarAI has no path assigned, disabling.", this);
            enabled = false;
            return;
        }

        // Begin at the nearest waypoint so the car does not cut across the map.
        float best = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            float d = Vector3.Distance(transform.position, path.GetWaypoint(i).position);
            if (d < best)
            {
                best = d;
                currentIndex = i;
            }
        }
    }

    void Update()
    {
        Transform target = path.GetWaypoint(currentIndex);
        Vector3 to = target.position - transform.position;
        to.y = 0f;

        if (to.magnitude < waypointReachDistance)
        {
            currentIndex = path.NextIndex(currentIndex);
            return;
        }

        Quaternion desired = Quaternion.LookRotation(to);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);

        float angle = Vector3.Angle(transform.forward, to);
        float targetSpeed = maxSpeed * Mathf.Lerp(1f, corneringSlowdown, Mathf.InverseLerp(10f, 90f, angle));
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        transform.position += transform.forward * (currentSpeed * Time.deltaTime);
    }
}
