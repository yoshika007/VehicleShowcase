using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks the authored oval centerline. Road contact plus ordered progress prevents shortcuts.
[DefaultExecutionOrder(100)]
public class TestDriveCleanLapChallenge : MonoBehaviour
{
    public enum Phase { Ready, Countdown, Armed, Running, Finished, Failed }
    public enum Medal { None, Bronze, Silver, Gold }
    [SerializeField] private MeshFilter circuit;
    [SerializeField] private MeshCollider asphalt;
    [SerializeField] private Transform legalCurbs;
    [SerializeField, Min(0f)] private float startLineHalfWidth = 8.2f;
    [SerializeField] private Transform startLine;
    [SerializeField, Min(1f)] private float goldSeconds = 35f;
    [SerializeField, Min(1f)] private float silverSeconds = 50f;
    [SerializeField, Min(0f)] private float offTrackGrace = .25f;
    [SerializeField] private TestDriveCleanLapView view;
    private TestDriveMovement vehicle;
    private Rigidbody body;
    private WheelCollider[] wheels;
    private readonly HashSet<Collider> curbColliders = new HashSet<Collider>();
    private readonly RaycastHit[] centerHits = new RaycastHit[16];
    private Vector3[] route;
    private float[] distances;
    private float length, lastDistance, travelled, offTrackTime;
    private Vector3 previousPosition;
    private double countdownEnd, startedAt;
    private int resetCount;
    private string saveKey, message;
    private bool briefingOpen;
    public void SetBriefingOpen(bool open) => briefingOpen = open;
    public Phase State { get; private set; }
    public Medal LastMedal { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public float BestSeconds { get; private set; }
    public int CheckpointsPassed { get; private set; }
    public float GoldSeconds => goldSeconds;
    public float SilverSeconds => silverSeconds;
    public int CountdownNumber => Mathf.Max(1, Mathf.CeilToInt((float)(countdownEnd - Time.timeAsDouble)));
    public string Message => message;
    public bool IsAvailable => vehicle != null && vehicle.isActiveAndEnabled;

    public void ConfigureMode(bool challengeMode, TestDriveMovement target)
    {
        BindVehicle(challengeMode ? target : null);
        if (view != null) view.gameObject.SetActive(challengeMode);
        gameObject.SetActive(challengeMode);
    }

    public void BindVehicle(TestDriveMovement target)
    {
        if (vehicle != null) vehicle.SetChallengeInputLocked(false);
        vehicle = target;
        State = Phase.Ready;
        LastMedal = Medal.None;
        ElapsedSeconds = 0;
        CheckpointsPassed = 0;
        if (target == null) return;
        body = target.GetComponent<Rigidbody>();
        wheels = target.GetComponentsInChildren<WheelCollider>();
        var paint = target.GetComponent<VehiclePaint>();
        string id = paint != null && paint.Palette != null ? paint.Palette.Id : target.name.Replace("(Clone)", "");
        saveKey = "VehicleShowcase.CleanLap.v1." + id + ".BestSeconds";
        float saved = PlayerPrefs.GetFloat(saveKey, 0f);
        BestSeconds = saved > 0 && !float.IsInfinity(saved) && !float.IsNaN(saved) ? saved : 0f;
        message = "Asphalt + curbs. No resets.";
        CacheRoute();
    }
    private void CacheRoute()
    {
        if (route != null) return;
        if (legalCurbs != null)
            foreach (var collider in legalCurbs.GetComponentsInChildren<Collider>()) curbColliders.Add(collider);
        var vertices = circuit.sharedMesh.vertices;
        route = new Vector3[vertices.Length / 2];
        distances = new float[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            route[i] = circuit.transform.TransformPoint((vertices[i * 2] + vertices[i * 2 + 1]) * .5f);
            if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(route[i - 1], route[i]);
        }
        length = distances[distances.Length - 1];
    }
    private float RouteDistance(Vector3 position)
    {
        float best = float.PositiveInfinity, along = 0f;
        position.y = 0;
        for (int i = 0; i < route.Length - 1; i++)
        {
            Vector3 a = route[i], b = route[i + 1]; a.y = b.y = 0;
            Vector3 segment = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(position - a, segment) / segment.sqrMagnitude);
            float square = (position - (a + segment * t)).sqrMagnitude;
            if (square < best) { best = square; along = Mathf.Lerp(distances[i], distances[i + 1], t); }
        }
        return along;
    }
    public Medal EvaluateMedal(float seconds)
    {
        if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return Medal.None;
        return seconds <= goldSeconds ? Medal.Gold : seconds <= silverSeconds ? Medal.Silver : Medal.Bronze;
    }
    public void HandleAction()
    {
        if (!IsAvailable || briefingOpen) return;
        if (State == Phase.Countdown || State == Phase.Armed)
        {
            vehicle.SetChallengeInputLocked(false);
            State = Phase.Ready;
            message = "Asphalt + curbs. No resets.";
            return;
        }
        StartAttempt();
    }
    public void StartAttempt()
    {
        if (!IsAvailable || briefingOpen) return;
        vehicle.ResetVehicle();
        resetCount = vehicle.ResetCount;
        vehicle.SetChallengeInputLocked(true);
        State = Phase.Countdown;
        countdownEnd = Time.timeAsDouble + 3;
        ElapsedSeconds = travelled = offTrackTime = 0;
        CheckpointsPassed = 0;
        LastMedal = Medal.None;
        message = "Get ready";
        previousPosition = body.position;
        if (view != null) view.Refresh(this);
    }
    private void Update()
    {
        if (State == Phase.Countdown && vehicle != null)
        {
            if (vehicle.ResetCount != resetCount) Fail("Reset used. Try again.");
            else if (Time.timeAsDouble >= countdownEnd)
            {
                vehicle.SetChallengeInputLocked(false);
                State = Phase.Armed;
                previousPosition = body.position;
                message = "Cross START / FINISH to begin.";
            }
        }
        if (view != null) view.Refresh(this);
    }
    private void FixedUpdate()
    {
        if (State != Phase.Armed && State != Phase.Running) return;
        if (!IsAvailable) { Fail("Attempt ended. Try again."); return; }
        if (vehicle.ResetCount != resetCount) { Fail("Reset used. Try again."); return; }
        Vector3 position = body.position;
        // Reject warps while allowing the maximum normal speed and physics jitter.
        float allowedStep = Mathf.Max(5f, body.linearVelocity.magnitude * Time.fixedDeltaTime * 2f + 1f);
        if (Vector3.Distance(position, previousPosition) > allowedStep) { Fail("Position changed. Try again."); return; }
        bool onRoad = IsLegalUnderBody(position);
        foreach (var wheel in wheels)
            if (wheel.GetGroundHit(out var hit) && !IsLegalSurface(hit.collider)) onRoad = false;
        offTrackTime = onRoad ? 0f : offTrackTime + Time.fixedDeltaTime;
        if (offTrackTime >= offTrackGrace) { Fail("Off track. Avoid the runoff."); return; }
        float before = Vector3.Dot(previousPosition - startLine.position, startLine.forward);
        float after = Vector3.Dot(position - startLine.position, startLine.forward);
        bool crossed = before < 0f && after >= 0f && onRoad
            && Mathf.Abs(Vector3.Dot(position - startLine.position, startLine.right)) <= startLineHalfWidth;
        float distance = RouteDistance(position);
        if (State == Phase.Armed && crossed)
        {
            State = Phase.Running;
            startedAt = Time.fixedTimeAsDouble;
            lastDistance = distance;
            message = "Asphalt + curbs. No resets.";
        }
        else if (State == Phase.Running)
        {
            float delta = Mathf.DeltaAngle(lastDistance / length * 360f, distance / length * 360f) * length / 360f;
            travelled = Mathf.Max(0f, travelled + delta);
            lastDistance = distance;
            CheckpointsPassed = Mathf.Clamp(Mathf.FloorToInt(travelled / length * 8f), 0, 7);
            ElapsedSeconds = (float)(Time.fixedTimeAsDouble - startedAt);
            if (crossed && travelled >= length - 2f) Finish();
        }
        previousPosition = position;
    }
    private bool IsLegalSurface(Collider surface) => surface == asphalt || curbColliders.Contains(surface);

