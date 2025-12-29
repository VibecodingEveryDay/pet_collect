using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Runtime.InteropServices;

public class FollowCamera : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // JavaScript плагин для WebGL
    [DllImport("__Internal")]
    private static extern void SetCursorToCanvasCenter();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
    
    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
    
    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(System.IntPtr hWnd, ref POINT lpPoint);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
#endif
    
    [Header("Целевой объект")]
    [Tooltip("Персонаж Player, за которым будет следовать камера. Если не назначен, будет выполнен автоматический поиск.")]
    public Transform target; // Персонаж Player
    
    [Header("Настройки камеры")]
    [SerializeField] private float distance = 6f; // Расстояние от персонажа
    [SerializeField] private float height = 2.5f; // Высота камеры относительно персонажа
    [SerializeField] private float smoothSpeed = 10f; // Скорость плавного следования
    
    [Header("Управление мышью")]
    [SerializeField] private float mouseSensitivity = 2.5f; // Чувствительность мыши
    [SerializeField] private float minVerticalAngle = -30f; // Минимальный угол наклона
    [SerializeField] private float maxVerticalAngle = 60f; // Максимальный угол наклона
    [SerializeField] private bool lockCursor = true; // Блокировать курсор
    [SerializeField] private float cursorShowDuration = 3f; // Длительность показа курсора после клика (секунды)
    
    [Header("Коллизии")]
    [SerializeField] private LayerMask collisionLayer = -1; // Слой для проверки коллизий
    [SerializeField] private float collisionRadius = 0.3f; // Радиус камеры для проверки коллизий
    [SerializeField] private float minDistance = 1f; // Минимальное расстояние при коллизии
    
    private float currentYaw = 0f; // Горизонтальный угол (вокруг Y оси)
    private float currentPitch = 20f; // Вертикальный угол (наклон вверх/вниз)
    private Vector3 currentVelocity; // Для плавного движения
    private float cursorHideTimer = 0f; // Таймер для автоматического скрытия курсора после бездействия
    private bool wasModalOpen = false; // Флаг для отслеживания предыдущего состояния модального окна
    private float cameraLockTimer = 0f; // Таймер блокировки камеры после клика (для desktop)
    private const float CAMERA_LOCK_DURATION = 1.3f; // Длительность блокировки камеры после клика (секунды)
    
    private void Start()
    {
        // Если target не назначен, попытаться найти Player
        if (target == null)
        {
            // Попытка 1: найти по тегу "Player"
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                // Попытка 2: найти по имени "Player"
                GameObject playerByName = GameObject.Find("Player");
                if (playerByName != null)
                {
                    target = playerByName.transform;
                }
                else
                {
                    // Попытка 3: найти объект с компонентом PlayerController
                    PlayerController playerController = FindObjectOfType<PlayerController>();
                    if (playerController != null)
                    {
                        target = playerController.transform;
                    }
                }
            }
        }
        
        // Инициализировать углы на основе текущего вращения камеры
        if (target != null)
        {
            Vector3 directionToCamera = transform.position - (target.position + Vector3.up * height);
            currentYaw = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;
            currentPitch = Mathf.Asin(directionToCamera.y / directionToCamera.magnitude) * Mathf.Rad2Deg;
        }
        
        // Заблокировать курсор, если включено (только для WebGL, на Windows курсор всегда видим)
#if !UNITY_WEBGL || UNITY_EDITOR
        // На Windows/Editor курсор не скрываем, так как игра будет только на WebGL
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#else
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }
    
    private void Update()
    {
        // Проверить, является ли устройство desktop
        bool isDesktop = !PlatformDetector.IsMobile() && !PlatformDetector.IsTablet();
        
#if !UNITY_WEBGL || UNITY_EDITOR
        // На Windows/Editor
        Cursor.lockState = CursorLockMode.None;
        
        // На desktop: логика блокировки камеры после клика
        if (isDesktop)
        {
            // Обработка клика мыши - показать курсор и заблокировать камеру на 1.3 секунды
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                cameraLockTimer = CAMERA_LOCK_DURATION;
                Cursor.visible = true;
                cursorHideTimer = 0f; // Сбросить таймер скрытия
            }
            
            // Обновить таймер блокировки камеры
            if (cameraLockTimer > 0f)
            {
                cameraLockTimer -= Time.deltaTime;
                
                // Если время блокировки истекло, скрыть курсор
                if (cameraLockTimer <= 0f)
                {
                    cameraLockTimer = 0f;
                    Cursor.visible = false;
                }
            }
            
            // Обработка ввода мыши/тача - камера вращается, если не заблокирована
            if (cameraLockTimer <= 0f)
            {
                HandleMouseInput();
            }
        }
        else
        {
            // На мобильных устройствах (в редакторе) - курсор всегда видим, обычная логика
            Cursor.visible = true;
            HandleMouseInput();
        }
