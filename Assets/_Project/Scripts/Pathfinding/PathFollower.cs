using System.Collections.Generic;
using UnityEngine;

/// Put on an enemy. Given a goal, NextPoint() returns the point to steer toward right now:
///   - the goal itself if a straight line is clear (no pathfinding used), or
///   - the next point on an A* path if a wall is in the way.
/// Paths are recomputed every few tenths of a second, not every frame.
public class PathFollower : MonoBehaviour
{
    [SerializeField] float repathInterval = 0.4f;
    [SerializeField] float waypointReach = 0.25f;
    [Tooltip("Recompute immediately if the goal moves further than this.")]
    [SerializeField] float goalMoveThreshold = 1f;

    readonly List<Vector2> path = new List<Vector2>();
    int index;
    Vector2 pathGoal;
    float nextRepathTime;
    bool hasPath;

    public Vector2 NextPoint(Vector2 goal)
    {
        PathGrid grid = PathGrid.Instance;
        if (grid == null) return goal; // no grid in scene: behave like a plain straight-line chase

        Vector2 pos = transform.position;

        // Straight line is free: just go.
        if (PathGrid.ClearLine(pos, goal, grid.AgentRadius * 0.9f, grid.WallMask))
        {
            hasPath = false;
            return goal;
        }

        bool goalMoved = !hasPath || Vector2.Distance(goal, pathGoal) > goalMoveThreshold;
        if (goalMoved || Time.time >= nextRepathTime)
        {
            // Small random offset so enemies don't all repath on the same frame.
            nextRepathTime = Time.time + repathInterval + Random.Range(0f, 0.1f);
            pathGoal = goal;
            hasPath = grid.FindPath(pos, goal, path);
            index = 0;
        }

        if (!hasPath || path.Count == 0) return goal;

        while (index < path.Count - 1 && Vector2.Distance(pos, path[index]) < waypointReach)
            index++;

        return path[index];
    }

    void OnDrawGizmosSelected()
    {
        if (!hasPath) return;
        Gizmos.color = Color.cyan;
        Vector3 prev = transform.position;
        for (int i = index; i < path.Count; i++)
        {
            Gizmos.DrawLine(prev, path[i]);
            Gizmos.DrawWireSphere(path[i], 0.12f);
            prev = path[i];
        }
    }
}
