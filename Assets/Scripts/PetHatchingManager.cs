using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PetRarity
{
    Common,      // 70% - голубой
    Epic,        // 20% - фиолетовый
    Legendary    // 10% - золотой
}

public class PetHatchingManager : MonoBehaviour
{
    [Header("Настройки яйца")]
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private float spawnDistanceFromPlayer = 2.5f;
    
    [Header("Система редкости")]
    [SerializeField] private float commonChance = 0.7f;      // 70%
    [SerializeField] private float epicChance = 0.2f;        // 20%
    [SerializeField] private float legendaryChance = 0.1f;   // 10% (используется в DeterminePetRarity)
    
    private PlayerController playerController;
    private GameObject currentEgg;
    private PetRarity currentRarity;
    
    private void Awake()
    {
        // Найти игрока
        playerController = FindObjectOfType<PlayerController>();
    }
    
    private void Start()
    {
        // Загрузить модель яйца, если не назначена
        if (eggPrefab == null)
        {
            LoadEggModel();
        }
    }
    
    /// <summary>
    /// Загрузить модель яйца из GLB файла
    /// </summary>
    private void LoadEggModel()
    {
#if UNITY_EDITOR
        // Попытка 1: Загрузить напрямую по пути
        string eggPath = "Assets/assets/Egg/egg.glb";
        eggPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(eggPath);
        
        // Попытка 2: Поиск по имени в проекте
        if (eggPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("egg t:GameObject");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                eggPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
        }
        
        // Попытка 3: Загрузить из Resources
        if (eggPrefab == null)
        {
            eggPrefab = Resources.Load<GameObject>("Egg/egg");
        }
        
#else
        // В Build используем Resources
        eggPrefab = Resources.Load<GameObject>("Egg/egg");
#endif
    }
    
    /// <summary>
    /// Начать процесс вылупления яйца
    /// </summary>
    public void StartHatching()
    {
        // Попытаться загрузить модель, если она не назначена
        if (eggPrefab == null)
        {
            LoadEggModel();
        }
        
        if (eggPrefab == null)
        {
            return;
        }
        
        if (playerController == null)
        {
            return;
        }
        
        // Определить редкость питомца
        currentRarity = DeterminePetRarity();
        
        // Заспавнить яйцо рядом с игроком
        SpawnEgg();
    }
    
    /// <summary>
    /// Определить редкость питомца на основе вероятностей
    /// </summary>
    private PetRarity DeterminePetRarity()
    {
        float randomValue = Random.value;
        
        if (randomValue < commonChance)
        {
            return PetRarity.Common; // 0.0 - 0.7 (70%)
        }
        else if (randomValue < commonChance + epicChance)
        {
            return PetRarity.Epic; // 0.7 - 0.9 (20%)
        }
        else
        {
            return PetRarity.Legendary; // 0.9 - 1.0 (10%)
        }
    }
    
