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
    private bool hasLoadedCoins = false; // Флаг, указывающий, что монеты были загружены из сохранения
    
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
    /// Проверить состояние игры и применить логику сброса при 0 питомцах
    /// </summary>
    private void CheckAndResetGameState()
    {
        PetInventory inventory = PetInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[GameSaveManager] PetInventory.Instance == null, не могу проверить состояние игры");
            return;
        }
        
        int petCount = inventory.GetTotalPetCount();
        Debug.Log($"[GameSaveManager] CheckAndResetGameState: Количество питомцев = {petCount}");
        
        // Цена яйца теперь рассчитывается динамически на основе количества питомцев
        // Сброс цены не требуется
        
        // Если у игрока 0 питомцев и <100 монет, пополнить баланс до 100 монет
        // НО только если монеты НЕ были загружены из сохранения (т.е. это действительно новый старт)
        if (petCount == 0)
        {
            int currentCoins = CoinManager.GetCoins();
            
            // Пополнять баланс только если:
            // 1. Монет < 100
            // 2. Монеты НЕ были загружены из сохранения (hasLoadedCoins == false)
            if (currentCoins < 100 && !hasLoadedCoins)
            {
                int coinsToAdd = 100 - currentCoins;
                CoinManager.AddCoins(coinsToAdd);
                
                Debug.Log($"[GameSaveManager] У игрока 0 питомцев и {currentCoins} монет (нет сохраненных), баланс пополнен до 100 монет (+{coinsToAdd})");
            }
            else if (currentCoins < 100 && hasLoadedCoins)
            {
                Debug.Log($"[GameSaveManager] У игрока 0 питомцев и {currentCoins} монет, но монеты были загружены из сохранения - не пополняем баланс");
            }
        }
    }
    