#else
        bool isModalOpen = InventoryUI.IsAnyModalOpen();
        
        // Если модальное окно открыто - курсор всегда видим и разблокирован
        if (isModalOpen)
        {
            if (!wasModalOpen)
            {
                // Модальное окно только что открылось - показать курсор
                ShowCursor(true);
            }
            wasModalOpen = true;
            }
            else
            {
            // Модальное окно закрыто - стандартное управление курсором
            if (wasModalOpen)
            {
                // Модальное окно только что закрылось - заблокировать курсор
                HideCursor();
            }
            wasModalOpen = false;
            
            // Переключение блокировки курсора через Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    ShowCursor(true);
                }
                else
                {
                    HideCursor();
            }
        }
        
            // На desktop: логика блокировки камеры после клика
            if (isDesktop)
            {
                // Обработка клика мыши - показать курсор и заблокировать камеру на 1.3 секунды
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    cameraLockTimer = CAMERA_LOCK_DURATION;
                    ShowCursor(true);
                    cursorHideTimer = 0f; // Сбросить таймер скрытия
                }
                
                // Обновить таймер блокировки камеры
                if (cameraLockTimer > 0f)
                {
                    cameraLockTimer -= Time.deltaTime;
                    
                    // Если время блокировки истекло, скрыть курсор
                    if (cameraLockTimer <= 0f)
                    {
                        cameraLockTimer = 0f;
                        HideCursor();
                    }
                }
                
                // Обработка ввода мыши/тача - камера вращается, если не заблокирована
                if (cameraLockTimer <= 0f)
                {
                    HandleMouseInput();
                }
            }
            else
            {
                // На мобильных устройствах - стандартная логика (без блокировки после клика)
                // Обработка клика мыши - показать курсор при клике
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        ShowCursor(true);
                    }
                    else
                    {
                        // Сбросить таймер при клике, если курсор уже видим
                        cursorHideTimer = cursorShowDuration;
                    }
                }
                
                // Автоматическое скрытие курсора после бездействия
                if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
                {
                    float mouseMovement = Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
                    
                    if (mouseMovement > 0.01f)
                    {
                        cursorHideTimer = cursorShowDuration;
                    }
                    else if (cursorHideTimer > 0f)
                    {
                        cursorHideTimer -= Time.deltaTime;
                        if (cursorHideTimer <= 0f && lockCursor)
                        {
                            HideCursor();
                        }
                    }
                }
                
                // Обработка ввода мыши/тача - камера вращается всегда
                HandleMouseInput();
            }
        }
