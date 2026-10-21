using System;
using UnityEngine;

// Shared countdown, reset guards, and results for the driving challenges.
[DefaultExecutionOrder(100)]
public class TestDriveCleanLapChallenge : MonoBehaviour
{
    public enum Phase { Ready, Countdown, Armed, Running, Finished, Failed }
    public enum Medal { None, Bronze, Silver, Gold }
    [SerializeField] private MeshFilter circuit;
    [SerializeField, Min(0f)] private float startLineHalfWidth = 8.2f;
    [SerializeField] private Transform startLine;
    [SerializeField, Min(1f)] private float goldSeconds = 18f;
    [SerializeField, Min(1f)] private float silverSeconds = 20f;
    [SerializeField, Min(1f)] private float bronzeSeconds = 22f;
    [SerializeField] private TestDriveCleanLapView view;
    [SerializeField] private CleanLapChallengeAudio challengeAudio;
    [Header("Checkpoint Sprint")]
    [SerializeField, Min(1f)] private float sprintStartSeconds = 10f;
    [SerializeField, Min(1f)] private float sprintBonusSeconds = 6f;
    private OffroadTrailCourse trailCourse;
    public bool IsTrail => TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.OffroadTrail;
    private string ReadyMessage => IsTrail ? "Follow the dirt loop through all eight gates." : IsSlalom ? "Weave through six gates. Cone hits add 2 seconds." : IsSprint ? "Reach each gate before time runs out." : "Avoid barriers. No resets.";
    private SlalomCourse slalomCourse;
    public bool IsSlalom => TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.Slalom;
    public float PenaltySeconds => slalomCourse != null ? slalomCourse.PenaltySeconds : 0f;
    public int ConesHit => slalomCourse != null ? slalomCourse.ConesHit : 0;
    public float ScoreSeconds => ElapsedSeconds + (IsSlalom ? PenaltySeconds : 0f);
    private float sprintBudget;
    private SprintCheckpointGates sprintGates;
    public bool IsSprint => TestDriveSelection.SelectedChallenge == TestDriveSelection.ChallengeKind.CheckpointSprint;
    public float SprintStartSeconds => sprintStartSeconds;
    public float SprintBonusSeconds => sprintBonusSeconds;
    public float RemainingSeconds => Mathf.Max(0, sprintBudget - ElapsedSeconds);
    private TestDriveMovement vehicle;
    private Rigidbody body;
    private WheelCollider[] wheels;
    private Vector3[] route;
    private float[] distances;
    private float length, lastDistance, travelled, gateHalfWidth;
    private Vector3 previousPosition;
    private double countdownEnd, startedAt;
    private int resetCount;
    private string saveKey, message;
    private bool briefingOpen;
    private GameObject circuitBarrier;
    public event Action<TestDriveCleanLapChallenge> AttemptEnded;
    public void SetBriefingOpen(bool open) => briefingOpen = open;
    public Phase State { get; private set; }
    public Medal LastMedal { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public float BestSeconds { get; private set; }
    public int CheckpointsPassed { get; private set; }
    public float GoldSeconds => goldSeconds;
    public float SilverSeconds => silverSeconds;
    public float BronzeSeconds => bronzeSeconds;
    public int CountdownNumber => Mathf.Max(1, Mathf.CeilToInt((float)(countdownEnd - Time.timeAsDouble)));
    public string Message => message;
    public bool IsAvailable => vehicle != null && vehicle.isActiveAndEnabled;

    public void ConfigureMode(bool challengeMode, TestDriveMovement target)
    {
        BindVehicle(challengeMode ? target : null);
        if (challengeMode && !IsTrail && circuitBarrier == null)
        {
            circuitBarrier = CleanLapCircuitBarrier.Create(circuit);
            if (circuitBarrier != null) circuitBarrier.transform.SetParent(transform, true);
        }
        if (circuitBarrier != null) circuitBarrier.SetActive(challengeMode && !IsTrail);
        if (view != null) view.gameObject.SetActive(challengeMode);
        gameObject.SetActive(challengeMode);
    }

    public void BindVehicle(TestDriveMovement target)
    {
        if (challengeAudio != null) challengeAudio.Stop();
        if (vehicle != null)
        {
            vehicle.ColliderContact -= ReportBarrierContact;
            vehicle.SetChallengeStopped(false);
            vehicle.SetChallengeInputLocked(false);
        }
        briefingOpen = false;
        vehicle = target;
        State = Phase.Ready;
        LastMedal = Medal.None;
        ElapsedSeconds = 0;
        CheckpointsPassed = 0;
        if (target == null) return;
        target.SetChallengeStopped(true);
        target.SetChallengeInputLocked(true);
        target.ColliderContact += ReportBarrierContact;
        body = target.GetComponent<Rigidbody>();
        wheels = target.GetComponentsInChildren<WheelCollider>();
        var paint = target.GetComponent<VehiclePaint>();
        string id = paint != null && paint.Palette != null ? paint.Palette.Id : target.name.Replace("(Clone)", "");
        saveKey = (IsTrail ? "VehicleShowcase.OffroadTrail.v1." : IsSlalom ? "VehicleShowcase.Slalom.v1." : IsSprint ? "VehicleShowcase.Sprint.v1." : "VehicleShowcase.CleanLap.v1.") + id + ".BestSeconds";
        float saved = PlayerPrefs.GetFloat(saveKey, 0f);
        BestSeconds = saved > 0 && !float.IsInfinity(saved) && !float.IsNaN(saved) ? saved : 0f;
        message = ReadyMessage;
        if (IsTrail)
        {
            trailCourse = FindAnyObjectByType<OffroadTrailCourse>();
            startLine = trailCourse.StartLine;
        }
        CacheRoute();
        if (sprintGates != null) Destroy(sprintGates.gameObject);
        if (IsSprint)
        {
            sprintGates = new GameObject("Sprint Checkpoint Gates").AddComponent<SprintCheckpointGates>();
            sprintGates.transform.SetParent(transform, false);
            var points = new Vector3[4]; var directions = new Vector3[4];
            float startDistance = RouteDistance(startLine.position);
            for (int i = 0; i < 4; i++)
            {
                float d = Mathf.Repeat(startDistance + length * (i + 1) / 4f, length);
                int row = 0;
                while (row < distances.Length - 2 && distances[row + 1] < d) row++;
                points[i] = Vector3.Lerp(route[row], route[row + 1], Mathf.InverseLerp(distances[row], distances[row + 1], d));
                directions[i] = (route[row + 1] - route[row]).normalized;
            }
            points[3] = startLine.position; directions[3] = startLine.forward;
            sprintGates.Initialize(points, directions, gateHalfWidth);
        }
        if (slalomCourse != null) Destroy(slalomCourse.gameObject);
        if (IsSlalom)
        {
            slalomCourse = new GameObject("Slalom Course").AddComponent<SlalomCourse>();
            slalomCourse.transform.SetParent(transform, false);
            slalomCourse.Initialize(this, target, startLine, gateHalfWidth);
        }
        sprintBudget = sprintStartSeconds;
    }
    private void CacheRoute()
    {
        if (route != null) return;
        if (IsTrail)
        {
            route = trailCourse.Route;
            distances = new float[route.Length];
            for (int i = 1; i < route.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            length = distances[distances.Length - 1];
            gateHalfWidth = trailCourse.HalfWidth;
            return;
        }
        var vertices = circuit.sharedMesh.vertices;
        route = new Vector3[vertices.Length / 2];
        distances = new float[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            route[i] = circuit.transform.TransformPoint((vertices[i * 2] + vertices[i * 2 + 1]) * .5f);
            if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(route[i - 1], route[i]);
        }
        length = distances[distances.Length - 1];
        int gateRow = 0;
        float closest = float.PositiveInfinity;
        for (int i = 0; i < route.Length - 1; i++)
        {
            float square = (route[i] - startLine.position).sqrMagnitude;
            if (square < closest) { closest = square; gateRow = i; }
        }
        Vector3 inner = vertices[gateRow * 2], outer = vertices[gateRow * 2 + 1];
        float roadWidth = Vector3.Distance(circuit.transform.TransformPoint(inner), circuit.transform.TransformPoint(outer));
        float roadScale = roadWidth / Vector3.Distance(inner, outer);
        // Include the enclosed runoff without allowing a crossing on the opposite straight.
        gateHalfWidth = Mathf.Max(startLineHalfWidth, roadWidth * .5f + CleanLapCircuitBarrier.GetInsideClearance(roadScale));
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
        return seconds <= goldSeconds ? Medal.Gold : seconds <= silverSeconds ? Medal.Silver : seconds <= bronzeSeconds ? Medal.Bronze : Medal.None;
    }
    public void HandleAction()
    {
        if (!IsAvailable || briefingOpen || State == Phase.Finished || State == Phase.Failed) return;
        if (State == Phase.Countdown || State == Phase.Armed)
        {
            if (challengeAudio != null) challengeAudio.Stop();
            vehicle.SetChallengeStopped(true);
            vehicle.SetChallengeInputLocked(true);
            State = Phase.Ready;
            message = ReadyMessage;
            if (view != null) view.Refresh(this);
            return;
        }
        StartAttempt();
    }
    public void StartAttempt()
    {
        if (!IsAvailable || briefingOpen) return;
        vehicle.SetChallengeStopped(false);
        vehicle.ResetVehicle();
        resetCount = vehicle.ResetCount;
        vehicle.SetChallengeInputLocked(true);
        State = Phase.Countdown;
        countdownEnd = Time.timeAsDouble + 3;
        ElapsedSeconds = travelled = 0;
        CheckpointsPassed = 0;
        LastMedal = Medal.None;
        sprintBudget = sprintStartSeconds;
        if (sprintGates != null) sprintGates.ShowNext(0);
        if (slalomCourse != null) slalomCourse.ResetAttempt();
        if (trailCourse != null) trailCourse.ShowStart();
        message = "Get ready";
        previousPosition = body.position;
        if (view != null) view.Refresh(this);
        if (challengeAudio != null) challengeAudio.BeginCountdown();
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
                if (challengeAudio != null) challengeAudio.PlayStart();
            }
            else if (challengeAudio != null) challengeAudio.PlayCountdown(CountdownNumber);
        }
        if (view != null) view.Refresh(this);
    }
    private void FixedUpdate()
    {
        if (IsAvailable && (State == Phase.Countdown || State == Phase.Armed || State == Phase.Running))
        {
            foreach (var wheel in wheels)
                if (wheel.GetGroundHit(out var hit)) ReportBarrierContact(hit.collider);
        }
        if (State != Phase.Armed && State != Phase.Running) return;
        if (!IsAvailable) { Fail("Attempt ended. Try again."); return; }
        if (vehicle.ResetCount != resetCount) { Fail("Reset used. Try again."); return; }
        Vector3 position = body.position;
        // Reject warps while allowing the maximum normal speed and physics jitter.
        float allowedStep = Mathf.Max(5f, body.linearVelocity.magnitude * Time.fixedDeltaTime * 2f + 1f);
        if (Vector3.Distance(position, previousPosition) > allowedStep) { Fail("Position changed. Try again."); return; }
        float before = Vector3.Dot(previousPosition - startLine.position, startLine.forward);
        float after = Vector3.Dot(position - startLine.position, startLine.forward);
        bool crossed = before < 0f && after >= 0f
            && Mathf.Abs(Vector3.Dot(position - startLine.position, startLine.right)) <= gateHalfWidth;
        float distance = RouteDistance(position);
        if (State == Phase.Armed && crossed)
        {
            State = Phase.Running;
            startedAt = Time.fixedTimeAsDouble;
            if (trailCourse != null) trailCourse.ShowNext(0);
            lastDistance = distance;
            message = ReadyMessage;
        }
        else if (State == Phase.Running)
        {
            float delta = Mathf.DeltaAngle(lastDistance / length * 360f, distance / length * 360f) * length / 360f;
            travelled = Mathf.Max(0f, travelled + delta);
            lastDistance = distance;
            ElapsedSeconds = (float)(Time.fixedTimeAsDouble - startedAt);
            if (IsTrail)
            {
                if (CheckpointsPassed < OffroadTrailCourse.GateCount
                    && travelled >= length * (CheckpointsPassed + 1) / OffroadTrailCourse.GateCount - 3f
                    && trailCourse.Crossed(CheckpointsPassed, previousPosition, position))
                {
                    CheckpointsPassed++;
                    trailCourse.ShowNext(CheckpointsPassed);
                    message = $"Trail gate {CheckpointsPassed}/8";
                    if (challengeAudio != null) challengeAudio.PlayCheckpoint();
                    if (CheckpointsPassed == OffroadTrailCourse.GateCount) Finish();
                }
            }
            else if (IsSlalom)
            {
                if (CheckpointsPassed < SlalomCourse.GateCount && slalomCourse.Crossed(CheckpointsPassed, previousPosition, position))
                {
                    CheckpointsPassed++;
                    slalomCourse.ShowNext(CheckpointsPassed);
                    message = $"Gate {CheckpointsPassed}/6 · +{PenaltySeconds:0}s penalties";
                    if (challengeAudio != null) challengeAudio.PlayCheckpoint();
                }
                if (CheckpointsPassed == SlalomCourse.GateCount && slalomCourse.Crossed(SlalomCourse.GateCount, previousPosition, position)) Finish();
            }
            else if (IsSprint)
            {
                if (RemainingSeconds <= 0f) { Fail("Time ran out. Try again."); return; }
                if (CheckpointsPassed < 3 && travelled >= length * (CheckpointsPassed + 1) / 4f - 3f
                    && sprintGates.Crossed(CheckpointsPassed, previousPosition, position))
                {
                    CheckpointsPassed++;
                    sprintBudget += sprintBonusSeconds;
                    sprintGates.ShowNext(CheckpointsPassed);
                    message = $"Checkpoint {CheckpointsPassed}/4  +{sprintBonusSeconds:0}s";
                    if (challengeAudio != null) challengeAudio.PlayCheckpoint();
                }
                if (CheckpointsPassed == 3 && crossed && travelled >= length - 2f) Finish();
            }
            else
            {
                CheckpointsPassed = Mathf.Clamp(Mathf.FloorToInt(travelled / length * 8f), 0, 7);
                if (crossed && travelled >= length - 2f) Finish();
            }
        }
        previousPosition = position;
    }
    public void ReportSlalomPenalty()
    {
        if (!IsSlalom || State != Phase.Running) return;
        message = $"Cone hit: +2s · {PenaltySeconds:0}s total penalties";
        if (challengeAudio != null) challengeAudio.PlayPenalty();
    }

