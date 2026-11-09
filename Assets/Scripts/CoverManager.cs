using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Main Menu in Level_0, including:
/// 1. Main Title Screen (Play button -> Level Select, Dress-Up button -> Dress-Up Panel)
/// 2. Level Select Panel (3x3 level grid, right-side level reward info card, and PLAY button)
/// 3. Dress-Up Panel (placeholder UI with currency counter, character preview, and item grid)
/// </summary>
public class CoverUIManager : MonoBehaviour
{
    [Header("-- Cover UI Root & Title --")]
    public GameObject coverUIRoot;
    public RectTransform titleRect;

    [Header("-- Floating Title Animation --")]
    public float floatSpeed = 2f;
    public float floatHeight = 15f;

    [Header("-- Sub-Panels --")]
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject dressUpPanel;

    [Header("-- Main Menu Buttons --")]
    [Tooltip("Button A: Opens the Level Select panel (uses StartBottom).")]
    public Button openLevelSelectButton;

    [Tooltip("Button B: Opens the Dress-Up panel.")]
    public Button openDressUpButton;

    [Header("-- Level Select UI References --")]
    public Button levelSelectBackButton;
    public Button[] levelSlotButtons = new Button[9];
    public Image[] levelSlotBackgrounds = new Image[9];
    public TextMeshProUGUI[] levelSlotLabels = new TextMeshProUGUI[9];
    public Image infoMonsterPreview;
    public TextMeshProUGUI selectedLevelTitleText;
    public Image availRewardIcon;
    public TextMeshProUGUI availRewardText;
    public Image getRewardIcon;
    public TextMeshProUGUI getRewardText;
    public Button confirmPlayButton;
    public Button nextPageButton;

