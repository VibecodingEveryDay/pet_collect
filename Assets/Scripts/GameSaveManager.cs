using System.Collections.Generic;
using UnityEngine;
#if Storage_yg
using YG;
#endif

/// <summary>
/// Менеджер для сохранения и загрузки игровых данных через YG2 Storage
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    private static GameSaveManager _instance;
    
    public static GameSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameSaveManager>();
                if (_instance == null)
                {
                    GameObject saveManagerObject = new GameObject("GameSaveManager");
                    _instance = saveManagerObject.AddComponent<GameSaveManager>();
                    DontDestroyOnLoad(saveManagerObject);
                }
            }
            return _instance;
        }
    }
    
    private bool isDataLoaded = false;
    private bool isLoading = false; // Флаг загрузки для предотвращения сохранения во время загрузки
    
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
    
#if Storage_yg
    private void OnEnable()
    {
        // Подписываемся на событие загрузки данных
        YG2.onGetSDKData += LoadGameData;
        YG2.onDefaultSaves += OnDefaultSaves;
    }
    
    private void OnDisable()
    {
        // Отписываемся от событий
        YG2.onGetSDKData -= LoadGameData;
        YG2.onDefaultSaves -= OnDefaultSaves;
    }
    
    private void OnDefaultSaves()
    {
        // При сбросе сохранений устанавливаем значения по умолчанию
        isDataLoaded = true;
        LoadGameData();
    }
    
    /// <summary>
    /// Загрузить игровые данные из YG2.saves
    /// </summary>
    private void LoadGameData()
    {
        if (!isDataLoaded)
        {
            isDataLoaded = true;
        }
        
        isLoading = true; // Устанавливаем флаг загрузки
        
        try
        {
            // Загрузить монеты
            if (YG2.saves.coins >= 0)
            {
                CoinManager.SetCoinsWithoutSave(YG2.saves.coins);
                Debug.Log($"[GameSaveManager] Загружены монеты: {YG2.saves.coins}");
            }
            
            // Загрузить питомцев
            if (YG2.saves.pets != null && YG2.saves.pets.Count > 0)
            {
                PetInventory inventory = PetInventory.Instance;
                if (inventory != null)
                {
                    inventory.ClearInventory();
                    
                    foreach (PetDataSerializable petDataSerializable in YG2.saves.pets)
                    {
                        if (petDataSerializable != null)
                        {
                            PetData petData = petDataSerializable.ToPetData();
                            inventory.AddPetWithoutSave(petData);
                        }
                    }
                    
                    Debug.Log($"[GameSaveManager] Загружено питомцев: {YG2.saves.pets.Count}");
                }
            }
            
            // Загрузить улучшения кристаллов
            if (YG2.saves.crystalHPLevel > 0)
            {
                CrystalUpgradeSystem.SetHPLevel(YG2.saves.crystalHPLevel);
                Debug.Log($"[GameSaveManager] Загружен уровень HP кристаллов: {YG2.saves.crystalHPLevel}");
            }
            
            // Загрузить локацию
            if (!string.IsNullOrEmpty(YG2.saves.currentMap))
            {
                MapUpgradeSystem.SetCurrentMapWithoutSave(YG2.saves.currentMap);
                Debug.Log($"[GameSaveManager] Загружена локация: {YG2.saves.currentMap}");
            }
            
            // Загрузить статус покупки карты
            if (YG2.saves.mapUpgradePurchased)
            {
                MapUpgradeSystem.PurchaseMapUpgradeWithoutSave();
                Debug.Log("[GameSaveManager] Загружен статус покупки карты: куплена");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при загрузке данных: {e.Message}");
        }
        finally
        {
            isLoading = false; // Сбрасываем флаг загрузки
        }
    }
    
    /// <summary>
    /// Сохранить игровые данные в YG2.saves
    /// </summary>
    public void SaveGameData()
    {
#if Storage_yg
        // Не сохранять во время загрузки
        if (isLoading)
        {
            return;
        }
        
        try
        {
            // Сохранить монеты
            YG2.saves.coins = CoinManager.GetCoins();
            Debug.Log($"[GameSaveManager] Сохранены монеты: {YG2.saves.coins}");
            
            // Сохранить питомцев
            PetInventory inventory = PetInventory.Instance;
            if (inventory != null)
            {
                List<PetData> pets = inventory.GetAllPets();
                YG2.saves.pets = new List<PetDataSerializable>();
                
                foreach (PetData petData in pets)
                {
                    if (petData != null)
                    {
                        YG2.saves.pets.Add(new PetDataSerializable(petData));
                    }
                }
                
                Debug.Log($"[GameSaveManager] Сохранено питомцев: {YG2.saves.pets.Count}");
            }
            
            // Сохранить улучшения кристаллов
            YG2.saves.crystalHPLevel = CrystalUpgradeSystem.GetHPLevel();
            Debug.Log($"[GameSaveManager] Сохранен уровень HP кристаллов: {YG2.saves.crystalHPLevel}");
            
            // Сохранить локацию
            YG2.saves.currentMap = MapUpgradeSystem.GetCurrentMap();
            Debug.Log($"[GameSaveManager] Сохранена локация: {YG2.saves.currentMap}");
            
            // Сохранить статус покупки карты
            YG2.saves.mapUpgradePurchased = MapUpgradeSystem.IsMapUpgradePurchased();
            Debug.Log($"[GameSaveManager] Сохранен статус покупки карты: {YG2.saves.mapUpgradePurchased}");
            
            // Сохранить в облако/локально
            YG2.SaveProgress();
            Debug.Log("[GameSaveManager] Данные успешно сохранены!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при сохранении данных: {e.Message}");
        }
#else
        Debug.LogWarning("[GameSaveManager] Модуль Storage не подключен! Сохранение невозможно.");
#endif
    }
    
    /// <summary>
    /// Принудительно загрузить данные (для вызова из других скриптов)
    /// </summary>
    public void ForceLoadGameData()
    {
#if Storage_yg
        LoadGameData();
#else
        Debug.LogWarning("[GameSaveManager] Модуль Storage не подключен!");
#endif
    }
#else
    // Если модуль Storage не подключен, методы ничего не делают
    public void SaveGameData()
    {
        Debug.LogWarning("[GameSaveManager] Модуль Storage не подключен! Сохранение невозможно.");
    }
    
    public void ForceLoadGameData()
    {
        Debug.LogWarning("[GameSaveManager] Модуль Storage не подключен!");
    }
#endif
}

