using UnityEngine;
using GuidanceLine;

/// <summary>
/// Управляет видимостью GuidanceLine от персонажа к кнопке магазина
/// Показывает линию только когда у игрока достаточно монет для покупки яйца
/// </summary>
[RequireComponent(typeof(GuidanceLine.GuidanceLine))]
[RequireComponent(typeof(LineRenderer))]
public class ShopGuidanceLineController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Компонент GuidanceLine для управления")]
    [SerializeField] private GuidanceLine.GuidanceLine guidanceLine;
    
    [Tooltip("LineRenderer компонент")]
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("Настройки")]
    [Tooltip("Проверять количество монет каждый кадр (иначе только при изменении)")]
    [SerializeField] private bool checkEveryFrame = false;
    
    private bool hasEnoughCoins = false;
    private int lastCoinAmount = 0;
    private int eggPrice = 0;
    
    private void Start()
    {
        InitializeComponents();
        SubscribeToEvents();
        UpdateLineVisibility();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void Update()
    {
        // Периодически проверять видимость (если включено)
        if (checkEveryFrame)
        {
            UpdateLineVisibility();
        }
        
        // Если линия должна быть видна, но скрыта - показать снова
        if (hasEnoughCoins && guidanceLine != null && lineRenderer != null)
        {
            if (!lineRenderer.enabled || !guidanceLine.enabled)
            {
                ShowLine();
            }
        }
    }
    
    /// <summary>
    /// Инициализировать компоненты
    /// </summary>
    private void InitializeComponents()
    {
        // Получить компоненты
        if (guidanceLine == null)
        {
            guidanceLine = GetComponent<GuidanceLine.GuidanceLine>();
        }
        
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        if (guidanceLine == null)
        {
            Debug.LogError("[ShopGuidanceLineController] GuidanceLine компонент не найден!");
        }
        
        if (lineRenderer == null)
        {
            Debug.LogError("[ShopGuidanceLineController] LineRenderer компонент не найден!");
        }
    }
    
    /// <summary>
    /// Подписаться на события изменения монет
    /// </summary>
    private void SubscribeToEvents()
    {
        CoinManager.OnCoinsChanged += OnCoinsChanged;
    }
    
    /// <summary>
    /// Отписаться от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        CoinManager.OnCoinsChanged -= OnCoinsChanged;
    }
    
    /// <summary>
    /// Обработчик изменения количества монет
    /// </summary>
    private void OnCoinsChanged(int newAmount)
    {
        lastCoinAmount = newAmount;
        UpdateLineVisibility();
    }
    
    /// <summary>
    /// Обновить видимость линии в зависимости от количества монет
    /// </summary>
    private void UpdateLineVisibility()
    {
        // Получить текущую цену яйца
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            eggPrice = shopManager.GetEggPrice();
        }
        else
        {
            // Fallback: дефолтная цена
            eggPrice = 100;
        }
        
        // Получить текущее количество монет
        int currentCoins = CoinManager.GetCoins();
        lastCoinAmount = currentCoins;
        
        // Проверить, достаточно ли монет для покупки яйца
        bool hasEnoughForEgg = currentCoins >= eggPrice;
        
        // Проверить, не превышает ли количество монет 2000 (если больше - скрыть линию)
        bool coinsExceedLimit = currentCoins > 2000;
        
        // Линия показывается только если достаточно монет для яйца И монет не больше 2000
        bool previousHasEnoughCoins = hasEnoughCoins;
        hasEnoughCoins = hasEnoughForEgg && !coinsExceedLimit;
        
        // Показать/скрыть линию
        if (guidanceLine != null && lineRenderer != null)
        {
            if (hasEnoughCoins)
            {
                if (!previousHasEnoughCoins)
                {
                    Debug.Log($"[ShopGuidanceLineController] Показываю линию - Coins: {currentCoins}, EggPrice: {eggPrice}");
                }
                ShowLine();
            }
            else
            {
                if (previousHasEnoughCoins)
                {
                    string reason = coinsExceedLimit ? "монет больше 2000" : "недостаточно монет";
                    Debug.Log($"[ShopGuidanceLineController] Скрываю линию - Coins: {currentCoins}, EggPrice: {eggPrice}, Причина: {reason}");
                }
                HideLine();
            }
        }
    }
    
    /// <summary>
    /// Показать линию
    /// </summary>
    private void ShowLine()
    {
        if (guidanceLine != null)
        {
            guidanceLine.enabled = true;
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }
    
    /// <summary>
    /// Скрыть линию
    /// </summary>
    private void HideLine()
    {
        if (guidanceLine != null)
        {
            guidanceLine.enabled = false;
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }
}

