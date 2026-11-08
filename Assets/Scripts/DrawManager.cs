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
            SetMaxPens(penImages != null ? penImages.Length : 0);
        }

        if (customPenCursor != null)
        {
            Cursor.visible = false;
        }
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

        Vector3 mouseScreenPos = Mouse.current.position.value;
        mouseScreenPos.z = 10f;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // Update custom pen cursor position and visibility
        if (customPenCursor != null)
        {
            customPenCursor.transform.position = mouseWorldPos + cursorOffset;

            if (pensLeft <= 0 && !hasStartedDrawingThisStroke)
            {
                customPenCursor.SetActive(false);
                Cursor.visible = true; // Restore the default system cursor
            }
            else
            {
                customPenCursor.SetActive(true);
                Cursor.visible = false;
            }
        }

        // Stop if no pens are left and not currently finishing a stroke
        if (pensLeft <= 0 && !hasStartedDrawingThisStroke) return;

        // Mouse button pressed: start drawing a new line
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            hasStartedDrawingThisStroke = true;
            CreateLine(mouseWorldPos);
        }

        // Mouse button held and moving: append points to the active line
        if (Mouse.current.leftButton.isPressed && hasStartedDrawingThisStroke)
        {
            if (mousePositions != null && Vector2.Distance(mouseWorldPos, mousePositions[mousePositions.Count - 1]) > 0.01f)
            {
                UpdateLine(mouseWorldPos);
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
