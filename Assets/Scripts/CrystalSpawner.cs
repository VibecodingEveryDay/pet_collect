using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrystalSpawner : MonoBehaviour
{
    [Header("Модели кристаллов")]
    [Tooltip("Назначьте 5 моделей кристаллов вручную или они будут загружены автоматически")]
    [SerializeField] private GameObject[] crystalPrefabs = new GameObject[5];
    
    [Header("Настройки спавна")]
    [SerializeField] private int crystalCount = 10;
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Область спавна")]
    [SerializeField] private SpawnAreaType spawnAreaType = SpawnAreaType.Bounds;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnBounds = new Vector3(20f, 0f, 20f); // Размер области спавна для типа Bounds
    [SerializeField] private float spawnRadius = 10f; // Радиус для SpawnAreaType.Radius ИЛИ размер для Bounds (если useRadiusForBounds = true)
    [SerializeField] private bool useRadiusForBounds = false; // Если true, использовать spawnRadius как размер области для типа Bounds (создаст квадратную область spawnRadius x spawnRadius)
    [SerializeField] private bool useUniformRadiusDistribution = false; // Если true - равномерно по радиусу (больше точек ближе к центру), если false - равномерно по площади
    
    [Header("Проверка поверхности")]
    [SerializeField] private bool checkGround = false; // По умолчанию отключено для упрощения
    [SerializeField] private float groundCheckDistance = 10f;
    [SerializeField] private LayerMask groundLayer = -1; // Все слои по умолчанию
    
    [Header("Минимальное расстояние между кристаллами")]
    [SerializeField] private float minDistanceBetweenCrystals = 1f; // Рекомендуемое значение: 1-2 для области 20x20
    [SerializeField] private bool ignoreMinDistance = false; // Временно отключить проверку расстояния для тестирования
    
    [Header("Запрещенная зона")]
    [SerializeField] private float playerSpawnRadius = 30f; // Радиус вокруг центра спавна, где кристаллы не должны появляться (зона появления игрока)
    [SerializeField] private bool excludePlayerSpawnArea = true; // Исключить зону появления игрока из спавна кристаллов
    
    private enum SpawnAreaType
    {
        Bounds,   // Прямоугольная область
        Radius    // Круглая область
    }
    
    private List<GameObject> spawnedCrystals = new List<GameObject>();
    private int initialCrystalCount = 0; // Изначальное количество кристаллов
    private bool hasCheckedRespawn = false; // Флаг, чтобы не спавнить несколько раз подряд
    
    private void Start()
    {
        // Загрузить модели кристаллов, если они не назначены
        LoadCrystalPrefabs();
        
        // Спавнить кристаллы при старте
        if (spawnOnStart)
        {
            SpawnCrystals();
        }
    }
    
    /// <summary>
    /// Очистить кристаллы при уничтожении объекта
    /// </summary>
    private void OnDestroy()
    {
        // Очистить все кристаллы при уничтожении спавнера
        foreach (GameObject crystal in spawnedCrystals)
        {
            if (crystal != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(crystal);
#else
                Destroy(crystal);
#endif
            }
        }
        spawnedCrystals.Clear();
    }
    
    /// <summary>
    /// Очистить кристаллы при выходе из приложения
    /// </summary>
    private void OnApplicationQuit()
    {
        // Очистить все кристаллы при выходе из приложения
        // Создаем копию списка, чтобы избежать ошибки изменения коллекции во время перечисления
        List<GameObject> crystalsToDestroy = new List<GameObject>(spawnedCrystals);
        
        foreach (GameObject crystal in crystalsToDestroy)
        {
            if (crystal != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(crystal);
#else
                Destroy(crystal);
#endif
            }
        }
        spawnedCrystals.Clear();
    }
    
    /// <summary>
    /// Загрузить модели кристаллов автоматически
    /// </summary>
    private void LoadCrystalPrefabs()
    {
        // Проверить, все ли модели назначены
        bool allAssigned = true;
        for (int i = 0; i < crystalPrefabs.Length; i++)
        {
            if (crystalPrefabs[i] == null)
            {
                allAssigned = false;
                break;
            }
        }
        
        if (allAssigned)
        {
            return;
        }
        
        // Попытаться загрузить префабы кристаллов
        string[] crystalNames = { "Crystal1", "Crystal2", "Crystal3", "Crystal4", "Crystal5" };
        
#if UNITY_EDITOR
        // В редакторе пробуем разные пути для префабов
        for (int i = 0; i < crystalPrefabs.Length && i < crystalNames.Length; i++)
        {
            if (crystalPrefabs[i] == null)
            {
                GameObject loaded = null;
                
                // Попытка 1: Загрузить префаб из Assets/Assets/Crystals/
                string[] possiblePaths = {
                    $"Assets/Assets/Crystals/{crystalNames[i]}.prefab",
                    $"Assets/Assets/Crystals/{crystalNames[i]}",
                    $"Assets/Crystals/{crystalNames[i]}.prefab",
                    $"Assets/Crystals/{crystalNames[i]}"
                };
                
                foreach (string path in possiblePaths)
                {
                    loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (loaded != null)
                    {
                        break;
                    }
                }
                
                // Попытка 2: Поиск по имени в проекте (только префабы)
                if (loaded == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"{crystalNames[i]} t:Prefab");
                    if (guids.Length > 0)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    }
                }
                
                // Попытка 3: Загрузить из Resources
                if (loaded == null)
                {
                    loaded = Resources.Load<GameObject>($"Crystals/{crystalNames[i]}");
                }
                
                if (loaded != null)
                {
                    crystalPrefabs[i] = loaded;
                }
            }
        }
