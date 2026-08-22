using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    public void StartShowcase()
    {
        SceneManager.LoadScene("ShowcaseScene");
    }

}
