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
    [SerializeField] private float commonChance = 0.7f;      // 70% - редкий
    [SerializeField] private float epicChance = 0.2f;       // 20% - эпический
    [SerializeField] private float legendaryChance = 0.1f;  // 10% - легендарный
    
    private PlayerController playerController;
    private GameObject currentEgg;
    private PetRarity currentRarity;
    
    /// <summary>
    /// Проверить, идет ли сейчас вылупление
    /// </summary>
    public bool IsHatching()
    {
        return currentEgg != null;
    }
    
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
        
        // Проверить и инициализировать вероятности, если они не установлены
        ValidateRarityChances();
    }
    
    /// <summary>
    /// Проверить и инициализировать вероятности редкости
    /// </summary>
    private void ValidateRarityChances()
    {
        // Проверка выполняется автоматически в DeterminePetRarity
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
        PetRarity determinedRarity = DeterminePetRarity();
        currentRarity = determinedRarity;
        
        // Заспавнить яйцо рядом с игроком
        SpawnEgg();
    }
    
    /// <summary>
    /// Определить редкость питомца на основе вероятностей
    /// </summary>
    private PetRarity DeterminePetRarity()
    {
        // Читаем значения из SerializeField
        float commonWeight = commonChance;
        float epicWeight = epicChance;
        float legendaryWeight = legendaryChance;
        
        // Вычисляем общий вес
        float totalWeight = commonWeight + epicWeight + legendaryWeight;
        
        // Если сумма равна 0 или очень мала, используем значения по умолчанию
        if (totalWeight <= 0.001f)
        {
            commonWeight = 0.7f;
            epicWeight = 0.2f;
            legendaryWeight = 0.1f;
            totalWeight = 1.0f;
        }
        
        // Нормализуем веса (на случай, если сумма не равна 1.0)
        float normalizedCommon = commonWeight / totalWeight;
        float normalizedEpic = epicWeight / totalWeight;
        
        // Генерируем случайное число от 0.0 до 1.0
        float randomRoll = Random.value;
        
        // Определяем редкость на основе кумулятивных вероятностей
        float commonThreshold = normalizedCommon;
        float epicThreshold = normalizedCommon + normalizedEpic;
        
        if (randomRoll < commonThreshold)
        {
            return PetRarity.Common;
        }
        else if (randomRoll < epicThreshold)
        {
            return PetRarity.Epic;
        }
        else
        {
            return PetRarity.Legendary;
        }
    }
    
    /// <summary>
    /// Заспавнить яйцо перед игроком
    /// </summary>
    public void SpawnEgg()
    {
        // Проверить, что playerController инициализирован
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                return;
            }
        }
        
        // Проверить, что eggPrefab загружен
        if (eggPrefab == null)
        {
            LoadEggModel();
            if (eggPrefab == null)
            {
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
        spawnPosition.y = playerPosition.y + 0.5f;
        
        Quaternion eggRotation = Quaternion.Euler(-90f, 0f, 0f);
        
        currentEgg = Instantiate(eggPrefab, spawnPosition, eggRotation);
        currentEgg.name = "Egg_Hatching";
        
        if (currentEgg == null)
        {
            return;
        }
        
        // Проверить наличие Renderer для видимости
        Renderer[] renderers = currentEgg.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
        
        // Увеличить яйцо в 1.5 раза
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
        
        // Использовать редкость, которая была определена при старте вылупления
        PetRarity finalRarity = currentRarity;
        
        // Создать PetData с редкостью
        string rarityName = GetRarityName(finalRarity);
        string petName = $"Питомец {rarityName}";
        string modelPath = GetPetModelPath(finalRarity);
        
        PetData newPet = new PetData(
            finalRarity,
            petName,
            Random.Range(1000, 9999),
            1f,
            GetRarityColor(finalRarity)
        );
        
        newPet.petModelPath = modelPath;
        
        // Добавить в инвентарь
        if (PetInventory.Instance != null)
        {
            PetInventory.Instance.AddPet(newPet);
            
            // Сбросить флаг покупки яйца, так как питомец успешно добавлен
            PlayerPrefs.SetInt("EggPurchased", 0);
            PlayerPrefs.Save();
        }
        
        // Показать уведомление
        ShowPetNotification(finalRarity);
        
        // Обновить UI инвентаря, если модальное окно открыто
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            var updateMethod = typeof(InventoryUI).GetMethod("UpdateUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateMethod != null)
            {
                updateMethod.Invoke(inventoryUI, null);
            }
            
            var updateModalMethod = typeof(InventoryUI).GetMethod("UpdateModalUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateModalMethod != null)
            {
                updateModalMethod.Invoke(inventoryUI, null);
            }
            
            // Разблокировать кнопку покупки яйца после завершения вылупления
            var updateButtonMethod = typeof(InventoryUI).GetMethod("UpdateBuyEggButtonAfterHatching", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateButtonMethod != null)
            {
                updateButtonMethod.Invoke(inventoryUI, null);
            }
        }
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

