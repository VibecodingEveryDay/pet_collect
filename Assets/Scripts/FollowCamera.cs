using UnityEngine;

public class FollowCamera : MonoBehaviour
{
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
    
    [Header("Коллизии")]
    [SerializeField] private LayerMask collisionLayer = -1; // Слой для проверки коллизий
    [SerializeField] private float collisionRadius = 0.3f; // Радиус камеры для проверки коллизий
    [SerializeField] private float minDistance = 1f; // Минимальное расстояние при коллизии
    
    private float currentYaw = 0f; // Горизонтальный угол (вокруг Y оси)
    private float currentPitch = 20f; // Вертикальный угол (наклон вверх/вниз)
    private Vector3 currentVelocity; // Для плавного движения
    
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
        
        // Заблокировать курсор, если включено
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void Update()
    {
        // Обработка разблокировки курсора
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // Обработка ввода мыши только если курсор заблокирован
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseInput();
        }
    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
        
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
    /// Обработка ввода мыши для вращения камеры
    /// </summary>
    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Обновить горизонтальный угол (yaw) - вращение вокруг персонажа
        currentYaw += mouseX;
        
        // Обновить вертикальный угол (pitch) - наклон вверх/вниз
        currentPitch -= mouseY;
        
        // Ограничить вертикальный угол
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
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
}

