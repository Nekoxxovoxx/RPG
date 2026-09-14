using System;
using UnityEngine;

public static class HealthBarDisplaySettings
{
    private const string PlayerHealthBarKey = "ShowPlayerOverheadHealthBars";
    private const string EnemyHealthBarKey = "ShowEnemyOverheadHealthBars";

    public static event Action OnSettingsChanged;

    public static bool ShowPlayerHealthBars
    {
        get => PlayerPrefs.GetInt(PlayerHealthBarKey, 1) == 1;
        set => SetBool(PlayerHealthBarKey, value);
    }

    public static bool ShowEnemyHealthBars
    {
        get => PlayerPrefs.GetInt(EnemyHealthBarKey, 1) == 1;
        set => SetBool(EnemyHealthBarKey, value);
    }

    public static bool ShouldShow(bool isPlayerHealthBar)
    {
        return isPlayerHealthBar ? ShowPlayerHealthBars : ShowEnemyHealthBars;
    }

    private static void SetBool(string key, bool value)
    {
        int intValue = value ? 1 : 0;

        if (PlayerPrefs.GetInt(key, 1) == intValue)
            return;

        PlayerPrefs.SetInt(key, intValue);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }
}
