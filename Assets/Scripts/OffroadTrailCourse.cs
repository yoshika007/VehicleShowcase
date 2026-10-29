using System.Collections.Generic;
using UnityEngine;

// Derive the challenge from the authored main dirt loop without moving scenery.
public class OffroadTrailCourse : MonoBehaviour
{
    public const int GateCount = 8;
    public Vector3[] Route { get; private set; }
    public Transform StartLine { get; private set; }
    public Transform Spawn { get; private set; }
    public float Length { get; private set; }
    public float HalfWidth { get; private set; }
    private float[] distances;
    private CheckpointGates gates;

    public void Initialize()
    {
        var points = new List<Vector3>();
        foreach (string name in new[] { "Dirt Hill 1", "Gravel Corner 1", "Dirt Hill 2", "Gravel Corner 2" })
        {
            var objectOnRoad = GameObject.Find(name);
            var road = objectOnRoad != null ? objectOnRoad.GetComponent<MeshFilter>() : null;
            var vertices = CleanLapCircuitBarrier.SourceMesh(road).vertices;
            if (vertices.Length < 4 || vertices.Length % 2 != 0) throw new System.InvalidOperationException("Invalid trail geometry: " + name);
            for (int row = 0; row < vertices.Length / 2; row++)
            {
                Vector3 a = road.transform.TransformPoint(vertices[row * 2]);
                Vector3 b = road.transform.TransformPoint(vertices[row * 2 + 1]);
                Vector3 center = (a + b) * .5f;
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], center) > .01f) points.Add(center);
                HalfWidth = HalfWidth == 0 ? Vector3.Distance(a, b) * .5f : Mathf.Min(HalfWidth, Vector3.Distance(a, b) * .5f);
            }
        }
        Route = points.ToArray();
        distances = new float[Route.Length];
        for (int i = 1; i < Route.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(Route[i - 1], Route[i]);
        Length = distances[distances.Length - 1];
        StartLine = new GameObject("Trail Start / Finish").transform;
        StartLine.SetParent(transform, false);
        StartLine.SetPositionAndRotation(Route[0], Quaternion.LookRotation(Direction(0)));
        Spawn = new GameObject("Trail Vehicle Spawn").transform;
        Spawn.SetParent(transform, false);
        Spawn.SetPositionAndRotation(Sample(Length - 7f) + Vector3.up, Quaternion.LookRotation(Direction(Length - 7f)));
        var positions = new Vector3[GateCount];
        var forwards = new Vector3[GateCount];
        for (int i = 0; i < GateCount; i++)
        {
            float d = Length * (i + 1) / GateCount;
            positions[i] = Sample(d);
            forwards[i] = Direction(d);
        }
        gates = new GameObject("Trail Checkpoint Gates").AddComponent<CheckpointGates>();
        gates.transform.SetParent(transform, false);
        gates.Initialize(positions, forwards, HalfWidth, "TRAIL");
        gates.ShowStart();
    }
    public Vector3 Sample(float distance)
    {
        float d = Mathf.Repeat(distance, Length);
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
    public void ShowStart() => gates.ShowStart();
    public void ShowNext(int next) => gates.ShowNext(next);
}