    [Header("-- Level Slot Colors --")]
    public Color selectedSlotColor = new Color(0.82f, 0.96f, 0.60f, 1f);
    public Color availableSlotColor = Color.white;
    public Color lockedSlotColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);

    [Header("-- Dress-Up UI References (Placeholder) --")]
    public Button dressUpBackButton;
    public Image dressUpCurrencyIcon;
    public TextMeshProUGUI dressUpCurrencyText;
    public Button dressUpResetButton;
    public Button dressUpSaveButton;

    private Vector2 titleStartPos;
    private bool isTitleFloating = true;
    private int selectedLevelIndex = 0;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;

        DisableGameplayObjectsInMainMenuScene();

        if (coverUIRoot != null)
        {
            coverUIRoot.SetActive(true);
        }

        if (titleRect != null)
        {
            titleStartPos = titleRect.anchoredPosition;
        }

        // If sub-panels have not been generated in the Editor yet, build them at runtime
        if (levelSelectPanel == null || dressUpPanel == null)
        {
            Sprite defaultRewardIcon = LevelDatabase.Instance != null ? LevelDatabase.Instance.pinkCandyIcon : null;
            BuildOrBindMainMenuUI(rewardIconSprite: defaultRewardIcon);
        }
        else
        {
            BindButtonEvents();
        }

        if (LevelController.OpenLevelSelectOnLoad)
        {
            LevelController.OpenLevelSelectOnLoad = false;
            ShowLevelSelectPanel();
        }
        else
        {
            ShowMainMenuPanel();
        }
    }

    void Update()
    {
        if (isTitleFloating && titleRect != null && (mainMenuPanel == null || mainMenuPanel.activeInHierarchy))
        {
            float newY = titleStartPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
            titleRect.anchoredPosition = new Vector2(titleStartPos.x, newY);
        }
    }

    /// <summary>
    /// Disables legacy tutorial gameplay objects in Level_0 so Level_0 acts as a dedicated Main Menu scene.
    /// </summary>
    public void DisableGameplayObjectsInMainMenuScene()
    {
        GameFlowManager flow = GetComponent<GameFlowManager>();
        if (flow != null) flow.enabled = false;

        DrawManager draw = GetComponent<DrawManager>();
        if (draw != null)
        {
            if (draw.customPenCursor != null) draw.customPenCursor.SetActive(false);
            draw.enabled = false;
        }

        TutorialSequence tutorial = FindAnyObjectByType<TutorialSequence>();
        if (tutorial != null)
        {
            tutorial.gameObject.SetActive(false);
        }

        Monster[] monsters = FindObjectsByType<Monster>(FindObjectsInactive.Include);
        foreach (Monster m in monsters)
        {
            if (m == null) continue;
            GameObject group = m.transform.parent != null ? m.transform.parent.gameObject : m.gameObject;
            group.SetActive(false);
        }

        string[] sceneObjectsToHide = { "Pipe", "SpawnPoint", "CandyQueue_pllot", "CustomPenCursor", "DeadZone" };
        foreach (string objName in sceneObjectsToHide)
        {
            GameObject go = GameObject.Find(objName);
            if (go != null) go.SetActive(false);
        }

        if (coverUIRoot != null && coverUIRoot.transform.parent != null)
        {
            Transform canvasTransform = coverUIRoot.transform.parent;
            string[] hudElementsToHide = { "Pen1", "Pen2", "Play_Pause", "Reset", "PreviewPanel" };
            foreach (string hudName in hudElementsToHide)
            {
                Transform child = canvasTransform.Find(hudName);
                if (child != null) child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Registers click listeners on all Main Menu, Level Select, and Dress-Up buttons.
    /// </summary>
    public void BindButtonEvents()
    {
        if (openLevelSelectButton != null)
        {
            openLevelSelectButton.onClick.RemoveAllListeners();
            openLevelSelectButton.onClick.AddListener(ShowLevelSelectPanel);
        }

        if (openDressUpButton != null)
        {
            openDressUpButton.onClick.RemoveAllListeners();
            openDressUpButton.onClick.AddListener(ShowDressUpPanel);
        }

        if (levelSelectBackButton != null)
        {
            levelSelectBackButton.onClick.RemoveAllListeners();
            levelSelectBackButton.onClick.AddListener(ShowMainMenuPanel);
        }

        if (dressUpBackButton != null)
        {
            dressUpBackButton.onClick.RemoveAllListeners();
            dressUpBackButton.onClick.AddListener(ShowMainMenuPanel);
        }

        if (confirmPlayButton != null)
        {
            confirmPlayButton.onClick.RemoveAllListeners();
            confirmPlayButton.onClick.AddListener(OnConfirmPlayClicked);
        }

        if (levelSlotButtons != null)
        {
            for (int i = 0; i < levelSlotButtons.Length; i++)
            {
                if (levelSlotButtons[i] == null) continue;
                int capturedIndex = i;
                levelSlotButtons[i].onClick.RemoveAllListeners();
                levelSlotButtons[i].onClick.AddListener(() => OnLevelSlotClicked(capturedIndex));
            }
        }
    }

    // =====================================================================
    // Navigation & Panel Switching
    // =====================================================================

    /// <summary>
    /// Legacy callback name kept so existing StartBottom persistent onClick calls open Level Select.
    /// </summary>
    public void OnStartButtonClicked()
    {
        ShowLevelSelectPanel();
    }

    /// <summary>
    /// Displays the Main Menu title screen and hides sub-panels.
    /// </summary>
    public void ShowMainMenuPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (dressUpPanel != null) dressUpPanel.SetActive(false);
        isTitleFloating = true;
    }

    /// <summary>
    /// Opens the Level Select popup panel and refreshes level reward information.
    /// </summary>
    public void ShowLevelSelectPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (dressUpPanel != null) dressUpPanel.SetActive(false);
        isTitleFloating = false;

        SelectLevelSlot(selectedLevelIndex);
    }

    /// <summary>
    /// Opens the Dress-Up panel and updates the displayed total currency.
    /// </summary>
    public void ShowDressUpPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (dressUpPanel != null) dressUpPanel.SetActive(true);
        isTitleFloating = false;

        if (dressUpCurrencyText != null)
        {
            dressUpCurrencyText.text = PlayerProgressManager.GetTotalCurrency().ToString();
        }
    }

    // =====================================================================
    // Level Selection & Info Card Logic
    // =====================================================================

    public void OnLevelSlotClicked(int slotIndex)
    {
        SelectLevelSlot(slotIndex);
    }

    /// <summary>
    /// Highlights the chosen level slot in the 3x3 grid and populates the right-side info card.
    /// </summary>
    public void SelectLevelSlot(int slotIndex)
    {
        selectedLevelIndex = Mathf.Clamp(slotIndex, 0, 8);
        LevelDatabase db = LevelDatabase.Instance;
        int totalDefinedLevels = (db != null && db.levels != null) ? db.levels.Count : 0;

        for (int i = 0; i < levelSlotButtons.Length; i++)
        {
            bool isImplemented = i < totalDefinedLevels && db.GetLevelByIndex(i) != null;
            if (levelSlotBackgrounds != null && i < levelSlotBackgrounds.Length && levelSlotBackgrounds[i] != null)
            {
                if (i == selectedLevelIndex)
                {
                    levelSlotBackgrounds[i].color = selectedSlotColor;
                }
                else
                {
                    levelSlotBackgrounds[i].color = isImplemented ? availableSlotColor : lockedSlotColor;
                }
            }

            if (levelSlotLabels != null && i < levelSlotLabels.Length && levelSlotLabels[i] != null)
            {
                levelSlotLabels[i].text = (i + 1).ToString();
            }
        }

        LevelData selectedData = (db != null) ? db.GetLevelByIndex(selectedLevelIndex) : null;
        bool canPlay = selectedData != null;

        if (selectedLevelTitleText != null)
        {
            selectedLevelTitleText.text = canPlay
                ? $"LEVEL {selectedLevelIndex + 1}"
                : $"LEVEL {selectedLevelIndex + 1} (LOCKED)";
        }

        int availReward = canPlay ? PlayerProgressManager.GetAvailableReward(selectedData) : 0;
        int earnedReward = canPlay ? PlayerProgressManager.GetEarnedReward(selectedData) : 0;

        if (availRewardText != null)
        {
            availRewardText.text = canPlay ? $"AVAIL:      x {availReward}" : "AVAIL:      x -";
        }

        if (getRewardText != null)
        {
            getRewardText.text = canPlay ? $"GET:        x {earnedReward}" : "GET:        x -";
        }

        if (confirmPlayButton != null)
        {
            confirmPlayButton.interactable = canPlay;
        }
    }

    /// <summary>
    /// Starts gameplay for the currently selected level slot.
    /// </summary>
    public void OnConfirmPlayClicked()
    {
        LevelDatabase db = LevelDatabase.Instance;
        if (db != null && db.GetLevelByIndex(selectedLevelIndex) != null)
        {
            LevelController.LoadLevelByIndex(selectedLevelIndex);
        }
    }

    // =====================================================================
    // Hierarchy Construction Helper (Used by Editor Builder & Runtime Fallback)
    // =====================================================================

    /// <summary>
    /// Constructs or re-binds the MainMenuPanel, LevelSelectPanel, and DressUpPanel inside coverUIRoot.
    /// </summary>
    public void BuildOrBindMainMenuUI(
        Sprite frameSprite = null,
        Sprite boardSprite = null,
        Sprite startBtnSprite = null,
        Sprite monsterCoverSprite = null,
        Sprite arrowSprite = null,
        Sprite resetSprite = null,
        Sprite rewardIconSprite = null)
    {
        if (coverUIRoot == null)
        {
            Debug.LogError("[CoverUIManager] coverUIRoot is not assigned!");
            return;
        }

        RectTransform rootRect = coverUIRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        // Ensure a solid background behind all panels inside CoverUI
        Transform existingBg = coverUIRoot.transform.Find("Background");
        Image rootBgImg = coverUIRoot.GetComponent<Image>();
        if (rootBgImg == null)
        {
            rootBgImg = coverUIRoot.AddComponent<Image>();
        }
        rootBgImg.color = new Color(0.97f, 0.96f, 0.92f, 1f);

        // Extract sprites from existing children if not explicitly passed
        if (existingBg != null && monsterCoverSprite == null)
        {
            Image bgImg = existingBg.GetComponent<Image>();
            if (bgImg != null) monsterCoverSprite = bgImg.sprite;
        }

        Transform existingStart = coverUIRoot.transform.Find("StartBottom");
        if (existingStart != null && startBtnSprite == null)
        {
            Image stImg = existingStart.GetComponent<Image>();
            if (stImg != null) startBtnSprite = stImg.sprite;
        }

        // -----------------------------------------------------------------
        // 1. MainMenuPanel (Title, Monster Preview, Button A: Play, Button B: DressUp)
        // -----------------------------------------------------------------
        Transform mainPanelTrans = coverUIRoot.transform.Find("MainMenuPanel");
        if (mainPanelTrans == null)
        {
            mainMenuPanel = CreateStretchPanel("MainMenuPanel", coverUIRoot.transform);
        }
        else
        {
            mainMenuPanel = mainPanelTrans.gameObject;
        }

        // Move existing Background, Title, and StartBottom into MainMenuPanel if they are still directly under CoverUI
        if (existingBg != null && existingBg.parent == coverUIRoot.transform)
        {
            existingBg.SetParent(mainMenuPanel.transform, true);
        }
        Transform existingTitle = coverUIRoot.transform.Find("Title");
        if (existingTitle != null && existingTitle.parent == coverUIRoot.transform)
        {
            existingTitle.SetParent(mainMenuPanel.transform, true);
            titleRect = existingTitle.GetComponent<RectTransform>();
        }
        if (existingStart != null && existingStart.parent == coverUIRoot.transform)
        {
            existingStart.SetParent(mainMenuPanel.transform, true);
        }

        // Locate StartBottom (Button A - Play) inside MainMenuPanel
        Transform playBtnTrans = mainMenuPanel.transform.Find("StartBottom");
        if (playBtnTrans != null)
        {
            RectTransform playRt = playBtnTrans.GetComponent<RectTransform>();
            playRt.anchoredPosition = new Vector2(120f, -260f);
            openLevelSelectButton = playBtnTrans.GetComponent<Button>();
        }

        // Create or locate Button B (DressUpButton) to the left of Play button
        Transform dressBtnTrans = mainMenuPanel.transform.Find("DressUpButton");
        if (dressBtnTrans == null)
        {
            GameObject dressBtnObj = CreateButtonObject(
                "DressUpButton",
                mainMenuPanel.transform,
                new Vector2(-180f, -260f),
                new Vector2(150f, 120f),
                frameSprite,
                "DRESS\nUP",
                26f);
            openDressUpButton = dressBtnObj.GetComponent<Button>();
        }
        else
        {
            openDressUpButton = dressBtnTrans.GetComponent<Button>();
        }

        // -----------------------------------------------------------------
        // 2. LevelSelectPanel (Back Button, 3x3 Grid, Right Info Card + PLAY)
        // -----------------------------------------------------------------
        Transform levelPanelTrans = coverUIRoot.transform.Find("LevelSelectPanel");
        if (levelPanelTrans != null)
        {
            DestroyImmediateOrRuntime(levelPanelTrans.gameObject);
        }

        levelSelectPanel = CreateStretchPanel("LevelSelectPanel", coverUIRoot.transform);
        Image levelSelectBg = levelSelectPanel.AddComponent<Image>();
        levelSelectBg.color = new Color(0.96f, 0.95f, 0.90f, 1f);

        // Top-Left Back Button (<-)
        GameObject lvlBackObj = CreateButtonObject(
            "BackButton",
            levelSelectPanel.transform,
            new Vector2(-780f, 420f),
            new Vector2(120f, 100f),
            frameSprite,
            "<-",
            44f);
        levelSelectBackButton = lvlBackObj.GetComponent<Button>();

        // 3x3 Level Grid Container (Left / Center)
        GameObject gridObj = new GameObject("LevelGridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObj.transform.SetParent(levelSelectPanel.transform, false);
        RectTransform gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchoredPosition = new Vector2(-240f, -20f);
        gridRt.sizeDelta = new Vector2(760f, 760f);

        GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(210f, 210f);
        grid.spacing = new Vector2(45f, 45f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        levelSlotButtons = new Button[9];
        levelSlotBackgrounds = new Image[9];
        levelSlotLabels = new TextMeshProUGUI[9];

        for (int i = 0; i < 9; i++)
        {
            GameObject slotObj = CreateButtonObject(
                $"LevelSlot_{i + 1}",
                gridObj.transform,
                Vector2.zero,
                new Vector2(210f, 210f),
                frameSprite,
                (i + 1).ToString(),
                64f);

            levelSlotButtons[i] = slotObj.GetComponent<Button>();
            levelSlotBackgrounds[i] = slotObj.GetComponent<Image>();
            levelSlotLabels[i] = slotObj.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Right Info Card
        GameObject infoCardObj = new GameObject("LevelInfoCard", typeof(RectTransform), typeof(Image));
        infoCardObj.transform.SetParent(levelSelectPanel.transform, false);
        RectTransform infoCardRt = infoCardObj.GetComponent<RectTransform>();
        infoCardRt.anchoredPosition = new Vector2(470f, -20f);
        infoCardRt.sizeDelta = new Vector2(520f, 760f);
        Image infoCardImg = infoCardObj.GetComponent<Image>();
        if (boardSprite != null)
        {
            infoCardImg.sprite = boardSprite;
            infoCardImg.type = Image.Type.Simple;
        }
        infoCardImg.color = Color.white;

        // Selected Level Title inside Info Card
        selectedLevelTitleText = CreateTextObject(
            "SelectedLevelTitle",
            infoCardObj.transform,
            new Vector2(0f, 300f),
            new Vector2(440f, 60f),
            "LEVEL 1",
            40f,
            TextAlignmentOptions.Center);

        // Monster Preview inside Info Card
        GameObject previewMonsterObj = new GameObject("MonsterPreview", typeof(RectTransform), typeof(Image));
        previewMonsterObj.transform.SetParent(infoCardObj.transform, false);
        RectTransform pmRt = previewMonsterObj.GetComponent<RectTransform>();
        pmRt.anchoredPosition = new Vector2(0f, 110f);
        pmRt.sizeDelta = new Vector2(420f, 260f);
        infoMonsterPreview = previewMonsterObj.GetComponent<Image>();
        infoMonsterPreview.sprite = monsterCoverSprite;
        infoMonsterPreview.preserveAspect = true;

        // AVAIL and GET Reward Texts + Icons
        availRewardText = CreateTextObject(
            "AvailRewardText",
            infoCardObj.transform,
            new Vector2(0f, -45f),
            new Vector2(420f, 65f),
            "AVAIL:      x 7",
            36f,
            TextAlignmentOptions.Center);

        availRewardIcon = CreateIconObject(
            "AvailRewardIcon",
            availRewardText.transform,
            new Vector2(20f, 0f),
            new Vector2(48f, 48f),
            rewardIconSprite);

        getRewardText = CreateTextObject(
            "GetRewardText",
            infoCardObj.transform,
            new Vector2(0f, -115f),
            new Vector2(420f, 65f),
            "GET:        x 0",
            36f,
            TextAlignmentOptions.Center);

        getRewardIcon = CreateIconObject(
            "GetRewardIcon",
            getRewardText.transform,
            new Vector2(20f, 0f),
            new Vector2(48f, 48f),
            rewardIconSprite);

        // Confirm PLAY Button inside Info Card
        GameObject confirmPlayObj = CreateButtonObject(
            "ConfirmPlayButton",
            infoCardObj.transform,
            new Vector2(0f, -250f),
            startBtnSprite != null ? new Vector2(360f, 360f) : new Vector2(340f, 110f),
            startBtnSprite != null ? startBtnSprite : frameSprite,
            startBtnSprite != null ? "" : "PLAY",
            46f);
        confirmPlayButton = confirmPlayObj.GetComponent<Button>();

        // Next Page Arrow Button on the far right (>)
        GameObject nextPageObj = CreateButtonObject(
            "NextPageButton",
            levelSelectPanel.transform,
            new Vector2(830f, -20f),
            new Vector2(90f, 120f),
            arrowSprite != null ? arrowSprite : frameSprite,
            arrowSprite != null ? "" : ">",
            44f);
        if (arrowSprite != null)
        {
            nextPageObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        nextPageButton = nextPageObj.GetComponent<Button>();

        // -----------------------------------------------------------------
        // 3. DressUpPanel (Back Button, Currency Box, Left Preview + Save/Reset, Right Grid)
        // -----------------------------------------------------------------
        Transform dressPanelTrans = coverUIRoot.transform.Find("DressUpPanel");
        if (dressPanelTrans != null)
        {
            DestroyImmediateOrRuntime(dressPanelTrans.gameObject);
        }

        dressUpPanel = CreateStretchPanel("DressUpPanel", coverUIRoot.transform);
        Image dressUpBg = dressUpPanel.AddComponent<Image>();
        dressUpBg.color = new Color(0.96f, 0.95f, 0.90f, 1f);

        // Top-Left Back Button (<-)
        GameObject dressBackObj = CreateButtonObject(
            "BackButton",
            dressUpPanel.transform,
            new Vector2(-780f, 420f),
            new Vector2(120f, 100f),
            frameSprite,
            "<-",
            44f);
        dressUpBackButton = dressBackObj.GetComponent<Button>();

        // Top-Right Currency Box (Icon + Amount)
        GameObject currencyBoxObj = new GameObject("CurrencyBox", typeof(RectTransform), typeof(Image));
        currencyBoxObj.transform.SetParent(dressUpPanel.transform, false);
        RectTransform cbRt = currencyBoxObj.GetComponent<RectTransform>();
        cbRt.anchoredPosition = new Vector2(620f, 420f);
        cbRt.sizeDelta = new Vector2(340f, 95f);
        Image cbImg = currencyBoxObj.GetComponent<Image>();
        if (frameSprite != null) cbImg.sprite = frameSprite;

        dressUpCurrencyIcon = CreateIconObject(
            "CurrencyIcon",
            currencyBoxObj.transform,
            new Vector2(-95f, 0f),
            new Vector2(56f, 56f),
            rewardIconSprite);

        dressUpCurrencyText = CreateTextObject(
            "CurrencyAmountText",
            currencyBoxObj.transform,
            new Vector2(35f, 0f),
            new Vector2(200f, 80f),
            "0",
            44f,
            TextAlignmentOptions.Center);

        // Left Character Preview Frame
        GameObject charBoxObj = new GameObject("CharacterPreviewBox", typeof(RectTransform), typeof(Image));
        charBoxObj.transform.SetParent(dressUpPanel.transform, false);
        RectTransform charBoxRt = charBoxObj.GetComponent<RectTransform>();
        charBoxRt.anchoredPosition = new Vector2(-430f, 20f);
        charBoxRt.sizeDelta = new Vector2(720f, 620f);
        Image charBoxImg = charBoxObj.GetComponent<Image>();
        if (boardSprite != null) charBoxImg.sprite = boardSprite;

        GameObject charMonsterImgObj = new GameObject("MonstersPreview", typeof(RectTransform), typeof(Image));
        charMonsterImgObj.transform.SetParent(charBoxObj.transform, false);
        RectTransform cmRt = charMonsterImgObj.GetComponent<RectTransform>();
        cmRt.anchoredPosition = Vector2.zero;
        cmRt.sizeDelta = new Vector2(640f, 440f);
        Image cmImg = charMonsterImgObj.GetComponent<Image>();
        cmImg.sprite = monsterCoverSprite;
        cmImg.preserveAspect = true;

        // Bottom-Left Reset & Save Buttons
        GameObject resetBtnObj = CreateButtonObject(
            "ResetOutfitButton",
            dressUpPanel.transform,
            new Vector2(-630f, -380f),
            new Vector2(280f, 110f),
            resetSprite != null ? resetSprite : frameSprite,
            resetSprite != null ? "" : "RESET",
            36f);
        dressUpResetButton = resetBtnObj.GetComponent<Button>();

        GameObject saveBtnObj = CreateButtonObject(
            "SaveOutfitButton",
            dressUpPanel.transform,
            new Vector2(-240f, -380f),
            new Vector2(420f, 110f),
            frameSprite,
            "SAVE",
            42f);
        dressUpSaveButton = saveBtnObj.GetComponent<Button>();

        // Right Outfit Grid Placeholder (2 rows x 4 columns)
        GameObject shopGridObj = new GameObject("ShopItemsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        shopGridObj.transform.SetParent(dressUpPanel.transform, false);
        RectTransform sgRt = shopGridObj.GetComponent<RectTransform>();
        sgRt.anchoredPosition = new Vector2(400f, 40f);
        sgRt.sizeDelta = new Vector2(820f, 540f);

        GridLayoutGroup sg = shopGridObj.GetComponent<GridLayoutGroup>();
        sg.cellSize = new Vector2(175f, 220f);
        sg.spacing = new Vector2(25f, 35f);
        sg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        sg.constraintCount = 4;
        sg.childAlignment = TextAnchor.MiddleCenter;

        int[] samplePrices = { 0, 30, 30, 45, 0, 60, 0, 35 };
        for (int i = 0; i < 8; i++)
        {
            string priceLabel = samplePrices[i] == 0 ? "GET" : $"x {samplePrices[i]}";
            CreateButtonObject(
                $"OutfitSlot_{i + 1}",
                shopGridObj.transform,
                Vector2.zero,
                new Vector2(175f, 220f),
                frameSprite,
                $"ITEM {i + 1}\n({priceLabel})",
                24f);
        }

        // Bottom Category Tabs (4 tabs)
        GameObject tabsObj = new GameObject("CategoryTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabsObj.transform.SetParent(dressUpPanel.transform, false);
        RectTransform tabsRt = tabsObj.GetComponent<RectTransform>();
        tabsRt.anchoredPosition = new Vector2(380f, -380f);
        tabsRt.sizeDelta = new Vector2(760f, 100f);
        HorizontalLayoutGroup hlg = tabsObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        for (int t = 0; t < 4; t++)
        {
            CreateButtonObject(
                $"CategoryTab_{t + 1}",
                tabsObj.transform,
                Vector2.zero,
                new Vector2(160f, 90f),
                frameSprite,
                $"TAB {t + 1}",
                26f);
        }

        BindButtonEvents();
        ShowMainMenuPanel();
    }

    private static GameObject CreateStretchPanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        return panel;
    }

    private static Image CreateIconObject(
        string name,
        Transform parent,
        Vector2 anchoredPos,
        Vector2 size,
        Sprite iconSprite)
    {
        GameObject iconObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(parent, false);

        RectTransform rt = iconObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = iconObj.GetComponent<Image>();
        img.sprite = iconSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static GameObject CreateButtonObject(
        string name,
        Transform parent,
        Vector2 anchoredPos,
        Vector2 size,
        Sprite bgSprite,
        string labelText,
        float fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonSound));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.preserveAspect = false;
        }
        img.color = Color.white;

        if (!string.IsNullOrEmpty(labelText))
        {
            CreateTextObject("Label", btnObj.transform, Vector2.zero, size, labelText, fontSize, TextAlignmentOptions.Center);
        }

        return btnObj;
    }

    private static TextMeshProUGUI CreateTextObject(
        string name,
        Transform parent,
        Vector2 anchoredPos,
        Vector2 size,
        string text,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(parent, false);

        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.22f, 0.20f, 0.18f, 1f);
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void DestroyImmediateOrRuntime(GameObject obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
            return;
        }
#endif
        Destroy(obj);
    }
}
