using UnityEngine;

/// <summary>
/// Менеджер для управления спавном карт
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("Префабы карт")]
    [SerializeField] private GameObject map1Prefab;
    [SerializeField] private GameObject level2MapPrefab;
    
    [Header("Позиция карт")]
    [SerializeField] private Vector3 mapPosition = new Vector3(-3.257f, -20.04f, -0.8f);
    
    private GameObject currentMap;
    
    private void Awake()
    {
        // Загрузить префабы, если они не назначены
        LoadMapPrefabs();
        
        // Заспавнить нужную карту
        SpawnMap();
    }
    
    /// <summary>
    /// Загрузить префабы карт
    /// </summary>
    private void LoadMapPrefabs()
    {
        // Если префабы не назначены, попробовать загрузить автоматически
        if (map1Prefab == null)
        {
            #if UNITY_EDITOR
            map1Prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Map/Map1.prefab");
            #endif
            
            // Если не удалось загрузить, попробовать через Resources
            if (map1Prefab == null)
            {
                map1Prefab = Resources.Load<GameObject>("Map/Map1");
            }
        }
        
        if (level2MapPrefab == null)
        {
            #if UNITY_EDITOR
            level2MapPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/Map/Level2Map.prefab");
            #endif
            
            // Если не удалось загрузить, попробовать через Resources
            if (level2MapPrefab == null)
            {
                level2MapPrefab = Resources.Load<GameObject>("Map/Level2Map");
            }
        }
        
        if (map1Prefab == null)
        {
            Debug.LogError("[MapManager] Map1 префаб не найден! Убедитесь, что префаб находится в правильной папке.");
        }
        
        if (level2MapPrefab == null)
        {
            Debug.LogError("[MapManager] Level2Map префаб не найден! Убедитесь, что префаб находится в правильной папке.");
        }
    }
    
    /// <summary>
    /// Заспавнить карту
    /// </summary>
    private void SpawnMap()
    {
        // Удалить существующую карту, если есть
        if (currentMap != null)
        {
            Destroy(currentMap);
            currentMap = null;
        }
        
        // Получить текущую карту из сохранения
        string currentMapName = MapUpgradeSystem.GetCurrentMap();
        
        // Если карта еще не куплена, всегда используем Map1
        if (!MapUpgradeSystem.IsMapUpgradePurchased())
        {
            currentMapName = "Map1";
            MapUpgradeSystem.SetCurrentMap("Map1");
        }
        
        GameObject mapToSpawn = (currentMapName == "Level2Map") ? level2MapPrefab : map1Prefab;
        string mapName = currentMapName;
        
        if (mapToSpawn == null)
        {
            Debug.LogError($"[MapManager] Префаб карты {mapName} не найден!");
            return;
        }
        
        // Заспавнить карту
        currentMap = Instantiate(mapToSpawn, mapPosition, Quaternion.identity);
        currentMap.name = mapName;
        
        Debug.Log($"[MapManager] Заспавнена карта: {mapName} в позиции {mapPosition}");
    }
    
    /// <summary>
    /// Переключить карту между солнечными лугами и сумеречными долинами
    /// </summary>
    public void ToggleMap()
    {
        if (!MapUpgradeSystem.IsMapUpgradePurchased())
        {
            Debug.LogWarning("[MapManager] Нельзя переключать карту, пока она не куплена!");
            return;
        }
        
        string currentMapName = MapUpgradeSystem.GetCurrentMap();
        string newMapName = (currentMapName == "Level2Map") ? "Map1" : "Level2Map";
        
        MapUpgradeSystem.SetCurrentMap(newMapName);
        SpawnMap();
        
        string mapDisplayName = (newMapName == "Level2Map") ? "сумеречные долины" : "солнечные луга";
        Debug.Log($"[MapManager] Переключена карта на: {mapDisplayName}");
    }
    
    /// <summary>
    /// Обновить карту (вызывается после покупки улучшения)
    /// </summary>
    public void RefreshMap()
    {
        SpawnMap();
    }
    
    /// <summary>
    /// Получить текущую карту
    /// </summary>
    public GameObject GetCurrentMap()
    {
        return currentMap;
    }
}

