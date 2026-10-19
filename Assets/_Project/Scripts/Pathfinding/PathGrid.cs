using System.Collections.Generic;
using UnityEngine;

/// Builds a grid over the level once at start (a cell is "blocked" if a wall is within
/// agentRadius of its centre) and finds paths across it with A*.
/// One PathGrid per scene. Everything solid (walls, crates, desks) must be on the Walls layer.
public class PathGrid : MonoBehaviour
{
    public static PathGrid Instance { get; private set; }

    [Header("Grid area (centred on this object)")]
    [SerializeField] Vector2 gridSize = new Vector2(40f, 24f);
    [SerializeField] float cellSize = 0.5f;

    [Header("Obstacles")]
    [Tooltip("Set slightly larger than the enemy collider radius. Walls are 'inflated' by this much.")]
    [SerializeField] float agentRadius = 0.4f;
    [SerializeField] LayerMask wallMask;

    [SerializeField] bool drawGizmos = true;

    public float AgentRadius => agentRadius;
    public LayerMask WallMask => wallMask;

    int cols, rows;
    Vector2 origin;
    bool[] walkable;

    // A* working memory, allocated once and reused. "Stamps" avoid clearing arrays every search.
    float[] gCost;
    int[] parent;
    int[] visitedStamp;
    int[] closedStamp;
    int searchId;

    struct Node { public float f; public int index; }
    readonly List<Node> open = new List<Node>();
    readonly List<Vector2> smoothBuffer = new List<Vector2>();