#if Storage_yg
    private void OnEnable()
    {
        // Подписываемся на событие загрузки данных
        YG2.onGetSDKData += LoadGameData;
        YG2.onDefaultSaves += OnDefaultSaves;
        
        // Сбросить стартовую цену яйца до 100 при первом запуске
        ResetEggPriceIfNeeded();
    }
    
    /// <summary>
    /// Сбросить стартовую цену яйца до 100, если это первый запуск
    /// </summary>
    private void ResetEggPriceIfNeeded()
    {
        // Проверить, есть ли сохраненные данные
        bool hasSavedData = false;
        
#if Storage_yg
        if (YG2.isSDKEnabled && YG2.saves != null)
        {
            // Проверить, есть ли сохраненные питомцы или другие данные
            hasSavedData = (YG2.saves.pets != null && YG2.saves.pets.Count > 0) || 
                          (YG2.saves.coins > 100) || // Если монет больше начальных 100, значит была игра
                          PlayerPrefs.HasKey("PetCount");
        }
#endif
        
        // Если нет сохраненных данных, сбросить цену до 100
        if (!hasSavedData && !PlayerPrefs.HasKey("PetCount"))
        {
            int startPrice = 100;
            PlayerPrefs.SetInt("EggPrice", startPrice);
            PlayerPrefs.Save();
            
#if Storage_yg
            if (YG2.isSDKEnabled && YG2.saves != null)
            {
                YG2.saves.eggPrice = startPrice;
            }
#endif
            
            Debug.Log($"[GameSaveManager] Сброшена стартовая цена яйца до: {startPrice}");
        }
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
            if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.coins >= 0)
            {
                CoinManager.SetCoinsWithoutSave(YG2.saves.coins);
                hasLoadedCoins = true;
                Debug.Log($"[GameSaveManager] Загружены монеты из YG2: {YG2.saves.coins}");
            }
            else
            {
                // Fallback на PlayerPrefs для локального тестирования
                if (PlayerPrefs.HasKey("Coins"))
                {
                    int coins = PlayerPrefs.GetInt("Coins");
                    CoinManager.SetCoinsWithoutSave(coins);
                    hasLoadedCoins = true;
                    Debug.Log($"[GameSaveManager] Загружены монеты из PlayerPrefs: {coins}");
                }
                else
                {
                    hasLoadedCoins = false;
                    Debug.Log($"[GameSaveManager] Нет сохраненных монет, будет установлено начальное значение");
                }
            }
            
            // Загрузить питомцев
            bool petsLoaded = false;
            if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.pets != null && YG2.saves.pets.Count > 0)
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
                    
                    Debug.Log($"[GameSaveManager] Загружено питомцев из YG2: {YG2.saves.pets.Count}");
                    petsLoaded = true;
                }
            }
            
            // Fallback на PlayerPrefs для локального тестирования
            if (!petsLoaded)
            {
                LoadPetsFromPlayerPrefs();
            }
            
            // Загрузить улучшения кристаллов
            if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.crystalHPLevel > 0)
            {
                CrystalUpgradeSystem.SetHPLevel(YG2.saves.crystalHPLevel);
                Debug.Log($"[GameSaveManager] Загружен уровень HP кристаллов из YG2: {YG2.saves.crystalHPLevel}");
            }
            else if (PlayerPrefs.HasKey("CrystalHPLevel"))
            {
                int level = PlayerPrefs.GetInt("CrystalHPLevel");
                CrystalUpgradeSystem.SetHPLevel(level);
                Debug.Log($"[GameSaveManager] Загружен уровень HP кристаллов из PlayerPrefs: {level}");
            }
            
            // Загрузить локацию
            if (YG2.isSDKEnabled && YG2.saves != null && !string.IsNullOrEmpty(YG2.saves.currentMap))
            {
                MapUpgradeSystem.SetCurrentMapWithoutSave(YG2.saves.currentMap);
                Debug.Log($"[GameSaveManager] Загружена локация из YG2: {YG2.saves.currentMap}");
            }
            else if (PlayerPrefs.HasKey("CurrentMap"))
            {
                string map = PlayerPrefs.GetString("CurrentMap");
                MapUpgradeSystem.SetCurrentMapWithoutSave(map);
                Debug.Log($"[GameSaveManager] Загружена локация из PlayerPrefs: {map}");
            }
            
            // Загрузить статус покупки карты
            if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.mapUpgradePurchased)
            {
                MapUpgradeSystem.PurchaseMapUpgradeWithoutSave();
                Debug.Log("[GameSaveManager] Загружен статус покупки карты из YG2: куплена");
            }
            else if (PlayerPrefs.HasKey("MapUpgradePurchased"))
            {
                bool purchased = PlayerPrefs.GetInt("MapUpgradePurchased") == 1;
                if (purchased)
                {
                    MapUpgradeSystem.PurchaseMapUpgradeWithoutSave();
                    Debug.Log("[GameSaveManager] Загружен статус покупки карты из PlayerPrefs: куплена");
                }
            }
            
            // Загрузить цену яйца
            if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.eggPrice > 0)
            {
                // Цена уже загружена в YG2.saves, ShopManager будет использовать её автоматически
                Debug.Log($"[GameSaveManager] Загружена цена яйца из YG2: {YG2.saves.eggPrice}");
            }
            else
            {
                // Fallback на PlayerPrefs для локального тестирования
                if (PlayerPrefs.HasKey("EggPrice"))
                {
                    int eggPrice = PlayerPrefs.GetInt("EggPrice");
                    if (eggPrice > 0)
                    {
                        Debug.Log($"[GameSaveManager] Загружена цена яйца из PlayerPrefs: {eggPrice}");
                        // Установить в YG2.saves, если SDK доступен
                        if (YG2.isSDKEnabled && YG2.saves != null)
                        {
                            YG2.saves.eggPrice = eggPrice;
                        }
                    }
            }
            else
            {
                // Если цена не установлена (первый запуск), установить начальную цену 100
                    int startPrice = 100;
                    if (YG2.isSDKEnabled && YG2.saves != null)
                    {
                        YG2.saves.eggPrice = startPrice;
                    }
                    PlayerPrefs.SetInt("EggPrice", startPrice);
                    PlayerPrefs.Save();
                    Debug.Log($"[GameSaveManager] Установлена начальная цена яйца: {startPrice}");
            }
            }
            
            // Загрузить активных питомцев (ID питомцев, которые должны быть заспавнены)
            LoadActivePets();
            
            // Проверить состояние игры и применить логику сброса
            CheckAndResetGameState();
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
        // Не сохранять во время загрузки
        if (isLoading)
        {
            Debug.LogWarning("[GameSaveManager] Попытка сохранить данные во время загрузки! Сохранение пропущено.");
            return;
        }
        
        // Не сохранять, если данные еще не загружены
        if (!isDataLoaded)
        {
            Debug.LogWarning("[GameSaveManager] Попытка сохранить данные до загрузки! Сохранение пропущено.");
            // Но все равно попытаться сохранить в PlayerPrefs для локального тестирования
            Debug.Log("[GameSaveManager] Попытка принудительного сохранения в PlayerPrefs (данные еще не загружены)");
            SaveGameDataToPlayerPrefs();
            return;
        }
        
