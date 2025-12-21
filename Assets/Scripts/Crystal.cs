using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Настройки HP")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("UI")]
    [SerializeField] private CrystalHealthBar healthBar;
    
    [Header("Коллайдер")]
    [SerializeField] private bool autoAddCollider = false; // Отключено по умолчанию, создается через UpdateCollider()
    [SerializeField] private ColliderType colliderType = ColliderType.MeshCollider;
    
    private enum ColliderType
    {
        MeshCollider,
        BoxCollider
    }
    
    private void Start()
    {
        // Инициализировать HP из CrystalUpgradeSystem
        maxHealth = CrystalUpgradeSystem.GetCurrentMaxHealth();
        currentHealth = maxHealth;
        
        // Подписаться на событие обновления HP
        CrystalUpgradeSystem.OnHPUpgraded += OnHPUpgraded;
        
        // Зарегистрировать кристалл в CrystalManager
        CrystalManager.RegisterCrystal(this);
        
        // Коллайдеры отключены - не добавляем коллайдер
        
        // Создать health bar
        try
        {
            CreateHealthBar();
            
            // Обновить health bar с начальным HP
            UpdateHealthBar();
        }
        catch (System.Exception e)
        {
        }
    }
    
    private void OnDestroy()
    {
        // Отписаться от события
        CrystalUpgradeSystem.OnHPUpgraded -= OnHPUpgraded;
        
        // Отменить регистрацию кристалла
        CrystalManager.UnregisterCrystal(this);
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
        UpdateHealthBar();
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
    /// Создать health bar для кристалла
    /// </summary>
    private void CreateHealthBar()
    {
        // Создать GameObject для health bar
        GameObject healthBarObj = new GameObject("CrystalHealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.transform.localRotation = Quaternion.identity;
        
        // Добавить компонент CrystalHealthBar
        healthBar = healthBarObj.AddComponent<CrystalHealthBar>();
        
        // Инициализировать health bar
        healthBar.Initialize(transform);
    }
    
    /// <summary>
    /// Обновить отображение health bar
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// Нанести урон кристаллу
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
        
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
        UpdateHealthBar();
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
}

