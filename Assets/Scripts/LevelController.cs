using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages level selection, progression index, and scene transitions.
/// Supports loading levels dynamically from LevelDatabase into the shared LevelEditor/Gameplay scene.
/// </summary>
public class LevelController : MonoBehaviour
{
    public const string GameplaySceneName = "LevelEditor";
    public const string MainMenuSceneName = "Menu";

    public static int CurrentLevelIndex { get; private set; } = 0;
    public static bool HasActiveLevelRequest { get; private set; } = false;
    public static LevelData ActiveLevelDataOverride { get; private set; } = null;

    /// <summary>
    /// When true, CoverUIManager in Level_0 will open the Level Select panel immediately upon loading.
    /// </summary>
    public static bool OpenLevelSelectOnLoad { get; set; } = false;

    /// <summary>
    /// Returns the LevelData that should be loaded for the current gameplay session.
    /// </summary>
    public static LevelData GetCurrentLevelData()
    {
        if (ActiveLevelDataOverride != null)
        {
            return ActiveLevelDataOverride;
        }

        if (LevelDatabase.Instance != null)
        {
            return LevelDatabase.Instance.GetLevelByIndex(CurrentLevelIndex);
        }

        return null;
    }

    /// <summary>
    /// Returns true if the current level is the last level in the LevelDatabase.
    /// </summary>
    public static bool IsFinalLevel()
    {
        if (HasActiveLevelRequest && LevelDatabase.Instance != null && LevelDatabase.Instance.levels.Count > 0)
        {
            return CurrentLevelIndex >= LevelDatabase.Instance.levels.Count - 1;
        }

        // Fallback for direct legacy scene execution
        return SceneManager.GetActiveScene().name == "Level_1";
    }

    /// <summary>
    /// Returns true if there is another unlocked level after the current level in the LevelDatabase.
    /// </summary>
    public static bool HasNextLevel()
    {
        if (LevelDatabase.Instance != null && LevelDatabase.Instance.levels.Count > 0)
        {
            int nextIndex = CurrentLevelIndex + 1;
            if (nextIndex < LevelDatabase.Instance.levels.Count)
            {
                LevelData nextData = LevelDatabase.Instance.GetLevelByIndex(nextIndex);
                return nextData != null && PlayerProgressManager.IsLevelUnlocked(nextData);
            }
            return false;
        }
        return !IsFinalLevel();
    }

    /// <summary>
    /// Loads a specific level by its index in the LevelDatabase.
    /// </summary>
    public static void LoadLevelByIndex(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Max(0, levelIndex);
        ActiveLevelDataOverride = null;
        HasActiveLevelRequest = true;
        Time.timeScale = 1f;

        LoadGameplayScene();
    }

    /// <summary>
    /// Loads a specific LevelData asset directly into the gameplay scene.
    /// </summary>
    public static void LoadLevelByData(LevelData levelData)
    {
        if (levelData == null) return;

        ActiveLevelDataOverride = levelData;
        if (LevelDatabase.Instance != null)
        {
            int idx = LevelDatabase.Instance.levels.IndexOf(levelData);
            if (idx >= 0)
            {
                CurrentLevelIndex = idx;
            }
        }

        HasActiveLevelRequest = true;
        Time.timeScale = 1f;

        LoadGameplayScene();
    }

    /// <summary>
    /// Advances to the next level in the LevelDatabase.
    /// </summary>
    public static void LoadNextLevel()
    {
        LoadLevelByIndex(CurrentLevelIndex + 1);
    }

    /// <summary>
    /// Loads the Main Menu scene (Level_0) and instructs CoverUIManager to open the Level Select screen directly.
    /// </summary>
    public static void ReturnToLevelSelect()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        OpenLevelSelectOnLoad = true;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    /// <summary>
    /// Instance method for UI Buttons to return to the Level Select screen in Level_0.
    /// </summary>
    public void OnReturnToLevelSelectClicked()
    {
        ReturnToLevelSelect();
    }

    private static void LoadGameplayScene()
    {
        if (Application.CanStreamedLevelBeLoaded(GameplaySceneName))
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
        else
        {
            // Fallback if LevelEditor scene is not yet in Build Settings
            string fallbackScene = $"Level_{CurrentLevelIndex + 1}";
            if (Application.CanStreamedLevelBeLoaded(fallbackScene))
            {
                SceneManager.LoadScene(fallbackScene);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    /// <summary>
    /// Instance method for UI Buttons to load a level by index.
    /// </summary>
    public void SelectLevel(int levelIndex)
    {
        LoadLevelByIndex(levelIndex);
    }

    /// <summary>
    /// Loads a scene by name, or routes level names (e.g., "Level_1") through the LevelDatabase.
    /// </summary>
    public void LoadSpecificScene(string sceneName)
    {
        Time.timeScale = 1f;

        if (LevelDatabase.Instance != null)
        {
            int levelIdx = LevelDatabase.Instance.GetLevelIndexByName(sceneName);
            if (levelIdx >= 0)
            {
                LoadLevelByIndex(levelIdx);
                return;
            }
        }

        SceneManager.LoadScene(sceneName);
    }
}
