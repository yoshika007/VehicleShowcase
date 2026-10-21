using UnityEngine;

// Dedicated 2D cues keep the challenge audible without interrupting driving or menu audio.
[RequireComponent(typeof(AudioSource))]
public class CleanLapChallengeAudio : MonoBehaviour
{
    [SerializeField] private AudioClip countdownTick;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip finishSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField, Range(.5f, 2f)] private float countdownPitch = 1.5f;
    private AudioSource source;
    private int lastCountdown;

    private void Awake() => source = GetComponent<AudioSource>();

    public void BeginCountdown()
    {
        lastCountdown = 0;
        PlayCountdown(3);
    }
    public void PlayCountdown(int number)
    {
        if (number == lastCountdown) return;
        lastCountdown = number;
        Play(countdownTick, countdownPitch);
    }
    public void PlayStart()
    {
        lastCountdown = 0;
        Play(startSound);
    }
    public void PlayCheckpoint() => Play(countdownTick, 1.8f);
    public void PlayResult(bool success)
    {
        lastCountdown = 0;
        Play(success ? finishSound : failSound);
    }
    public void Stop()
    {
        lastCountdown = 0;
        if (source != null) source.Stop();
    }
    private void Play(AudioClip clip, float pitch = 1f)
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.Stop();
        source.pitch = pitch;
        if (clip != null && isActiveAndEnabled) source.PlayOneShot(clip);
    }
    private void OnDisable() => Stop();
}