#if Storage_yg
        // Если SDK не инициализирован, использовать PlayerPrefs для локального тестирования
        if (!YG2.isSDKEnabled)
        {
            Debug.Log("[GameSaveManager] SDK не инициализирован, используем PlayerPrefs для локального сохранения");
            SaveGameDataToPlayerPrefs();
            return;
        }
        
        try
        {
            // Сохранить монеты
            int coins = CoinManager.GetCoins();
            if (YG2.isSDKEnabled && YG2.saves != null)
            {
                YG2.saves.coins = coins;
                Debug.Log($"[GameSaveManager] Сохранены монеты в YG2: {coins}");
            }
            // ВСЕГДА также сохранить в PlayerPrefs для локального тестирования и резервной копии
            PlayerPrefs.SetInt("Coins", coins);
            PlayerPrefs.Save();
            Debug.Log($"[GameSaveManager] Сохранены монеты в PlayerPrefs: {coins}");
            
            // Сохранить питомцев
            PetInventory inventory = PetInventory.Instance;
            if (inventory != null)
            {
                List<PetData> pets = inventory.GetAllPets();
                Debug.Log($"[GameSaveManager] SaveGameData: Найдено {pets.Count} питомцев для сохранения");
                
                if (YG2.isSDKEnabled && YG2.saves != null)
                {
                YG2.saves.pets = new List<PetDataSerializable>();
                
                foreach (PetData petData in pets)
                {
                    if (petData != null)
                    {
                        YG2.saves.pets.Add(new PetDataSerializable(petData));
                    }
                }
                
                    Debug.Log($"[GameSaveManager] Сохранено питомцев в YG2: {YG2.saves.pets.Count}");
                }
                else
                {
                    Debug.Log("[GameSaveManager] YG2 SDK не доступен, сохраняем только в PlayerPrefs");
                }
                
                // ВСЕГДА также сохранить в PlayerPrefs для локального тестирования и резервной копии
                Debug.Log($"[GameSaveManager] SaveGameData: Вызываем SavePetsToPlayerPrefs() для сохранения {pets.Count} питомцев");
                SavePetsToPlayerPrefs(pets);
            }
            else
            {
                Debug.LogWarning("[GameSaveManager] SaveGameData: PetInventory.Instance == null, питомцы не могут быть сохранены");
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
            
            // Сохранить цену яйца (получаем из ShopManager)
            // ВАЖНО: Не перезаписывать цену, если она уже установлена в YG2.saves
            // Цена должна сохраняться только через IncreaseEggPrice(), а не через SaveGameData()
            // Это предотвратит перезапись увеличенной цены обратно в старую
            if (YG2.saves.eggPrice > 0)
            {
                // Цена уже установлена, не перезаписываем её
                Debug.Log($"[GameSaveManager] Цена яйца уже установлена: {YG2.saves.eggPrice}, не перезаписываем");
            }
            else
            {
                // Только если цена не установлена, получить из ShopManager
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                    int managerPrice = shopManager.GetEggPrice();
                    if (managerPrice > 0)
                    {
                        YG2.saves.eggPrice = managerPrice;
                        Debug.Log($"[GameSaveManager] Установлена цена яйца из ShopManager: {YG2.saves.eggPrice}");
                    }
                }
            }
            
            // Также проверить PlayerPrefs для локального тестирования (без SDK)
            // НЕ перезаписывать цену в PlayerPrefs, если она уже установлена
            if (PlayerPrefs.HasKey("EggPrice"))
            {
                int playerPrefsPrice = PlayerPrefs.GetInt("EggPrice");
                if (playerPrefsPrice > 0)
                {
                    Debug.Log($"[GameSaveManager] Цена яйца в PlayerPrefs уже установлена: {playerPrefsPrice}, не перезаписываем");
                }
            }
            
            // Сохранить активных питомцев (ID питомцев, которые заспавнены в мире)
            SaveActivePets();
            
            // Сохранить в облако/локально (сначала YG2, если доступен)
            if (YG2.isSDKEnabled)
            {
            YG2.SaveProgress();
                Debug.Log("[GameSaveManager] Данные сохранены в YG2 через YG2.SaveProgress()");
            }
            
            // ВСЕГДА также сохранить в PlayerPrefs для локального тестирования и резервной копии
            // Это гарантирует, что данные сохранятся даже при проблемах с облачным сохранением
            // Сохраняем ПОСЛЕ YG2.SaveProgress(), чтобы наши данные не перезаписывались
            Debug.Log("[GameSaveManager] Вызываем SaveGameDataToPlayerPrefs() для сохранения в PlayerPrefs");
            SaveGameDataToPlayerPrefs();
            
            Debug.Log("[GameSaveManager] Данные успешно сохранены в YG2 и PlayerPrefs!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при сохранении данных: {e.Message}");
            // Даже при ошибке попытаться сохранить в PlayerPrefs
            try
            {
                SaveGameDataToPlayerPrefs();
                Debug.Log("[GameSaveManager] Резервное сохранение в PlayerPrefs выполнено после ошибки");
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"[GameSaveManager] Ошибка при резервном сохранении в PlayerPrefs: {e2.Message}");
            }
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
        if (YG2.isSDKEnabled)
        {
        LoadGameData();
        }
        else
        {
            // Fallback на PlayerPrefs для локального тестирования
            LoadGameDataFromPlayerPrefs();
        }
