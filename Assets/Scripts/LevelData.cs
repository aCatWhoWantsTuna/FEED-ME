using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the visual and target color type of a monster.
/// </summary>
public enum MonsterType
{
    Pink = 0,
    Blue = 1
}

/// <summary>
/// Defines which side of the monster the speech bubble is placed on.
/// </summary>
public enum SpeechBubbleDirection
{
    RightOfMonster = 0,
    LeftOfMonster = 1,
    Custom = 2
}

/// <summary>
/// Serializable data representing a single monster group instance in a level.
/// </summary>
[Serializable]
public class MonsterSpawnData
{
    [Tooltip("Monster type (Pink targets RedCandy, Blue targets BlueCandy).")]
    public MonsterType monsterType = MonsterType.Pink;

    [Tooltip("Tag of the candy this monster wants to eat (e.g., RedCandy or BlueCandy).")]
    public string targetColorTag = "RedCandy";

    [Tooltip("Number of candies required to satisfy this monster.")]
    public int targetAmount = 3;

    [Tooltip("World position of the root MonsterGroup transform.")]
    public Vector3 groupPosition;

    [Tooltip("World position of the Monster body itself (used for exact placement regardless of pivot offset).")]
    public Vector3 monsterWorldPosition;

    [Tooltip("Which side of the monster the speech bubble appears on.")]
    public SpeechBubbleDirection speechBubbleDirection = SpeechBubbleDirection.RightOfMonster;

    [Tooltip("World-space offset of the SpeechBubble relative to the Monster body.")]
    public Vector3 speechBubbleOffsetFromMonster = new Vector3(2.08f, 0.74f, 0f);

    [Tooltip("Anchored position of the NumberText inside the SpeechBubble.")]
    public Vector2 numberTextAnchoredPosition = new Vector2(-1.2f, -1.67f);

    [Tooltip("Local position of the CandyIcon inside the SpeechBubble.")]
    public Vector3 candyIconLocalPosition = new Vector3(0.7f, -0.8f, 0f);
}

/// <summary>
/// Serializable data representing an extra obstacle, platform, or tutorial visual prop in a level.
/// </summary>
[Serializable]
public class ObstacleSpawnData
{
    [Tooltip("Name of the spawned object in the hierarchy.")]
    public string objectName = "Obstacle";

    [Tooltip("Optional prefab to instantiate for this obstacle.")]
    public GameObject obstaclePrefab;

    [Tooltip("Optional identifier if resolved via LevelDatabase.")]
    public string obstacleId;

    [Tooltip("Sprite to display when spawned without a prefab (e.g., platform or tutorial hint sprite).")]
    public Sprite sprite;

    [Tooltip("Sorting order for the SpriteRenderer when spawned without a prefab.")]
    public int sortingOrder = 0;

    [Tooltip("Whether to attach a BoxCollider2D when spawned without a prefab.")]
    public bool hasBoxCollider = false;

    [Tooltip("BoxCollider2D offset when hasBoxCollider is true.")]
    public Vector2 colliderOffset = Vector2.zero;

    [Tooltip("BoxCollider2D size when hasBoxCollider is true.")]
    public Vector2 colliderSize = Vector2.one;

    public Vector3 position;
    public Vector3 eulerAngles;
    public Vector3 scale = Vector3.one;
}

/// <summary>
/// ScriptableObject storing all configuration data needed to initialize a level.
/// </summary>
[CreateAssetMenu(fileName = "Level_New", menuName = "FeedMe/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("-- Basic Level Info --")]
    [Tooltip("Display or identifier name for this level (e.g., Lv1).")]
    public string levelName = "Level_1";

    [Tooltip("Prerequisite level that must be cleared before this level is unlocked. Leave empty (None) if unlocked by default.")]
    public LevelData prerequisiteLevel;

    [Tooltip("Maximum number of pen strokes allowed in this level.")]
    public int maxPens = 2;

    [Header("-- Pipe & Candy Spawn Settings --")]
    [Tooltip("World position of the candy SpawnPoint.")]
    public Vector3 spawnPointPosition = new Vector3(0f, 4.68f, 0f);

    [Tooltip("World position of the Pipe visual object.")]
    public Vector3 pipePosition = new Vector3(0f, 6.94f, 0f);

    [Tooltip("Time interval (in seconds) between each spawned candy.")]
    public float spawnInterval = 0.8f;

    [Tooltip("Random horizontal offset range applied when spawning candies.")]
    public float spawnRandomOffset = 0.05f;

    [Header("-- Candy Queue (Waves) --")]
    [Tooltip("Ordered sequence of candy waves spawned from the pipe.")]
    public List<CandyWave> candyWaves = new List<CandyWave>();

    [Header("-- Monsters in Level --")]
    [Tooltip("List of all monster groups and their positions/targets in this level.")]
    public List<MonsterSpawnData> monsters = new List<MonsterSpawnData>();

    [Header("-- Extra Obstacles / Platforms --")]
    [Tooltip("Optional extra obstacles or platforms placed in this level.")]
    public List<ObstacleSpawnData> obstacles = new List<ObstacleSpawnData>();
}
