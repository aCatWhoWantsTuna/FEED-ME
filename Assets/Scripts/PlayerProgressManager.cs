using UnityEngine;

/// <summary>
/// Manages player progression, per-level earned candy/star rewards, and total currency for the dress-up shop.
/// Uses each level's total candy count as the completion reward.
/// </summary>
public static class PlayerProgressManager
{
    private const string TotalCurrencyKey = "FeedMe_TotalCurrency";
    private const string LevelRewardPrefix = "FeedMe_LevelReward_";

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
        string key = string.IsNullOrEmpty(levelData.levelName) ? levelData.name : levelData.levelName;
        return GetEarnedReward(key);
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
    /// Records level completion and awards any newly earned candy currency to the player's total balance.
    /// </summary>
    public static void RecordLevelCompleted(LevelData levelData)
    {
        if (levelData == null) return;

        string key = string.IsNullOrEmpty(levelData.levelName) ? levelData.name : levelData.levelName;
        int available = GetAvailableReward(levelData);
        int previousEarned = GetEarnedReward(key);

        if (available > previousEarned)
        {
            int newlyEarned = available - previousEarned;
            PlayerPrefs.SetInt(LevelRewardPrefix + key, available);
            AddCurrency(newlyEarned);
            Debug.Log($"[PlayerProgressManager] Cleared '{key}'! Awarded +{newlyEarned} currency (Total: {GetTotalCurrency()}).");
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
