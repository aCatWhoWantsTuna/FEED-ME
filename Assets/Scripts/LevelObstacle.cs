using UnityEngine;

/// <summary>
/// Attach this component to any extra obstacle or platform in the LevelEditor scene
/// so that LevelEditorManager exports its transform and prefab reference into LevelData.
/// </summary>
public class LevelObstacle : MonoBehaviour
{
    [Tooltip("Reference to the source prefab used to spawn this obstacle at runtime.")]
    public GameObject sourcePrefab;

    [Tooltip("Optional obstacle identifier.")]
    public string obstacleId;
}
