using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(TestDriveMovement))]
public class TestDriveExhaust : MonoBehaviour
{
    [SerializeField] private Transform[] outlets = new Transform[0];
    [SerializeField] private Material exhaustMaterial;

    private Rigidbody body;
    private TestDriveMovement movement;
    private WheelCollider[] wheels;
    private ParticleSystem[] puffs;
    private float emissionRemainder;
    private int lastResetCount;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        movement = GetComponent<TestDriveMovement>();
        wheels = GetComponentsInChildren<WheelCollider>();
        puffs = new ParticleSystem[outlets.Length];
        if (exhaustMaterial == null) return;
        for (int i = 0; i < outlets.Length; i++)
        {
            if (outlets[i] == null) continue;
            var cloud = new GameObject("Exhaust vapor");
            cloud.transform.SetParent(outlets[i], false);
            var particles = cloud.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.55f, .85f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .12f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -.015f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(.4f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .6f, 1f, 3.5f));
            var renderer = cloud.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = exhaustMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            puffs[i] = particles;
        }
    }

    private void OnEnable()
    {
        lastResetCount = movement.ResetCount;
        foreach (var particles in puffs)
            if (particles != null) particles.Play();
    }

    private void Update()
    {
        if (movement.ResetCount != lastResetCount || !movement.isActiveAndEnabled)
        {
            ClearPuffs();
            lastResetCount = movement.ResetCount;
            if (!movement.isActiveAndEnabled) return;
        }
        float torque = 0f;
        foreach (var wheel in wheels) torque = Mathf.Max(torque, Mathf.Abs(wheel.motorTorque));
        float load = Mathf.Clamp01(torque / 600f);
        // Keep idle vapor, then disperse it as airflow builds (fully gone at 30 km/h).
        // Use total speed so reversing fades the effect in the same way.
        float visibility = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(.5f, 30f / 3.6f, body.linearVelocity.magnitude));
        emissionRemainder += Mathf.Lerp(5f, 11f, load) * visibility * Time.deltaTime;
        int count = Mathf.FloorToInt(emissionRemainder);
        emissionRemainder -= count;
        for (int i = 0; i < puffs.Length; i++)
        {
            if (puffs[i] == null || outlets[i] == null) continue;
            for (int j = 0; j < count; j++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = outlets[i].position,
                    velocity = body.GetPointVelocity(outlets[i].position) * .65f
                        + outlets[i].forward * Mathf.Lerp(.45f, .75f, load)
                        + Vector3.up * .12f + Random.insideUnitSphere * .035f,
                    startLifetime = Random.Range(.55f, .85f) * Mathf.Lerp(.4f, 1f, visibility),
                    startColor = new Color(.85f, .87f, .89f, Mathf.Lerp(.2f, .28f, load) * visibility)
                };
                puffs[i].Emit(emit, 1);
            }
        }
    }

    private void ClearPuffs()
    {
        emissionRemainder = 0f;
        if (puffs == null) return;
        foreach (var particles in puffs)
            if (particles != null) particles.Clear();
    }

    private void OnDisable()
    {
        ClearPuffs();
        if (puffs == null) return;
        foreach (var particles in puffs)
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