#endif
    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
        
        // Если модальное окно открыто, не обновлять позицию камеры
        if (InventoryUI.IsAnyModalOpen())
        {
            return;
        }
        
        // Вычислить желаемую позицию камеры
        Vector3 desiredPosition = CalculateDesiredPosition();
        
        // Проверить коллизии и скорректировать позицию
        desiredPosition = CheckCollision(desiredPosition);
        
        // Плавно переместить камеру к желаемой позиции
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
        
        // Направить камеру на персонажа (с небольшим смещением вверх для лучшего обзора)
        Vector3 lookTarget = target.position + Vector3.up * height * 0.5f;
        Vector3 lookDirection = lookTarget - transform.position;
        
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
    
    /// <summary>
    /// Обработка ввода мыши/тача для вращения камеры
    /// </summary>
    private void HandleMouseInput()
    {
        // Проверить готовность игры - если игра не готова, блокируем управление камерой
        if (!GameReadyManager.IsGameReady)
        {
            return;
        }
        
        // Проверить, открыто ли модальное окно - если да, не обрабатывать ввод
        if (InventoryUI.IsAnyModalOpen())
        {
            return;
        }
        
        float mouseX = 0f;
        float mouseY = 0f;
        
        // Проверить, является ли устройство мобильным или планшетом
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        
        // Использовать touch input на мобильных устройствах (телефоны) И на планшетах
        // Если YG2 SDK определил планшет, то используем touch input (даже если Input.touchSupported = false в редакторе)
        bool useTouchInput = isMobile || isTablet;
        
        if (useTouchInput && Input.touchCount > 0)
        {
            // На мобильных устройствах используем touch input
            // Проверить все touch события и найти тот, который не на джойстике
            // Это позволяет использовать джойстик одной рукой, а камеру - другой
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                
                // Проверить, не находится ли touch на джойстике
                bool isOnJoystick = IsTouchOnJoystick(touch.position);
                
                if (!isOnJoystick)
                {
                    // Обработать touch для вращения камеры
                    // На мобильных устройствах чувствительность увеличена
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        // Использовать deltaPosition для плавного вращения
                        // Коэффициент 0.03f = увеличен на 50% от 0.02f (0.02f * 1.5 = 0.03f)
                        mouseX = touch.deltaPosition.x * mouseSensitivity * 0.03f;
                        mouseY = touch.deltaPosition.y * mouseSensitivity * 0.03f;
                        break; // Использовать только первый touch, который не на джойстике
                    }
                }
            }
        }
        else
        {
            // На десктопе камера вращается движением мыши (без клика)
            // НО не должна вращаться при зажатой кнопке мыши (drag)
            // Проверить, не находится ли клик на джойстике
            bool isOnJoystick = IsTouchOnJoystick(Input.mousePosition);
            
            if (!isOnJoystick)
            {
                // Проверить, не зажата ли кнопка мыши (drag)
                bool isMouseButtonDown = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                
                // Если кнопка мыши не зажата, обрабатываем движение мыши для вращения камеры
                if (!isMouseButtonDown)
                {
                    // На desktop уменьшаем чувствительность мыши в 2 раза
                    bool isDesktop = !isMobile && !isTablet;
                    float effectiveSensitivity = isDesktop ? mouseSensitivity * 0.5f : mouseSensitivity;
                    mouseX = Input.GetAxis("Mouse X") * effectiveSensitivity;
                    mouseY = Input.GetAxis("Mouse Y") * effectiveSensitivity;
                }
                // Если кнопка зажата - не вращаем камеру (drag не должен вращать камеру)
            }
        }
        
        // В редакторе/симуляторе также обрабатываем touch, если есть (для тестирования мобильных устройств)
        #if UNITY_EDITOR
        if (Input.touchCount > 0 && isMobile)
        {
            Touch touch = Input.GetTouch(0);
            bool isOnJoystick = IsTouchOnJoystick(touch.position);
            
            if (!isOnJoystick && touch.phase == TouchPhase.Moved)
            {
                // На мобильных устройствах чувствительность увеличена
                // Коэффициент 0.03f = увеличен на 50% от 0.02f (0.02f * 1.5 = 0.03f)
                mouseX = touch.deltaPosition.x * mouseSensitivity * 0.03f;
                mouseY = touch.deltaPosition.y * mouseSensitivity * 0.03f;
            }
        }
        #endif
        
        // Обновить горизонтальный угол (yaw) - вращение вокруг персонажа
        currentYaw += mouseX;
        
        // Обновить вертикальный угол (pitch) - наклон вверх/вниз
        currentPitch -= mouseY;
        
        // Ограничить вертикальный угол
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
    }
    
    /// <summary>
    /// Проверить, находится ли touch на джойстике
    /// </summary>
    private bool IsTouchOnJoystick(Vector2 screenPosition)
    {
        // Найти JoystickManager и проверить позицию
        JoystickManager joystickManager = FindObjectOfType<JoystickManager>();
        if (joystickManager != null)
        {
            // Получить Canvas джойстика
            Canvas joystickCanvas = joystickManager.GetComponentInChildren<Canvas>();
            if (joystickCanvas != null)
            {
                // Проверить, есть ли UI элемент под этой позицией
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    PointerEventData pointerData = new PointerEventData(eventSystem);
                    pointerData.position = screenPosition;
                    
                    var results = new System.Collections.Generic.List<RaycastResult>();
                    eventSystem.RaycastAll(pointerData, results);
                    
                    foreach (var result in results)
                    {
                        // Проверить, является ли это элементом джойстика
                        if (result.gameObject.name.Contains("Joystick") || 
                            result.gameObject.name.Contains("JoystickContainer") ||
                            result.gameObject.name.Contains("JoystickBackground") ||
                            result.gameObject.name.Contains("JoystickHandle"))
                        {
                            return true;
                        }
                        
                        // Также проверить родительские объекты
                        Transform parent = result.gameObject.transform.parent;
                        while (parent != null)
                        {
                            if (parent.name.Contains("Joystick"))
                            {
                                return true;
                            }
                            parent = parent.parent;
                        }
                    }
                }
                
                // Дополнительная проверка: проверить расстояние от центра джойстика
                VirtualJoystick virtualJoystick = joystickCanvas.GetComponentInChildren<VirtualJoystick>();
                if (virtualJoystick != null && virtualJoystick.joystickBackground != null)
                {
                    RectTransform backgroundRect = virtualJoystick.joystickBackground;
                    Vector2 localPoint;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        backgroundRect,
                        screenPosition,
                        joystickCanvas.worldCamera,
                        out localPoint))
                    {
                        // Проверить, находится ли точка в пределах фона джойстика (с запасом)
                        float radius = backgroundRect.sizeDelta.x / 2f + 50f; // Добавляем запас в 50 пикселей
                        if (localPoint.magnitude <= radius)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Вычислить желаемую позицию камеры на основе углов и расстояния
    /// </summary>
    private Vector3 CalculateDesiredPosition()
    {
        // Вычислить направление камеры на основе углов
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 direction = rotation * Vector3.back; // Назад от персонажа
        
        // Вычислить позицию камеры
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition + direction * distance;
        
        return desiredPosition;
    }
    
    /// <summary>
    /// Проверить коллизии камеры со стенами и скорректировать позицию
    /// </summary>
    private Vector3 CheckCollision(Vector3 desiredPosition)
    {
        if (target == null)
            return desiredPosition;
        
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 direction = desiredPosition - targetPosition;
        float desiredDistance = direction.magnitude;
        
        // Проверить коллизию между персонажем и желаемой позицией камеры
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, collisionRadius, direction.normalized, out hit, desiredDistance, collisionLayer))
        {
            // Если есть коллизия, переместить камеру ближе к персонажу
            float newDistance = hit.distance - collisionRadius;
            newDistance = Mathf.Max(newDistance, minDistance);
            desiredPosition = targetPosition + direction.normalized * newDistance;
        }
        
        return desiredPosition;
    }
    
    /// <summary>
    /// Получить текущий горизонтальный угол (для других скриптов)
    /// </summary>
    public float GetYaw()
    {
        return currentYaw;
    }
    
    /// <summary>
    /// Получить текущий вертикальный угол (для других скриптов)
    /// </summary>
    public float GetPitch()
    {
        return currentPitch;
    }
    
    /// <summary>
    /// Установить углы камеры (для внешнего управления)
    /// </summary>
    public void SetAngles(float yaw, float pitch)
    {
        currentYaw = yaw;
        currentPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }
    
    /// <summary>
    /// Показать курсор и установить его в центр
    /// </summary>
    private void ShowCursor(bool centerIt)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorHideTimer = cursorShowDuration;
        
        if (centerIt)
        {
            // Использовать корутину для установки курсора в центр с задержкой
            StartCoroutine(SetCursorToCenterDelayed());
        }
    }
    
    /// <summary>
    /// Скрыть курсор
    /// </summary>
    private void HideCursor()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // На Windows/Editor курсор не скрываем, всегда видим
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorHideTimer = 0f;
#else
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        cursorHideTimer = 0f;
#endif
    }
    
    /// <summary>
    /// Установить курсор по центру с задержкой (несколько попыток для надежности)
    /// </summary>
    private IEnumerator SetCursorToCenterDelayed()
    {
        // Подождать один кадр, чтобы API успел обработать разблокировку
        yield return null;
        
        // Попытка 1
        SetCursorToCenterSimple();
        yield return null;
        
        // Попытка 2 (для надежности)
        SetCursorToCenterSimple();
    }
    
    /// <summary>
    /// Универсальная установка курсора по центру экрана/canvas
    /// </summary>
    private void SetCursorToCenterSimple()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Для WebGL используем JavaScript плагин
        // В браузере курсор автоматически появится в центре canvas при выходе из Pointer Lock
        SetCursorToCanvasCenter();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Для Windows десктопа используем Windows API
        // Важно: используем координаты относительно клиентской области окна Unity
        System.IntPtr hWnd = GetActiveWindow();
        if (hWnd == System.IntPtr.Zero)
        {
            return;
        }
        
        // Получить верхний левый угол клиентской области окна в экранных координатах
        POINT clientTopLeft = new POINT { X = 0, Y = 0 };
        if (!ClientToScreen(hWnd, ref clientTopLeft))
        {
            return;
        }
        
        // Центр клиентской области окна Unity (в координатах клиентской области)
        // Screen.width и Screen.height - это размеры клиентской области окна Unity, а не всего экрана
        int centerXClient = Screen.width / 2;
        int centerYClient = Screen.height / 2;
        
        // Преобразовать координаты клиентской области в экранные координаты
        // В Windows: (0,0) клиентской области - верхний левый угол, Y растет вниз
        // ClientToScreen преобразует координаты клиентской области в экранные
        // Поэтому для центра просто добавляем половину ширины/высоты к верхнему левому углу
        int screenX = clientTopLeft.X + centerXClient;
        int screenY = clientTopLeft.Y + centerYClient;
        
        // Установить позицию курсора в экранных координатах
        SetCursorPos(screenX, screenY);
#else
        // На других платформах (Linux, Mac) используем стандартное поведение Unity
        // Курсор появится в центре окна при следующем движении мыши или клике
#endif
    }
}


