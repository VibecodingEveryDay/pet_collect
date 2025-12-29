using UnityEngine;

/// <summary>
/// Менеджер для управления паузой игры при потере фокуса окна
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    private static GamePauseManager _instance;
    
    [Header("Настройки паузы")]
    [SerializeField] private bool pauseOnFocusLoss = true; // Ставить на паузу при потере фокуса
    [SerializeField] private bool pauseOnApplicationPause = true; // Ставить на паузу при паузе приложения (мобильные)
    
    private float savedTimeScale = 1f; // Сохраненное значение Time.timeScale
    private bool isPaused = false; // Флаг паузы
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static GamePauseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GamePauseManager>();
                if (_instance == null)
                {
                    GameObject pauseManagerObject = new GameObject("GamePauseManager");
                    _instance = pauseManagerObject.AddComponent<GamePauseManager>();
                    DontDestroyOnLoad(pauseManagerObject);
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Проверить, находится ли игра на паузе
    /// </summary>
    public static bool IsPaused()
    {
        if (_instance != null)
        {
            return _instance.isPaused;
        }
        return false;
    }
    
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
    }
    
    private void Start()
    {
        // Убедиться, что игра не на паузе при старте
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
        savedTimeScale = 1f;
        isPaused = false;
        
        Debug.Log("[GamePauseManager] Инициализирован");
    }
    
    /// <summary>
    /// Инициализировать менеджер (вызывается автоматически при загрузке сцены)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        // Автоматически создать менеджер при загрузке сцены, если его еще нет
        if (_instance == null)
        {
            // Проверить, нет ли уже экземпляра в сцене
            _instance = FindObjectOfType<GamePauseManager>();
            if (_instance == null)
            {
                GameObject pauseManagerObject = new GameObject("GamePauseManager");
                _instance = pauseManagerObject.AddComponent<GamePauseManager>();
                DontDestroyOnLoad(pauseManagerObject);
                Debug.Log("[GamePauseManager] Автоматически создан при загрузке");
            }
        }
    }
    
    /// <summary>
    /// Вызывается когда приложение получает или теряет фокус
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!pauseOnFocusLoss)
            return;
        
        if (!hasFocus)
        {
            // Потеря фокуса - поставить на паузу
            PauseGame();
        }
        else if (hasFocus && isPaused)
        {
            // Получение фокуса - возобновить игру (только если была на паузе)
            ResumeGame();
        }
    }
    
    /// <summary>
    /// Вызывается когда приложение ставится на паузу или возобновляется (актуально для мобильных устройств)
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseOnApplicationPause)
            return;
        
        if (pauseStatus)
        {
            // Приложение поставлено на паузу - поставить игру на паузу
            PauseGame();
        }
        else if (!pauseStatus && isPaused)
        {
            // Приложение возобновлено - возобновить игру (только если была на паузе)
            ResumeGame();
        }
    }
    
    /// <summary>
    /// Поставить игру на паузу
    /// </summary>
    public void PauseGame()
    {
        if (isPaused)
            return;
        
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        
        Debug.Log("[GamePauseManager] Игра поставлена на паузу");
    }
    
    /// <summary>
    /// Возобновить игру
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused)
            return;
        
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        isPaused = false;
        
        Debug.Log("[GamePauseManager] Игра возобновлена");
    }
    
    /// <summary>
    /// Принудительно поставить игру на паузу (для внешнего управления)
    /// </summary>
    public static void Pause()
    {
        if (_instance != null)
        {
            _instance.PauseGame();
        }
    }
    
    /// <summary>
    /// Принудительно возобновить игру (для внешнего управления)
    /// </summary>
    public static void Resume()
    {
        if (_instance != null)
        {
            _instance.ResumeGame();
        }
    }
}

