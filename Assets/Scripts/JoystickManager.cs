using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Менеджер для создания и управления виртуальным джойстиком
/// </summary>
public class JoystickManager : MonoBehaviour
{
    private static JoystickManager _instance;
    
    [Header("Настройки джойстика")]
    [SerializeField] private Canvas joystickCanvas;
    [SerializeField] private GameObject joystickPrefab; // Опциональный префаб джойстика
    [SerializeField] private Vector2 joystickPosition = new Vector2(150, 150); // Позиция джойстика на экране
    
    private VirtualJoystick virtualJoystick;
    private PlayerController playerController;
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static JoystickManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<JoystickManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("JoystickManager");
                    _instance = managerObject.AddComponent<JoystickManager>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
    }
    
    private void Start()
    {
        // Принудительно сбросить кэш для правильного определения
        PlatformDetector.ResetCache();
        
        // Проверить, является ли устройство мобильным или планшетом
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        
        // Джойстик нужен на мобильных устройствах (телефоны) И на планшетах
        // Если YG2 SDK определил планшет или мобильное устройство, то джойстик нужен
        // (даже если Input.touchSupported = false в редакторе)
        bool needsJoystick = isMobile || isTablet;
        
        // Дополнительная проверка для симулятора в редакторе (только если YG2 SDK не определил устройство)
        #if UNITY_EDITOR
        if (!needsJoystick)
        {
            // В редакторе проверяем явные признаки мобильного устройства
            // НЕ используем разрешение экрана, так как мониторы ПК могут иметь любые разрешения
            string deviceModel = SystemInfo.deviceModel.ToLower();
            bool isSimulatorDevice = deviceModel.Contains("simulator") || 
                                    deviceModel.Contains("ipad") || 
                                    deviceModel.Contains("iphone");
            
            // В редакторе определяем как мобильное только если:
            // 1. Touch поддерживается
            // 2. ИЛИ deviceModel указывает на симулятор/мобильное устройство
            // 3. ИЛИ Application.isMobilePlatform = true
            if (Input.touchSupported || isSimulatorDevice || Application.isMobilePlatform)
            {
                needsJoystick = true;
                Debug.Log($"[JoystickManager] Обнаружен мобильный режим в редакторе: touchSupported={Input.touchSupported}, deviceModel='{deviceModel}', isMobilePlatform={Application.isMobilePlatform}");
            }
        }
        else
        {
            // Если YG2 SDK уже определил устройство как мобильное или планшет, логируем это
            Debug.Log($"[JoystickManager] YG2 SDK определил устройство: isMobile={isMobile}, isTablet={isTablet}");
        }
        #endif
        
        // Используем PlatformDetector методы для получения размеров (одинаковые с InventoryUI)
        float screenWidth = PlatformDetector.GetScreenWidth();
        float screenHeight = PlatformDetector.GetScreenHeight();
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Для WebGL также показываем реальные размеры экрана для диагностики
        int realWidth = PlatformDetector.GetRealScreenWidth();
        int realHeight = PlatformDetector.GetRealScreenHeight();
        float canvasWidth = Screen.width;
        float canvasHeight = Screen.height;
        Debug.Log($"[JoystickManager] Проверка платформы: isMobile={isMobile}, isTablet={isTablet}, needsJoystick={needsJoystick}, touchSupported={Input.touchSupported}, platform={Application.platform}, deviceModel={SystemInfo.deviceModel}, canvas={canvasWidth}x{canvasHeight}, realScreen={realWidth}x{realHeight}, UI={screenWidth}x{screenHeight}");
        #else
        Debug.Log($"[JoystickManager] Проверка платформы: isMobile={isMobile}, isTablet={isTablet}, needsJoystick={needsJoystick}, touchSupported={Input.touchSupported}, platform={Application.platform}, deviceModel={SystemInfo.deviceModel}, resolution={screenWidth}x{screenHeight}");
        #endif
        
        if (!needsJoystick)
        {
            // На ПК джойстик не нужен - скрыть, но не деактивировать GameObject
            // чтобы можно было включить вручную для тестирования
            if (joystickCanvas != null)
            {
                joystickCanvas.gameObject.SetActive(false);
            }
            Debug.Log("[JoystickManager] ПК устройство - джойстик не создан");
            return;
        }
        
        Debug.Log($"[JoystickManager] Мобильное устройство обнаружено. Touch supported: {Input.touchSupported}, Touch count: {Input.touchCount}");
        
        // Найти PlayerController
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[JoystickManager] PlayerController не найден!");
            return;
        }
        
        // Создать Canvas для джойстика, если не назначен
        if (joystickCanvas == null)
        {
            CreateJoystickCanvas();
        }
        
        // Убедиться, что Canvas активен
        if (joystickCanvas != null)
        {
            joystickCanvas.gameObject.SetActive(true);
            joystickCanvas.enabled = true;
            
            // Убедиться, что Canvas видим
            CanvasGroup canvasGroup = joystickCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = joystickCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            Debug.Log($"[JoystickManager] Canvas создан и активирован: activeSelf={joystickCanvas.gameObject.activeSelf}, enabled={joystickCanvas.enabled}, renderMode={joystickCanvas.renderMode}");
        }
        
        // Создать джойстик
        CreateJoystick();
        Debug.Log("[JoystickManager] Джойстик создан");
    }
    
    /// <summary>
    /// Создать Canvas для джойстика
    /// </summary>
    private void CreateJoystickCanvas()
    {
        GameObject canvasObj = new GameObject("JoystickCanvas");
        canvasObj.transform.SetParent(transform);
        
        joystickCanvas = canvasObj.AddComponent<Canvas>();
        joystickCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        joystickCanvas.sortingOrder = 1000; // Очень высокий порядок отрисовки, чтобы быть поверх всего
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Убедиться, что Canvas активен
        canvasObj.SetActive(true);
        
        Debug.Log($"[JoystickManager] Canvas создан: {canvasObj.name}, Активен: {canvasObj.activeSelf}, RenderMode: {joystickCanvas.renderMode}, SortingOrder: {joystickCanvas.sortingOrder}");
        
        // Добавить EventSystem, если его нет
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[JoystickManager] EventSystem создан");
        }
        else
        {
            Debug.Log("[JoystickManager] EventSystem уже существует");
        }
    }
    
    /// <summary>
    /// Создать виртуальный джойстик
    /// </summary>
    private void CreateJoystick()
    {
        // Создать контейнер для джойстика
        GameObject joystickContainer = new GameObject("JoystickContainer");
        joystickContainer.transform.SetParent(joystickCanvas.transform, false);
        
        RectTransform containerRect = joystickContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.anchoredPosition = joystickPosition;
        containerRect.sizeDelta = new Vector2(400, 400); // Увеличено с 200 до 400
        
        // Создать фон джойстика
        GameObject backgroundObj = new GameObject("JoystickBackground");
        backgroundObj.transform.SetParent(joystickContainer.transform, false);
        
        RectTransform backgroundRect = backgroundObj.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(300, 300); // Увеличено с 150 до 300
        
        Image backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.3f);
        // Создать круглый спрайт для фона
        backgroundImage.sprite = CreateCircleSprite(300, new Color(1f, 1f, 1f, 0.3f)); // Увеличено с 150 до 300
        backgroundImage.raycastTarget = true; // Включить обработку touch событий
        
        // Создать ручку джойстика
        GameObject handleObj = new GameObject("JoystickHandle");
        handleObj.transform.SetParent(joystickContainer.transform, false);
        
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(120, 120); // Увеличено с 80 до 120
        
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.8f);
        handleImage.sprite = CreateCircleSprite(120, new Color(1f, 1f, 1f, 0.8f)); // Увеличено с 80 до 120
        handleImage.raycastTarget = false;
        
        // Добавить компонент VirtualJoystick
        virtualJoystick = joystickContainer.AddComponent<VirtualJoystick>();
        
        // Установить ссылки напрямую (поля теперь public)
        virtualJoystick.joystickBackground = backgroundRect;
        virtualJoystick.joystickHandle = handleRect;
        
        // Подписаться на события джойстика
        virtualJoystick.OnJoystickInput += OnJoystickInput;
        
        // Убедиться, что джойстик виден
        if (joystickContainer != null)
        {
            joystickContainer.SetActive(true);
            Debug.Log($"[JoystickManager] JoystickContainer активен: {joystickContainer.activeSelf}");
        }
        if (backgroundObj != null)
        {
            backgroundObj.SetActive(true);
            Debug.Log($"[JoystickManager] Background активен: {backgroundObj.activeSelf}, Позиция: {backgroundRect.anchoredPosition}");
        }
        if (handleObj != null)
        {
            handleObj.SetActive(true);
            Debug.Log($"[JoystickManager] Handle активен: {handleObj.activeSelf}");
        }
        
        // Принудительно показать джойстик
        if (virtualJoystick != null)
        {
            virtualJoystick.SetJoystickVisible(true);
            Debug.Log("[JoystickManager] Джойстик принудительно показан");
        }
    }
    
    /// <summary>
    /// Создать круглый спрайт
    /// </summary>
    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Обработка ввода от джойстика
    /// </summary>
    private void OnJoystickInput(Vector2 input)
    {
        if (playerController != null)
        {
            playerController.SetJoystickInput(input);
        }
        else
        {
            // Попытаться найти PlayerController снова
            playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetJoystickInput(input);
            }
            else
            {
                Debug.LogWarning("[JoystickManager] PlayerController не найден при попытке передать ввод от джойстика!");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (virtualJoystick != null && virtualJoystick.OnJoystickInput != null)
        {
            virtualJoystick.OnJoystickInput -= OnJoystickInput;
        }
    }
}

