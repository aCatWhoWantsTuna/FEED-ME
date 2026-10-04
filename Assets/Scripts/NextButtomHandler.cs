using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement; 

public class NextButtonHandler : MonoBehaviour
{
    public VictoryUIAnimator animator;
    public string nextSceneName; 
    
    [Header("--button (cant click twice in short time)--")]
    public Button nextButton; 

    public void OnNextButtonClicked()
    {
        if (nextButton != null)
        {
            nextButton.interactable = false; 
        }

        Time.timeScale = 1f; 

        // find canvas
        Canvas uiCanvas = animator.GetComponentInParent<Canvas>();
        
        if (uiCanvas != null)
        {
            // make it independent, live even when it's switching scene
            uiCanvas.transform.SetParent(null);
            DontDestroyOnLoad(uiCanvas.gameObject);
            uiCanvas.sortingOrder = 999; // Top layer
        }

        SceneManager.LoadScene(nextSceneName);

        // fly toward upper region
        StartCoroutine(animator.PlayExitSequence(() => {
            if (uiCanvas != null)
            {
                Destroy(uiCanvas.gameObject);
            }
        }));
    }
}