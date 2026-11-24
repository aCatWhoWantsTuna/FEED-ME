using UnityEngine;

/// <summary>
/// Manages player progression, per-level earned candy/star rewards, and total currency for the dress-up shop.
/// Uses each level's total candy count as the completion reward.
/// </summary>
public static class PlayerProgressManager
{
    private const string TotalCurrencyKey = "FeedMe_TotalCurrency";
    private const string LevelRewardPrefix = "FeedMe_LevelReward_";
    private const string LevelClearedPrefix = "FeedMe_LevelCleared_";

    /// <summary>
    /// Returns the persistent storage key used for a given LevelData asset.
    /// </summary>
    public static string GetLevelKey(LevelData levelData)
    {
        if (levelData == null) return string.Empty;
        return string.IsNullOrEmpty(levelData.levelName) ? levelData.name : levelData.levelName;
    }

    /// <summary>
    /// Returns the total currency currently owned by the player.
    /// </summary>
    public static int GetTotalCurrency()
    {
        return PlayerPrefs.GetInt(TotalCurrencyKey, 0);
    }

    /// <summary>
    /// Adds or deducts currency and saves to PlayerPrefs.
    /// </summary>
    public static void AddCurrency(int amount)
    {
        int next = Mathf.Max(0, GetTotalCurrency() + amount);
        PlayerPrefs.SetInt(TotalCurrencyKey, next);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Calculates the available reward for a given level based on its total candy count.
    /// </summary>
    public static int GetAvailableReward(LevelData levelData)
    {
        if (levelData == null || levelData.candyWaves == null)
        {
            return 0;
        }

        int totalCandies = 0;
        foreach (CandyWave wave in levelData.candyWaves)
        {
            if (wave != null && wave.amount > 0)
            {
                totalCandies += wave.amount;
            }
        }
        return totalCandies;
    }

    /// <summary>
    /// Returns the reward amount already earned by the player on the specified level.
    /// </summary>
    public static int GetEarnedReward(LevelData levelData)
    {
        if (levelData == null) return 0;
        return GetEarnedReward(GetLevelKey(levelData));
    }

    /// <summary>
    /// Returns the reward amount already earned by the player for the given level name.
    /// </summary>
    public static int GetEarnedReward(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return 0;
        return PlayerPrefs.GetInt(LevelRewardPrefix + levelName, 0);
    }

    /// <summary>
    /// Returns true if the specified level has been cleared at least once by the player.
    /// </summary>
    public static bool IsLevelCleared(LevelData levelData)
    {
        if (levelData == null) return false;
        return IsLevelCleared(GetLevelKey(levelData));
    }

    /// <summary>
    /// Returns true if the level with the given name has been cleared at least once by the player.
    /// </summary>
    public static bool IsLevelCleared(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return false;
        if (PlayerPrefs.GetInt(LevelClearedPrefix + levelName, 0) == 1)
        {
            return true;
        }
        return GetEarnedReward(levelName) > 0;
    }

    /// <summary>
    /// Returns true if the level has no prerequisite level, or if its prerequisite level has been cleared.
    /// </summary>
    public static bool IsLevelUnlocked(LevelData levelData)
    {
        if (levelData == null) return false;
        if (levelData.prerequisiteLevel == null)
        {
            return true;
        }
        return IsLevelCleared(levelData.prerequisiteLevel);
    }

    /// <summary>
    /// Records level completion, marks the level as cleared, and awards any newly earned candy currency.
    /// </summary>
    public static void RecordLevelCompleted(LevelData levelData)
    {
        if (levelData == null) return;

        string key = GetLevelKey(levelData);
        if (!string.IsNullOrEmpty(key))
        {
            PlayerPrefs.SetInt(LevelClearedPrefix + key, 1);
        }

        int available = GetAvailableReward(levelData);
        int previousEarned = GetEarnedReward(key);

        if (available > previousEarned)
        {
            int newlyEarned = available - previousEarned;
            PlayerPrefs.SetInt(LevelRewardPrefix + key, available);
            AddCurrency(newlyEarned);
            Debug.Log($"[PlayerProgressManager] Cleared '{key}'! Awarded +{newlyEarned} currency (Total: {GetTotalCurrency()}).");
        }
        else
        {
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Clears all saved level rewards and currency (useful for testing).
    /// </summary>
    public static void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[PlayerProgressManager] All player progress and currency have been reset.");
    }
}
