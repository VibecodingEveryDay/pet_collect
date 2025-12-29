using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float walkSpeed = 5f; // Скорость ходьбы
    [SerializeField] private float runSpeed = 10f; // Скорость бега
    [SerializeField] private float rotationSpeed = 15f; // Быстрее поворот
    [SerializeField] private bool useSimpleControls = true; // Простое управление для детей
    [SerializeField] private float runThreshold = 0.7f; // Порог для перехода в бег (0-1)
    
    [Header("Прыжок")]
    [SerializeField] private float jumpForce = 15f; // Сила прыжка (быстрый прыжок без зависания)
    [SerializeField] private KeyCode jumpKey = KeyCode.Space; // Клавиша прыжка
    [SerializeField] private float fallMultiplier = 3f; // Множитель гравитации при падении (для быстрого падения)
    [SerializeField] private float lowJumpMultiplier = 2.5f; // Множитель для ускорения падения в верхней точке прыжка

    [Header("Физика")]
    [SerializeField] private float groundCheckDistance = 1f; // Увеличено для надежности
    
    [Header("Анимации")]
    [SerializeField] private Animator animator;
    [SerializeField] private float idleSwitchInterval = 3f; // Интервал переключения idle анимаций
    
    [Header("Модель персонажа")]
    [SerializeField] private Transform playerModel; // Ссылка на дочерний объект с моделью персонажа
    
    [Header("Ограничение движения")]
    [SerializeField] private float maxDistanceFromSpawn = 60f; // Максимальное расстояние от точки спавна
    
    [Header("Мобильное управление")]
    [SerializeField] private bool useJoystick = true; // Использовать джойстик на мобильных устройствах
    
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private int groundCollisions = 0; // Счетчик коллизий с землей
    private float currentSpeed;
    private bool isRunning = false;
    private float idleTimer = 0f;
    private bool useIdle1 = true; // Переключатель между idle_1 и idle_2
    private bool jumpTriggerSet = false; // Флаг для предотвращения повторного срабатывания триггера прыжка
    private Vector3 spawnPosition; // Точка спавна персонажа
    private Vector2 joystickInput = Vector2.zero; // Ввод от джойстика
    
    private void Start()
    {
        // Получить или добавить Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.freezeRotation = true; // Запретить поворот от физики
            rb.useGravity = true;
            rb.mass = 1f;
        }
        
        // Получить или найти CapsuleCollider
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponentInChildren<CapsuleCollider>();
        }
        
        // Получить или найти Animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        // Найти дочерний объект с моделью персонажа (если не назначен вручную)
        if (playerModel == null)
        {
            // Попытаться найти дочерний объект с Renderer (модель)
            Renderer modelRenderer = GetComponentInChildren<Renderer>();
            if (modelRenderer != null)
            {
                playerModel = modelRenderer.transform;
            }
            else
            {
                // Если не найден, попробовать найти первый дочерний объект
                if (transform.childCount > 0)
                {
                    playerModel = transform.GetChild(0);
                }
                else
                {
                    // Если нет дочерних объектов, использовать сам transform
                    playerModel = transform;
                }
            }
        }
        
        // Зафиксировать поворот по X и Z (только Y может вращаться)
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        // Сохранить точку спавна (начальную позицию)
        spawnPosition = transform.position;
        
        // Инициализировать состояние - считаем, что персонаж на земле
        isGrounded = true;
        groundCollisions = 1; // Начальное значение для возможности прыжка
        
        // Инициализировать анимации (только если Animator найден)
        if (animator != null)
        {
            InitializeAnimations();
        }
    }
    
    /// <summary>
    /// Инициализировать систему анимаций
    /// </summary>
    private void InitializeAnimations()
    {
        if (animator == null)
        {
            return;
        }
        
        // Инициализировать параметры аниматора
        try
        {
            // Установить начальные значения параметров
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsGrounded", true);
            
            // Проверить и установить UseIdle1, если параметр существует
            bool hasUseIdle1 = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "UseIdle1")
                {
                    hasUseIdle1 = true;
                    break;
                }
            }
            
            if (hasUseIdle1)
            {
                animator.SetBool("UseIdle1", true);
            }
        }
        catch (System.Exception e)
        {
        }
    }
    
    private void Update()
    {
        // Проверка готовности игры - если игра не готова, блокируем управление
        if (!GameReadyManager.IsGameReady)
        {
            // Остановить движение, если игра не готова
            moveDirectionToApply = Vector3.zero;
            currentSpeed = 0f;
            joystickInput = Vector2.zero; // Сбросить ввод джойстика
            return;
        }
        
        // Проверка на земле в начале Update для актуальности
        CheckGrounded();
        
        HandleJump();
        HandleMovement();
        UpdateAnimations();
    }
    
    private void FixedUpdate()
    {
        // Движение через Rigidbody в FixedUpdate для плавности
        if (rb != null)
        {
            ApplyMovement();
            ApplyFastFall(); // Применить быструю гравитацию при падении
        }
    }
    
    /// <summary>
    /// Проверка нахождения на земле
    /// </summary>
    private void CheckGrounded()
    {
        if (rb == null)
        {
            isGrounded = false;
            return;
        }
        
        // Если персонаж движется вверх (прыгает), точно не на земле
        if (rb.linearVelocity.y > 0.1f)
        {
            isGrounded = false;
            return;
        }
        
        // Используем счетчик коллизий (более надежный способ)
        isGrounded = groundCollisions > 0;
        
        // Дополнительная проверка через Raycast, если коллизии не сработали
        if (!isGrounded && capsuleCollider != null)
        {
            float radius = capsuleCollider.radius;
            Vector3 bottomPoint = transform.position + Vector3.down * (capsuleCollider.height / 2f - radius);
            float checkDistance = groundCheckDistance + 1f;
            isGrounded = Physics.Raycast(bottomPoint, Vector3.down, checkDistance);
        }
    }
    
    /// <summary>
    /// Обработка коллизии с землей
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Упрощенная проверка: любая коллизия снизу считается землей
        if (!collision.collider.isTrigger)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // Проверяем, что нормаль направлена вверх (земля снизу)
                if (contact.normal.y > 0.3f)
                {
                    groundCollisions++;
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Обработка выхода из коллизии
    /// </summary>
    private void OnCollisionExit(Collision collision)
    {
        // Уменьшаем счетчик коллизий
        if (!collision.collider.isTrigger && groundCollisions > 0)
        {
            groundCollisions--;
        }
    }
    
    /// <summary>
    /// Публичный метод для вызова прыжка из UI
    /// </summary>
    public void Jump()
    {
        PerformJump();
    }
    
    /// <summary>
    /// Обработка прыжка
    /// </summary>
    private void HandleJump()
    {
        if (Input.GetKeyDown(jumpKey) && rb != null)
        {
            PerformJump();
        }
    }
    
    /// <summary>
    /// Выполнить прыжок (используется как из клавиатуры, так и из UI)
    /// </summary>
    private void PerformJump()
    {
        if (rb == null) return;
        
            // Получить позицию Y модели персонажа (дочерний объект)
            float modelY = playerModel != null ? playerModel.position.y : transform.position.y;
            
            // Проверка: прыгать можно только если Y позиция модели меньше -14
            if (modelY >= -14f)
            {
                return; // Модель персонажа слишком высоко, блокируем прыжок
            }
            
            // Проверка: если персонаж уже в прыжке (движется вверх), блокируем повторный прыжок
            if (rb.linearVelocity.y > 0.1f)
            {
                return; // Персонаж уже в прыжке, блокируем повторный прыжок
            }
            
            // Максимально простая проверка: прыгать можно если не движемся сильно вверх
            if (rb.linearVelocity.y <= 2.0f)
            {
                // Множественная проверка - хотя бы одна должна сработать
                bool canJump = false;
                string jumpReason = "";
                
                // 1. Проверка через счетчик коллизий (самый надежный)
                if (groundCollisions > 0)
                {
                    canJump = true;
                    jumpReason = "коллизии";
                }
                // 2. Проверка через isGrounded
                else if (isGrounded)
                {
                    canJump = true;
                    jumpReason = "isGrounded";
                }
                // 3. Простая проверка через Raycast от центра
                else if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit1, 3f))
                {
                    canJump = true;
                    jumpReason = $"raycast центр (расстояние: {hit1.distance})";
                }
                // 4. Проверка через Raycast от нижней точки капсулы
                else if (capsuleCollider != null)
                {
                    Vector3 bottomPoint = transform.position + Vector3.down * (capsuleCollider.height / 2f);
                    if (Physics.Raycast(bottomPoint, Vector3.down, out RaycastHit hit2, 2f))
                    {
                        canJump = true;
                        jumpReason = $"raycast снизу (расстояние: {hit2.distance})";
                    }
                }
                
                if (canJump)
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    isGrounded = false;
                    // НЕ сбрасываем groundCollisions сразу - пусть OnCollisionExit обработает
            }
        }
    }
    
    /// <summary>
    /// Обработка движения персонажа
    /// </summary>
    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;
        
        // Проверить, используется ли джойстик на мобильном устройстве
        bool isMobile = PlatformDetector.IsMobile();
        
        // Проверить, есть ли ввод от джойстика
        bool hasJoystickInput = joystickInput.magnitude > 0.1f;
        
        // Проверить, есть ли ввод с клавиатуры
        bool hasKeyboardInput = (useSimpleControls && 
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
             Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))) ||
            (!useSimpleControls && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f));
        
        // На мобильных устройствах: если есть ввод от джойстика и useJoystick включен, использовать джойстик
        // Клавиатура имеет приоритет только если она используется активно
        if (isMobile && useJoystick && hasJoystickInput)
        {
            // Использовать ввод от джойстика (если нет активного ввода с клавиатуры)
            if (!hasKeyboardInput)
            {
                moveDirection = GetCameraRelativeDirection(joystickInput.x, joystickInput.y);
            }
        }
        // Если есть ввод от джойстика на любом устройстве (включая симулятор), но нет клавиатурного ввода
        else if (hasJoystickInput && !hasKeyboardInput)
        {
            moveDirection = GetCameraRelativeDirection(joystickInput.x, joystickInput.y);
            }
        
        // Если есть ввод с клавиатуры, использовать его (приоритет над джойстиком)
        if (hasKeyboardInput)
        {
            if (useSimpleControls)
        {
            // Простое управление для детей - движение по стрелкам или WASD
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveDirection += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveDirection += Vector3.back;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveDirection += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveDirection += Vector3.right;
            
            // Нормализовать для диагонального движения
            moveDirection = moveDirection.normalized;
            
            // Преобразовать в направление относительно камеры
            if (moveDirection.magnitude > 0.1f)
            {
                moveDirection = GetCameraRelativeDirection(moveDirection.x, moveDirection.z);
            }
        }
        else
        {
            // Стандартное управление
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveDirection = GetCameraRelativeDirection(horizontal, vertical);
        }
        }
        // Если на мобильном устройстве нет ни клавиатуры, ни джойстика (или джойстик отключен), не двигаться
        
        // Определить, бежит ли персонаж (по скорости движения)
        float moveMagnitude = moveDirection.magnitude;
        isRunning = moveMagnitude > runThreshold;
        currentSpeed = isRunning ? runSpeed : walkSpeed;
        
        if (moveDirection.magnitude > 0.1f)
        {
            // Повернуть персонажа в направлении движения (только по Y оси)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            float targetYRotation = targetRotation.eulerAngles.y;
            Quaternion fixedRotation = Quaternion.Euler(0, targetYRotation, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, fixedRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Если нет направления движения, остановить персонажа
            currentSpeed = 0f;
            isRunning = false;
            moveDirection = Vector3.zero; // Убедиться, что направление нулевое
        }
        
        // Сохранить направление движения для применения в FixedUpdate
        moveDirectionToApply = moveDirection;
    }
    
    private Vector3 moveDirectionToApply = Vector3.zero;
    
    /// <summary>
    /// Применить движение через Rigidbody
    /// </summary>
    private void ApplyMovement()
    {
        if (moveDirectionToApply.magnitude > 0.1f)
        {
            // Вычислить скорость движения
            Vector3 targetVelocity = moveDirectionToApply * currentSpeed;
            
            // Сохранить вертикальную скорость от физики (гравитация, прыжок)
            targetVelocity.y = rb.linearVelocity.y;
            
            // Проверить ограничение расстояния от точки спавна
            Vector3 currentPosition = transform.position;
            Vector3 horizontalPosition = new Vector3(currentPosition.x, 0, currentPosition.z);
            Vector3 horizontalSpawn = new Vector3(spawnPosition.x, 0, spawnPosition.z);
            float distanceFromSpawn = Vector3.Distance(horizontalPosition, horizontalSpawn);
            
            // Если персонаж слишком далеко от спавна, ограничить движение
            if (distanceFromSpawn >= maxDistanceFromSpawn)
            {
                // Вычислить направление от спавна к текущей позиции
                Vector3 directionFromSpawn = (horizontalPosition - horizontalSpawn).normalized;
                
                // Ограничить движение: разрешить только движение обратно к спавну
                Vector3 horizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);
                float velocityTowardsSpawn = Vector3.Dot(horizontalVelocity, -directionFromSpawn);
                
                // Если движение направлено от спавна, заблокировать его
                if (velocityTowardsSpawn < 0)
                {
                    // Разрешить только движение к спавну (проекция на направление к спавну)
                    horizontalVelocity = -directionFromSpawn * Mathf.Max(0, -velocityTowardsSpawn);
                    targetVelocity.x = horizontalVelocity.x;
                    targetVelocity.z = horizontalVelocity.z;
                }
            }
            
            // Применить скорость
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            // Остановить горизонтальное движение, сохранить вертикальное
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }
    
    /// <summary>
    /// Применить быструю гравитацию при падении для более быстрого падения
    /// Убирает зависание в верхней точке прыжка
    /// </summary>
    private void ApplyFastFall()
    {
        if (rb == null || isGrounded)
            return;
        
        // Если персонаж падает - применить сильную гравитацию
        if (rb.linearVelocity.y < 0)
        {
            // Применить дополнительную силу вниз для быстрого падения
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        // Если персонаж в верхней точке прыжка (малая скорость вверх) - ускорить падение
        else if (rb.linearVelocity.y > 0 && rb.linearVelocity.y < 2f)
        {
            // Применить дополнительную силу вниз, чтобы убрать зависание в верхней точке
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    
    /// <summary>
    /// Установить ввод от джойстика (вызывается из JoystickManager)
    /// </summary>
    public void SetJoystickInput(Vector2 input)
    {
        // Если игра не готова, игнорировать ввод джойстика
        if (!GameReadyManager.IsGameReady)
        {
            joystickInput = Vector2.zero;
            return;
        }
        
        // Если ввод очень маленький, установить в ноль для точной остановки
        if (input.magnitude < 0.01f)
        {
            joystickInput = Vector2.zero;
        }
        else
        {
            joystickInput = input;
        }
    }
    
    /// <summary>
    /// Получить направление движения относительно камеры
    /// </summary>
    private Vector3 GetCameraRelativeDirection(float horizontal, float vertical)
    {
        // Получить направление камеры (без наклона по Y)
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // Если камера не найдена, использовать мировые координаты
            return new Vector3(horizontal, 0, vertical).normalized;
        }
        
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Игнорировать Y компонент для движения по горизонтали
        cameraForward.y = 0;
        cameraRight.y = 0;
        
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        // Вычислить направление движения относительно камеры
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        return moveDirection;
    }
    
    /// <summary>
    /// Получить скорость движения персонажа (для других скриптов)
    /// </summary>
    public Vector3 GetVelocity()
    {
        if (rb != null)
        {
            return rb.linearVelocity;
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Проверить, движется ли персонаж
    /// </summary>
    public bool IsMoving()
    {
        if (rb != null)
        {
            return rb.linearVelocity.magnitude > 0.1f;
        }
        return false;
    }
    
    /// <summary>
    /// Обновить анимации персонажа
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null)
            return;
        
        // Проверить, движется ли персонаж
        bool isMoving = currentSpeed > 0.1f;
        bool isJumping = !isGrounded && (rb != null && rb.linearVelocity.y > 0.1f);
        
        // Установить основные параметры для аниматора
        try
        {
            // Параметр Speed - используется для определения ходьбы/бега
            animator.SetFloat("Speed", currentSpeed);
            
            // Параметр IsGrounded - для проверки нахождения на земле
            animator.SetBool("IsGrounded", isGrounded);
            
            // Обработка прыжка - триггер должен срабатывать только один раз за прыжок
            if (isJumping && !jumpTriggerSet)
            {
                // Прыжок - используем Trigger (только один раз)
                animator.SetTrigger("Jump");
                jumpTriggerSet = true;
            }
            else if (isGrounded && jumpTriggerSet)
            {
                // Сбросить флаг, когда персонаж приземлился
                jumpTriggerSet = false;
            }
            
            // Обработка движения и idle
            if (isMoving)
            {
                // Движение - используем параметр Speed для определения ходьбы/бега
                // Сбросить таймер idle при движении
                idleTimer = 0f;
                
                // Установить UseIdle1 в false при движении (чтобы не переключаться на idle)
                if (animator.parameters.Length > 0)
                {
                    bool hasUseIdle1 = false;
                    foreach (AnimatorControllerParameter param in animator.parameters)
                    {
                        if (param.name == "UseIdle1")
                        {
                            hasUseIdle1 = true;
                            break;
                        }
                    }
                    
                    if (hasUseIdle1)
                    {
                        animator.SetBool("UseIdle1", false);
                    }
                }
            }
            else
            {
                // Idle - переключение между idle_1 и idle_2
                idleTimer += Time.deltaTime;
                
                // Переключить idle анимации через заданный интервал
                if (idleTimer >= idleSwitchInterval)
                {
                    useIdle1 = !useIdle1;
                    idleTimer = 0f;
                    
                    // Установить параметр UseIdle1, если он существует
                    if (animator.parameters.Length > 0)
                    {
                        bool hasUseIdle1 = false;
                        foreach (AnimatorControllerParameter param in animator.parameters)
                        {
                            if (param.name == "UseIdle1")
                            {
                                hasUseIdle1 = true;
                                break;
                            }
                        }
                        
                        if (hasUseIdle1)
                        {
                            animator.SetBool("UseIdle1", useIdle1);
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
        }
    }
}
