using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central ScriptableObject registry holding all levels and shared level prefabs/sprites.
/// Stored at Assets/Resources/LevelDatabase.asset for runtime access across scenes.
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "FeedMe/Level Database", order = 0)]
public class LevelDatabase : ScriptableObject
{
    private static LevelDatabase _instance;

    /// <summary>
    /// Loads the default LevelDatabase from Resources/LevelDatabase.
    /// </summary>
    public static LevelDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<LevelDatabase>("LevelDatabase");
            }
            return _instance;
        }
    }

    [Header("-- Ordered Level List --")]
    [Tooltip("All playable levels in progression order (Index 0 = Level_1, etc.).")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("-- Monster Group Prefabs --")]
    [Tooltip("Prefab for MonsterGroup_Pink.")]
    public GameObject pinkMonsterGroupPrefab;

    [Tooltip("Prefab for MonsterGroup_Blue.")]
    public GameObject blueMonsterGroupPrefab;

    [Header("-- Candy Prefabs & Icons --")]
    [Tooltip("Prefab for Pink Candy (Tag: RedCandy).")]
    public GameObject pinkCandyPrefab;

    [Tooltip("UI preview icon for Pink Candy.")]
    public Sprite pinkCandyIcon;

    [Tooltip("Prefab for Blue Candy (Tag: BlueCandy).")]
    public GameObject blueCandyPrefab;

    [Tooltip("UI preview icon for Blue Candy.")]
    public Sprite blueCandyIcon;

    [Header("-- Speech Bubble Sprites --")]
    [Tooltip("Speech bubble sprite when placed on the RIGHT side of a monster (tail points left: pic_eat_text_left).")]
    public Sprite bubbleRightOfMonsterSprite;

    [Tooltip("Speech bubble sprite when placed on the LEFT side of a monster (tail points right: pic_eat_text_right).")]
    public Sprite bubbleLeftOfMonsterSprite;

    [Header("-- Shared UI Prefabs --")]
    [Tooltip("Optional custom PauseMenuPanel prefab to instantiate if a scene does not already have one under Canvas.")]
    public GameObject pauseMenuPrefab;

    /// <summary>
    /// Returns the LevelData at the specified index, or null if out of bounds.
    /// </summary>
    public LevelData GetLevelByIndex(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
        {
            return null;
        }
        return levels[index];
    }

    /// <summary>
    /// Finds the index of a level matching the given levelName (case-insensitive), or -1 if not found.
    /// </summary>
    public int GetLevelIndexByName(string levelName)
    {
        if (levels == null || string.IsNullOrEmpty(levelName))
        {
            return -1;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null &&
                (string.Equals(levels[i].levelName, levelName, System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(levels[i].name, levelName, System.StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns the appropriate MonsterGroup prefab for the given MonsterType.
    /// </summary>
    public GameObject GetMonsterGroupPrefab(MonsterType type)
    {
        return type == MonsterType.Blue ? blueMonsterGroupPrefab : pinkMonsterGroupPrefab;
    }

    /// <summary>
    /// Returns the speech bubble sprite corresponding to the given SpeechBubbleDirection.
    /// </summary>
    public Sprite GetSpeechBubbleSprite(SpeechBubbleDirection direction)
    {
        return direction == SpeechBubbleDirection.LeftOfMonster
            ? bubbleLeftOfMonsterSprite
            : bubbleRightOfMonsterSprite;
    }
}
