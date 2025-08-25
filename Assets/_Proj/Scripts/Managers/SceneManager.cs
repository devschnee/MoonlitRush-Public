using UnityEngine;
using UnityEngine.SceneManagement;

// Use it by attaching it to an empty game object.
public class SceneLoader : MonoBehaviour
{
  public static SceneLoader Instance;

    public AudioSource source;
    public AudioClip clip;

  void Awake()
  {
    if(Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject); // Maintain when changing scenes
    }
    else
    {
      Destroy(gameObject); // Duplicate prevention
    }
  }

  // Load by scene name
  public void LoadScene(string sceneName)
  {
    if(!string.IsNullOrEmpty(sceneName))
      SceneManager.LoadScene(sceneName);

    source.PlayOneShot(clip);
  }

  // Load by scene index
  public void LoadScene(int sceneIdx)
  {
    if(sceneIdx >= 0 && sceneIdx < SceneManager.sceneCountInBuildSettings)
      SceneManager.LoadScene(sceneIdx);
        source.PlayOneShot(clip);
    }

  // Reload current scene
  public void ReloadScene()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        source.PlayOneShot(clip);
    }

  public void QuitGame()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }
}
