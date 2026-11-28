using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawManager : MonoBehaviour
{
    public GameObject linePrefab;

    [Header("-- Pen UI Setting --")]
    public Image[] penImages;
    public Sprite usedPenSprite;

    [Header("-- Custom Pen Cursor --")]
    public GameObject customPenCursor;
    public Vector2 cursorOffset = new Vector2(0, 0);

    [Header("-- Drawable Play Area (Normalized Viewport 0~1) --")]
    [Tooltip("Restricts drawing and the custom pen cursor to the framed play area.")]
    public bool restrictToPlayArea = true;

    [Tooltip("Normalized viewport rect (x, y, width, height) matching the interior of frame.png.")]
    public Rect playAreaViewportRect = new Rect(0.152f, 0.024f, 0.817f, 0.956f);

    private int activePenCapacity;
    private int pensLeft;
    private Sprite[] originalPenSprites;
    private GameObject currentLine;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private List<Vector2> mousePositions;
    private bool hasStartedDrawingThisStroke = false;
    private bool isInitialized = false;

    void Awake()
    {
        CacheOriginalPenSprites();
    }

    void Start()
    {
        if (!isInitialized)
        {
            SetMaxPens(GetActivePenCapacity());
        }

        if (customPenCursor != null)
        {
            customPenCursor.SetActive(false);
        }
        Cursor.visible = true;
    }

    void OnDisable()
    {
        if (customPenCursor != null)
        {
            customPenCursor.SetActive(false);
        }
        Cursor.visible = true;
    }

    private void CacheOriginalPenSprites()
    {
        if (originalPenSprites != null || penImages == null) return;
        originalPenSprites = new Sprite[penImages.Length];
        for (int i = 0; i < penImages.Length; i++)
        {
            if (penImages[i] != null)
            {
                originalPenSprites[i] = penImages[i].sprite;
            }
        }
    }

    /// <summary>
    /// Returns the number of active pen slots configured in the scene.
    /// </summary>
    public int GetActivePenCapacity()
    {
        if (activePenCapacity > 0) return activePenCapacity;
        if (penImages == null) return 0;

        int activeCount = 0;
        for (int i = 0; i < penImages.Length; i++)
        {
            if (penImages[i] != null && penImages[i].gameObject.activeSelf)
            {
                activeCount++;
            }
        }
        return activeCount > 0 ? activeCount : penImages.Length;
    }

    /// <summary>
    /// Configures the maximum number of pens available for the current level and updates the UI icons.
    /// </summary>
    public void SetMaxPens(int maxPensCount)
    {
        CacheOriginalPenSprites();
        int maxPossible = penImages != null ? penImages.Length : 0;
        activePenCapacity = Mathf.Clamp(maxPensCount, 0, maxPossible);
        pensLeft = activePenCapacity;
        isInitialized = true;

        if (penImages != null)
        {
            for (int i = 0; i < penImages.Length; i++)
            {
                if (penImages[i] == null) continue;
                bool shouldBeActive = i < activePenCapacity;
                penImages[i].gameObject.SetActive(shouldBeActive);
                if (shouldBeActive && originalPenSprites != null && originalPenSprites[i] != null)
                {
                    penImages[i].sprite = originalPenSprites[i];
                }
            }
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // If game is frozen (e.g., VictoryPanel or PauseMenu), restore system cursor and skip drawing
        if (Time.timeScale <= 0f)
        {
            if (customPenCursor != null) customPenCursor.SetActive(false);
            Cursor.visible = true;
            return;
        }

        Vector2 mouseScreenPos2D = Mouse.current.position.value;
        Vector3 mouseScreenPos = new Vector3(mouseScreenPos2D.x, mouseScreenPos2D.y, Mathf.Abs(cam.transform.position.z));
        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        Vector2 viewportPos = cam.ScreenToViewportPoint(mouseScreenPos);

        bool isInsidePlayArea = !restrictToPlayArea || playAreaViewportRect.Contains(viewportPos);
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Vector2 clampedWorldPos = restrictToPlayArea ? ClampWorldPointToPlayArea(mouseWorldPos, cam) : mouseWorldPos;

        // Update custom pen cursor position and visibility
        if (customPenCursor != null)
        {
            bool canShowPenCursor = hasStartedDrawingThisStroke || (pensLeft > 0 && isInsidePlayArea && !isPointerOverUI);
            if (canShowPenCursor)
            {
                Vector2 cursorWorldPos = hasStartedDrawingThisStroke ? clampedWorldPos : mouseWorldPos;
                customPenCursor.transform.position = cursorWorldPos + cursorOffset;
                customPenCursor.SetActive(true);
                Cursor.visible = false;
            }
            else
            {
                customPenCursor.SetActive(false);
                Cursor.visible = true; // Restore default system cursor outside play area or over UI buttons
            }
        }

        // Stop if no pens are left and not currently finishing a stroke
        if (pensLeft <= 0 && !hasStartedDrawingThisStroke) return;

        // Mouse button pressed: start drawing a new line only inside the framed play area
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isPointerOverUI || !isInsidePlayArea) return;

            hasStartedDrawingThisStroke = true;
            CreateLine(clampedWorldPos);
        }

        // Mouse button held and moving: append clamped points to the active line
        if (Mouse.current.leftButton.isPressed && hasStartedDrawingThisStroke)
        {
            if (mousePositions != null && Vector2.Distance(clampedWorldPos, mousePositions[mousePositions.Count - 1]) > 0.01f)
            {
                UpdateLine(clampedWorldPos);
            }
        }

        // Mouse button released: finish stroke and consume one pen
        if (Mouse.current.leftButton.wasReleasedThisFrame && hasStartedDrawingThisStroke)
        {
            hasStartedDrawingThisStroke = false;

            int penIndex = activePenCapacity - pensLeft;
            if (penImages != null && penIndex >= 0 && penIndex < penImages.Length && penImages[penIndex] != null)
            {
                penImages[penIndex].sprite = usedPenSprite;
            }

            pensLeft--;
            Debug.Log("Pen stroke completed! Pens remaining: " + pensLeft);
        }
    }

    /// <summary>
    /// Computes the world-space rectangle corresponding to playAreaViewportRect for the given camera.
    /// </summary>
    public Rect GetPlayAreaWorldRect(Camera cam = null)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return new Rect(-7.6f, -4.95f, 15.2f, 9.9f);

        float zDist = Mathf.Abs(cam.transform.position.z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(playAreaViewportRect.xMin, playAreaViewportRect.yMin, zDist));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(playAreaViewportRect.xMax, playAreaViewportRect.yMax, zDist));
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }

    /// <summary>
    /// Clamps a world-space point to remain inside the framed play area.
    /// </summary>
    public Vector2 ClampWorldPointToPlayArea(Vector2 worldPos, Camera cam = null)
    {
        Rect worldRect = GetPlayAreaWorldRect(cam);
        return new Vector2(
            Mathf.Clamp(worldPos.x, worldRect.xMin, worldRect.xMax),
            Mathf.Clamp(worldPos.y, worldRect.yMin, worldRect.yMax));
    }

    private void OnDrawGizmosSelected()
    {
        if (!restrictToPlayArea) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Rect worldRect = GetPlayAreaWorldRect(cam);
        Gizmos.color = new Color(0.2f, 0.85f, 0.45f, 0.9f);
        Gizmos.DrawWireCube(worldRect.center, new Vector3(worldRect.width, worldRect.height, 0f));
    }

    void CreateLine(Vector2 startPos)
    {
        currentLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        lineRenderer = currentLine.GetComponent<LineRenderer>();
        edgeCollider = currentLine.GetComponent<EdgeCollider2D>();
        mousePositions = new List<Vector2>();

        mousePositions.Add(startPos);
        mousePositions.Add(startPos);

        lineRenderer.SetPosition(0, mousePositions[0]);
        lineRenderer.SetPosition(1, mousePositions[1]);
        edgeCollider.points = mousePositions.ToArray();
    }

    void UpdateLine(Vector2 newPos)
    {
        mousePositions.Add(newPos);
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, newPos);

        edgeCollider.points = mousePositions.ToArray();
    }
}
