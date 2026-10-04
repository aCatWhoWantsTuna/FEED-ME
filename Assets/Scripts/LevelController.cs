using UnityEngine;
using UnityEngine.SceneManagement; 

public class LevelController : MonoBehaviour
{
    public void LoadSpecificScene(string sceneName)
    {
        Time.timeScale = 1f; // recovery of time!
        SceneManager.LoadScene(sceneName); 
    }
}

// reminder: 
// adding levels: 
// setting final level in GameFlowManeger, 
// VictoryUIAnimator, 
// copy next button to victorpanel, 
// and build profile adding scene list