    static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };
    static readonly float[] COST = { 1f, 1f, 1f, 1f, 1.41421f, 1.41421f, 1.41421f, 1.41421f };

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }
    void Start() => Build();

    [ContextMenu("Rebuild Grid")]
    public void Build()
    {
        cols = Mathf.Max(1, Mathf.CeilToInt(gridSize.x / cellSize));
        rows = Mathf.Max(1, Mathf.CeilToInt(gridSize.y / cellSize));
        origin = (Vector2)transform.position - new Vector2(cols, rows) * cellSize * 0.5f;

        int total = cols * rows;
        walkable = new bool[total];
        gCost = new float[total];
        parent = new int[total];
        visitedStamp = new int[total];
        closedStamp = new int[total];
        searchId = 0;

        // Doors are solid when closed, but guards must be able to path through them,
        // so switch every door's collider off while scanning.
        foreach (Door door in Door.All) door.SetPathBuildMode(true);
        Physics2D.SyncTransforms(); // make sure colliders are up to date before querying

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
                walkable[y * cols + x] = Physics2D.OverlapCircle(CellCenter(x, y), agentRadius, wallMask) == null;

        foreach (Door door in Door.All) door.SetPathBuildMode(false);
    }

    Vector2 CellCenter(int x, int y) =>
        origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    bool TryWorldToCell(Vector2 p, out int x, out int y)
    {
        x = Mathf.FloorToInt((p.x - origin.x) / cellSize);
        y = Mathf.FloorToInt((p.y - origin.y) / cellSize);
        return x >= 0 && y >= 0 && x < cols && y < rows;
    }

    /// If the cell is blocked, look in growing rings for the nearest walkable one.
    bool SnapToWalkable(ref int x, ref int y)
    {
        if (walkable[y * cols + x]) return true;

        for (int r = 1; r <= 4; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
                    if (!walkable[ny * cols + nx]) continue;
                    x = nx; y = ny;
                    return true;
                }
        return false;
    }

    /// Fills 'result' with world-space points to walk through (start excluded, goal included).
    /// Returns false if no path exists.
    public bool FindPath(Vector2 startPos, Vector2 goalPos, List<Vector2> result)
    {
        result.Clear();
        if (walkable == null) return false;

        if (!TryWorldToCell(startPos, out int sx, out int sy)) return false;
        if (!TryWorldToCell(goalPos, out int gx, out int gy)) return false;

        bool goalWasBlocked = !walkable[gy * cols + gx];
        if (!SnapToWalkable(ref sx, ref sy)) return false;
        if (!SnapToWalkable(ref gx, ref gy)) return false;

        searchId++;
        open.Clear();

        int start = sy * cols + sx;
        int goal = gy * cols + gx;

        visitedStamp[start] = searchId;
        gCost[start] = 0f;
        parent[start] = -1;
        Push(Heuristic(sx, sy, gx, gy), start);

        while (open.Count > 0)
        {
            int current = Pop();
            if (closedStamp[current] == searchId) continue; // stale duplicate entry
            closedStamp[current] = searchId;

            if (current == goal)
            {
                Reconstruct(start, goal, goalPos, goalWasBlocked, result);
                Smooth(startPos, result);
                return true;
            }

            int cx = current % cols;
            int cy = current / cols;

            for (int n = 0; n < 8; n++)
            {
                int nx = cx + DX[n];
                int ny = cy + DY[n];
                if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;

                int ni = ny * cols + nx;
                if (!walkable[ni] || closedStamp[ni] == searchId) continue;

                // No cutting corners: a diagonal step needs both side cells free.
                if (DX[n] != 0 && DY[n] != 0 &&
                    (!walkable[cy * cols + nx] || !walkable[ny * cols + cx])) continue;

                float newG = gCost[current] + COST[n];
                if (visitedStamp[ni] != searchId || newG < gCost[ni])
                {
                    visitedStamp[ni] = searchId;
                    gCost[ni] = newG;
                    parent[ni] = current;
                    Push(newG + Heuristic(nx, ny, gx, gy), ni);
                }
            }
        }
        return false;
    }

    void Reconstruct(int start, int goal, Vector2 goalPos, bool goalWasBlocked, List<Vector2> result)
    {
        for (int c = goal; c != -1 && c != start; c = parent[c])
            result.Add(CellCenter(c % cols, c / cols));
        result.Reverse();

        if (!goalWasBlocked)
        {
            if (result.Count > 0) result[result.Count - 1] = goalPos; // end exactly on the target
            else result.Add(goalPos);
        }
    }

    /// Drops points that are unnecessary: from each anchor, jump to the furthest point that
    /// can be reached in a straight, wall-free line. Removes the staircase look of grid paths.
    void Smooth(Vector2 startPos, List<Vector2> points)
    {
        smoothBuffer.Clear();
        Vector2 anchor = startPos;
        int i = 0;

        while (i < points.Count)
        {
            int j = points.Count - 1;
            while (j > i && !ClearLine(anchor, points[j], agentRadius * 0.9f, wallMask)) j--;

            smoothBuffer.Add(points[j]);
            anchor = points[j];
            i = j + 1;
        }

        points.Clear();
        points.AddRange(smoothBuffer);
    }

    /// True if a circle of 'radius' can travel from 'from' to 'to' without touching a wall.
    public static bool ClearLine(Vector2 from, Vector2 to, float radius, LayerMask mask)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.001f) return true;
        return Physics2D.CircleCast(from, radius, delta / dist, dist, mask).collider == null;
    }

    // Octile distance: the exact cost on an 8-direction grid with no obstacles.
    static float Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);
        return (dx + dy) + (1.41421f - 2f) * Mathf.Min(dx, dy);
    }

    // ---- minimal binary min-heap (the "open list"), ordered by f = g + h ----

    void Push(float f, int index)
    {
        open.Add(new Node { f = f, index = index });
        int i = open.Count - 1;
        while (i > 0)
        {
            int p = (i - 1) / 2;
            if (open[p].f <= open[i].f) break;
            (open[p], open[i]) = (open[i], open[p]);
            i = p;
        }
    }

    int Pop()
    {
        int result = open[0].index;
        int last = open.Count - 1;
        open[0] = open[last];
        open.RemoveAt(last);

        int i = 0;
        int n = open.Count;
        while (true)
        {
            int l = 2 * i + 1, r = l + 1, s = i;
            if (l < n && open[l].f < open[s].f) s = l;
            if (r < n && open[r].f < open[s].f) s = r;
            if (s == i) break;
            (open[s], open[i]) = (open[i], open[s]);
            i = s;
        }
        return result;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, gridSize.y, 0f));

        if (walkable == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
                if (!walkable[y * cols + x])
                    Gizmos.DrawCube(CellCenter(x, y), new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0f));
    }
}
