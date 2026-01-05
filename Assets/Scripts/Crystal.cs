using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Настройки HP")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    private CrystalHealthBar healthBar;
    
    [Header("Коллайдер")]
    [SerializeField] private bool autoAddCollider = false; // Отключено по умолчанию, создается через UpdateCollider()
    [SerializeField] private ColliderType colliderType = ColliderType.MeshCollider;
    
    [Header("Эффект тряски при добыче")]
    [SerializeField] private float shakeIntensity = 0.05f; // Интенсивность тряски
    [SerializeField] private float shakeSpeed = 30f; // Скорость тряски (частота)
    
    private enum ColliderType
    {
        MeshCollider,
        BoxCollider
    }
    
    // Для эффекта тряски
    private Vector3 originalPosition; // Исходная позиция кристалла
    private float shakeTime = 0f; // Время для генерации случайных значений тряски
    private bool wasBeingMined = false; // Кэш состояния добычи для оптимизации
    private float lastMiningCheck = 0f; // Время последней проверки добычи
    private const float MINING_CHECK_INTERVAL = 0.1f; // Проверять добычу раз в 0.1 секунды вместо каждого кадра
    
    private void Start()
    {
        // Сохранить исходную позицию для эффекта тряски
        // Используем задержку, чтобы убедиться, что позиция установлена правильно после спавна
        if (originalPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            originalPosition = transform.position;
        }
        else if (originalPosition == Vector3.zero)
        {
            // Если позиция все еще нулевая, попробовать установить через небольшую задержку
            Invoke(nameof(InitializeOriginalPosition), 0.1f);
        }
        
        // Инициализировать HP из CrystalUpgradeSystem
        maxHealth = CrystalUpgradeSystem.GetCurrentMaxHealth();
        currentHealth = maxHealth;
        
        // Подписаться на событие обновления HP
        CrystalUpgradeSystem.OnHPUpgraded += OnHPUpgraded;
        
        // Зарегистрировать кристалл в CrystalManager
        CrystalManager.RegisterCrystal(this);
        
        // Коллайдеры отключены - не добавляем коллайдер
        
        // Найти health bar компонент
        healthBar = GetComponentInChildren<CrystalHealthBar>(true);
        if (healthBar != null)
        {
            healthBar.Initialize(transform);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// Инициализировать исходную позицию с задержкой (на случай, если позиция устанавливается после Start)
    /// </summary>
    private void InitializeOriginalPosition()
    {
        if (originalPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            originalPosition = transform.position;
            Debug.Log($"[Crystal] Исходная позиция инициализирована с задержкой: {originalPosition}");
        }
    }
    
    private void Update()
    {
        // Если исходная позиция еще не установлена, установить её
        if (originalPosition == Vector3.zero && transform.position != Vector3.zero)
        {
            originalPosition = transform.position;
        }
        
        // Оптимизация: проверять добычу не каждый кадр, а раз в 0.1 секунды
        bool isBeingMined = false;
        if (Time.time - lastMiningCheck >= MINING_CHECK_INTERVAL)
        {
            isBeingMined = IsBeingMined();
            wasBeingMined = isBeingMined;
            lastMiningCheck = Time.time;
        }
        else
        {
            // Использовать кэшированное значение между проверками
            isBeingMined = wasBeingMined;
        }
        
        // Применить эффект тряски, если кристалл добывается
        if (isBeingMined)
        {
            ApplyShakeEffect();
        }
        else
        {
            // Обновить исходную позицию только если кристалл действительно переместился (не из-за тряски)
            // Это предотвратит постоянный сброс позиции в нулевую
            // Оптимизация: проверять только если позиция изменилась
            if (originalPosition != Vector3.zero && Vector3.SqrMagnitude(transform.position - originalPosition) > 0.0001f)
            {
                // Вернуть к исходной позиции, если не добывается
                transform.position = originalPosition;
            }
            shakeTime = 0f;
        }
    }
    
    private void OnDestroy()
    {
        // Отписаться от события
        CrystalUpgradeSystem.OnHPUpgraded -= OnHPUpgraded;
        
        // Отменить регистрацию кристалла
        CrystalManager.UnregisterCrystal(this);
        
        // Уведомить CrystalSpawner о уничтожении кристалла для проверки респавна
        CrystalSpawner spawner = FindObjectOfType<CrystalSpawner>();
        if (spawner != null)
        {
            spawner.OnCrystalDestroyed(gameObject);
        }
    }
    
    /// <summary>
    /// Обработчик обновления HP от CrystalUpgradeSystem
    /// </summary>
    private void OnHPUpgraded()
    {
        float newMaxHealth = CrystalUpgradeSystem.GetCurrentMaxHealth();
        SetMaxHealth(newMaxHealth);
    }
    
    /// <summary>
    /// Установить максимальное HP и обновить текущее HP пропорционально
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        if (newMaxHealth <= 0)
        {
            return;
        }
        
        // Сохранить процент текущего HP
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 1f;
        
        // Обновить максимальное HP
        maxHealth = newMaxHealth;
        
        // Обновить текущее HP пропорционально
        currentHealth = maxHealth * healthPercent;
        
        // Обновить health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// Добавить коллайдер к кристаллу
    /// </summary>
    private void AddCollider()
    {
        // Проверить, есть ли уже коллайдер
        if (GetComponent<Collider>() != null)
        {
            return;
        }
        
        // Найти MeshRenderer для определения размеров
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            return;
        }
        
        Collider collider = null;
        
        if (colliderType == ColliderType.MeshCollider)
        {
            // Попытаться добавить MeshCollider
            MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
                MeshCollider meshCollider = collider as MeshCollider;
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true; // Convex для лучшей производительности
                
                // MeshCollider автоматически учитывает масштаб через sharedMesh
                // Но нужно убедиться, что он правильно масштабируется
            }
        }
        
        // Если MeshCollider не удалось создать, использовать BoxCollider
        if (collider == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            
            // Получить bounds с учетом текущего масштаба (bounds уже учитывает масштаб transform)
            Bounds bounds = meshRenderer.bounds;
            
            // Преобразовать мировые координаты bounds в локальные координаты transform
            // Центр bounds в локальных координатах
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            
            // Размер bounds в локальных координатах
            // bounds.size - это размер в мировых координатах, нужно преобразовать в локальные
            // Используем Transform.InverseTransformVector для правильного преобразования с учетом масштаба
            Vector3 worldSize = bounds.size;
            Vector3 localSize = new Vector3(
                worldSize.x / transform.lossyScale.x,
                worldSize.y / transform.lossyScale.y,
                worldSize.z / transform.lossyScale.z
            );
            
            boxCollider.center = localCenter;
            boxCollider.size = localSize;
            
            collider = boxCollider;
        }
        
        // Установить как триггер для взаимодействия (если нужно)
        // collider.isTrigger = true;
    }
    
    /// <summary>
    /// Обновить коллайдер после изменения масштаба (вызывается извне)
    /// </summary>
    public void UpdateCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
        {
            DestroyImmediate(existingCollider);
        }
        AddCollider();
    }
    
    /// <summary>
    /// Нанести урон кристаллу
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Обновить health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }
    
    /// <summary>
    /// Восстановить HP
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Обновить health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// Обработка уничтожения кристалла
    /// </summary>
    private void OnDestroyed()
    {
        // Здесь можно добавить эффекты, звуки и т.д.
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Получить текущее HP
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Получить максимальное HP
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Проверить, жив ли кристалл
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    /// <summary>
    /// Проверить, добывается ли кристалл сейчас (только когда питомец действительно добывает, а не просто идет к кристаллу)
    /// </summary>
    private bool IsBeingMined()
    {
        if (!IsAlive())
        {
            return false;
        }
        
        // Проверить через CrystalManager, что кристалл занят
        if (!CrystalManager.IsCrystalOccupied(this))
        {
            return false;
        }
        
        // Получить питомца, который добывает этот кристалл
        PetBehavior miningPet = CrystalManager.GetPetMiningCrystal(this);
        if (miningPet == null)
        {
            return false;
        }
        
        // Проверить, что питомец действительно добывает (isMining = true)
        // Используем рефлексию или публичный метод для проверки состояния добычи
        return miningPet.IsMining();
    }
    
    /// <summary>
    /// Применить эффект тряски к кристаллу
    /// </summary>
    private void ApplyShakeEffect()
    {
        // Увеличить время для генерации случайных значений
        shakeTime += Time.deltaTime * shakeSpeed;
        
        // Генерировать случайные смещения в трех направлениях
        float offsetX = Mathf.Sin(shakeTime * 1.3f) * shakeIntensity;
        float offsetY = Mathf.Cos(shakeTime * 1.7f) * shakeIntensity;
        float offsetZ = Mathf.Sin(shakeTime * 1.1f) * shakeIntensity;
        
        // Применить смещение к исходной позиции
        Vector3 shakeOffset = new Vector3(offsetX, offsetY, offsetZ);
        transform.position = originalPosition + shakeOffset;
    }
}

