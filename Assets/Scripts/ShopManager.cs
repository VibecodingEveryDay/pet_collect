using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if Storage_yg
using YG;
#endif

/// <summary>
/// Менеджер магазина с динамической ценой яиц и улучшением HP кристаллов в стиле Roblox
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PetHatchingManager hatchingManager;
    
    [Header("UI Элементы")]
    [SerializeField] private Button buyEggButton;
    [SerializeField] private Text eggPriceText;
    [SerializeField] private Button upgradeHPButton;
    [SerializeField] private Text upgradePriceText;
    
    // Цвета в стиле Roblox
    private Color robloxBlue = new Color(0.2f, 0.6f, 1f, 1f);
    private Color robloxGreen = new Color(0.4f, 0.8f, 0.4f, 1f);
    private Color robloxWhite = new Color(1f, 1f, 1f, 1f);
    private Color robloxBlack = new Color(0.1f, 0.1f, 0.1f, 1f);
    
    private PetInventory petInventory;
    
    private void Awake()
    {
        petInventory = PetInventory.Instance;
        
        if (hatchingManager == null)
        {
            hatchingManager = FindObjectOfType<PetHatchingManager>();
        }
        
        // Подписаться на изменение монет для обновления цен
        CoinManager.OnCoinsChanged += OnCoinsChanged;
    }
    
    private void OnDestroy()
    {
        CoinManager.OnCoinsChanged -= OnCoinsChanged;
    }
    
    /// <summary>
    /// Обработчик изменения количества монет
    /// </summary>
    private void OnCoinsChanged(int newAmount)
    {
        UpdatePrices();
    }
    
    /// <summary>
    /// Создать UI элементы магазина в стиле Roblox
    /// </summary>
    public void CreateShopUI(Transform shopSection)
    {
        if (shopSection == null)
        {
            return;
        }
        
        // Контейнер для кнопок
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(shopSection.transform, false);
        
        RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0.92f);
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup layoutGroup = buttonsContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20f;
        layoutGroup.padding = new RectOffset(15, 15, 15, 15);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        
        // Кнопка покупки яйца
        CreateBuyEggButton(buttonsContainer.transform);
        
        // Кнопка улучшения HP кристаллов
        CreateUpgradeHPButton(buttonsContainer.transform);
        
        // Обновить цены
        UpdatePrices();
    }
    
    /// <summary>
    /// Создать кнопку покупки яйца в стиле Roblox
    /// </summary>
    private void CreateBuyEggButton(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
        
        GameObject buttonObj = new GameObject("BuyEggButton");
        
        // Добавить RectTransform ПЕРЕД установкой parent
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(0f, 70f);
        btnRect.anchoredPosition = Vector2.zero;
        
        // Теперь установить parent
        buttonObj.transform.SetParent(parent, false);
        
        buyEggButton = buttonObj.AddComponent<Button>();
        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = robloxBlue;
        
        // Толстая обводка кнопки в стиле Roblox
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(buttonObj.transform, false);
        Image outlineImage = outlineObj.AddComponent<Image>();
        outlineImage.color = robloxBlack;
        
        RectTransform outlineRect = outlineObj.AddComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.sizeDelta = new Vector2(6f, 6f);
        outlineRect.anchoredPosition = Vector2.zero;
        outlineObj.transform.SetAsFirstSibling();
        
        // Текст цены
        GameObject priceObj = new GameObject("PriceText");
        priceObj.transform.SetParent(buttonObj.transform, false);
        eggPriceText = priceObj.AddComponent<Text>();
        eggPriceText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        eggPriceText.fontSize = 22;
        eggPriceText.fontStyle = FontStyle.Bold;
        eggPriceText.color = robloxWhite;
        eggPriceText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.anchorMin = Vector2.zero;
        priceRect.anchorMax = Vector2.one;
        priceRect.sizeDelta = Vector2.zero;
        priceRect.anchoredPosition = Vector2.zero;
        
        Outline priceOutline = priceObj.AddComponent<Outline>();
        priceOutline.effectColor = robloxBlack;
        priceOutline.effectDistance = new Vector2(3f, 3f);
        
        buyEggButton.onClick.AddListener(BuyEgg);
    }
    
    /// <summary>
    /// Создать кнопку улучшения HP кристаллов в стиле Roblox
    /// </summary>
    private void CreateUpgradeHPButton(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
        
        GameObject buttonObj = new GameObject("UpgradeHPButton");
        
        // Добавить RectTransform ПЕРЕД установкой parent
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(0f, 70f);
        btnRect.anchoredPosition = Vector2.zero;
        
        // Теперь установить parent
        buttonObj.transform.SetParent(parent, false);
        
        upgradeHPButton = buttonObj.AddComponent<Button>();
        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = robloxGreen;
        
        // Толстая обводка кнопки в стиле Roblox
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(buttonObj.transform, false);
        Image outlineImage = outlineObj.AddComponent<Image>();
        outlineImage.color = robloxBlack;
        
        RectTransform outlineRect = outlineObj.AddComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.sizeDelta = new Vector2(6f, 6f);
        outlineRect.anchoredPosition = Vector2.zero;
        outlineObj.transform.SetAsFirstSibling();
        
        // Текст цены
        GameObject priceObj = new GameObject("PriceText");
        priceObj.transform.SetParent(buttonObj.transform, false);
        upgradePriceText = priceObj.AddComponent<Text>();
        upgradePriceText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        upgradePriceText.fontSize = 22;
        upgradePriceText.fontStyle = FontStyle.Bold;
        upgradePriceText.color = robloxWhite;
        upgradePriceText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.anchorMin = Vector2.zero;
        priceRect.anchorMax = Vector2.one;
        priceRect.sizeDelta = Vector2.zero;
        priceRect.anchoredPosition = Vector2.zero;
        
        Outline priceOutline = priceObj.AddComponent<Outline>();
        priceOutline.effectColor = robloxBlack;
        priceOutline.effectDistance = new Vector2(3f, 3f);
        
        upgradeHPButton.onClick.AddListener(UpgradeCrystalHP);
    }
    
    /// <summary>
    /// Получить текущую цену яйца на основе количества питомцев в инвентаре
    /// </summary>
    public int GetEggPrice()
    {
        int petCount = PetInventory.Instance != null ? PetInventory.Instance.GetTotalPetCount() : 0;
        
        // Рассчитать цену на основе количества питомцев:
        // 0 питомцев - 100
        // 1 питомец - 100
        // 2 питомца - 200
        // 3 питомца - 400
        // 4 питомца - 600
        // 5 питомцев - 800
        // Больше 5 питомцев - всегда 1000
        int price;
        switch (petCount)
        {
            case 0:
            case 1:
                price = 100;
                break;
            case 2:
                price = 200;
                break;
            case 3:
                price = 400;
                break;
            case 4:
                price = 600;
                break;
            case 5:
                price = 800;
                break;
            default:
                price = 1000; // Больше 5 питомцев - всегда 1000
                break;
        }
        
        Debug.Log($"[ShopManager] GetEggPrice: У игрока {petCount} питомцев, цена яйца: {price}");
        return price;
    }
    
    /// <summary>
    /// Увеличить цену яйца после покупки (теперь не используется, так как цена рассчитывается динамически)
    /// Оставлено для обратной совместимости, но ничего не делает
    /// </summary>
    public void IncreaseEggPrice()
    {
        // Цена теперь рассчитывается динамически на основе количества питомцев
        // Этот метод больше не нужен, но оставлен для обратной совместимости
        Debug.Log($"[ShopManager] IncreaseEggPrice() вызван, но цена теперь рассчитывается динамически на основе количества питомцев");
    }
    
    /// <summary>
    /// Покупка яйца
    /// </summary>
    public void BuyEgg()
    {
        int price = GetEggPrice();
        
        if (!CoinManager.HasEnoughCoins(price))
        {
            return;
        }
        
        // Списываем монеты
        CoinManager.SpendCoins(price);
        
        // Цена теперь рассчитывается динамически на основе количества питомцев
        // Увеличение цены не требуется
        
        // Запускаем вылупление яйца
        if (hatchingManager != null)
        {
            hatchingManager.StartHatching();
        }
        
        // Обновляем цены в UI (цена обновится автоматически после добавления питомца)
        UpdatePrices();
    }
    
    /// <summary>
    /// Обновить цены с небольшой задержкой для гарантии сохранения
    /// </summary>
    private System.Collections.IEnumerator UpdatePricesDelayed()
    {
        yield return null; // Подождать один кадр
        UpdatePrices();
    }
    
    /// <summary>
    /// Улучшить HP кристаллов
    /// </summary>
    public void UpgradeCrystalHP()
    {
        int price = CrystalUpgradeSystem.GetUpgradePrice();
        
        if (!CoinManager.HasEnoughCoins(price))
        {
            return;
        }
        
        CoinManager.SpendCoins(price);
        CrystalUpgradeSystem.UpgradeHP();
        UpdatePrices();
    }
    
    /// <summary>
    /// Обновить отображение цен
    /// </summary>
    public void UpdatePrices()
    {
        // Обновить цену яйца
        if (eggPriceText != null)
        {
            int eggPrice = GetEggPrice();
            eggPriceText.text = $"КУПИТЬ ЯЙЦО\n{eggPrice} монет";
            
            // Изменить цвет, если недостаточно монет
            if (!CoinManager.HasEnoughCoins(eggPrice))
            {
                eggPriceText.color = Color.red;
            }
            else
            {
                eggPriceText.color = robloxWhite;
            }
        }
        
        // Обновить цену улучшения HP
        if (upgradePriceText != null)
        {
            int upgradePrice = CrystalUpgradeSystem.GetUpgradePrice();
            upgradePriceText.text = $"УЛУЧШИТЬ HP КРИСТАЛЛОВ\n{upgradePrice} монет";
            
            // Изменить цвет, если недостаточно монет
            if (!CoinManager.HasEnoughCoins(upgradePrice))
            {
                upgradePriceText.color = Color.red;
            }
            else
            {
                upgradePriceText.color = robloxWhite;
            }
        }
    }
}