#else
        // Fallback на PlayerPrefs для локального тестирования
        LoadGameDataFromPlayerPrefs();
#endif
    }
    
    /// <summary>
    /// Сохранить все данные в PlayerPrefs (для локального тестирования)
    /// </summary>
    private void SaveGameDataToPlayerPrefs()
    {
        try
        {
            // Сохранить монеты
            int coins = CoinManager.GetCoins();
            PlayerPrefs.SetInt("Coins", coins);
            Debug.Log($"[GameSaveManager] Сохранены монеты в PlayerPrefs: {coins}");
            
            // Сохранить питомцев
            PetInventory inventory = PetInventory.Instance;
            if (inventory != null)
            {
                List<PetData> pets = inventory.GetAllPets();
                Debug.Log($"[GameSaveManager] SaveGameDataToPlayerPrefs: Найдено {pets.Count} питомцев для сохранения");
                SavePetsToPlayerPrefs(pets);
            }
            else
            {
                Debug.LogWarning("[GameSaveManager] SaveGameDataToPlayerPrefs: PetInventory.Instance == null, питомцы не могут быть сохранены");
            }
            
            // Сохранить активных питомцев
            SaveActivePets();
            
            // Сохранить цену яйца
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                int eggPrice = shopManager.GetEggPrice();
                PlayerPrefs.SetInt("EggPrice", eggPrice);
                Debug.Log($"[GameSaveManager] Сохранена цена яйца в PlayerPrefs: {eggPrice}");
            }
            
            // Сохранить улучшения кристаллов
            int crystalHPLevel = CrystalUpgradeSystem.GetHPLevel();
            PlayerPrefs.SetInt("CrystalHPLevel", crystalHPLevel);
            
            // Сохранить локацию
            string currentMap = MapUpgradeSystem.GetCurrentMap();
            PlayerPrefs.SetString("CurrentMap", currentMap);
            
            // Сохранить статус покупки карты
            bool mapUpgradePurchased = MapUpgradeSystem.IsMapUpgradePurchased();
            PlayerPrefs.SetInt("MapUpgradePurchased", mapUpgradePurchased ? 1 : 0);
            
            // ВАЖНО: Явно вызвать PlayerPrefs.Save() для гарантированного сохранения
            PlayerPrefs.Save();
            Debug.Log("[GameSaveManager] SaveGameDataToPlayerPrefs: УСПЕХ! Все данные сохранены в PlayerPrefs и PlayerPrefs.Save() вызван!");
            
            // Дополнительная проверка: убедиться, что данные действительно сохранились
            int verifyPetCount = PlayerPrefs.GetInt("PetCount", -1);
            int verifyCoins = PlayerPrefs.GetInt("Coins", -1);
            Debug.Log($"[GameSaveManager] SaveGameDataToPlayerPrefs: Проверка сохранения - PetCount: {verifyPetCount}, Coins: {verifyCoins}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при сохранении данных в PlayerPrefs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Загрузить питомцев из PlayerPrefs (для локального тестирования)
    /// </summary>
    private void LoadPetsFromPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey("PetCount"))
        {
            Debug.Log("[GameSaveManager] Нет сохраненных питомцев в PlayerPrefs");
            return;
        }
        
        int petCount = PlayerPrefs.GetInt("PetCount");
        if (petCount <= 0)
        {
            return;
        }
        
        PetInventory inventory = PetInventory.Instance;
        if (inventory == null)
        {
            return;
        }
        
        inventory.ClearInventory();
        
        for (int i = 0; i < petCount; i++)
        {
            string petKey = $"Pet_{i}";
            if (PlayerPrefs.HasKey($"{petKey}_ID"))
            {
                // Загрузить данные питомца из PlayerPrefs
                int petID = PlayerPrefs.GetInt($"{petKey}_ID");
                string petName = PlayerPrefs.GetString($"{petKey}_Name", $"Питомец {petID}");
                int rarityInt = PlayerPrefs.GetInt($"{petKey}_Rarity", 0);
                float size = PlayerPrefs.GetFloat($"{petKey}_Size", 1f);
                string modelPath = PlayerPrefs.GetString($"{petKey}_ModelPath", "");
                
                // Восстановить цвет
                float r = PlayerPrefs.GetFloat($"{petKey}_ColorR", 1f);
                float g = PlayerPrefs.GetFloat($"{petKey}_ColorG", 1f);
                float b = PlayerPrefs.GetFloat($"{petKey}_ColorB", 1f);
                float a = PlayerPrefs.GetFloat($"{petKey}_ColorA", 1f);
                Color petColor = new Color(r, g, b, a);
                
                PetRarity rarity = (PetRarity)rarityInt;
                
                PetData petData = new PetData(rarity, petName, petID, size, petColor);
                petData.petModelPath = modelPath;
                
                inventory.AddPetWithoutSave(petData);
            }
        }
        
        Debug.Log($"[GameSaveManager] Загружено питомцев из PlayerPrefs: {petCount}");
    }
    
    /// <summary>
    /// Сохранить питомцев в PlayerPrefs (для локального тестирования)
    /// </summary>
    private void SavePetsToPlayerPrefs(List<PetData> pets)
    {
        if (pets == null)
        {
            Debug.LogWarning("[GameSaveManager] SavePetsToPlayerPrefs: pets == null, сохранение пропущено");
            return;
        }
        
        Debug.Log($"[GameSaveManager] SavePetsToPlayerPrefs: Начало сохранения {pets.Count} питомцев в PlayerPrefs");
        
        // Сначала очистить старые данные питомцев
        int oldPetCount = PlayerPrefs.HasKey("PetCount") ? PlayerPrefs.GetInt("PetCount") : 0;
        for (int i = 0; i < oldPetCount; i++)
        {
            string petKey = $"Pet_{i}";
            PlayerPrefs.DeleteKey($"{petKey}_ID");
            PlayerPrefs.DeleteKey($"{petKey}_Name");
            PlayerPrefs.DeleteKey($"{petKey}_Rarity");
            PlayerPrefs.DeleteKey($"{petKey}_Size");
            PlayerPrefs.DeleteKey($"{petKey}_ModelPath");
            PlayerPrefs.DeleteKey($"{petKey}_ColorR");
            PlayerPrefs.DeleteKey($"{petKey}_ColorG");
            PlayerPrefs.DeleteKey($"{petKey}_ColorB");
            PlayerPrefs.DeleteKey($"{petKey}_ColorA");
        }
        
        PlayerPrefs.SetInt("PetCount", pets.Count);
        Debug.Log($"[GameSaveManager] SavePetsToPlayerPrefs: PetCount установлен в {pets.Count}");
        
        for (int i = 0; i < pets.Count; i++)
        {
            PetData pet = pets[i];
            if (pet == null)
            {
                Debug.LogWarning($"[GameSaveManager] SavePetsToPlayerPrefs: Питомец с индексом {i} == null, пропущен");
                continue;
            }
            
            string petKey = $"Pet_{i}";
            PlayerPrefs.SetInt($"{petKey}_ID", pet.petID);
            PlayerPrefs.SetString($"{petKey}_Name", pet.petName ?? $"Питомец {pet.petID}");
            PlayerPrefs.SetInt($"{petKey}_Rarity", (int)pet.rarity);
            PlayerPrefs.SetFloat($"{petKey}_Size", pet.size);
            PlayerPrefs.SetString($"{petKey}_ModelPath", pet.petModelPath ?? "");
            
            // Сохранить цвет
            PlayerPrefs.SetFloat($"{petKey}_ColorR", pet.petColor.r);
            PlayerPrefs.SetFloat($"{petKey}_ColorG", pet.petColor.g);
            PlayerPrefs.SetFloat($"{petKey}_ColorB", pet.petColor.b);
            PlayerPrefs.SetFloat($"{petKey}_ColorA", pet.petColor.a);
            
            Debug.Log($"[GameSaveManager] SavePetsToPlayerPrefs: Сохранен питомец {i}: {pet.petName} (ID: {pet.petID}, Rarity: {pet.rarity})");
        }
        
        PlayerPrefs.Save();
        Debug.Log($"[GameSaveManager] SavePetsToPlayerPrefs: УСПЕХ! Сохранено {pets.Count} питомцев в PlayerPrefs, PlayerPrefs.Save() вызван");
        
        // Проверка сохранения
        int savedCount = PlayerPrefs.GetInt("PetCount", -1);
        if (savedCount != pets.Count)
        {
            Debug.LogError($"[GameSaveManager] SavePetsToPlayerPrefs: ОШИБКА! PetCount не совпадает! Ожидалось: {pets.Count}, сохранено: {savedCount}");
        }
        else
        {
            Debug.Log($"[GameSaveManager] SavePetsToPlayerPrefs: Проверка пройдена, PetCount = {savedCount}");
        }
    }
    
    /// <summary>
    /// Сохранить активных питомцев (ID питомцев, которые заспавнены в мире)
    /// </summary>
    private void SaveActivePets()
    {
        List<int> activePetIDs = new List<int>();
        
        if (PetSpawner.Instance != null)
        {
            List<PetData> activePets = PetSpawner.Instance.GetActivePetsList();
            foreach (PetData pet in activePets)
            {
                if (pet != null)
                {
                    activePetIDs.Add(pet.petID);
                }
            }
        }
        
        if (YG2.isSDKEnabled && YG2.saves != null)
        {
            YG2.saves.activePetIDs = activePetIDs;
        }
        
        // Также сохранить в PlayerPrefs
        PlayerPrefs.SetInt("ActivePetCount", activePetIDs.Count);
        for (int i = 0; i < activePetIDs.Count; i++)
        {
            PlayerPrefs.SetInt($"ActivePet_{i}", activePetIDs[i]);
        }
        PlayerPrefs.Save();
        
        Debug.Log($"[GameSaveManager] Сохранено активных питомцев: {activePetIDs.Count}");
    }
    
    /// <summary>
    /// Загрузить активных питомцев и заспавнить их
    /// </summary>
    private void LoadActivePets()
    {
        List<int> activePetIDs = new List<int>();
        
        // Загрузить из YG2.saves
        if (YG2.isSDKEnabled && YG2.saves != null && YG2.saves.activePetIDs != null && YG2.saves.activePetIDs.Count > 0)
        {
            activePetIDs = new List<int>(YG2.saves.activePetIDs);
            Debug.Log($"[GameSaveManager] Загружено активных питомцев из YG2: {activePetIDs.Count}");
        }
        else
        {
            // Fallback на PlayerPrefs
            if (PlayerPrefs.HasKey("ActivePetCount"))
            {
                int count = PlayerPrefs.GetInt("ActivePetCount");
                for (int i = 0; i < count; i++)
                {
                    if (PlayerPrefs.HasKey($"ActivePet_{i}"))
                    {
                        activePetIDs.Add(PlayerPrefs.GetInt($"ActivePet_{i}"));
                    }
                }
                Debug.Log($"[GameSaveManager] Загружено активных питомцев из PlayerPrefs: {activePetIDs.Count}");
            }
        }
        
        // Заспавнить активных питомцев
        if (activePetIDs.Count > 0 && PetInventory.Instance != null && PetSpawner.Instance != null)
        {
            List<PetData> allPets = PetInventory.Instance.GetAllPets();
            foreach (int petID in activePetIDs)
            {
                PetData pet = allPets.Find(p => p != null && p.petID == petID);
                if (pet != null)
                {
                    PetSpawner.Instance.SpawnPetInWorld(pet);
                    Debug.Log($"[GameSaveManager] Заспавнен активный питомец: {pet.petName} (ID: {petID})");
                }
            }
        }
    }
    
    /// <summary>
    /// Загрузить все данные из PlayerPrefs (для локального тестирования без SDK)
    /// </summary>
    private void LoadGameDataFromPlayerPrefs()
    {
        isLoading = true;
        
        try
        {
            // Загрузить монеты
            if (PlayerPrefs.HasKey("Coins"))
            {
                int coins = PlayerPrefs.GetInt("Coins");
                CoinManager.SetCoinsWithoutSave(coins);
                hasLoadedCoins = true;
                Debug.Log($"[GameSaveManager] Загружены монеты из PlayerPrefs: {coins}");
            }
            else
            {
                hasLoadedCoins = false;
                Debug.Log($"[GameSaveManager] Нет сохраненных монет в PlayerPrefs");
            }
            
            // Загрузить питомцев
            LoadPetsFromPlayerPrefs();
            
            // Загрузить активных питомцев
            LoadActivePets();
            
            // ВАЖНО: Проверить состояние игры ПЕРЕД загрузкой цены яйца
            // Если у игрока 0 питомцев, цена будет сброшена до 100
            CheckAndResetGameState();
            
            // Загрузить цену яйца (после проверки состояния, чтобы использовать сброшенную цену)
            if (PlayerPrefs.HasKey("EggPrice"))
            {
                int eggPrice = PlayerPrefs.GetInt("EggPrice");
                Debug.Log($"[GameSaveManager] Загружена цена яйца из PlayerPrefs: {eggPrice}");
            }
            else
            {
                // Установить начальную цену
                PlayerPrefs.SetInt("EggPrice", 100);
                PlayerPrefs.Save();
                Debug.Log($"[GameSaveManager] Установлена начальная цена яйца: 100");
            }
            
            isDataLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при загрузке данных из PlayerPrefs: {e.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
#else
    // Если модуль Storage не подключен, использовать PlayerPrefs
    public void SaveGameData()
    {
        SaveGameDataToPlayerPrefs();
    }
    
    public void ForceLoadGameData()
    {
        LoadGameDataFromPlayerPrefs();
    }
    
    /// <summary>
    /// Сохранить все данные в PlayerPrefs
    /// </summary>
    private void SaveGameDataToPlayerPrefs()
    {
        try
        {
            // Сохранить монеты
            int coins = CoinManager.GetCoins();
            PlayerPrefs.SetInt("Coins", coins);
            
            // Сохранить питомцев
            PetInventory inventory = PetInventory.Instance;
            if (inventory != null)
            {
                List<PetData> pets = inventory.GetAllPets();
                SavePetsToPlayerPrefs(pets);
            }
            
            // Сохранить активных питомцев
            SaveActivePets();
            
            // Сохранить цену яйца
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                int eggPrice = shopManager.GetEggPrice();
                PlayerPrefs.SetInt("EggPrice", eggPrice);
            }
            
            PlayerPrefs.Save();
            Debug.Log("[GameSaveManager] Данные сохранены в PlayerPrefs!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при сохранении данных в PlayerPrefs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Загрузить все данные из PlayerPrefs
    /// </summary>
    private void LoadGameDataFromPlayerPrefs()
    {
        isLoading = true;
        
        try
        {
            // Загрузить монеты
            if (PlayerPrefs.HasKey("Coins"))
            {
                int coins = PlayerPrefs.GetInt("Coins");
                CoinManager.SetCoinsWithoutSave(coins);
                hasLoadedCoins = true;
                Debug.Log($"[GameSaveManager] Загружены монеты из PlayerPrefs: {coins}");
            }
            else
            {
                hasLoadedCoins = false;
                Debug.Log($"[GameSaveManager] Нет сохраненных монет в PlayerPrefs");
            }
            
            // Загрузить питомцев
            LoadPetsFromPlayerPrefs();
            
            // Загрузить активных питомцев
            LoadActivePets();
            
            // ВАЖНО: Проверить состояние игры ПЕРЕД загрузкой цены яйца
            // Если у игрока 0 питомцев, цена будет сброшена до 100
            CheckAndResetGameState();
            
            // Загрузить цену яйца (после проверки состояния, чтобы использовать сброшенную цену)
            if (PlayerPrefs.HasKey("EggPrice"))
            {
                int eggPrice = PlayerPrefs.GetInt("EggPrice");
                Debug.Log($"[GameSaveManager] Загружена цена яйца из PlayerPrefs: {eggPrice}");
            }
            else
            {
                // Установить начальную цену
                PlayerPrefs.SetInt("EggPrice", 100);
                PlayerPrefs.Save();
                Debug.Log($"[GameSaveManager] Установлена начальная цена яйца: 100");
            }
            
            isDataLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveManager] Ошибка при загрузке данных из PlayerPrefs: {e.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
    
    /// <summary>
    /// Загрузить питомцев из PlayerPrefs
    /// </summary>
    private void LoadPetsFromPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey("PetCount"))
        {
            Debug.Log("[GameSaveManager] Нет сохраненных питомцев в PlayerPrefs");
            return;
        }
        
        int petCount = PlayerPrefs.GetInt("PetCount");
        if (petCount <= 0)
        {
            return;
        }
        
        PetInventory inventory = PetInventory.Instance;
        if (inventory == null)
        {
            return;
        }
        
        inventory.ClearInventory();
        
        for (int i = 0; i < petCount; i++)
        {
            string petKey = $"Pet_{i}";
            if (PlayerPrefs.HasKey($"{petKey}_ID"))
            {
                // Загрузить данные питомца из PlayerPrefs
                int petID = PlayerPrefs.GetInt($"{petKey}_ID");
                string petName = PlayerPrefs.GetString($"{petKey}_Name", $"Питомец {petID}");
                int rarityInt = PlayerPrefs.GetInt($"{petKey}_Rarity", 0);
                float size = PlayerPrefs.GetFloat($"{petKey}_Size", 1f);
                string modelPath = PlayerPrefs.GetString($"{petKey}_ModelPath", "");
                
                // Восстановить цвет
                float r = PlayerPrefs.GetFloat($"{petKey}_ColorR", 1f);
                float g = PlayerPrefs.GetFloat($"{petKey}_ColorG", 1f);
                float b = PlayerPrefs.GetFloat($"{petKey}_ColorB", 1f);
                float a = PlayerPrefs.GetFloat($"{petKey}_ColorA", 1f);
                Color petColor = new Color(r, g, b, a);
                
                PetRarity rarity = (PetRarity)rarityInt;
                
                PetData petData = new PetData(rarity, petName, petID, size, petColor);
                petData.petModelPath = modelPath;
                
                inventory.AddPetWithoutSave(petData);
            }
        }
        
        Debug.Log($"[GameSaveManager] Загружено питомцев из PlayerPrefs: {petCount}");
    }
    
    /// <summary>
    /// Сохранить питомцев в PlayerPrefs
    /// </summary>
    private void SavePetsToPlayerPrefs(List<PetData> pets)
    {
        if (pets == null)
        {
            return;
        }
        
        PlayerPrefs.SetInt("PetCount", pets.Count);
        
        for (int i = 0; i < pets.Count; i++)
        {
            PetData pet = pets[i];
            if (pet == null)
            {
                continue;
            }
            
            string petKey = $"Pet_{i}";
            PlayerPrefs.SetInt($"{petKey}_ID", pet.petID);
            PlayerPrefs.SetString($"{petKey}_Name", pet.petName);
            PlayerPrefs.SetInt($"{petKey}_Rarity", (int)pet.rarity);
            PlayerPrefs.SetFloat($"{petKey}_Size", pet.size);
            PlayerPrefs.SetString($"{petKey}_ModelPath", pet.petModelPath ?? "");
            
            // Сохранить цвет
            PlayerPrefs.SetFloat($"{petKey}_ColorR", pet.petColor.r);
            PlayerPrefs.SetFloat($"{petKey}_ColorG", pet.petColor.g);
            PlayerPrefs.SetFloat($"{petKey}_ColorB", pet.petColor.b);
            PlayerPrefs.SetFloat($"{petKey}_ColorA", pet.petColor.a);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"[GameSaveManager] Сохранено питомцев в PlayerPrefs: {pets.Count}");
    }
    
    /// <summary>
    /// Сохранить активных питомцев
    /// </summary>
    private void SaveActivePets()
    {
        List<int> activePetIDs = new List<int>();
        
        if (PetSpawner.Instance != null)
        {
            List<PetData> activePets = PetSpawner.Instance.GetActivePetsList();
            foreach (PetData pet in activePets)
            {
                if (pet != null)
                {
                    activePetIDs.Add(pet.petID);
                }
            }
        }
        
        PlayerPrefs.SetInt("ActivePetCount", activePetIDs.Count);
        for (int i = 0; i < activePetIDs.Count; i++)
        {
            PlayerPrefs.SetInt($"ActivePet_{i}", activePetIDs[i]);
        }
        PlayerPrefs.Save();
        
        Debug.Log($"[GameSaveManager] Сохранено активных питомцев: {activePetIDs.Count}");
    }
    
    /// <summary>
    /// Загрузить активных питомцев и заспавнить их
    /// </summary>
    private void LoadActivePets()
    {
        List<int> activePetIDs = new List<int>();
        
        if (PlayerPrefs.HasKey("ActivePetCount"))
        {
            int count = PlayerPrefs.GetInt("ActivePetCount");
            for (int i = 0; i < count; i++)
            {
                if (PlayerPrefs.HasKey($"ActivePet_{i}"))
                {
                    activePetIDs.Add(PlayerPrefs.GetInt($"ActivePet_{i}"));
                }
            }
            Debug.Log($"[GameSaveManager] Загружено активных питомцев из PlayerPrefs: {activePetIDs.Count}");
        }
        
        // Заспавнить активных питомцев
        if (activePetIDs.Count > 0 && PetInventory.Instance != null && PetSpawner.Instance != null)
        {
            List<PetData> allPets = PetInventory.Instance.GetAllPets();
            foreach (int petID in activePetIDs)
            {
                PetData pet = allPets.Find(p => p != null && p.petID == petID);
                if (pet != null)
                {
                    PetSpawner.Instance.SpawnPetInWorld(pet);
                    Debug.Log($"[GameSaveManager] Заспавнен активный питомец: {pet.petName} (ID: {petID})");
                }
            }
        }
    }
#endif
}