#else
        // В билде пробуем только Resources
        for (int i = 0; i < crystalPrefabs.Length && i < crystalNames.Length; i++)
        {
            if (crystalPrefabs[i] == null)
            {
                GameObject loaded = Resources.Load<GameObject>($"Crystals/{crystalNames[i]}");
                if (loaded != null)
                {
                    crystalPrefabs[i] = loaded;
                }
            }
        }
#endif
        
        // Проверить, что хотя бы одна модель загружена
        int loadedCount = 0;
        foreach (GameObject prefab in crystalPrefabs)
        {
            if (prefab != null)
            {
                loadedCount++;
            }
        }
        
        if (loadedCount == 0)
        {
        }
    }
    
    /// <summary>
    /// Спавнить все кристаллы
    /// </summary>
    public void SpawnCrystals()
    {
        // Очистить ранее заспавненные кристаллы
        ClearCrystals();
        
        // Сохранить изначальное количество кристаллов
        initialCrystalCount = crystalCount;
        hasCheckedRespawn = false;
        
        // Проверить наличие моделей
        List<GameObject> availablePrefabs = new List<GameObject>();
        foreach (GameObject prefab in crystalPrefabs)
        {
            if (prefab != null)
            {
                availablePrefabs.Add(prefab);
            }
        }
        
        if (availablePrefabs.Count == 0)
        {
            return;
        }
        
        
        // Проверка: предупредить, если минимальное расстояние слишком большое для области
        if (spawnAreaType == SpawnAreaType.Bounds && !ignoreMinDistance)
        {
            float areaSize = Mathf.Min(spawnBounds.x, spawnBounds.z);
            float maxPossibleCrystals = Mathf.FloorToInt((areaSize / minDistanceBetweenCrystals) * (areaSize / minDistanceBetweenCrystals));
            
            if (maxPossibleCrystals < crystalCount)
            {
            }
        }
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = crystalCount * 500; // Еще больше попыток
        int tooCloseCount = 0;
        int nullPrefabCount = 0;
        
        
        while (spawned < crystalCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Получить случайную позицию
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // Проверить минимальное расстояние от других кристаллов (если не отключено)
            if (!ignoreMinDistance && !IsPositionValid(spawnPosition))
            {
                tooCloseCount++;
                continue;
            }
            
            // Выбрать случайную модель
            if (availablePrefabs.Count == 0)
            {
                break;
            }
            
            GameObject randomPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
            
            if (randomPrefab == null)
            {
                nullPrefabCount++;
                if (nullPrefabCount > 10)
                {
                    break;
                }
                continue;
            }
            
            try
            {
                // Спавнить кристалл
                GameObject crystal = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);
                
                if (crystal == null)
                {
                    continue;
                }
                
                crystal.name = $"Crystal_{spawned + 1}_{randomPrefab.name}";
                
                // Добавить компонент Crystal ПЕРЕД изменением масштаба
                Crystal crystalComponent = crystal.GetComponent<Crystal>();
                if (crystalComponent == null)
                {
                    crystalComponent = crystal.AddComponent<Crystal>();
                }
                
                if (crystalComponent == null)
                {
                    DestroyImmediate(crystal);
                    continue;
                }
                
                // Уменьшить масштаб кристалла на 25% + еще 10%
                crystal.transform.localScale = Vector3.one * 0.3375f;
                
                // Добавить случайный поворот для разнообразия
                crystal.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                
                spawnedCrystals.Add(crystal);
                spawned++;
            }
            catch (System.Exception e)
            {
            }
        }
    }
    
    /// <summary>
    /// Получить случайную позицию для спавна
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPosition;
        
        if (spawnAreaType == SpawnAreaType.Bounds)
        {
            // Случайная позиция в прямоугольной области
            // Используем равномерное распределение по всей области
            float halfX, halfZ;
            
            if (useRadiusForBounds)
            {
                // Использовать spawnRadius как размер области (создаст квадратную область)
                halfX = spawnRadius / 2f;
                halfZ = spawnRadius / 2f;
            }
            else
            {
                // Использовать spawnBounds
                halfX = spawnBounds.x / 2f;
                halfZ = spawnBounds.z / 2f;
            }
            
            // Используем Random.value для более равномерного распределения
            float randomX = (Random.value * 2f - 1f) * halfX; // От -halfX до +halfX
            float randomZ = (Random.value * 2f - 1f) * halfZ; // От -halfZ до +halfZ
            
            randomPosition = spawnCenter + new Vector3(randomX, 0f, randomZ);
        }
        else // SpawnAreaType.Radius
        {
            // Случайная позиция в круговой области
            float randomAngle = Random.value * 2f * Mathf.PI; // От 0 до 2π (равномерно)
            float randomRadius;
            
            if (useUniformRadiusDistribution)
            {
                // Равномерное распределение по радиусу (больше точек ближе к центру)
                randomRadius = Random.value * spawnRadius;
            }
            else
            {
                // Равномерное распределение по площади круга (равномерно по всей области)
                // Формула: sqrt(random) * radius гарантирует равномерное распределение по площади
                randomRadius = Mathf.Sqrt(Random.value) * spawnRadius;
            }
            
            randomPosition = spawnCenter + new Vector3(
                Mathf.Cos(randomAngle) * randomRadius,
                0f,
                Mathf.Sin(randomAngle) * randomRadius
            );
        }
        
        // Проверить поверхность, если нужно
        if (checkGround)
        {
            RaycastHit hit;
            Vector3 rayStart = randomPosition + Vector3.up * groundCheckDistance;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance * 2f, groundLayer))
            {
                randomPosition = hit.point;
                // Добавить небольшое смещение вверх, чтобы кристалл не был в земле
                randomPosition.y += 0.1f;
            }
            else
            {
                // Если не найдена поверхность, использовать Y координату центра спавна
                randomPosition.y = spawnCenter.y;
            }
        }
        else
        {
            // Если проверка поверхности отключена, использовать Y координату центра
            randomPosition.y = spawnCenter.y;
        }
        
        return randomPosition;
    }
    
    /// <summary>
    /// Проверить, валидна ли позиция (минимальное расстояние от других кристаллов и от зоны появления игрока)
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        // Проверка 1: Исключить зону появления игрока (радиус вокруг центра спавна)
        if (excludePlayerSpawnArea && playerSpawnRadius > 0)
        {
            Vector3 horizontalDiff = new Vector3(
                position.x - spawnCenter.x,
                0f,
                position.z - spawnCenter.z
            );
            float distanceFromCenter = horizontalDiff.magnitude;
            
            if (distanceFromCenter < playerSpawnRadius)
            {
                return false; // Позиция слишком близко к центру (зоне появления игрока)
            }
        }
        
        // Проверка 2: Минимальное расстояние от других кристаллов
        if (!ignoreMinDistance && minDistanceBetweenCrystals > 0)
        {
            foreach (GameObject crystal in spawnedCrystals)
            {
                if (crystal != null)
                {
                    // Проверяем только горизонтальное расстояние (X и Z), игнорируя Y
                    Vector3 crystalPos = crystal.transform.position;
                    Vector3 horizontalDiff = new Vector3(
                        position.x - crystalPos.x,
                        0f,
                        position.z - crystalPos.z
                    );
                    float horizontalDistance = horizontalDiff.magnitude;
                    
                    if (horizontalDistance < minDistanceBetweenCrystals)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Очистить все заспавненные кристаллы
    /// </summary>
    public void ClearCrystals()
    {
        int clearedCount = 0;
        foreach (GameObject crystal in spawnedCrystals)
        {
            if (crystal != null)
            {
                DestroyImmediate(crystal);
                clearedCount++;
            }
        }
        spawnedCrystals.Clear();
    }
    
    /// <summary>
    /// Уничтожить кристалл (для внешнего вызова)
    /// </summary>
    public void RemoveCrystal(GameObject crystal)
    {
        if (spawnedCrystals.Contains(crystal))
        {
            spawnedCrystals.Remove(crystal);
            Destroy(crystal);
        }
        
        // Проверить, нужно ли заспавнить новые кристаллы
        CheckAndRespawnCrystals();
    }
    
    /// <summary>
    /// Вызывается при уничтожении кристалла
    /// </summary>
    public void OnCrystalDestroyed(GameObject crystal)
    {
        if (spawnedCrystals.Contains(crystal))
        {
            spawnedCrystals.Remove(crystal);
        }
        
        // Проверить, нужно ли заспавнить новые кристаллы
        CheckAndRespawnCrystals();
    }
    
    /// <summary>
    /// Получить количество заспавненных кристаллов
    /// </summary>
    public int GetSpawnedCrystalCount()
    {
        return spawnedCrystals.Count;
    }
    
    /// <summary>
    /// Проверить количество кристаллов и заспавнить новые, если осталось 50% или меньше
    /// </summary>
    private void CheckAndRespawnCrystals()
    {
        // Если изначальное количество не установлено, не проверять
        if (initialCrystalCount <= 0)
        {
            return;
        }
        
        // Очистить null ссылки из списка
        spawnedCrystals.RemoveAll(c => c == null);
        
        // Получить текущее количество живых кристаллов
        int currentCount = CrystalManager.GetCrystalCount();
        
        // Если осталось 50% или меньше от изначального количества
        float threshold = initialCrystalCount * 0.5f;
        if (currentCount <= threshold && !hasCheckedRespawn)
        {
            hasCheckedRespawn = true;
            
            // Вычислить, сколько нужно заспавнить (50% от изначального)
            int crystalsToSpawn = Mathf.RoundToInt(initialCrystalCount * 0.5f);
            
            Debug.Log($"[CrystalSpawner] Кристаллов осталось {currentCount} из {initialCrystalCount} (50% = {threshold}). Заспавню {crystalsToSpawn} новых кристаллов.");
            
            // Заспавнить дополнительные кристаллы
            SpawnAdditionalCrystals(crystalsToSpawn);
        }
    }
    
    /// <summary>
    /// Заспавнить дополнительные кристаллы без очистки существующих
    /// </summary>
    private void SpawnAdditionalCrystals(int count)
    {
        // Проверить наличие моделей
        List<GameObject> availablePrefabs = new List<GameObject>();
        foreach (GameObject prefab in crystalPrefabs)
        {
            if (prefab != null)
            {
                availablePrefabs.Add(prefab);
            }
        }
        
        if (availablePrefabs.Count == 0)
        {
            Debug.LogWarning("[CrystalSpawner] Нет доступных префабов кристаллов для спавна!");
            return;
        }
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 500;
        
        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;
            
            // Получить случайную позицию
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            // Проверить минимальное расстояние от других кристаллов (если не отключено)
            if (!ignoreMinDistance && !IsPositionValid(spawnPosition))
            {
                continue;
            }
            
            // Выбрать случайную модель
            GameObject randomPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
            
            if (randomPrefab == null)
            {
                continue;
            }
            
            try
            {
                // Спавнить кристалл
                GameObject crystal = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);
                
                if (crystal == null)
                {
                    continue;
                }
                
                crystal.name = $"Crystal_{spawnedCrystals.Count + spawned + 1}_{randomPrefab.name}";
                
                // Добавить компонент Crystal ПЕРЕД изменением масштаба
                Crystal crystalComponent = crystal.GetComponent<Crystal>();
                if (crystalComponent == null)
                {
                    crystalComponent = crystal.AddComponent<Crystal>();
                }
                
                if (crystalComponent == null)
                {
                    DestroyImmediate(crystal);
                    continue;
                }
                
                // Уменьшить масштаб кристалла на 25% + еще 10%
                crystal.transform.localScale = Vector3.one * 0.3375f;
                
                // Добавить случайный поворот для разнообразия
                crystal.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                
                spawnedCrystals.Add(crystal);
                spawned++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CrystalSpawner] Ошибка при спавне дополнительного кристалла: {e.Message}");
            }
        }
        
        // Сбросить флаг после небольшой задержки, чтобы не спавнить несколько раз подряд
        StartCoroutine(ResetRespawnFlag());
    }
    
    /// <summary>
    /// Сбросить флаг проверки респавна через небольшую задержку
    /// </summary>
    private IEnumerator ResetRespawnFlag()
    {
        yield return new WaitForSeconds(1f);
        hasCheckedRespawn = false;
    }
    
    /// <summary>
    /// Вывести статистику распределения кристаллов
    /// </summary>
    [ContextMenu("Показать статистику распределения")]
    public void ShowDistributionStats()
    {
        if (spawnedCrystals.Count == 0)
        {
            return;
        }
        
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float sumX = 0, sumZ = 0;
        
        foreach (GameObject crystal in spawnedCrystals)
        {
            if (crystal != null)
            {
                Vector3 pos = crystal.transform.position;
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minZ = Mathf.Min(minZ, pos.z);
                maxZ = Mathf.Max(maxZ, pos.z);
                sumX += pos.x;
                sumZ += pos.z;
            }
        }
        
        float avgX = sumX / spawnedCrystals.Count;
        float avgZ = sumZ / spawnedCrystals.Count;
        float rangeX = maxX - minX;
        float rangeZ = maxZ - minZ;
    }
    
    /// <summary>
    /// Проверить настройки и вывести информацию для отладки
    /// </summary>
    [ContextMenu("Проверить настройки")]
    public void CheckSettings()
    {
        int assignedCount = 0;
        for (int i = 0; i < crystalPrefabs.Length; i++)
        {
            if (crystalPrefabs[i] != null)
            {
                assignedCount++;
            }
        }
    }
    
    /// <summary>
    /// Тестовый метод для спавна одного кристалла в центре (для отладки)
    /// </summary>
    [ContextMenu("Спавнить тестовый кристалл")]
    public void SpawnTestCrystal()
    {
        LoadCrystalPrefabs();
        
        List<GameObject> availablePrefabs = new List<GameObject>();
        foreach (GameObject prefab in crystalPrefabs)
        {
            if (prefab != null)
            {
                availablePrefabs.Add(prefab);
            }
        }
        
        if (availablePrefabs.Count == 0)
        {
            return;
        }
        
        GameObject testPrefab = availablePrefabs[0];
        Vector3 testPosition = spawnCenter;
        
        if (checkGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(testPosition + Vector3.up * groundCheckDistance, Vector3.down, out hit, groundCheckDistance * 2f, groundLayer))
            {
                testPosition = hit.point + Vector3.up * 0.1f;
            }
        }
        
        GameObject crystal = Instantiate(testPrefab, testPosition, Quaternion.identity);
        crystal.name = $"TestCrystal_{testPrefab.name}";
        
        // Добавить компонент Crystal ПЕРЕД изменением масштаба
        Crystal crystalComponent = crystal.GetComponent<Crystal>();
        if (crystalComponent == null)
        {
            crystalComponent = crystal.AddComponent<Crystal>();
        }
        
        // Уменьшить масштаб кристалла на 25% + еще 10%
        crystal.transform.localScale = Vector3.one * 0.23625f;
    }
    
    // Визуализация области спавна в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        if (spawnAreaType == SpawnAreaType.Bounds)
        {
            Vector3 boundsSize;
            if (useRadiusForBounds)
            {
                // Использовать spawnRadius как размер области
                boundsSize = new Vector3(spawnRadius, 0, spawnRadius);
            }
            else
            {
                boundsSize = spawnBounds;
            }
            
            // Нарисовать прямоугольную область
            Gizmos.DrawWireCube(spawnCenter, boundsSize);
            
            // Нарисовать углы для лучшей видимости
            Vector3 halfSize = boundsSize * 0.5f;
            Gizmos.DrawLine(
                spawnCenter + new Vector3(-halfSize.x, 0, -halfSize.z),
                spawnCenter + new Vector3(halfSize.x, 0, -halfSize.z)
            );
            Gizmos.DrawLine(
                spawnCenter + new Vector3(halfSize.x, 0, -halfSize.z),
                spawnCenter + new Vector3(halfSize.x, 0, halfSize.z)
            );
            Gizmos.DrawLine(
                spawnCenter + new Vector3(halfSize.x, 0, halfSize.z),
                spawnCenter + new Vector3(-halfSize.x, 0, halfSize.z)
            );
            Gizmos.DrawLine(
                spawnCenter + new Vector3(-halfSize.x, 0, halfSize.z),
                spawnCenter + new Vector3(-halfSize.x, 0, -halfSize.z)
            );
        }
        else // SpawnAreaType.Radius
        {
            // Нарисовать круглую область (в виде цилиндра)
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
            
            // Нарисовать круг на плоскости XZ
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 prevPoint = spawnCenter + new Vector3(spawnRadius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = spawnCenter + new Vector3(
                    Mathf.Cos(angle) * spawnRadius,
                    0,
                    Mathf.Sin(angle) * spawnRadius
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
        
        // Показать центр спавна
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnCenter, 0.5f);
        
        // Показать запрещенную зону вокруг центра (зона появления игрока)
        if (excludePlayerSpawnArea && playerSpawnRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnCenter, playerSpawnRadius);
            
            // Нарисовать круг на плоскости XZ
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 prevPoint = spawnCenter + new Vector3(playerSpawnRadius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = spawnCenter + new Vector3(
                    Mathf.Cos(angle) * playerSpawnRadius,
                    0,
                    Mathf.Sin(angle) * playerSpawnRadius
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
        
        // Показать заспавненные кристаллы
        Gizmos.color = Color.green;
        foreach (GameObject crystal in spawnedCrystals)
        {
            if (crystal != null)
            {
                Gizmos.DrawWireSphere(crystal.transform.position, 0.5f);
                // Линия от центра к кристаллу для визуализации распределения
                Gizmos.DrawLine(spawnCenter, crystal.transform.position);
            }
        }
    }
}

