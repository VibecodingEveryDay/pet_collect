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
        // Формула: baseMaxHealth * (1.5 ^ (hpLevel - 1))
        return baseMaxHealth * Mathf.Pow(1.5f, hpLevel - 1);
    }
    
    /// <summary>
    /// Получить цену улучшения HP
    /// </summary>
    public static int GetUpgradePrice()
    {
        // Формула: 100 * (1.5 ^ (hpLevel - 1))
        return Mathf.RoundToInt(100f * Mathf.Pow(1.5f, hpLevel - 1));
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
    /// Установить уровень HP (для тестирования)
    /// </summary>
    public static void SetHPLevel(int level)
    {
        hpLevel = Mathf.Max(1, level);
        UpdateAllCrystals();
    }
}

