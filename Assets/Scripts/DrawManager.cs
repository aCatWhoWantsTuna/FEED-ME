using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawManager : MonoBehaviour
{
    public GameObject linePrefab;
    
    [Header("--Pen UI Setting--")]
    public Image[] penImages; 
    public Sprite usedPenSprite; 
    
    // ==========================================
    [Header("--Little Pen--")]
    public GameObject customPenCursor; 
    public Vector2 cursorOffset = new Vector2(0, 0); 
    // ==========================================

    private int pensLeft; 
    private GameObject currentLine;
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    private List<Vector2> mousePositions;
    private bool hasStartedDrawingThisStroke = false;

    void Start()
    {
        pensLeft = penImages.Length; 
        
        if (customPenCursor != null)
        {
            Cursor.visible = false; 
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector3 mouseScreenPos = Mouse.current.position.value;
        mouseScreenPos.z = 10f; 
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // ==========================================
        if (customPenCursor != null)
        {
            customPenCursor.transform.position = mouseWorldPos + cursorOffset;
            
            if (pensLeft <= 0 && !hasStartedDrawingThisStroke)
            {
                customPenCursor.SetActive(false); 
                Cursor.visible = true;            // 把系統原本的游標叫回來 
            }
            else
            {
                customPenCursor.SetActive(true);  
                Cursor.visible = false;           
            }
        }
        // ==========================================

        // no pencil and not drawing: 
        if (pensLeft <= 0 && !hasStartedDrawingThisStroke) return;

        // clicked:
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            hasStartedDrawingThisStroke = true;
            CreateLine(mouseWorldPos);
        }
        
        // clicked and moving
        if (Mouse.current.leftButton.isPressed && hasStartedDrawingThisStroke)
        {
            if (mousePositions != null && Vector2.Distance(mouseWorldPos, mousePositions[mousePositions.Count - 1]) > 0.01f)
            {
                UpdateLine(mouseWorldPos);
            }
        }

        // stop clicking:
        if (Mouse.current.leftButton.wasReleasedThisFrame && hasStartedDrawingThisStroke)
        {
            hasStartedDrawingThisStroke = false;
            
            int penIndex = penImages.Length - pensLeft;
            if(penIndex >= 0 && penIndex < penImages.Length)
            {
                penImages[penIndex].sprite = usedPenSprite; 
            }

            pensLeft--; 
            Debug.Log("draw one! left:" + pensLeft);
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