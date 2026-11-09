using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the in-level ESC pause/options menu.
/// Can automatically generate the PauseMenuPanel UI hierarchy under the scene Canvas
/// if none is assigned, allowing the designer to copy the generated object in Play Mode
/// (or generate it in Edit Mode) and customize it in the Hierarchy.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("-- UI Panel & Button References --")]
    [Tooltip("Root GameObject of the ESC pause menu panel inside the Canvas. Auto-generated if left empty.")]
    public GameObject pauseMenuPanel;

    [Tooltip("Button to close the pause menu and resume gameplay.")]
    public Button resumeButton;

    [Tooltip("Button to restart the current level.")]
    public Button restartButton;

    [Tooltip("Button to return to the Level Select screen in Level_0.")]
    public Button levelSelectButton;

    [Header("-- Optional Sprites for Auto-Generation --")]
    public Sprite windowBoardSprite;
    public Sprite buttonFrameSprite;

    [Header("-- Optional Component References --")]
    [Tooltip("Reference to VictoryPanel so ESC is ignored after clearing the level (auto-found if left empty).")]
    public GameObject victoryPanel;

    [Tooltip("Reference to DrawManager to disable drawing while the pause menu is open (auto-found if left empty).")]
    public DrawManager drawManager;

    private bool isPaused = false;

    void Start()
    {
        if (drawManager == null)
        {
            drawManager = GetComponent<DrawManager>();
            if (drawManager == null) drawManager = FindAnyObjectByType<DrawManager>();
        }

        if (victoryPanel == null)
        {
            GameFlowManager flow = GetComponent<GameFlowManager>();
            if (flow == null) flow = FindAnyObjectByType<GameFlowManager>();
            if (flow != null) victoryPanel = flow.victoryPanel;
        }

        // If no pauseMenuPanel is assigned in the scene, check if one already exists under Canvas or generate it
        if (pauseMenuPanel == null)
        {
            Canvas canvas = FindTargetCanvas();
            if (canvas != null)
            {
                Transform existing = canvas.transform.Find("PauseMenuPanel");
                if (existing != null)
                {
                    pauseMenuPanel = existing.gameObject;
                }
            }
        }

        if (pauseMenuPanel == null)
        {
            GeneratePauseMenuPanel();
        }
        else
        {
            AutoResolveAndBindButtons();
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        isPaused = false;
    }

    void Update()
    {
        // Ignore ESC if the victory screen is already active
        if (victoryPanel != null && victoryPanel.activeInHierarchy)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    /// <summary>
    /// Automatically finds buttons inside pauseMenuPanel if not explicitly assigned and binds click events.
    /// This ensures a PauseMenuPanel copied from Play Mode and pasted into Edit Mode works out of the box.
    /// </summary>
    public void AutoResolveAndBindButtons()
    {
        if (pauseMenuPanel == null) return;

        if (resumeButton == null)
        {
            Transform t = pauseMenuPanel.transform.Find("Window/ResumeButton");
            if (t == null) t = pauseMenuPanel.transform.Find("ResumeButton");
            if (t != null) resumeButton = t.GetComponent<Button>();
        }

        if (restartButton == null)
        {
            Transform t = pauseMenuPanel.transform.Find("Window/RestartButton");
            if (t == null) t = pauseMenuPanel.transform.Find("RestartButton");
            if (t != null) restartButton = t.GetComponent<Button>();
        }

        if (levelSelectButton == null)
        {
            Transform t = pauseMenuPanel.transform.Find("Window/LevelSelectButton");
            if (t == null) t = pauseMenuPanel.transform.Find("LevelSelectButton");
            if (t != null) levelSelectButton = t.GetComponent<Button>();
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(ReturnToLevelSelect);
        }
    }

    /// <summary>
    /// Procedurally constructs the PauseMenuPanel hierarchy inside the main Canvas.
    /// Can be called at runtime or from the Inspector context menu in Edit Mode.
    /// </summary>
    [ContextMenu("Generate PauseMenuPanel in Canvas")]
    public GameObject GeneratePauseMenuPanel()
    {
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
        {
            Debug.LogError("[PauseMenuManager] No Canvas found in scene to place PauseMenuPanel!");
            return null;
        }

        // If a custom PauseMenuPanel prefab is registered in LevelDatabase, instantiate it directly
        if (LevelDatabase.Instance != null && LevelDatabase.Instance.pauseMenuPrefab != null)
        {
            pauseMenuPanel = Instantiate(LevelDatabase.Instance.pauseMenuPrefab, targetCanvas.transform, false);
            pauseMenuPanel.name = "PauseMenuPanel";
            pauseMenuPanel.transform.SetAsLastSibling();
            AutoResolveAndBindButtons();
            return pauseMenuPanel;
        }

        // 1. Root Full-Screen Dimmer Overlay
        pauseMenuPanel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pauseMenuPanel.transform.SetParent(targetCanvas.transform, false);
        pauseMenuPanel.transform.SetAsLastSibling();

        RectTransform rootRt = pauseMenuPanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        Image overlayImg = pauseMenuPanel.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);

        // 2. Centered Dialog Window
        GameObject windowObj = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        windowObj.transform.SetParent(pauseMenuPanel.transform, false);

        RectTransform winRt = windowObj.GetComponent<RectTransform>();
        winRt.anchorMin = new Vector2(0.5f, 0.5f);
        winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.anchoredPosition = Vector2.zero;
        winRt.sizeDelta = new Vector2(680f, 600f);

        Image winImg = windowObj.GetComponent<Image>();
        if (windowBoardSprite != null)
        {
            winImg.sprite = windowBoardSprite;
            winImg.color = Color.white;
        }
        else
        {
            winImg.color = new Color(0.97f, 0.95f, 0.90f, 1f);
        }

        // 3. Title Text ("PAUSED")
        CreateText(
            "TitleText",
            windowObj.transform,
            new Vector2(0f, 195f),
            new Vector2(520f, 80f),
            "PAUSED",
            52f);

        // 4. Resume Button
        resumeButton = CreateMenuButton(
            "ResumeButton",
            windowObj.transform,
            new Vector2(0f, 65f),
            new Vector2(440f, 105f),
            "RESUME",
            36f,
            new Color(0.82f, 0.95f, 0.68f, 1f));

        // 5. Restart Button
        restartButton = CreateMenuButton(
            "RestartButton",
            windowObj.transform,
            new Vector2(0f, -65f),
            new Vector2(440f, 105f),
            "RESTART",
            36f,
            new Color(0.95f, 0.92f, 0.78f, 1f));

        // 6. Level Select Button
        levelSelectButton = CreateMenuButton(
            "LevelSelectButton",
            windowObj.transform,
            new Vector2(0f, -195f),
            new Vector2(440f, 105f),
            "LEVEL SELECT",
            36f,
            new Color(0.82f, 0.90f, 0.98f, 1f));

        AutoResolveAndBindButtons();
        return pauseMenuPanel;
    }

    private Canvas FindTargetCanvas()
    {
        GameFlowManager flow = GetComponent<GameFlowManager>();
        if (flow == null) flow = FindAnyObjectByType<GameFlowManager>();
        if (flow != null && flow.previewPanel != null)
        {
            Canvas c = flow.previewPanel.GetComponentInParent<Canvas>();
            if (c != null) return c;
        }
        return FindAnyObjectByType<Canvas>();
    }

    private Button CreateMenuButton(
        string name,
        Transform parent,
        Vector2 anchoredPos,
        Vector2 size,
        string labelText,
        float fontSize,
        Color tintColor)
    {
        GameObject btnObj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(UIButtonSound));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        if (buttonFrameSprite != null)
        {
            img.sprite = buttonFrameSprite;
        }
        img.color = tintColor;

        CreateText("Label", btnObj.transform, Vector2.zero, size, labelText, fontSize);
        return btnObj.GetComponent<Button>();
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchoredPos,
        Vector2 size,
        string content,
        float fontSize)
    {
        GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(parent, false);

        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.22f, 0.20f, 0.18f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    // =====================================================================
    // Pause / Resume / Navigation Actions
    // =====================================================================

    /// <summary>
    /// Toggles the pause menu open or closed.
    /// </summary>
    public void TogglePauseMenu()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Opens the pause menu, freezes game time, and disables line drawing.
    /// </summary>
    public void PauseGame()
    {
        if (pauseMenuPanel == null)
        {
            GeneratePauseMenuPanel();
        }
        if (pauseMenuPanel == null) return;

        isPaused = true;
        pauseMenuPanel.transform.SetAsLastSibling();
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        if (drawManager != null)
        {
            if (drawManager.customPenCursor != null)
            {
                drawManager.customPenCursor.SetActive(false);
            }
            drawManager.enabled = false;
        }

        Cursor.visible = true;
    }

    /// <summary>
    /// Closes the pause menu, restores game time, and re-enables line drawing.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        if (drawManager != null)
        {
            drawManager.enabled = true;
        }
    }

    /// <summary>
    /// Button callback: Restores time scale and returns to the Level Select screen in Level_0.
    /// </summary>
    public void ReturnToLevelSelect()
    {
        isPaused = false;
        LevelController.ReturnToLevelSelect();
    }

    /// <summary>
    /// Button callback: Restarts the current level.
    /// </summary>
    public void RestartLevel()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
