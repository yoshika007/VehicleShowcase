using UnityEngine;

// A point-to-point ascent over the existing truck ridge; terrain stays authored in the scene.
public class HillClimbCourse : MonoBehaviour
{
    public const int GateCount = 3;
    public Vector3[] Route { get; private set; }
    public Transform StartLine { get; private set; }
    public Transform Spawn { get; private set; }
    public float Length { get; private set; }
    public float HalfWidth { get; private set; }
    public float ElevationGain => Route[Route.Length - 1].y - Route[0].y;
    private float[] distances;
    private SprintCheckpointGates gates, startGate;

    public void Initialize()
    {
        var road = GameObject.Find("Truck ridge branch").GetComponent<MeshFilter>();
        var vertices = road.sharedMesh.vertices;
        var centers = new Vector3[vertices.Length / 2];
        int summit = 0;
        for (int row = 0; row < centers.Length; row++)
        {
            Vector3 a = road.transform.TransformPoint(vertices[row * 2]);
            Vector3 b = road.transform.TransformPoint(vertices[row * 2 + 1]);
            centers[row] = (a + b) * .5f;
            if (centers[row].y > centers[summit].y + .01f) summit = row;
            HalfWidth = row == 0 ? Vector3.Distance(a, b) * .5f : Mathf.Min(HalfWidth, Vector3.Distance(a, b) * .5f);
        }
        // Leave a short approach before the timer; finish safely onto the summit plateau.
        summit = Mathf.Min(summit + 6, centers.Length - 1);
        int first = 0;
        float approach = 0;
        while (first < summit - 2 && approach < 6f) { approach += Vector3.Distance(centers[first], centers[first + 1]); first++; }
        Route = new Vector3[summit - first + 1];
        System.Array.Copy(centers, first, Route, 0, Route.Length);
        distances = new float[Route.Length];
        for (int i = 1; i < Route.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(Route[i - 1], Route[i]);
        Length = distances[distances.Length - 1];
        StartLine = new GameObject("Hill Climb Start").transform;
        StartLine.SetParent(transform, false);
        StartLine.SetPositionAndRotation(Route[0], Quaternion.LookRotation(Direction(0)));
        Spawn = new GameObject("Hill Climb Vehicle Spawn").transform;
        Spawn.SetParent(transform, false);
        Vector3 approachDirection = centers[1] - centers[0]; approachDirection.y = 0;
        Spawn.SetPositionAndRotation(centers[0] + Vector3.up, Quaternion.LookRotation(approachDirection));
        var positions = new Vector3[GateCount];
        var forwards = new Vector3[GateCount];
        for (int i = 0; i < GateCount; i++)
        {
            float d = Length * (i + 1) / GateCount;
            positions[i] = Sample(d); forwards[i] = Direction(d);
        }
        gates = new GameObject("Hill Climb Gates").AddComponent<SprintCheckpointGates>();
        gates.transform.SetParent(transform, false);
        gates.Initialize(positions, forwards, HalfWidth, "CLIMB");
        gates.SetLabel(GateCount - 1, "SUMMIT  3 / 3");
        startGate = new GameObject("Hill Climb Start Gate").AddComponent<SprintCheckpointGates>();
        startGate.transform.SetParent(transform, false);
        startGate.Initialize(new[] { StartLine.position }, new[] { StartLine.forward }, HalfWidth);
        startGate.SetLabel(0, "START");
        ShowStart();
    }
    public Vector3 Sample(float distance)
    {
        float d = Mathf.Clamp(distance, 0, Length);
        int row = 0;
        while (row < distances.Length - 2 && distances[row + 1] < d) row++;
        return Vector3.Lerp(Route[row], Route[row + 1], Mathf.InverseLerp(distances[row], distances[row + 1], d));
    }
    private Vector3 Direction(float distance)
    {
        Vector3 direction = Sample(distance + .5f) - Sample(distance - .5f);
        direction.y = 0;
        return direction.normalized;
    }
    public bool Crossed(int gate, Vector3 before, Vector3 after) => gates.Crossed(gate, before, after);
    public void ShowStart() { startGate.gameObject.SetActive(true); startGate.ShowNext(0); gates.ShowNext(-1); }
    public void ShowNext(int next) { startGate.gameObject.SetActive(false); gates.ShowNext(next); }
}
