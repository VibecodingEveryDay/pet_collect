using UnityEngine;

/// <summary>
/// Статический класс для управления покупкой улучшенной карты
/// </summary>
public static class MapUpgradeSystem
{
    private const string MAP_UPGRADE_KEY = "MapUpgrade_Purchased";
    private const string CURRENT_MAP_KEY = "CurrentMap"; // "Map1" или "Level2Map"
    private const int MAP_PRICE = 500;
    
    /// <summary>
    /// Проверить, куплена ли улучшенная карта
    /// </summary>
    public static bool IsMapUpgradePurchased()
    {
        return PlayerPrefs.GetInt(MAP_UPGRADE_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Получить цену улучшенной карты (0 если уже куплена)
    /// </summary>
    public static int GetMapPrice()
    {
        return IsMapUpgradePurchased() ? 0 : MAP_PRICE;
    }
    
    /// <summary>
    /// Купить улучшенную карту
    /// </summary>
    public static void PurchaseMapUpgrade()
    {
        if (!IsMapUpgradePurchased())
        {
            PlayerPrefs.SetInt(MAP_UPGRADE_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("[MapUpgradeSystem] Улучшенная карта куплена!");
            // Автоматическое сохранение при покупке карты
            GameSaveManager.Instance?.SaveGameData();
        }
    }
    
    /// <summary>
    /// Получить текущую карту ("Map1" или "Level2Map")
    /// </summary>
    public static string GetCurrentMap()
    {
        string savedMap = PlayerPrefs.GetString(CURRENT_MAP_KEY, "Map1");
        return savedMap;
    }
    
    /// <summary>
    /// Установить текущую карту
    /// </summary>
    public static void SetCurrentMap(string mapName)
    {
        if (mapName == "Map1" || mapName == "Level2Map")
        {
            PlayerPrefs.SetString(CURRENT_MAP_KEY, mapName);
            PlayerPrefs.Save();
            // Автоматическое сохранение при смене карты
            GameSaveManager.Instance?.SaveGameData();
        }
    }
    
    /// <summary>
    /// Установить текущую карту без сохранения (для загрузки)
    /// </summary>
    public static void SetCurrentMapWithoutSave(string mapName)
    {
        if (mapName == "Map1" || mapName == "Level2Map")
        {
            PlayerPrefs.SetString(CURRENT_MAP_KEY, mapName);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Купить улучшенную карту без сохранения (для загрузки)
    /// </summary>
    public static void PurchaseMapUpgradeWithoutSave()
    {
        if (!IsMapUpgradePurchased())
        {
            PlayerPrefs.SetInt(MAP_UPGRADE_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("[MapUpgradeSystem] Улучшенная карта куплена (без сохранения)!");
        }
    }
    
    /// <summary>
    /// Проверить, используется ли карта сумеречных долин (Level2Map)
    /// </summary>
    public static bool IsTwilightValleyActive()
    {
        return GetCurrentMap() == "Level2Map";
    }
    
    /// <summary>
    /// Сбросить покупку карты (для тестирования)
    /// </summary>
    public static void ResetMapUpgrade()
    {
        PlayerPrefs.DeleteKey(MAP_UPGRADE_KEY);
        PlayerPrefs.DeleteKey(CURRENT_MAP_KEY);
        Debug.Log("[MapUpgradeSystem] Покупка карты сброшена");
    }
}

