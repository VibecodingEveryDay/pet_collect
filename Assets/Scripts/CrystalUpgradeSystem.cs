using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Статический класс для управления уровнем HP кристаллов
/// </summary>
public static class CrystalUpgradeSystem
{
    private static int hpLevel = 1;
    private static float baseMaxHealth = 100f;
    
    public static event Action OnHPUpgraded;
    
    /// <summary>
    /// Получить текущий уровень HP
    /// </summary>
    public static int GetHPLevel()
    {
        return hpLevel;
    }
    
    /// <summary>
    /// Получить текущее максимальное HP кристаллов
    /// </summary>
    public static float GetCurrentMaxHealth()
    {
        // HP увеличивается на 50% с каждым уровнем: baseMaxHealth * (1.5 ^ (hpLevel - 1))
        return baseMaxHealth * Mathf.Pow(1.5f, hpLevel - 1);
    }
    
    /// <summary>
    /// Получить цену улучшения HP
    /// </summary>
    public static int GetUpgradePrice()
    {
        // Цена увеличивается в 2 раза с каждой покупкой: 200 * (2 ^ (hpLevel - 1))
        int basePrice = 200;
        int price = basePrice * (int)Mathf.Pow(2f, hpLevel - 1);
        return price;
    }
    
    /// <summary>
    /// Улучшить HP кристаллов
    /// </summary>
    public static void UpgradeHP()
    {
        // Увеличиваем уровень
        hpLevel++;
        
        float newMaxHealth = GetCurrentMaxHealth();
        
        // Обновляем все кристаллы
        UpdateAllCrystals();
        
        // Вызываем событие
        OnHPUpgraded?.Invoke();
        
        // Автоматическое сохранение при улучшении
        GameSaveManager.Instance?.SaveGameData();
    }
    
    /// <summary>
    /// Обновить HP всех кристаллов в сцене
    /// </summary>
    private static void UpdateAllCrystals()
    {
        Crystal[] crystals = UnityEngine.Object.FindObjectsOfType<Crystal>();
        float newMaxHealth = GetCurrentMaxHealth();
        
        foreach (Crystal crystal in crystals)
        {
            crystal.SetMaxHealth(newMaxHealth);
        }
    }
    
    /// <summary>
    /// Установить уровень HP (без сохранения, для загрузки)
    /// </summary>
    public static void SetHPLevel(int level)
    {
        hpLevel = Mathf.Max(1, level);
        UpdateAllCrystals();
        // Примечание: SetHPLevel не вызывает сохранение, чтобы избежать цикла при загрузке
    }
}

