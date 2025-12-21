using UnityEngine;

/// <summary>
/// Менеджер для управления монетами игрока (Singleton)
/// </summary>
public class CoinManager : MonoBehaviour
{
    private static CoinManager _instance;
    private static int _coins = -1; // -1 означает, что монеты еще не инициализированы
    
    /// <summary>
    /// Событие изменения количества монет
    /// </summary>
    public static System.Action<int> OnCoinsChanged;
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static CoinManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Попытаться найти существующий экземпляр
                _instance = FindObjectOfType<CoinManager>();
                
                // Если не найден, создать новый
                if (_instance == null)
                {
                    GameObject coinManagerObject = new GameObject("CoinManager");
                    _instance = coinManagerObject.AddComponent<CoinManager>();
                    DontDestroyOnLoad(coinManagerObject);
                }
            }
            return _instance;
        }
    }
    
    [Header("Настройки")]
    [SerializeField] private int startingCoins = 500; // Начальное количество монет
    
    private void Awake()
    {
        // Убедиться, что только один экземпляр существует
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Инициализировать монеты, если они еще не были установлены
        if (_coins < 0)
        {
            _coins = startingCoins;
            OnCoinsChanged?.Invoke(_coins);
        }
    }
    
    /// <summary>
    /// Получить текущее количество монет
    /// </summary>
    public static int GetCoins()
    {
        // Если монеты еще не инициализированы, инициализировать их
        if (_coins < 0)
        {
            // Убедиться, что экземпляр существует
            if (_instance == null)
            {
                _instance = FindObjectOfType<CoinManager>();
                if (_instance == null)
                {
                    GameObject coinManagerObject = new GameObject("CoinManager");
                    _instance = coinManagerObject.AddComponent<CoinManager>();
                    DontDestroyOnLoad(coinManagerObject);
                }
            }
            _coins = _instance.startingCoins;
            OnCoinsChanged?.Invoke(_coins);
        }
        return _coins;
    }
    
    /// <summary>
    /// Установить количество монет
    /// </summary>
    public static void SetCoins(int amount)
    {
        _coins = Mathf.Max(0, amount);
        OnCoinsChanged?.Invoke(_coins);
    }
    
    /// <summary>
    /// Добавить монеты
    /// </summary>
    public static void AddCoins(int amount)
    {
        if (amount > 0)
        {
            _coins += amount;
            OnCoinsChanged?.Invoke(_coins);
        }
    }
    
    /// <summary>
    /// Потратить монеты
    /// </summary>
    public static void SpendCoins(int amount)
    {
        if (amount > 0 && _coins >= amount)
        {
            _coins -= amount;
            OnCoinsChanged?.Invoke(_coins);
        }
    }
    
    /// <summary>
    /// Проверить, достаточно ли монет
    /// </summary>
    public static bool HasEnoughCoins(int amount)
    {
        return _coins >= amount;
    }
    
    /// <summary>
    /// Сбросить монеты до начального значения
    /// </summary>
    public void ResetCoins()
    {
        _coins = startingCoins;
        OnCoinsChanged?.Invoke(_coins);
    }
}
