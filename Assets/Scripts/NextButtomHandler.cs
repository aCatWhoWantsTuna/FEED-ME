using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NextButtonHandler : MonoBehaviour
{
    public VictoryUIAnimator animator;
    public string nextSceneName;

    [Header("-- Buttons (Prevent Double Clicks) --")]
    public Button nextButton;
    public Button levelSelectButton;

    public void OnNextButtonClicked()
    {
        DisableButtons();
        Time.timeScale = 1f;

        // Find parent canvas to persist across the scene transition during the exit animation
        Canvas uiCanvas = PrepareCanvasForTransition();

        // Advance using LevelController if levels are registered in LevelDatabase; return to Level Select after final level
        if (LevelController.IsFinalLevel())
        {
            LevelController.ReturnToLevelSelect();
        }
        else if (LevelDatabase.Instance != null && LevelDatabase.Instance.levels.Count > 0)
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

        PlayExitAndCleanup(uiCanvas);
    }

    /// <summary>
    /// Callback for the Level Select button on VictoryPanel: returns to Level_0 and opens the Level Select screen.
    /// </summary>
    public void OnLevelSelectButtonClicked()
    {
        DisableButtons();
        Time.timeScale = 1f;

        Canvas uiCanvas = PrepareCanvasForTransition();
        LevelController.ReturnToLevelSelect();
        PlayExitAndCleanup(uiCanvas);
    }

    private void DisableButtons()
    {
        if (nextButton != null) nextButton.interactable = false;
        if (levelSelectButton != null) levelSelectButton.interactable = false;
    }

    private Canvas PrepareCanvasForTransition()
    {
        Canvas uiCanvas = animator != null ? animator.GetComponentInParent<Canvas>() : null;
        if (uiCanvas != null)
        {
            // Detach canvas and keep it alive during scene switch
            uiCanvas.transform.SetParent(null);
            DontDestroyOnLoad(uiCanvas.gameObject);
            uiCanvas.sortingOrder = 999; // Render on top layer
        }
        return uiCanvas;
    }

    private void PlayExitAndCleanup(Canvas uiCanvas)
    {
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
        else if (uiCanvas != null)
        {
            Destroy(uiCanvas.gameObject);
        }
    }
}