    /// <summary>
    /// Заспавнить яйцо перед игроком
    /// </summary>
    public void SpawnEgg()
    {
        Debug.Log("SpawnEgg() вызван");
        
        // Проверить, что playerController инициализирован
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController не найден! Невозможно заспавнить яйцо.");
                return;
            }
        }
        
        // Проверить, что eggPrefab загружен
        if (eggPrefab == null)
        {
            Debug.LogWarning("eggPrefab не назначен, пытаюсь загрузить...");
            LoadEggModel();
            if (eggPrefab == null)
            {
                Debug.LogError("Не удалось загрузить eggPrefab! Невозможно заспавнить яйцо.");
                return;
            }
        }
        
        if (currentEgg != null)
        {
            Destroy(currentEgg);
        }
        
        // Вычислить позицию перед игроком
        Vector3 playerPosition = playerController.transform.position;
        Vector3 playerForward = playerController.transform.forward;
        Vector3 spawnPosition = playerPosition + playerForward * spawnDistanceFromPlayer;
        spawnPosition.y = playerPosition.y + 0.5f; // Поднять на 0.5f выше уровня игрока
        
        // Повернуть яйцо вертикально (поворот на 90 градусов по оси X)
        // Если модель уже вертикальная, можно попробовать без поворота или с другим углом
        Quaternion eggRotation = Quaternion.Euler(-90f, 0f, 0f);
        
        // Альтернативный вариант: если модель уже вертикальная, используем identity
        // Quaternion eggRotation = Quaternion.identity;
        
        Debug.Log($"Спавню яйцо в позиции: {spawnPosition}");
        currentEgg = Instantiate(eggPrefab, spawnPosition, eggRotation);
        currentEgg.name = "Egg_Hatching";
        Debug.Log("Яйцо успешно заспавнено!");
        
        // Проверить, что яйцо создано
        if (currentEgg == null)
        {
            return;
        }
        
        // Проверить наличие Renderer для видимости
        Renderer[] renderers = currentEgg.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // Попробовать найти MeshRenderer или SkinnedMeshRenderer
            MeshRenderer meshRenderer = currentEgg.GetComponentInChildren<MeshRenderer>();
            SkinnedMeshRenderer skinnedRenderer = currentEgg.GetComponentInChildren<SkinnedMeshRenderer>();
        }
        else
        {
            foreach (Renderer renderer in renderers)
            {
                // Убедиться, что Renderer включен
                renderer.enabled = true;
            }
        }
        
        // Проверить MeshFilter
        MeshFilter[] meshFilters = currentEgg.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
        }
        
        // Увеличить яйцо в 1.5 раза (умножить текущий масштаб на 1.5)
        Vector3 originalScale = currentEgg.transform.localScale;
        currentEgg.transform.localScale = originalScale * 1.5f;
        
        // Убедиться, что объект активен
        currentEgg.SetActive(true);
        
        // Добавить компонент анимации вылупления
        EggHatchingAnimation hatchingAnimation = currentEgg.AddComponent<EggHatchingAnimation>();
        hatchingAnimation.Initialize(currentRarity, OnHatchingComplete);
    }
    
    /// <summary>
    /// Callback при завершении анимации вылупления
    /// </summary>
    private void OnHatchingComplete(PetRarity rarity)
    {
        // Уничтожить яйцо
        if (currentEgg != null)
        {
            Destroy(currentEgg);
            currentEgg = null;
        }
        
        // Создать PetData с редкостью
        string rarityName = GetRarityName(rarity);
        string petName = $"Питомец {rarityName}";
        
        // Определить путь к модели
        string modelPath = GetPetModelPath(rarity);
        
        PetData newPet = new PetData(
            rarity,
            petName,
            Random.Range(1000, 9999),
            1f,
            GetRarityColor(rarity)
        );
        
        newPet.petModelPath = modelPath;
        
        // Добавить в инвентарь (НЕ добавлять в активные автоматически)
        if (PetInventory.Instance != null)
        {
            PetInventory.Instance.AddPet(newPet);
        }
        
        // Показать уведомление
        ShowPetNotification(rarity);
        
        // Обновить UI инвентаря, если модальное окно открыто
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            // Обновить UI через рефлексию или публичный метод
            var updateMethod = typeof(InventoryUI).GetMethod("UpdateUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateMethod != null)
            {
                updateMethod.Invoke(inventoryUI, null);
            }
            
            // Обновить модальное окно, если оно открыто
            var updateModalMethod = typeof(InventoryUI).GetMethod("UpdateModalUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateModalMethod != null)
            {
                updateModalMethod.Invoke(inventoryUI, null);
            }
        }
        
        Debug.Log($"Питомец {rarityName} создан и добавлен в инвентарь!");
    }
    
    /// <summary>
    /// Получить путь к модели питомца по редкости
    /// </summary>
    private string GetPetModelPath(PetRarity rarity)
    {
        switch (rarity)
        {
            case PetRarity.Common:
                // Случайный выбор из rare1-4
                int randomIndex = Random.Range(1, 5);
                return $"Assets/Assets/Pets/rare{randomIndex}.glb";
            case PetRarity.Epic:
                return "Assets/Assets/Pets/epic.glb";
            case PetRarity.Legendary:
                return "Assets/Assets/Pets/legendary.glb";
            default:
                return "Assets/Assets/Pets/rare1.glb";
        }
    }
    
    /// <summary>
    /// Показать уведомление о выигрыше питомца
    /// </summary>
    private void ShowPetNotification(PetRarity rarity)
    {
        PetNotificationUI.ShowNotification(rarity);
    }
    
    /// <summary>
    /// Получить цвет редкости
    /// </summary>
    public static Color GetRarityColor(PetRarity rarity)
    {
        switch (rarity)
        {
            case PetRarity.Common:
                return Color.cyan; // Голубой
            case PetRarity.Epic:
                return new Color(0.5f, 0f, 1f); // Фиолетовый
            case PetRarity.Legendary:
                return new Color(1f, 0.84f, 0f); // Золотой
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Получить название редкости на русском
    /// </summary>
    public static string GetRarityName(PetRarity rarity)
    {
        switch (rarity)
        {
            case PetRarity.Common:
                return "редкого";
            case PetRarity.Epic:
                return "эпического";
            case PetRarity.Legendary:
                return "легендарного";
            default:
                return "неизвестного";
        }
    }
}

