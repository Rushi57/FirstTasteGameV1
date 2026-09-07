using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneChanger : MonoBehaviour
{
   public void GoToMainMenuScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void GoToMapScene()
    {
        SceneManager.LoadScene("MapScene");
    }
}
