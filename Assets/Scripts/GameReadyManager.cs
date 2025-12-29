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
    [SerializeField] private float minLoadTime = 1f; // Минимальное время загрузки (секунды)
    [SerializeField] private bool waitForSDK = true; // Ждать загрузки SDK
    
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
        
        // Если нужно ждать SDK, ждем его загрузки
        if (waitForSDK)
        {
#if EnvirData_yg || Storage_yg || Localization_yg
            while (!sdkLoaded && !YG2.isSDKEnabled)
            {
                yield return new WaitForSeconds(0.1f);
            }
#else
            // Если SDK не используется, просто ждем немного
            yield return new WaitForSeconds(0.5f);
#endif
        }
        
        // Ждать, пока все основные компоненты загрузятся
        yield return WaitForComponents();
        
        // Игра готова - вызвать GameReadyAPI
        MarkGameAsReady();
    }
    
    /// <summary>
    /// Ждать загрузки основных компонентов
    /// </summary>
    private IEnumerator WaitForComponents()
    {
        // Ждать загрузки PlayerController
        while (FindObjectOfType<PlayerController>() == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Ждать загрузки камеры
        while (FindObjectOfType<FollowCamera>() == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Ждать загрузки UI
        while (FindObjectOfType<InventoryUI>() == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Дополнительная небольшая задержка для инициализации всех компонентов
        yield return new WaitForSeconds(0.5f);
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

