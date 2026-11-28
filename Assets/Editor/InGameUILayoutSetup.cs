using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Step 1 setup utility for migrating the in-game Canvas and Main Camera
/// to the new framed layout (left UI sidebar + right framed play area).
/// </summary>
public static class InGameUILayoutSetup
{
    private const string FrameSpritePath = "Assets/Image/inGame/frame.png";
    private const string MenuButtonSpritePath = "Assets/Image/inGame/pic_cover_menu.png";
    private const string CandyQueueSpritePath = "Assets/Image/pic_candy_queue.png";
    private const string EscWindowSpritePath = "Assets/Image/inGame/esc_window.png";
    private const string EscDenySpritePath = "Assets/Image/inGame/esc_deny.png";
    private const string EscConfirmSpritePath = "Assets/Image/inGame/esc_confirm.png";

    // Frame interior normalized bounds measured from Assets/Image/inGame/frame.png (2560x1440):
    // Left border: 378/2560 = 0.1477, Right border: 2491/2560 = 0.9730
    // Top border:   21/1440 = 0.0146, Bottom border: 1413/1440 = 0.9812
    public const float TargetCameraOrthoSize = 5.2f;
    public const float TargetCameraOffsetX = -1.12f;

    [MenuItem("Tools/Level Editor/Step 1 - Apply New In-Game UI Layout")]
    public static void ApplyStep1LayoutInActiveScene()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[InGameUILayoutSetup] No Canvas found in the active scene!");
            return;
        }

        Undo.SetCurrentGroupName("Step 1 - Apply New In-Game UI Layout");
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Add / update FrameOverlay as the bottom-most child of Canvas
        Sprite frameSprite = LoadFirstSpriteAtPath(FrameSpritePath);
        GameObject frameObj = FindOrCreateUIObject(canvas.transform, "FrameOverlay");
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        Undo.RecordObject(frameRt, "Configure FrameOverlay");
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;
        frameRt.localScale = Vector3.one;
        frameObj.transform.SetSiblingIndex(0);

        Image frameImg = GetOrAddComponent<Image>(frameObj);
        Undo.RecordObject(frameImg, "Configure FrameOverlay Image");
        if (frameSprite != null)
        {
            frameImg.sprite = frameSprite;
        }
        else
        {
            Debug.LogWarning($"[InGameUILayoutSetup] Could not load Sprite at {FrameSpritePath}");
        }
        frameImg.color = Color.white;
        frameImg.preserveAspect = false;
        frameImg.raycastTarget = false;

        // 2. Reposition Pen1 & Pen2 into the top of the left sidebar (0..284px on 1920x1080 Canvas)
        RepositionCenteredUI(canvas.transform.Find("Pen1"), new Vector2(-875f, 430f), new Vector3(0.5f, 0.5f, 0.5f));
        RepositionCenteredUI(canvas.transform.Find("Pen2"), new Vector2(-770f, 430f), new Vector3(0.5f, 0.5f, 0.5f));

        // 3. Create CandyQueueTitle on Canvas (replacing legacy world-space "Square")
        Sprite candyQueueSprite = LoadFirstSpriteAtPath(CandyQueueSpritePath);
        GameObject queueTitleObj = FindOrCreateUIObject(canvas.transform, "CandyQueueTitle");
        RectTransform queueTitleRt = queueTitleObj.GetComponent<RectTransform>();
        Undo.RecordObject(queueTitleRt, "Configure CandyQueueTitle");
        queueTitleRt.anchorMin = new Vector2(0f, 1f);
        queueTitleRt.anchorMax = new Vector2(0f, 1f);
        queueTitleRt.pivot = new Vector2(0.5f, 0.5f);
        queueTitleRt.anchoredPosition = new Vector2(142f, -255f);
        queueTitleRt.sizeDelta = new Vector2(270f, 270f);
        queueTitleRt.localScale = Vector3.one;

        Image queueTitleImg = GetOrAddComponent<Image>(queueTitleObj);
        Undo.RecordObject(queueTitleImg, "Configure CandyQueueTitle Image");
        if (candyQueueSprite != null)
        {
            queueTitleImg.sprite = candyQueueSprite;
        }
        queueTitleImg.color = Color.white;
        queueTitleImg.preserveAspect = true;
        queueTitleImg.raycastTarget = false;

        // Disable legacy world-space Candy Queue sprite ("Square") so it doesn't sit behind the frame
        GameObject legacySquare = GameObject.Find("Square");
        if (legacySquare != null && legacySquare.activeSelf)
        {
            Undo.RecordObject(legacySquare, "Disable Legacy World-Space Square");
            legacySquare.SetActive(false);
        }

        // 4. Reposition PreviewPanel right below CandyQueueTitle inside the left sidebar
        Transform previewTf = canvas.transform.Find("PreviewPanel");
        if (previewTf != null)
        {
            RectTransform previewRt = previewTf.GetComponent<RectTransform>();
            if (previewRt != null)
            {
                Undo.RecordObject(previewRt, "Reposition PreviewPanel");
                previewRt.anchorMin = new Vector2(0f, 1f);
                previewRt.anchorMax = new Vector2(0f, 1f);
                previewRt.pivot = new Vector2(0f, 1f);
                previewRt.anchoredPosition = new Vector2(38f, -300f);
                previewRt.localScale = new Vector3(0.88f, 0.88f, 1f);
            }
        }

        // 5. Create bottom-left MenuButton (wired to PauseMenuManager in Step 4, ready in hierarchy now)
        Sprite menuBtnSprite = LoadFirstSpriteAtPath(MenuButtonSpritePath);
        GameObject menuBtnObj = FindOrCreateUIObject(canvas.transform, "MenuButton");
        RectTransform menuBtnRt = menuBtnObj.GetComponent<RectTransform>();
        Undo.RecordObject(menuBtnRt, "Configure MenuButton");
        menuBtnRt.anchorMin = new Vector2(0f, 0f);
        menuBtnRt.anchorMax = new Vector2(0f, 0f);
        menuBtnRt.pivot = new Vector2(0.5f, 0.5f);
        menuBtnRt.anchoredPosition = new Vector2(142f, 82f);
        menuBtnRt.sizeDelta = new Vector2(340f, 340f);
        menuBtnRt.localScale = Vector3.one;

        Image menuBtnImg = GetOrAddComponent<Image>(menuBtnObj);
        Undo.RecordObject(menuBtnImg, "Configure MenuButton Image");
        if (menuBtnSprite != null)
        {
            menuBtnImg.sprite = menuBtnSprite;
        }
        menuBtnImg.color = Color.white;
        menuBtnImg.preserveAspect = true;
        menuBtnImg.raycastTarget = true;

        Button menuBtn = GetOrAddComponent<Button>(menuBtnObj);
        Navigation nav = menuBtn.navigation;
        nav.mode = Navigation.Mode.None;
        menuBtn.navigation = nav;
        GetOrAddComponent<UIButtonSound>(menuBtnObj);

        // 6. Create LevelTitleText inside top-left of the right game frame
        GameObject levelTitleObj = FindOrCreateUIObject(canvas.transform, "LevelTitleText");
        RectTransform levelTitleRt = levelTitleObj.GetComponent<RectTransform>();
        Undo.RecordObject(levelTitleRt, "Configure LevelTitleText");
        levelTitleRt.anchorMin = new Vector2(0f, 1f);
        levelTitleRt.anchorMax = new Vector2(0f, 1f);
        levelTitleRt.pivot = new Vector2(0f, 1f);
        levelTitleRt.anchoredPosition = new Vector2(335f, -55f);
        levelTitleRt.sizeDelta = new Vector2(340f, 80f);
        levelTitleRt.localScale = Vector3.one;

        TextMeshProUGUI levelTitleTmp = GetOrAddComponent<TextMeshProUGUI>(levelTitleObj);
        Undo.RecordObject(levelTitleTmp, "Configure LevelTitleText TMP");
        levelTitleTmp.text = "LEVEL 1";
        levelTitleTmp.fontSize = 46f;
        levelTitleTmp.fontStyle = FontStyles.Bold;
        levelTitleTmp.color = new Color(0.14f, 0.13f, 0.12f, 1f);
        levelTitleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        levelTitleTmp.raycastTarget = false;

        // 7. Adjust Play_Pause & Reset buttons to sit cleanly inside the top-right corner of the frame
        RepositionCenteredUI(canvas.transform.Find("Play_Pause"), new Vector2(675f, 415f), null);
        RepositionCenteredUI(canvas.transform.Find("Reset"), new Vector2(815f, 415f), null);

        // Keep VictoryPanel and PauseMenuPanel on top of all HUD elements
        Transform victoryTf = canvas.transform.Find("VictoryPanel");
        if (victoryTf != null) victoryTf.SetAsLastSibling();
        Transform pauseTf = canvas.transform.Find("PauseMenuPanel");
        if (pauseTf != null) pauseTf.SetAsLastSibling();

        // Wire GameFlowManager.levelTitleText
        GameFlowManager flow = Object.FindAnyObjectByType<GameFlowManager>();
        if (flow != null)
        {
            Undo.RecordObject(flow, "Assign LevelTitleText to GameFlowManager");
            flow.levelTitleText = levelTitleTmp;
            EditorUtility.SetDirty(flow);
        }

        // 8. Shift & slightly zoom out Main Camera so world x=0 centers inside the right frame
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindAnyObjectByType<Camera>();
        if (mainCam != null)
        {
            Undo.RecordObject(mainCam.transform, "Shift Main Camera for Framed Play Area");
            Undo.RecordObject(mainCam, "Adjust Main Camera Orthographic Size");
            Vector3 camPos = mainCam.transform.position;
            mainCam.transform.position = new Vector3(TargetCameraOffsetX, 0f, camPos.z);
            mainCam.orthographicSize = TargetCameraOrthoSize;
        }

        // 9. Pull DeadZone_Left & DeadZone_Right inward to match the new framed world bounds (~[-7.63, +7.63])
        GameObject dzLeft = GameObject.Find("DeadZone_Left");
        if (dzLeft != null)
        {
            Undo.RecordObject(dzLeft.transform, "Update DeadZone_Left Position");
            Vector3 wp = dzLeft.transform.position;
            dzLeft.transform.position = new Vector3(-8.2f, wp.y, wp.z);
        }

        GameObject dzRight = GameObject.Find("DeadZone_Right");
        if (dzRight != null)
        {
            Undo.RecordObject(dzRight.transform, "Update DeadZone_Right Position");
            Vector3 wp = dzRight.transform.position;
            dzRight.transform.position = new Vector3(8.2f, wp.y, wp.z);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("[InGameUILayoutSetup] Step 1 layout applied! Please check Game View (16:9) and Save Scene (Ctrl+S).");
    }

    [MenuItem("Tools/Level Editor/Step 4 - Apply New Pause Menu (Leave Dialog)")]
    public static void ApplyStep4PauseMenuInActiveScene()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[InGameUILayoutSetup] No Canvas found in the active scene!");
            return;
        }

        PauseMenuManager pauseManager = Object.FindAnyObjectByType<PauseMenuManager>();
        if (pauseManager == null)
        {
            GameFlowManager flow = Object.FindAnyObjectByType<GameFlowManager>();
            if (flow != null)
            {
                pauseManager = Undo.AddComponent<PauseMenuManager>(flow.gameObject);
            }
            else
            {
                Debug.LogError("[InGameUILayoutSetup] No PauseMenuManager found in the active scene!");
                return;
            }
        }

        Undo.SetCurrentGroupName("Step 4 - Apply New Pause Menu");
        int undoGroup = Undo.GetCurrentGroup();

        // Remove any existing PauseMenuPanel under Canvas so it is rebuilt cleanly with the new 2-button artwork
        Transform oldPanel = canvas.transform.Find("PauseMenuPanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        Undo.RecordObject(pauseManager, "Configure PauseMenuManager Sprites");
        pauseManager.pauseMenuPanel = null;
        pauseManager.resumeButton = null;
        pauseManager.restartButton = null;
        pauseManager.levelSelectButton = null;
        pauseManager.windowBoardSprite = LoadFirstSpriteAtPath(EscWindowSpritePath);
        pauseManager.denyButtonSprite = LoadFirstSpriteAtPath(EscDenySpritePath);
        pauseManager.confirmButtonSprite = LoadFirstSpriteAtPath(EscConfirmSpritePath);

        // Trim transparent raycast padding on MenuButton so only the visible red pill is clickable
        Transform menuBtnTf = canvas.transform.Find("MenuButton");
        if (menuBtnTf != null)
        {
            pauseManager.menuButton = menuBtnTf.GetComponent<Button>();
            Image menuImg = menuBtnTf.GetComponent<Image>();
            RectTransform menuRt = menuBtnTf.GetComponent<RectTransform>();
            if (menuImg != null && menuRt != null)
            {
                Undo.RecordObject(menuImg, "Trim MenuButton Raycast Padding");
                Vector2 sz = menuRt.sizeDelta;
                menuImg.raycastPadding = new Vector4(sz.x * 0.214f, sz.y * 0.372f, sz.x * 0.223f, sz.y * 0.403f);
            }
        }

        GameObject created = pauseManager.GeneratePauseMenuPanel();
        if (created != null)
        {
            Undo.RegisterCreatedObjectUndo(created, "Create New PauseMenuPanel");
            created.SetActive(false);
        }

        EditorUtility.SetDirty(pauseManager);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("[InGameUILayoutSetup] Step 4 Leave Confirmation Pause Menu built & wired to MenuButton! Save Scene (Ctrl+S).");
    }

    private static Sprite LoadFirstSpriteAtPath(string assetPath)
    {
        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (direct != null) return direct;

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object obj in subAssets)
        {
            if (obj is Sprite sprite)
            {
                return sprite;
            }
        }
        return null;
    }

    private static GameObject FindOrCreateUIObject(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return obj;
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = Undo.AddComponent<T>(obj);
        }
        return comp;
    }

    private static void RepositionCenteredUI(Transform tf, Vector2 anchoredPos, Vector3? scale)
    {
        if (tf == null) return;
        RectTransform rt = tf.GetComponent<RectTransform>();
        if (rt == null) return;

        Undo.RecordObject(rt, "Reposition " + tf.name);
        rt.anchoredPosition = anchoredPos;
        if (scale.HasValue)
        {
            rt.localScale = scale.Value;
        }
    }
}
