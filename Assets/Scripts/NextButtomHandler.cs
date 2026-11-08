using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NextButtonHandler : MonoBehaviour
{
    public VictoryUIAnimator animator;
    public string nextSceneName;

    [Header("-- Button (Prevent Double Clicks) --")]
    public Button nextButton;

    public void OnNextButtonClicked()
    {
        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        Time.timeScale = 1f;

        // Find parent canvas to persist across the scene transition during the exit animation
        Canvas uiCanvas = animator != null ? animator.GetComponentInParent<Canvas>() : null;

        if (uiCanvas != null)
        {
            // Detach canvas and keep it alive during scene switch
            uiCanvas.transform.SetParent(null);
            DontDestroyOnLoad(uiCanvas.gameObject);
            uiCanvas.sortingOrder = 999; // Render on top layer
        }

        // Advance using LevelController if levels are registered in LevelDatabase
        if (LevelDatabase.Instance != null && LevelDatabase.Instance.levels.Count > 0)
        {
            if (LevelController.HasActiveLevelRequest)
            {
                LevelController.LoadNextLevel();
            }
            else if (!string.IsNullOrEmpty(nextSceneName))
            {
                int targetIdx = LevelDatabase.Instance.GetLevelIndexByName(nextSceneName);
                if (targetIdx >= 0)
                {
                    LevelController.LoadLevelByIndex(targetIdx);
                }
                else
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            }
            else
            {
                LevelController.LoadNextLevel();
            }
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }

        // Play exit fly-up animation and destroy the temporary canvas when complete
        if (animator != null)
        {
            StartCoroutine(animator.PlayExitSequence(() =>
            {
                if (uiCanvas != null)
                {
                    Destroy(uiCanvas.gameObject);
                }
            }));
        }
    }
}
