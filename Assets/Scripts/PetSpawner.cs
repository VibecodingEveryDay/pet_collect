using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Менеджер для спавна и деспавна питомцев в мире
/// </summary>
public class PetSpawner : MonoBehaviour
{
    private static PetSpawner _instance;
    private Dictionary<PetData, GameObject> spawnedPets = new Dictionary<PetData, GameObject>();
    
    [Header("Настройки спавна")]
    [SerializeField] private float spawnDistanceFromPlayer = 3f;
    [SerializeField] private float fixedYPosition = 0f; // Фиксированная позиция Y для питомцев
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static PetSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetSpawner>();
                if (_instance == null)
                {
                    GameObject spawnerObject = new GameObject("PetSpawner");
                    _instance = spawnerObject.AddComponent<PetSpawner>();
                    DontDestroyOnLoad(spawnerObject);
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
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Заспавнить питомца в мире
    /// </summary>
    public void SpawnPetInWorld(PetData petData)
    {
        Debug.Log($"[PetSpawner] Начало спавна питомца: {petData?.petName ?? "null"}");
        
        if (petData == null)
        {
            Debug.LogError("[PetSpawner] PetData is null!");
            return;
        }
        
        // Проверить, не заспавнен ли уже
        if (spawnedPets.ContainsKey(petData) && spawnedPets[petData] != null)
        {
            Debug.LogWarning($"[PetSpawner] Питомец {petData.petName} уже заспавнен в мире!");
            return;
        }
        
        // Найти игрока
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[PetSpawner] PlayerController не найден! Невозможно заспавнить питомца.");
            return;
        }
        
        Debug.Log($"[PetSpawner] PlayerController найден, позиция: {playerController.transform.position}");
        
        // Загрузить модель питомца
        GameObject petPrefab = LoadPetModel(petData);
        if (petPrefab == null)
        {
            Debug.LogError($"[PetSpawner] Не удалось загрузить модель питомца: {petData.petModelPath}");
            return;
        }
        
        Debug.Log($"[PetSpawner] Модель питомца загружена: {petPrefab.name}");
        
        // Вычислить позицию спавна рядом с игроком
        Vector3 playerPosition = playerController.transform.position;
        Vector3 playerForward = playerController.transform.forward;
        Vector3 spawnPosition = playerPosition + playerForward * spawnDistanceFromPlayer;
        
        // Временная позиция Y (будет пересчитана по редкости)
        spawnPosition.y = fixedYPosition;
        
        // Спавнить питомца с правильным поворотом
        // GLB модели часто требуют поворот на 90 градусов по оси X (если модель лежит на боку)
        Quaternion spawnRotation = Quaternion.Euler(90, 0, 0); // Поворот на 90 градусов по X для исправления ориентации
        
        GameObject petInstance = Instantiate(petPrefab, spawnPosition, spawnRotation);
        petInstance.name = $"Pet_{petData.petName}_{petData.petID}";
        
        // Убедиться, что объект активен
        if (!petInstance.activeSelf)
        {
            petInstance.SetActive(true);
            Debug.LogWarning($"[PetSpawner] Питомец {petData.petName} был неактивен после инстанцирования, активирован принудительно");
        }
        
        // Активировать все дочерние объекты
        foreach (Transform child in petInstance.GetComponentsInChildren<Transform>(true))
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
        }
        