    private void ReportBarrierContact(Collider surface)
    {
        if (IsTrail || IsSprint || IsSlalom || !isActiveAndEnabled || !IsAvailable || surface == null) return;
        if (State != Phase.Countdown && State != Phase.Armed && State != Phase.Running) return;
        if (surface.GetComponentInParent<CleanLapCircuitBarrier>() != null)
            Fail("Barrier hit. Try again.");
    }

    private void OnDestroy()
    {
        if (vehicle != null) vehicle.ColliderContact -= ReportBarrierContact;
    }

    private void Finish()
    {
        // A completed attempt can award only once; another award requires a retry.
        if (State != Phase.Running) return;
        State = Phase.Finished;
        CheckpointsPassed = IsTrail ? OffroadTrailCourse.GateCount : IsSlalom ? SlalomCourse.GateCount : IsSprint ? 4 : 8;
        if (slalomCourse != null) slalomCourse.ShowNext(SlalomCourse.GateCount + 1);
        if (sprintGates != null) sprintGates.ShowNext(4);
        LastMedal = IsTrail || IsSprint || IsSlalom ? Medal.None : EvaluateMedal(ElapsedSeconds);
        bool improved = BestSeconds <= 0 || ScoreSeconds < BestSeconds;
        if (improved)
        {
            BestSeconds = ScoreSeconds;
            PlayerPrefs.SetFloat(saveKey, BestSeconds);
            PlayerPrefs.Save();
        }
        message = IsTrail ? (improved ? "Trail complete. New best!" : "Trail complete.")
            : IsSlalom ? (improved ? "Slalom complete. New best!" : "Slalom complete.")
            : IsSprint ? (improved ? "Sprint complete. New best!" : "Sprint complete.")
            : improved ? "Clean lap complete. New best!" : "Clean lap complete.";
        EndAttempt();
    }
    private void Fail(string reason)
    {
        if (State == Phase.Finished || State == Phase.Failed) return;
        State = Phase.Failed;
        LastMedal = Medal.None;
        message = reason;
        EndAttempt();
    }
    private void EndAttempt()
    {
        if (vehicle != null)
        {
            if (State == Phase.Finished) vehicle.SetChallengeCoasting();
            else
            {
                vehicle.SetChallengeInputLocked(true);
                vehicle.SetChallengeStopped(true);
            }
        }
        if (view != null) view.Refresh(this);
        if (challengeAudio != null) challengeAudio.PlayResult(State == Phase.Finished);
        AttemptEnded?.Invoke(this);
    }
    private void OnDisable()
    {
        if (challengeAudio != null) challengeAudio.Stop();
        if (circuitBarrier != null) circuitBarrier.SetActive(false);
        if (vehicle != null)
        {
            vehicle.SetChallengeStopped(false);
            vehicle.SetChallengeInputLocked(false);
        }
    }
}
