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

    [Tooltip("Optional in-game MENU button (bottom-left) that also opens/toggles this pause dialog.")]
    public Button menuButton;

    [Tooltip("Button to close the pause menu and resume gameplay ('No :(' / ResumeButton).")]
    public Button resumeButton;

    [Tooltip("Optional button to restart the current level (not used in the 2-button leave dialog).")]
    public Button restartButton;

    [Tooltip("Button to confirm leaving and return to the Level Select screen ('Yup.' / LevelSelectButton).")]
    public Button levelSelectButton;

    [Header("-- Sprites for Leave Confirmation Dialog --")]
    public Sprite windowBoardSprite;
    public Sprite denyButtonSprite;
    public Sprite confirmButtonSprite;
    [HideInInspector] public Sprite buttonFrameSprite;

    [Header("-- Dialog Layout --")]
    [Tooltip("Anchored offset of the centered dialog window (shifted right to center inside the framed play area).")]
    public Vector2 dialogAnchoredOffset = new Vector2(116f, 0f);
    public Vector2 dialogSize = new Vector2(960f, 668f);

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
        // Resolve and bind the bottom-left MENU button on the main Canvas
        if (menuButton == null)
        {
            Canvas canvas = FindTargetCanvas();
            if (canvas != null)
            {
                Transform menuTf = canvas.transform.Find("MenuButton");
                if (menuTf != null) menuButton = menuTf.GetComponent<Button>();
            }
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(TogglePauseMenu);
            menuButton.onClick.AddListener(TogglePauseMenu);
        }

        if (pauseMenuPanel == null) return;

        if (resumeButton == null)
        {
            Transform t = pauseMenuPanel.transform.Find("Window/DenyButton");
            if (t == null) t = pauseMenuPanel.transform.Find("Window/ResumeButton");
            if (t == null) t = pauseMenuPanel.transform.Find("DenyButton");
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
            Transform t = pauseMenuPanel.transform.Find("Window/ConfirmButton");
            if (t == null) t = pauseMenuPanel.transform.Find("Window/LevelSelectButton");
            if (t == null) t = pauseMenuPanel.transform.Find("ConfirmButton");
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

        // 1. Root Full-Screen Blocker Overlay (subtle tint so the level stays visible like the design mockup)
        pauseMenuPanel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pauseMenuPanel.layer = targetCanvas.gameObject.layer;
        pauseMenuPanel.transform.SetParent(targetCanvas.transform, false);
        pauseMenuPanel.transform.SetAsLastSibling();

        RectTransform rootRt = pauseMenuPanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        Image overlayImg = pauseMenuPanel.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.08f);
        overlayImg.raycastTarget = true;

        // 2. Centered Leave Confirmation Window (aligned to the framed play area center)
        GameObject windowObj = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        windowObj.layer = targetCanvas.gameObject.layer;
        windowObj.transform.SetParent(pauseMenuPanel.transform, false);

        RectTransform winRt = windowObj.GetComponent<RectTransform>();
        winRt.anchorMin = new Vector2(0.5f, 0.5f);
        winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.anchoredPosition = dialogAnchoredOffset;
        winRt.sizeDelta = dialogSize;

        Image winImg = windowObj.GetComponent<Image>();
        winImg.raycastTarget = false;
        if (windowBoardSprite != null)
        {
            winImg.sprite = windowBoardSprite;
            winImg.color = Color.white;
            winImg.preserveAspect = true;
        }
        else
        {
            winImg.color = new Color(1f, 1f, 1f, 0.92f);
        }

        // Reset legacy button references before binding the new 2-button layout
        resumeButton = null;
        restartButton = null;
        levelSelectButton = null;

        bool hasCustomArtButtons = denyButtonSprite != null && confirmButtonSprite != null;
        if (hasCustomArtButtons)
        {
            // 3. Deny Button ("No :(") — layered 1264x880 sprite aligned to Window, with exact raycastPadding
            resumeButton = CreateLayeredDialogButton(
                "DenyButton",
                windowObj.transform,
                denyButtonSprite,
                new Vector4(
                    dialogSize.x * 0.212f,
                    dialogSize.y * 0.248f,
                    dialogSize.x * 0.551f,
                    dialogSize.y * 0.625f));

            // 4. Confirm Button ("Yup.") — layered 1264x880 sprite aligned to Window, with exact raycastPadding
            levelSelectButton = CreateLayeredDialogButton(
                "ConfirmButton",
                windowObj.transform,
                confirmButtonSprite,
                new Vector4(
                    dialogSize.x * 0.543f,
                    dialogSize.y * 0.246f,
                    dialogSize.x * 0.220f,
                    dialogSize.y * 0.625f));
        }
        else
        {
            // Fallback text buttons if custom sprites are not assigned
            CreateText(
                "TitleText",
                windowObj.transform,
                new Vector2(0f, 80f),
                new Vector2(680f, 140f),
                "You are about to leave this level.\nAre you sure?",
                40f);

            resumeButton = CreateMenuButton(
                "DenyButton",
                windowObj.transform,
                new Vector2(-145f, -95f),
                new Vector2(230f, 84f),
                "No :(",
                34f,
                new Color(0.88f, 0.84f, 0.85f, 1f));

            levelSelectButton = CreateMenuButton(
                "ConfirmButton",
                windowObj.transform,
                new Vector2(145f, -95f),
                new Vector2(230f, 84f),
                "Yup.",
                34f,
                new Color(0.96f, 0.22f, 0.36f, 1f));
        }

        AutoResolveAndBindButtons();
        return pauseMenuPanel;
    }

    private Button CreateLayeredDialogButton(
        string name,
        Transform parent,
        Sprite sprite,
        Vector4 raycastPadding)
    {
        GameObject btnObj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(UIButtonSound));
        btnObj.layer = parent.gameObject.layer;
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = true;
        img.raycastPadding = raycastPadding;

        Button btn = btnObj.GetComponent<Button>();
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        return btn;
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