        // Убедиться, что все рендереры включены
        Renderer[] allRenderers = petInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null && !renderer.enabled)
            {
                renderer.enabled = true;
                Debug.LogWarning($"[PetSpawner] Рендерер {renderer.name} был отключен, включен принудительно");
            }
        }
        
        // Вычислить Y позицию в зависимости от редкости питомца и модели
        float yPosition = GetYPositionByRarity(petData);
        
        // Установить позицию Y
        Vector3 pos = petInstance.transform.position;
        pos.y = yPosition;
        petInstance.transform.position = pos;
        
        // Повернуть питомца к игроку (добавляем поворот по Y к уже повернутой модели)
        // Разворачиваем на 180 градусов, чтобы питомец смотрел лицом, а не спиной
        Vector3 directionToPlayer = (playerPosition - petInstance.transform.position).normalized;
        directionToPlayer.y = 0; // Только горизонтальный поворот
        if (directionToPlayer != Vector3.zero)
        {
            float yRotation = Quaternion.LookRotation(directionToPlayer).eulerAngles.y + 180f;
            petInstance.transform.rotation = Quaternion.Euler(90, yRotation, 0);
        }
        
        // Добавить компонент PetBehavior
        PetBehavior petBehavior = petInstance.AddComponent<PetBehavior>();
        petBehavior.Initialize(petData);
        
        // Установить фиксированную позицию Y из PetSpawner в PetBehavior
        petBehavior.SetFixedYPosition(yPosition);
        
        // Сохранить ссылки
        petData.worldInstance = petInstance;
        spawnedPets[petData] = petInstance;
        
        // Проверить, что объект действительно создан и активен
        if (petInstance != null && petInstance.activeInHierarchy)
        {
            Debug.Log($"[PetSpawner] Питомец {petData.petName} успешно заспавнен в мире! Позиция: {petInstance.transform.position}, Активен: {petInstance.activeSelf}, В иерархии: {petInstance.activeInHierarchy}");
            
            // Проверить наличие рендереров
            Renderer[] renderers = petInstance.GetComponentsInChildren<Renderer>();
            Debug.Log($"[PetSpawner] Найдено рендереров: {renderers.Length}");
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    Debug.Log($"[PetSpawner] Рендерер: {r.name}, Включен: {r.enabled}, Видимый: {r.isVisible}");
                }
            }
        }
        else
        {
            Debug.LogError($"[PetSpawner] ОШИБКА: Питомец {petData.petName} создан, но не активен! petInstance: {petInstance}, activeSelf: {petInstance?.activeSelf}, activeInHierarchy: {petInstance?.activeInHierarchy}");
        }
    }
    
    /// <summary>
    /// Удалить питомца из мира
    /// </summary>
    public void DespawnPet(PetData petData)
    {
        if (petData == null)
        {
            return;
        }
        
        if (spawnedPets.ContainsKey(petData))
        {
            GameObject petInstance = spawnedPets[petData];
            if (petInstance != null)
            {
                Destroy(petInstance);
            }
            spawnedPets.Remove(petData);
            petData.worldInstance = null;
            
            Debug.Log($"Питомец {petData.petName} удален из мира!");
        }
    }
    
    /// <summary>
    /// Загрузить модель питомца
    /// </summary>
    private GameObject LoadPetModel(PetData petData)
    {
        if (string.IsNullOrEmpty(petData.petModelPath))
        {
            Debug.LogError("PetModelPath не указан!");
            return null;
        }
        
        GameObject petPrefab = null;
        
#if UNITY_EDITOR
        // В редакторе используем AssetDatabase
        petPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(petData.petModelPath);
        
        // Если не найден, попробовать поиск по имени файла
        if (petPrefab == null)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(petData.petModelPath);
            string[] guids = AssetDatabase.FindAssets($"{fileName} t:GameObject");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                petPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
        }
#else
        // В билде используем Resources
        string resourcePath = petData.petModelPath.Replace("Assets/Assets/Pets/", "").Replace(".glb", "");
        petPrefab = Resources.Load<GameObject>($"Pets/{resourcePath}");
#endif
        
        return petPrefab;
    }
    
    /// <summary>
    /// Получить все заспавненные питомцы
    /// </summary>
    public Dictionary<PetData, GameObject> GetSpawnedPets()
    {
        // Очистить null ссылки
        List<PetData> keysToRemove = new List<PetData>();
        foreach (var kvp in spawnedPets)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            spawnedPets.Remove(key);
        }
        
        return spawnedPets;
    }
    
    /// <summary>
    /// Получить список активных питомцев (PetData)
    /// </summary>
    public List<PetData> GetActivePetsList()
    {
        List<PetData> activePets = new List<PetData>();
        Dictionary<PetData, GameObject> spawnedPets = GetSpawnedPets();
        
        foreach (var kvp in spawnedPets)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                activePets.Add(kvp.Key);
            }
        }
        
        return activePets;
    }
    
    /// <summary>
    /// Проверить, заспавнен ли питомец
    /// </summary>
    public bool IsPetSpawned(PetData petData)
    {
        if (petData == null)
        {
            return false;
        }
        
        return spawnedPets.ContainsKey(petData) && spawnedPets[petData] != null;
    }
    
    /// <summary>
    /// Получить Y позицию в зависимости от редкости питомца и модели
    /// </summary>
    private float GetYPositionByRarity(PetData petData)
    {
        float yOffset = 0f;
        
        // Проверить путь модели для определения конкретной модели
        bool isRare2OrRare3 = false;
        bool isLegendary = false;
        
        if (!string.IsNullOrEmpty(petData.petModelPath))
        {
            string modelPath = petData.petModelPath.ToLower();
            isRare2OrRare3 = modelPath.Contains("rare2") || modelPath.Contains("rare3");
            isLegendary = modelPath.Contains("legendary");
        }
        
        // rare2, rare3 и legendary на 0.5 единицы ниже базовой высоты
        if (isRare2OrRare3 || isLegendary)
        {
            yOffset = -0.5f; // На 0.5 единицы ниже базовой высоты (на 4 единицы выше остальных)
        }
        else
        {
            // epic и остальные (rare1, rare4) на 4.5 координаты ниже
            switch (petData.rarity)
            {
                case PetRarity.Common:    // rare1, rare4
                case PetRarity.Epic:      // epic
                    yOffset = -4.5f;
                    break;
                case PetRarity.Legendary: // rare4 (если не legendary модель)
                    yOffset = -4.5f;
                    break;
            }
        }
        
        return fixedYPosition + yOffset;
    }
}

