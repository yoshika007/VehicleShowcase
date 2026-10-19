using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(TestDriveMovement))]
public class TestDriveAudio : MonoBehaviour
{
    [Header("Loop recordings")]
    [SerializeField] private AudioClip engineLoop;
    [SerializeField] private AudioClip asphaltLoop;
    [SerializeField] private AudioClip dirtLoop;
    [SerializeField] private AudioClip gravelLoop;
    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float engineVolume = 0.48f;
    [SerializeField, Range(0f, 1f)] private float roadVolume = 0.55f;
    [SerializeField, Range(0.5f, 1.5f)] private float engineTone = 1f;

    private Rigidbody body;
    private TestDriveMovement movement;
    private WheelCollider[] wheels;
    private AudioSource engine;
    private AudioSource asphalt;
    private AudioSource dirt;
    private AudioSource gravel;
    private readonly float[] contacts = new float[4];
    private int gear;
    private static readonly float[] GearSpeeds = { 0f, 8f, 16f, 25f, 36f, 50f };

    public float EnginePitch => engine != null ? engine.pitch : 0f;
    public float AsphaltLevel => asphalt != null ? asphalt.volume : 0f;
    public float DirtLevel => dirt != null ? dirt.volume : 0f;
    public float GravelLevel => gravel != null ? gravel.volume : 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        movement = GetComponent<TestDriveMovement>();
        wheels = GetComponentsInChildren<WheelCollider>();
        engine = CreateLoop("Engine sound", engineLoop);
        asphalt = CreateLoop("Asphalt tire sound", asphaltLoop);
        dirt = CreateLoop("Dirt tire sound", dirtLoop);
        gravel = CreateLoop("Gravel tire sound", gravelLoop);
    }

    private AudioSource CreateLoop(string label, AudioClip clip)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        // The chase camera is distant from the vehicle; keep the player's mix stable.
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0f;
        source.priority = 100;
        return source;
    }

    private void OnEnable()
    {
        gear = 0;
        StartLoop(engine);
        StartLoop(asphalt);
        StartLoop(dirt);
        StartLoop(gravel);
    }

    private static void StartLoop(AudioSource source)
    {
        if (source != null && source.clip != null) source.Play();
    }

    private void Update()
    {
        if (movement == null || !movement.enabled)
        {
            Silence();
            return;
        }
        float speed = body.linearVelocity.magnitude;
        float forwardSpeed = Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.forward));
        // Cosmetic gearing only: it has no connection to the physics drivetrain.
        while (gear < 4 && forwardSpeed > GearSpeeds[gear + 1] + 1f) gear++;
        while (gear > 0 && forwardSpeed < GearSpeeds[gear] - 2f) gear--;
        float revs = Mathf.InverseLerp(GearSpeeds[gear], GearSpeeds[gear + 1] + 2f, forwardSpeed);
        float load = 0f;
        float wheelSpeed = 0f;
        float slip = 0f;
        System.Array.Clear(contacts, 0, contacts.Length);
        foreach (var wheel in wheels)
        {
            load = Mathf.Max(load, Mathf.Abs(wheel.motorTorque));
            wheelSpeed = Mathf.Max(wheelSpeed, Mathf.Abs(wheel.rpm) * wheel.radius * Mathf.PI / 30f);
            if (!wheel.GetGroundHit(out var hit)) continue;
            if (hit.collider.TryGetComponent<TestDriveAudioSurface>(out var surface))
                contacts[(int)surface.SurfaceKind] += 1f / Mathf.Max(1, wheels.Length);
            slip = Mathf.Max(slip, Mathf.Abs(hit.forwardSlip), Mathf.Abs(hit.sidewaysSlip));
        }
        float throttle = Mathf.Clamp01(load / 350f);
        float spin = Mathf.InverseLerp(forwardSpeed + 2f, forwardSpeed + 16f, wheelSpeed);
        float targetPitch = engineTone * Mathf.Lerp(0.78f, 2.05f, Mathf.Clamp01(revs * 0.8f + throttle * 0.2f + spin * 0.4f));
        float blend = 1f - Mathf.Exp(-7f * Time.deltaTime);
        engine.pitch = Mathf.Lerp(engine.pitch, targetPitch, blend);
        engine.volume = Mathf.Lerp(engine.volume, engineVolume * Mathf.Lerp(0.38f, 1f, Mathf.Clamp01(revs * 0.5f + throttle * 0.65f)), blend);
        float rolling = Mathf.InverseLerp(0.3f, 25f, speed);
        float loose = Mathf.Clamp01(rolling + Mathf.InverseLerp(0.2f, 1f, slip) * Mathf.InverseLerp(1f, 8f, wheelSpeed) * 0.4f);
        MixRoad(asphalt, rolling * contacts[0] * 0.65f, speed, blend);
        MixRoad(dirt, loose * (contacts[1] + contacts[3] * 0.55f), speed, blend);
        MixRoad(gravel, loose * contacts[2], speed, blend);
    }

    private void MixRoad(AudioSource source, float level, float speed, float blend)
    {
        source.volume = Mathf.Lerp(source.volume, roadVolume * level, blend);
        source.pitch = Mathf.Lerp(0.75f, 1.3f, Mathf.InverseLerp(0f, 30f, speed));
    }

    private void Silence()
    {
        engine.volume = asphalt.volume = dirt.volume = gravel.volume = 0f;
    }

    private void OnDisable()
    {
        if (engine == null) return;
        Silence();
        engine.Stop(); asphalt.Stop(); dirt.Stop(); gravel.Stop();
    }
}
