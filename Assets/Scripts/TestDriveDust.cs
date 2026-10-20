using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestDriveDust : MonoBehaviour
{
    [SerializeField] private Material dustMaterial;
    [SerializeField, Min(0f)] private float minimumSpeed = 0.8f;
    [SerializeField, Min(0f)] private float maximumParticlesPerWheel = 24f;

    private WheelCollider[] wheels;
    private ParticleSystem[] clouds;
    private float[] emissionRemainders;
    private bool[] wetClouds;
    private Rigidbody vehicleBody;
    private TestDriveMovement movement;

    private void Awake()
    {
        vehicleBody = GetComponent<Rigidbody>();
        movement = GetComponent<TestDriveMovement>();
        wheels = GetComponentsInChildren<WheelCollider>();
        clouds = new ParticleSystem[wheels.Length];
        emissionRemainders = new float[wheels.Length];
        wetClouds = new bool[wheels.Length];
        if (dustMaterial == null) return;

        for (int i = 0; i < wheels.Length; i++)
        {
            var cloud = new GameObject(wheels[i].name + " Dust");
            cloud.transform.SetParent(transform, false);
            var particles = cloud.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.65f, 1.15f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.015f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.45f, 1f, 1.8f));
            var renderer = cloud.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = dustMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            clouds[i] = particles;
        }
    }

    private void FixedUpdate()
    {
        if (dustMaterial == null || movement == null || !movement.enabled) return;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (!wheels[i].GetGroundHit(out WheelHit hit) ||
                !hit.collider.TryGetComponent<TestDriveSurface>(out var surface))
            {
                emissionRemainders[i] = 0f;
                continue;
            }
            bool wet = surface.SurfaceKind != TestDriveSurface.Kind.Dry;
            if (wetClouds[i] != wet)
            {
                // Existing airborne particles must not switch from rising dust to falling droplets.
                clouds[i].Clear();
                emissionRemainders[i] = 0f;
                var main = clouds[i].main;
                main.gravityModifier = wet ? 0.9f : -0.015f;
                var size = clouds[i].sizeOverLifetime;
                size.enabled = !wet;
                wetClouds[i] = wet;
            }
            Vector3 velocity = vehicleBody.GetPointVelocity(hit.point);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, hit.normal);
            float speed = planarVelocity.magnitude;
            float slip = Mathf.Max(Mathf.Abs(hit.forwardSlip), Mathf.Abs(hit.sidewaysSlip));
            float speedDust = Mathf.InverseLerp(minimumSpeed, 14f, speed);
            // Wheelspin can throw dust while the body is moving only slowly.
            float spinSpeed = Mathf.Abs(wheels[i].rpm) * (Mathf.PI * 2f / 60f) * wheels[i].radius;
            float slipDust = Mathf.InverseLerp(0.2f, 1f, slip) * Mathf.InverseLerp(1f, 6f, Mathf.Max(speed, spinSpeed));
            float intensity = Mathf.Clamp01(speedDust + slipDust * 0.65f);
            emissionRemainders[i] += maximumParticlesPerWheel * intensity * Time.fixedDeltaTime;
            int count = Mathf.FloorToInt(emissionRemainders[i]);
            emissionRemainders[i] -= count;
            float scale = Mathf.Clamp(wheels[i].radius / 0.4f, 0.8f, 1.6f);
            for (int p = 0; p < count; p++)
            {
                ParticleSystem.EmitParams emit;
                if (wet)
                {
                    Vector3 travelDirection = speed > 0.001f ? planarVelocity / speed :
                        Vector3.ProjectOnPlane(wheels[i].transform.forward, hit.normal).normalized;
                    Vector3 sideDirection = Vector3.Cross(hit.normal, travelDirection);
                    float height = surface.SurfaceKind == TestDriveSurface.Kind.Water ? 0.3f : 0.08f;
                    Vector3 emissionPoint = hit.point + hit.normal * height;
                    emissionPoint.y = Mathf.Max(emissionPoint.y, surface.VisibleWetSurfaceHeight + 0.025f);
                    emit = new ParticleSystem.EmitParams
                    {
                        position = emissionPoint +
                            Vector3.ProjectOnPlane(Random.insideUnitSphere, hit.normal) * 0.06f,
                        velocity = planarVelocity * 0.18f + sideDirection * Random.Range(-1.2f, 1.2f) +
                            hit.normal * Random.Range(0.9f, 2.1f),
                        startColor = surface.DustColor,
                        startLifetime = Random.Range(0.25f, 0.55f),
                        startSize = Random.Range(0.08f, 0.2f) * scale
                    };
                }
                else
                {
                    emit = new ParticleSystem.EmitParams
                    {
                        position = hit.point + hit.normal * 0.12f + Random.insideUnitSphere * 0.12f,
                        velocity = velocity * 0.12f + hit.normal * Random.Range(0.3f, 0.75f) + Random.insideUnitSphere * 0.3f,
                        startColor = surface.DustColor,
                        startSize = Random.Range(0.65f, 1.15f) * scale * Mathf.Lerp(0.7f, 1.2f, intensity)
                    };
                }
                clouds[i].Emit(emit, 1);
            }
        }
    }

    public void ClearDust()
    {
        if (clouds == null) return;
        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] != null) clouds[i].Clear();
            emissionRemainders[i] = 0f;
        }
    }

    private void OnDisable() => ClearDust();
}
