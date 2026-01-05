using UnityEngine;
using System.Collections;
#if Localization_yg || EnvirData_yg || Storage_yg
using YG;
#endif

/// <summary>
/// Менеджер для отслеживания готовности игры и вызова GameReadyAPI
/// </summary>
public class GameReadyManager : MonoBehaviour
{
    private static GameReadyManager _instance;
    private static bool _isGameReady = false;
    
    public static bool IsGameReady => _isGameReady;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Автоматически создать GameReadyManager при старте игры
        if (_instance == null)
        {
            GetOrCreateInstance();
        }
    }
    
    [Header("Настройки загрузки")]
    [SerializeField] private float minLoadTime = 0.1f; // Минимальное время загрузки (секунды) - уменьшено для быстрой загрузки
    [SerializeField] private bool waitForSDK = true; // Ждать загрузки SDK
    [SerializeField] private int maxComponentCheckAttempts = 5; // Максимум попыток проверки компонентов
    [SerializeField] private float componentCheckInterval = 0.05f; // Интервал проверки компонентов (уменьшено)
    
    private bool sdkLoaded = false;
    private float loadStartTime;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        loadStartTime = Time.time;
    }
    
    /// <summary>
    /// Получить или создать экземпляр GameReadyManager
    /// </summary>
    public static GameReadyManager GetOrCreateInstance()
    {
        if (_instance == null)
        {
            GameObject managerObject = new GameObject("GameReadyManager");
            _instance = managerObject.AddComponent<GameReadyManager>();
            DontDestroyOnLoad(managerObject);
        }
        return _instance;
    }
    
    private void Start()
    {
        StartCoroutine(WaitForGameReady());
        
#if EnvirData_yg || Storage_yg || Localization_yg
        // Подписаться на событие загрузки SDK
        YG2.onGetSDKData += OnSDKDataLoaded;
        
        // Если SDK уже загружен
        if (YG2.isSDKEnabled)
        {
            OnSDKDataLoaded();
        }
#endif
    }
    
    private void OnDestroy()
    {
#if EnvirData_yg || Storage_yg || Localization_yg
        YG2.onGetSDKData -= OnSDKDataLoaded;
#endif
    }
    
    private void OnSDKDataLoaded()
    {
        sdkLoaded = true;
    }
    
    /// <summary>
    /// Корутина для ожидания готовности игры
    /// </summary>
    private IEnumerator WaitForGameReady()
    {
        // Ждать минимальное время загрузки
        yield return new WaitForSeconds(minLoadTime);
        
        // Если нужно ждать SDK, ждем его загрузки (с таймаутом)
        if (waitForSDK)
        {
#if EnvirData_yg || Storage_yg || Localization_yg
            float sdkWaitStartTime = Time.time;
            float sdkTimeout = 3f; // Таймаут ожидания SDK - 3 секунды
            
            while (!sdkLoaded && !YG2.isSDKEnabled && (Time.time - sdkWaitStartTime) < sdkTimeout)
            {
                yield return new WaitForSeconds(0.05f); // Уменьшено с 0.1f до 0.05f
            }
            
            // Если SDK все еще не загружен после таймаута, продолжаем без него
            if (!sdkLoaded && !YG2.isSDKEnabled)
            {
                Debug.LogWarning("[GameReadyManager] SDK не загрузился в течение таймаута, продолжаем без него");
            }
#else
            // Если SDK не используется, пропускаем ожидание
#endif
        }
        
        // Быстрая проверка основных компонентов (не блокируем загрузку)
        yield return WaitForComponentsFast();
        
        // Игра готова - вызвать GameReadyAPI
        MarkGameAsReady();
    }
    
    /// <summary>
    /// Быстрая проверка основных компонентов (не блокирует загрузку)
    /// </summary>
    private IEnumerator WaitForComponentsFast()
    {
        // Проверяем компоненты с ограниченным количеством попыток
        int attempts = 0;
        bool playerControllerFound = false;
        bool cameraFound = false;
        bool uiFound = false;
        
        while (attempts < maxComponentCheckAttempts && (!playerControllerFound || !cameraFound || !uiFound))
        {
            // Кешируем результаты поиска для одного кадра
            if (!playerControllerFound)
            {
                playerControllerFound = FindObjectOfType<PlayerController>() != null;
            }
            
            if (!cameraFound)
            {
                cameraFound = FindObjectOfType<FollowCamera>() != null;
            }
            
            if (!uiFound)
            {
                uiFound = FindObjectOfType<InventoryUI>() != null;
            }
            
            attempts++;
            
            // Если все компоненты найдены, выходим
            if (playerControllerFound && cameraFound && uiFound)
            {
                break;
            }
            
            yield return new WaitForSeconds(componentCheckInterval);
        }
        
        // Минимальная задержка для инициализации компонентов (уменьшена)
        yield return new WaitForSeconds(0.1f);
        
        if (!playerControllerFound || !cameraFound || !uiFound)
        {
            Debug.LogWarning($"[GameReadyManager] Некоторые компоненты не найдены после {attempts} попыток. PlayerController: {playerControllerFound}, Camera: {cameraFound}, UI: {uiFound}");
        }
    }
    
    /// <summary>
    /// Отметить игру как готовую и вызвать GameReadyAPI
    /// </summary>
    private void MarkGameAsReady()
    {
        if (_isGameReady)
        {
            return; // Уже вызвано
        }
        
        _isGameReady = true;
        
#if Localization_yg || EnvirData_yg || Storage_yg
        // Вызвать GameReadyAPI через YG2
        YG2.GameReadyAPI();
        Debug.Log("[GameReadyManager] Game Ready API вызван!");
#else
        Debug.Log("[GameReadyManager] Игра готова (YG2 SDK не подключен)");
#endif
    }
    
    /// <summary>
    /// Принудительно вызвать GameReadyAPI (для ручного вызова, если нужно)
    /// </summary>
    public static void ForceGameReady()
    {
        if (_instance != null)
        {
            _instance.MarkGameAsReady();
        }
    }
}

