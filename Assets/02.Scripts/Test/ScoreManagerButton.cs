using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManagerButton : MonoBehaviour
{
    public void LoadNextScene()
    {
        ScoreManager.Instance?.BeginRun();
        SceneManager.LoadScene("IngameScene");
    }
}
