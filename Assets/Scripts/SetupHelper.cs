using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Вспомогательный скрипт для автоматической настройки системы питомцев
/// </summary>
public class SetupHelper : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Pet Collect/Setup Pet System")]
    public static void SetupPetSystem()
    {
        // 1. Проверить/создать CrystalManager
        CrystalManager crystalManager = FindObjectOfType<CrystalManager>();
        if (crystalManager == null)
        {
            GameObject crystalManagerObj = new GameObject("CrystalManager");
            crystalManager = crystalManagerObj.AddComponent<CrystalManager>();
            Debug.Log("✓ CrystalManager создан");
        }
        else
        {
            Debug.Log("✓ CrystalManager уже существует");
        }
        
        // 2. Проверить/создать PetSpawner
        PetSpawner petSpawner = FindObjectOfType<PetSpawner>();
        if (petSpawner == null)
        {
            GameObject petSpawnerObj = new GameObject("PetSpawner");
            petSpawner = petSpawnerObj.AddComponent<PetSpawner>();
            Debug.Log("✓ PetSpawner создан");
        }
        else
        {
            Debug.Log("✓ PetSpawner уже существует");
        }
        
        // 3. Проверить/создать PetNotificationUI
        PetNotificationUI notificationUI = FindObjectOfType<PetNotificationUI>();
        if (notificationUI == null)
        {
            GameObject notificationObj = new GameObject("PetNotificationUI");
            notificationUI = notificationObj.AddComponent<PetNotificationUI>();
            
            // Загрузить UXML и USS
            VisualTreeAsset notificationAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/PetNotification.uxml");
            StyleSheet robloxStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/RobloxStyle.uss");
            
            if (notificationAsset != null)
            {
                notificationUI.notificationAsset = notificationAsset;
                Debug.Log("✓ PetNotification.uxml загружен");
            }
            else
            {
                Debug.LogWarning("⚠ PetNotification.uxml не найден по пути: Assets/UI Toolkit/PetNotification.uxml");
            }
            
            if (robloxStyle != null)
            {
                notificationUI.robloxStyleSheet = robloxStyle;
                Debug.Log("✓ RobloxStyle.uss загружен");
            }
            else
            {
                Debug.LogWarning("⚠ RobloxStyle.uss не найден по пути: Assets/UI Toolkit/RobloxStyle.uss");
            }
            
            Debug.Log("✓ PetNotificationUI создан");
        }
        else
        {
            Debug.Log("✓ PetNotificationUI уже существует");
        }
        
        // 4. Проверить PetInventory
        PetInventory petInventory = FindObjectOfType<PetInventory>();
        if (petInventory == null)
        {
            GameObject inventoryObj = new GameObject("PetInventory");
            inventoryObj.AddComponent<PetInventory>();
            Debug.Log("✓ PetInventory создан");
        }
        else
        {
            Debug.Log("✓ PetInventory уже существует");
        }
        
        // 5. Проверить PetHatchingManager
        PetHatchingManager hatchingManager = FindObjectOfType<PetHatchingManager>();
        if (hatchingManager == null)
        {
            Debug.LogWarning("⚠ PetHatchingManager не найден на сцене! Добавьте его вручную.");
        }
        else
        {
            Debug.Log("✓ PetHatchingManager найден");
        }
        
        // 6. Проверить InventoryUI
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogWarning("⚠ InventoryUI не найден на сцене! Добавьте его вручную.");
        }
        else
        {
            Debug.Log("✓ InventoryUI найден");
        }
        
        Debug.Log("=== Настройка завершена ===");
        Debug.Log("Проверьте консоль на наличие предупреждений.");
    }
    
    [MenuItem("Pet Collect/Check Pet Models")]
    public static void CheckPetModels()
    {
        string[] petModels = { "rare1.glb", "rare2.glb", "rare3.glb", "rare4.glb", "epic.glb", "legendary.glb" };
        bool allFound = true;
        
        foreach (string modelName in petModels)
        {
            string path = $"Assets/Assets/Pets/{modelName}";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (model != null)
            {
                Debug.Log($"✓ {modelName} найден");
            }
            else
            {
                Debug.LogError($"✗ {modelName} НЕ найден по пути: {path}");
                allFound = false;
            }
        }
        
        if (allFound)
        {
            Debug.Log("=== Все модели питомцев найдены ===");
        }
        else
        {
            Debug.LogError("=== Некоторые модели питомцев не найдены! Проверьте пути. ===");
        }
    }
#endif
}

