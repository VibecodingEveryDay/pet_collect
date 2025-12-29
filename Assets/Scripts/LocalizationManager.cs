using UnityEngine;
#if Localization_yg
using YG;
#endif

/// <summary>
/// Менеджер локализации для игры
/// </summary>
public static class LocalizationManager
{
    private static string currentLanguage = "ru";
    public static event System.Action<string> OnLanguageChangedEvent;

#if Localization_yg
    static LocalizationManager()
    {
        // Инициализировать язык из YG2
        if (YG2.lang != null)
        {
            currentLanguage = YG2.lang;
        }
        
        // Подписаться на изменение языка
        YG2.onSwitchLang += OnLanguageChanged;
    }
    
    private static void OnLanguageChanged(string lang)
    {
        currentLanguage = lang;
        OnLanguageChangedEvent?.Invoke(lang);
    }
#else
    static LocalizationManager()
    {
        currentLanguage = "ru";
    }
#endif

    /// <summary>
    /// Получить текущий язык
    /// </summary>
    public static string GetCurrentLanguage()
    {
#if Localization_yg
        if (YG2.lang != null)
        {
            return YG2.lang;
        }
#endif
        return currentLanguage;
    }

    // ========== Подсказки ==========
    
    /// <summary>
    /// Получить текст подсказки "Купите яйцо"
    /// </summary>
    public static string GetHintBuyEgg()
    {
        return GetCurrentLanguage() == "en" ? "Buy an egg" : "Купите яйцо";
    }
    
    /// <summary>
    /// Получить текст подсказки "Сделайте активным питомца в рюкзаке"
    /// </summary>
    public static string GetHintActivatePet()
    {
        return GetCurrentLanguage() == "en" ? "Activate a pet in your backpack" : "Сделайте активным питомца в рюкзаке";
    }
    
    /// <summary>
    /// Получить текст подсказки "Нажмите на питомца, чтобы ускорить"
    /// </summary>
    public static string GetHintSpeedUpPet()
    {
        return GetCurrentLanguage() == "en" ? "Tap on the pet to speed up" : "Нажмите на питомца, чтобы ускорить";
    }

    // ========== Магазин ==========
    
    /// <summary>
    /// Получить заголовок магазина
    /// </summary>
    public static string GetShopTitle()
    {
        return GetCurrentLanguage() == "en" ? "Shop" : "Магазин";
    }
    
    /// <summary>
    /// Получить текст кнопки "Купить яйцо"
    /// </summary>
    public static string GetShopBuyEgg()
    {
        return GetCurrentLanguage() == "en" ? "Buy egg" : "Купить яйцо";
    }
    
    /// <summary>
    /// Получить текст кнопки "Улучшить кристаллы"
    /// </summary>
    public static string GetShopUpgradeCrystals()
    {
        return GetCurrentLanguage() == "en" ? "Upgrade crystals" : "Улучшить кристаллы";
    }
    
    /// <summary>
    /// Получить текст кнопки "Улучшить карту: сумеречные долины"
    /// </summary>
    public static string GetShopUpgradeMap()
    {
        return GetCurrentLanguage() == "en" ? "Upgrade map: Twilight Valleys" : "Улучшить карту: сумеречные долины";
    }
    
    /// <summary>
    /// Получить текст кнопки "Перейти в солнечные луга"
    /// </summary>
    public static string GetShopGoToSunnyMeadows()
    {
        return GetCurrentLanguage() == "en" ? "Go to Sunny Meadows" : "Перейти в солнечные луга";
    }
    
    /// <summary>
    /// Получить текст кнопки "Перейти в сумеречные долины"
    /// </summary>
    public static string GetShopGoToTwilightValleys()
    {
        return GetCurrentLanguage() == "en" ? "Go to Twilight Valleys" : "Перейти в сумеречные долины";
    }

    // ========== Рюкзак ==========
    
    /// <summary>
    /// Получить короткое название редкости питомца
    /// </summary>
    public static string GetRarityShortName(PetRarity rarity)
    {
        if (GetCurrentLanguage() == "en")
        {
            switch (rarity)
            {
                case PetRarity.Common:
                    return "Common";
                case PetRarity.Epic:
                    return "Epic";
                case PetRarity.Legendary:
                    return "Legend";
                default:
                    return "?";
            }
        }
        else
        {
            switch (rarity)
            {
                case PetRarity.Common:
                    return "Обычн.";
                case PetRarity.Epic:
                    return "Эпик";
                case PetRarity.Legendary:
                    return "Легенд.";
                default:
                    return "?";
            }
        }
    }
    
    /// <summary>
    /// Получить текст пагинации "Страница X из Y"
    /// </summary>
    public static string GetPaginationText(int currentPage, int totalPages)
    {
        if (GetCurrentLanguage() == "en")
        {
            return $"Page {currentPage + 1} of {totalPages}";
        }
        else
        {
            return $"Страница {currentPage + 1} из {totalPages}";
        }
    }
}

