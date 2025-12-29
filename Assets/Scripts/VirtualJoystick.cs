using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Виртуальный джойстик для мобильных устройств
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Настройки джойстика")]
    public RectTransform joystickBackground; // Фон джойстика
    public RectTransform joystickHandle; // Ручка джойстика
    [SerializeField] private float joystickRange = 90f; // Радиус движения ручки (увеличено с 50 до 90 для большего диапазона)
    [SerializeField] private float smoothReturnSpeed = 10f; // Скорость возврата ручки в центр
    
    [Header("Визуальные настройки")]
    [SerializeField] private bool showJoystick = true; // Показывать джойстик
    [SerializeField] private Color backgroundColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color handleColor = new Color(1f, 1f, 1f, 0.8f);
    
    private Vector2 inputVector = Vector2.zero; // Вектор ввода (от -1 до 1)
    private bool isDragging = false;
    private Vector2 startPosition;
    private Canvas parentCanvas;
    private Image backgroundImage;
    private Image handleImage;
    
    // События для передачи ввода
    public System.Action<Vector2> OnJoystickInput;
    
    private void Awake()
    {
        // Найти Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }
        
        // Инициализировать компоненты изображений
        if (joystickBackground != null)
        {
            backgroundImage = joystickBackground.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = joystickBackground.gameObject.AddComponent<Image>();
            }
            backgroundImage.color = backgroundColor;
        }
        
        if (joystickHandle != null)
        {
            handleImage = joystickHandle.GetComponent<Image>();
            if (handleImage == null)
            {
                handleImage = joystickHandle.gameObject.AddComponent<Image>();
            }
            handleImage.color = handleColor;
        }
        
        // Скрыть джойстик по умолчанию
        SetJoystickVisible(false);
    }
    
    private void Start()
    {
        // Принудительно сбросить кэш для правильного определения
        PlatformDetector.ResetCache();
        
        // Показать джойстик только на мобильных устройствах
        bool isMobile = PlatformDetector.IsMobile();
        
        // Дополнительная проверка для симулятора в редакторе
        #if UNITY_EDITOR
        if (!isMobile)
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
                isMobile = true;
            }
        }
        #endif
        
        if (isMobile)
        {
            SetJoystickVisible(true);
            // Убедиться, что компоненты активны
            if (joystickBackground != null)
            {
                joystickBackground.gameObject.SetActive(true);
                if (backgroundImage != null)
                {
                    backgroundImage.enabled = true;
                }
            }
            if (joystickHandle != null)
            {
                joystickHandle.gameObject.SetActive(true);
                if (handleImage != null)
                {
                    handleImage.enabled = true;
                }
            }
            Debug.Log($"[VirtualJoystick] Джойстик показан: background.active={joystickBackground?.gameObject.activeSelf}, handle.active={joystickHandle?.gameObject.activeSelf}");
        }
        else
        {
            SetJoystickVisible(false);
            Debug.Log("[VirtualJoystick] Не мобильное устройство - джойстик скрыт");
        }
    }
    
    private void Update()
    {
        // Скрыть джойстик, если открыто модальное окно
        bool shouldShow = showJoystick && !InventoryUI.IsAnyModalOpen();
        if (joystickBackground != null && joystickBackground.gameObject.activeSelf != shouldShow)
        {
            SetJoystickVisible(shouldShow);
        }
        
        // Если модальное окно открыто, не обрабатывать ввод
        if (InventoryUI.IsAnyModalOpen())
        {
            if (isDragging)
            {
                isDragging = false;
                inputVector = Vector2.zero;
                UpdateHandlePosition();
            }
            return;
        }
        
        // Если не перетаскиваем, плавно возвращаем ручку в центр
        if (!isDragging)
        {
            inputVector = Vector2.Lerp(inputVector, Vector2.zero, Time.deltaTime * smoothReturnSpeed);
            UpdateHandlePosition();
        }
        
        // Передать ввод всегда (даже когда ноль), чтобы персонаж останавливался
        if (OnJoystickInput != null)
        {
            // Если inputVector очень маленький, установить его в ноль
            if (inputVector.magnitude < 0.01f)
            {
                inputVector = Vector2.zero;
            }
            OnJoystickInput(inputVector);
        }
    }
    
    /// <summary>
    /// Обработка нажатия на джойстик
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Не обрабатывать ввод, если открыто модальное окно
        if (InventoryUI.IsAnyModalOpen())
        {
            return;
        }
        
        // Проверить, что это действительно касание в зоне джойстика
        if (joystickBackground == null)
        {
            Debug.LogWarning("[VirtualJoystick] OnPointerDown: joystickBackground равен null!");
            return;
        }
        
        isDragging = true;
        
        // Получить локальную позицию касания
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, 
            eventData.position, 
            parentCanvas.worldCamera, 
            out localPoint
        );
        
        startPosition = localPoint;
        UpdateJoystick(localPoint);
        
        Debug.Log($"[VirtualJoystick] OnPointerDown: localPoint={localPoint}, inputVector={inputVector}");
        
        // Остановить распространение события, чтобы камера не обрабатывала этот touch
        eventData.Use();
    }
    
    /// <summary>
    /// Обработка перетаскивания джойстика
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || joystickBackground == null) return;
        
        // Получить локальную позицию касания
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, 
            eventData.position, 
            parentCanvas.worldCamera, 
            out localPoint
        );
        
        UpdateJoystick(localPoint);
        
        // Остановить распространение события, чтобы камера не обрабатывала этот touch
        eventData.Use();
    }
    
    /// <summary>
    /// Обработка отпускания джойстика
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        inputVector = Vector2.zero;
        UpdateHandlePosition();
        
        // Остановить распространение события
        eventData.Use();
    }
    
    /// <summary>
    /// Обновить позицию джойстика
    /// </summary>
    private void UpdateJoystick(Vector2 localPoint)
    {
        // Вычислить смещение от центра (localPoint уже относительно центра joystickBackground)
        Vector2 offset = localPoint;
        
        // Ограничить радиусом
        if (offset.magnitude > joystickRange)
        {
            offset = offset.normalized * joystickRange;
        }
        
        // Обновить позицию ручки
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = offset;
        }
        
        // Вычислить вектор ввода (нормализованный от -1 до 1)
        inputVector = offset / joystickRange;
        
        Debug.Log($"[VirtualJoystick] UpdateJoystick: localPoint={localPoint}, offset={offset}, inputVector={inputVector}, magnitude={inputVector.magnitude}");
    }
    
    /// <summary>
    /// Обновить позицию ручки
    /// </summary>
    private void UpdateHandlePosition()
    {
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = inputVector * joystickRange;
        }
    }
    
    /// <summary>
    /// Получить текущий вектор ввода
    /// </summary>
    public Vector2 GetInput()
    {
        return inputVector;
    }
    
    /// <summary>
    /// Установить видимость джойстика
    /// </summary>
    public void SetJoystickVisible(bool visible)
    {
        // Скрыть джойстик, если открыто модальное окно
        bool shouldShow = visible && showJoystick && !InventoryUI.IsAnyModalOpen();
        
        if (joystickBackground != null)
        {
            joystickBackground.gameObject.SetActive(shouldShow);
            if (backgroundImage != null)
            {
                backgroundImage.enabled = shouldShow;
                if (shouldShow)
                {
                    backgroundImage.color = backgroundColor;
                }
            }
            Debug.Log($"[VirtualJoystick] SetJoystickVisible: {visible}, Background активен: {joystickBackground.gameObject.activeSelf}, Image.enabled: {backgroundImage?.enabled}");
        }
        if (joystickHandle != null)
        {
            joystickHandle.gameObject.SetActive(shouldShow);
            if (handleImage != null)
            {
                handleImage.enabled = shouldShow;
                if (shouldShow)
                {
                    handleImage.color = handleColor;
                }
            }
            Debug.Log($"[VirtualJoystick] Handle активен: {joystickHandle.gameObject.activeSelf}, Image.enabled: {handleImage?.enabled}");
        }
        
        // Убедиться, что родительский Canvas тоже активен
        if (parentCanvas != null && shouldShow)
        {
            parentCanvas.gameObject.SetActive(true);
            parentCanvas.enabled = true;
        }
    }
    
    /// <summary>
    /// Установить позицию джойстика на экране
    /// </summary>
    public void SetJoystickPosition(Vector2 screenPosition)
    {
        if (joystickBackground != null && parentCanvas != null)
        {
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                parentCanvas.worldCamera,
                out localPoint
            );
            joystickBackground.anchoredPosition = localPoint;
        }
    }
}

