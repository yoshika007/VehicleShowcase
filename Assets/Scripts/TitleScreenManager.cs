using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button enterShowcaseButton;
    [SerializeField] private AudioSource buttonAudio;
    [SerializeField, Min(0f)] private float transitionDelay = 0.22f;

    private bool isTransitioning;

    public void StartShowcase()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        if (enterShowcaseButton != null)
            enterShowcaseButton.interactable = false;
        StartCoroutine(EnterShowcase());
    }

    private IEnumerator EnterShowcase()
    {
        float delay = transitionDelay;
        if (buttonAudio != null && buttonAudio.clip != null)
        {
            buttonAudio.Play();
            // Let the entire sound finish before its scene object is destroyed.
            delay = Mathf.Max(delay, buttonAudio.clip.length / Mathf.Max(0.01f, Mathf.Abs(buttonAudio.pitch)));
        }
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene("ShowcaseScene");
    }
}