    private bool IsLegalUnderBody(Vector3 position)
    {
        var ray = new Ray(position + Vector3.up * 3f, Vector3.down);
        if (asphalt.Raycast(ray, out _, 6f)) return true;
        // Only authored curb colliders count: widening the asphalt bounds would also
        // accept the adjacent runoff. Reuse the hit buffer to avoid per-tick allocations.
        int count = Physics.RaycastNonAlloc(ray, centerHits, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (curbColliders.Contains(centerHits[i].collider)) return true;
        return false;
    }

    private void Finish()
    {
        // A completed attempt can award only once; another award requires START LAP.
        State = Phase.Finished;
        CheckpointsPassed = 8;
        LastMedal = EvaluateMedal(ElapsedSeconds);
        bool improved = BestSeconds <= 0 || ElapsedSeconds < BestSeconds;
        if (improved)
        {
            BestSeconds = ElapsedSeconds;
            PlayerPrefs.SetFloat(saveKey, BestSeconds);
            PlayerPrefs.Save();
        }
        message = improved ? "Clean lap complete. New best!" : "Clean lap complete.";
    }
    private void Fail(string reason)
    {
        if (vehicle != null) vehicle.SetChallengeInputLocked(false);
        State = Phase.Failed;
        LastMedal = Medal.None;
        message = reason;
    }
    private void OnDisable()
    {
        if (vehicle != null) vehicle.SetChallengeInputLocked(false);
    }
}
