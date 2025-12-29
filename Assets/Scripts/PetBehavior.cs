using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент для поведения питомца в мире
/// </summary>
public class PetBehavior : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float searchInterval = 1f; // Интервал поиска кристаллов в секундах
    [SerializeField] private float miningDistance = 1f; // Расстояние для начала добычи (уменьшено в два раза)
    
    [Header("Настройки высоты")]
    [SerializeField] private float fixedYPosition = 0f; // Фиксированная позиция Y для питомцев
    
    [Header("Эффекты")]
    [SerializeField] private GameObject miningEffectPrefab; // Префаб эффекта электричества при добыче
    
    private PetData petData;
    private Crystal targetCrystal;
    private float lastSearchTime;
    private bool isMining = false;
    private float baseMiningRate = 5f; // 5 HP в секунду
    
    // Для добавления монет каждую секунду
    private float lastCoinTime = 0f;
    private float coinInterval = 1f; // Интервал добавления монет (1 секунда)
    
    // Позиция и направление при начале добычи
    private Vector3 miningPosition;
    private Vector3 miningDirection;
    private Vector3 crystalMiningPosition; // Позиция кристалла в момент начала добычи (для поворота)
    private bool positionLocked = false; // Флаг блокировки позиции
    private Rigidbody petRigidbody; // Кэш Rigidbody для отключения физики во время добычи
    
    
    // Эффект добычи
    private GameObject miningEffect; // Эффект электричества во время добычи
    
    // Для правильного вращения визуальной модели
    private Transform visualModelTransform; // Transform визуальной модели (дочерний объект с рендерером)
    private Vector3 visualModelOffset; // Смещение визуальной модели относительно корня
    
    // Система ускорения
    private bool isBoosted = false; // Флаг, что питомец ускорен
    private float boostEndTime = 0f; // Время окончания эффекта ускорения
    private GameObject speedBoostEffect; // VFX эффект ускорения
    private Vector3 originalScale; // Исходный размер питомца
    
    /// <summary>
    /// Инициализировать поведение питомца
    /// </summary>
    public void Initialize(PetData data)
    {
        petData = data;
        lastSearchTime = Time.time;
        lastCoinTime = Time.time; // Инициализировать время для монет
        
        // Получить или создать Rigidbody для контроля физики
        petRigidbody = GetComponent<Rigidbody>();
        if (petRigidbody == null)
        {
            petRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // Настроить Rigidbody для кинематического движения
        petRigidbody.isKinematic = true; // Используем кинематический режим для полного контроля позиции
        petRigidbody.useGravity = false; // Отключаем гравитацию
        
        // Установить фиксированную позицию Y
        Vector3 pos = transform.position;
        pos.y = fixedYPosition;
        transform.position = pos;
        
        // Найти визуальную модель (дочерний объект с рендерером)
        FindVisualModel();
        
        // Сохранить исходный размер после небольшой задержки (чтобы модель успела загрузиться)
        StartCoroutine(SaveOriginalScale());
    }
    
    /// <summary>
    /// Сохранить исходный размер питомца
    /// </summary>
    private IEnumerator SaveOriginalScale()
    {
        yield return null; // Подождать один кадр
        
        if (visualModelTransform != null)
        {
            originalScale = visualModelTransform.localScale;
        }
        else
        {
            originalScale = transform.localScale;
        }
    }
    
    /// <summary>
    /// Найти визуальную модель питомца для правильного вращения
    /// </summary>
    private void FindVisualModel()
    {
        // Найти первый дочерний объект с рендерером (это должна быть визуальная модель)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length > 0)
        {
            // Найти самый глубокий дочерний объект с рендерером или первый найденный
            Renderer visualRenderer = renderers[0];
            
            // Если есть несколько, выбираем тот, который не является UI элементом
            foreach (Renderer r in renderers)
            {
                if (r != null && !r.GetComponent<Canvas>() && r.gameObject.activeInHierarchy)
                {
                    visualRenderer = r;
                    break;
                }
            }
            
            if (visualRenderer != null)
            {
                visualModelTransform = visualRenderer.transform;
                
                // Сохранить смещение визуальной модели относительно корня (в локальных координатах)
                visualModelOffset = visualModelTransform.localPosition;
                
                Debug.Log($"[PetBehavior] Найдена визуальная модель: {visualRenderer.name}, Offset: {visualModelOffset}");
            }
        }
        
        // Если визуальная модель не найдена, использовать корневой объект
        if (visualModelTransform == null)
        {
            visualModelTransform = transform;
            visualModelOffset = Vector3.zero;
            Debug.LogWarning("[PetBehavior] Визуальная модель не найдена, используется корневой объект");
        }
    }
    
    /// <summary>
    /// Установить фиксированную позицию Y (вызывается из PetSpawner)
    /// </summary>
    public void SetFixedYPosition(float yPosition)
    {
        fixedYPosition = yPosition;
        Vector3 pos = transform.position;
        pos.y = fixedYPosition;
        transform.position = pos;
    }
    
    private void Update()
    {
        if (petData == null)
        {
            return;
        }
        
        // Проверить, истек ли эффект ускорения
        if (isBoosted && Time.time >= boostEndTime)
        {
            EndSpeedBoost();
        }
        
        // Если позиция заблокирована (во время добычи), только добывать
        if (positionLocked && isMining)
        {
            MineCrystal();
            return;
        }
        
        // Поиск кристалла каждые N секунд (только если не добываем)
        if (!isMining && Time.time - lastSearchTime >= searchInterval)
        {
            FindNearestCrystal();
            lastSearchTime = Time.time;
        }
        
        // Если есть целевой кристалл
        if (targetCrystal != null && targetCrystal.IsAlive())
        {
            float distance = Vector3.Distance(transform.position, targetCrystal.transform.position);
            
            if (distance <= miningDistance)
            {
                // Достигли нужного расстояния - начать добычу
                if (!isMining || !positionLocked)
                {
                    StartMining();
                }
                
                // Добывать кристалл (позиция фиксируется в LateUpdate)
                MineCrystal();
            }
            else
            {
                // Двигаться к кристаллу только если не добываем
                if (!isMining)
                {
                    MoveToCrystal();
                }
                else
                {
                    // Сбросить состояние добычи, если расстояние слишком большое
                    if (distance > miningDistance * 2f)
                    {
                        StopMining();
                    }
                }
            }
        }
        else
        {
            // Кристалл уничтожен или не найден
            if (targetCrystal != null)
            {
                CrystalManager.UnregisterPetMining(targetCrystal, this);
            }
            StopMining();
            targetCrystal = null;
        }
    }
    
    private void LateUpdate()
    {
        // В LateUpdate фиксируем позицию, чтобы она устанавливалась после всех обновлений
        if (positionLocked && isMining)
        {
            Vector3 fixedPos = miningPosition;
            fixedPos.y = fixedYPosition;
            transform.position = fixedPos;
        }
    }
    
    /// <summary>
    /// Начать добычу
    /// </summary>
    private void StartMining()
    {
        // Сохранить текущую позицию и направление
        miningPosition = transform.position;
        miningPosition.y = fixedYPosition;
        
        // Сохранить позицию кристалла в момент начала добычи (до тряски)
        if (targetCrystal != null)
        {
            crystalMiningPosition = targetCrystal.transform.position;
            miningDirection = new Vector3(crystalMiningPosition.x - transform.position.x, 0, crystalMiningPosition.z - transform.position.z).normalized;
        }
        
        if (miningDirection == Vector3.zero)
        {
            miningDirection = transform.forward;
        }
        
        
        // Отключить физику, если есть Rigidbody
        if (petRigidbody != null)
        {
            petRigidbody.isKinematic = true;
            petRigidbody.linearVelocity = Vector3.zero;
            petRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Заблокировать позицию
        positionLocked = true;
        isMining = true;
        
        // Создать эффект электричества при добыче
        CreateMiningEffect();
        
        // Показать эмоцию при начале добычи
        PetEmotionUI emotionUI = GetComponent<PetEmotionUI>();
        if (emotionUI != null)
        {
            emotionUI.ShowMiningStartEmotion();
        }
    }
    
    /// <summary>
    /// Остановить добычу
    /// </summary>
    private void StopMining()
    {
        positionLocked = false;
        isMining = false;
        
        // Скрыть эмоцию добычи
        PetEmotionUI emotionUI = GetComponent<PetEmotionUI>();
        if (emotionUI != null)
        {
            emotionUI.HideEmotion();
        }
        
        // Уничтожить эффект добычи
        DestroyMiningEffect();
        
        // Включить физику обратно, если нужно (но оставляем kinematic для контроля)
        if (petRigidbody != null)
        {
            petRigidbody.isKinematic = true; // Оставляем kinematic для полного контроля
        }
    }
    
    /// <summary>
    /// Проверить, добывает ли питомец сейчас кристалл
    /// </summary>
    public bool IsMining()
    {
        return isMining && positionLocked;
    }
    
    /// <summary>
    /// Создать эффект электричества при добыче кристалла
    /// </summary>
    private void CreateMiningEffect()
    {
        // Уничтожить предыдущий эффект, если есть
        DestroyMiningEffect();
        
        if (targetCrystal == null)
        {
            return;
        }
        
        // Загрузить префаб эффекта
        GameObject effectPrefab = miningEffectPrefab;
        
        // Если не назначен в инспекторе, попробовать загрузить из Resources (работает и в редакторе, и в билде)
        if (effectPrefab == null)
        {
            effectPrefab = Resources.Load<GameObject>("vfx_Electricity_01");
        }
        
        // Если не найдено в Resources, попробовать загрузить через AssetDatabase (только в редакторе)
        #if UNITY_EDITOR
        if (effectPrefab == null)
        {
            effectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downloads/GabrielAguiarProductions/FreeQuickEffectsVol1/Prefabs/vfx_Electricity_01.prefab");
        }
        #endif
        
        if (effectPrefab == null)
        {
            return;
        }
        
        // Позиция эффекта по центру кристалла
        Vector3 effectPosition = crystalMiningPosition;
        
        // Создать эффект по центру кристалла
        miningEffect = Instantiate(effectPrefab, effectPosition, Quaternion.identity);
        
        // Сделать эффект дочерним объектом кристалла, чтобы он следовал за ним
        if (targetCrystal != null)
        {
            miningEffect.transform.SetParent(targetCrystal.transform);
            miningEffect.transform.localPosition = Vector3.zero; // Центр кристалла
        }
    }
    
    /// <summary>
    /// Уничтожить эффект добычи
    /// </summary>
    private void DestroyMiningEffect()
    {
        if (miningEffect != null)
        {
            Destroy(miningEffect);
            miningEffect = null;
        }
    }
    
    /// <summary>
    /// Найти ближайший кристалл
    /// </summary>
    private void FindNearestCrystal()
    {
        if (targetCrystal != null && targetCrystal.IsAlive())
        {
            // Уже есть целевой кристалл
            return;
        }
        
        // Освободить предыдущий кристалл, если был
        if (targetCrystal != null)
        {
            CrystalManager.UnregisterPetMining(targetCrystal, this);
        }
        
        // Найти ближайший незанятый кристалл
        targetCrystal = CrystalManager.GetNearestCrystal(transform.position, this);
        
        if (targetCrystal != null)
        {
            // Зарегистрировать, что этот питомец добывает этот кристалл
            CrystalManager.RegisterPetMining(targetCrystal, this);
        }
    }
    
    /// <summary>
    /// Двигаться к кристаллу
    /// </summary>
    private void MoveToCrystal()
    {
        if (targetCrystal == null || positionLocked || isMining)
        {
            return;
        }
        
        Vector3 targetPosition = targetCrystal.transform.position;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Если уже достаточно близко, начать добычу немедленно
        if (distance <= miningDistance)
        {
            StartMining();
            return;
        }
        
        // Вычислить направление только по горизонтали
        Vector3 directionVector = new Vector3(targetPosition.x - transform.position.x, 0, targetPosition.z - transform.position.z);
        float horizontalDistance = directionVector.magnitude;
        
        // Если горизонтальное расстояние очень маленькое (меньше 0.1), значит питомец уже достаточно близко
        if (horizontalDistance < 0.1f)
        {
            StartMining();
            return;
        }
        
        // Нормализовать направление
        Vector3 direction = directionVector.normalized;
        
        // Если направление все еще нулевое (не должно произойти после проверки выше), начать добычу
        if (direction == Vector3.zero)
        {
            StartMining();
            return;
        }
        
        // Вычислить базовый поворот к кристаллу (сохраняя поворот по X для правильной ориентации)
        // Разворачиваем на 180 градусов, чтобы питомец шёл лицом, а не спиной
        float baseYRotation = Quaternion.LookRotation(direction).eulerAngles.y + 180f;
        
        // Применить поворот без качания
        // Применить вращение к визуальной модели, а не к корневому объекту
        // Это исправляет проблему с неправильным pivot
        if (visualModelTransform != null && visualModelTransform != transform)
        {
            // Вращаем визуальную модель относительно корня
            visualModelTransform.localRotation = Quaternion.Euler(90, baseYRotation, 0);
            
            // Сохраняем локальную позицию (pivot должен быть в центре модели)
            visualModelTransform.localPosition = visualModelOffset;
            
            // Корневой объект не вращаем, только позиционируем
            transform.rotation = Quaternion.identity;
        }
        else
        {
            // Если визуальная модель не найдена, использовать стандартное вращение
            transform.rotation = Quaternion.Euler(90, baseYRotation, 0);
        }
        
        // Вычислить максимальное расстояние, на которое можно двигаться
        // Остановиться точно на miningDistance
        float maxMoveDistance = distance - miningDistance;
        float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, maxMoveDistance);
        
        // Если moveDistance очень маленький или отрицательный, значит мы уже достаточно близко
        if (maxMoveDistance <= 0.1f)
        {
            StartMining();
            return;
        }
        
        // Двигаться вперед по горизонтали
        Vector3 newPosition = transform.position + direction * moveDistance;
        
        // Сохранить фиксированную позицию Y
        newPosition.y = fixedYPosition;
        
        transform.position = newPosition;
    }
    
    
    
    /// <summary>
    /// Добывать кристалл
    /// </summary>
    private void MineCrystal()
    {
        if (targetCrystal == null || !targetCrystal.IsAlive())
        {
            // Кристалл уничтожен - показать эмоцию
            if (targetCrystal != null)
            {
                PetEmotionUI emotionUI = GetComponent<PetEmotionUI>();
                if (emotionUI != null)
                {
                    emotionUI.ShowMiningCompleteEmotion();
                }
                
                CrystalManager.UnregisterPetMining(targetCrystal, this);
            }
            StopMining();
            targetCrystal = null;
            return;
        }
        
        // Позиция эффекта обновляется автоматически, так как он дочерний объект кристалла
        
        // НЕ устанавливать позицию здесь - это делается в LateUpdate
        // Только добывать кристалл
        
        // Вычислить множитель скорости добычи по редкости
        float rarityMultiplier = GetRarityMiningMultiplier(petData.rarity);
        
        // Получить множитель карты (1.5x если куплена улучшенная карта)
        float mapMultiplier = MapUpgradeSystem.IsMapUpgradePurchased() ? 1.5f : 1f;
        
        // Получить множитель ускорения (1.5x если питомец ускорен)
        float boostMultiplier = isBoosted ? 1.5f : 1f;
        
        float damage = baseMiningRate * rarityMultiplier * mapMultiplier * boostMultiplier * Time.deltaTime;
        
        // Сохранить состояние кристалла до нанесения урона
        bool wasAlive = targetCrystal != null && targetCrystal.IsAlive();
        float healthBefore = targetCrystal != null ? targetCrystal.GetCurrentHealth() : 0f;
        
        // Нанести урон кристаллу
        if (targetCrystal != null)
        {
            targetCrystal.TakeDamage(damage);
            
            // Проверить, был ли кристалл уничтожен после нанесения урона
            // Проверяем healthAfter, так как Destroy() может быть вызван, но объект еще не уничтожен
            float healthAfter = targetCrystal != null ? targetCrystal.GetCurrentHealth() : 0f;
            bool wasDestroyed = wasAlive && healthBefore > 0 && healthAfter <= 0;
            
            if (wasDestroyed)
            {
                // Кристалл только что уничтожен - показать эмоцию с небольшой задержкой
                StartCoroutine(ShowMiningCompleteEmotionDelayed());
            }
        }
        
        // Повернуться к кристаллу (используем сохраненную позицию, а не текущую трясущуюся)
        if (targetCrystal != null)
        {
            // Использовать сохраненную позицию кристалла в момент начала добычи, чтобы питомец не трясся вместе с кристаллом
            Vector3 directionToCrystal = crystalMiningPosition - transform.position;
            directionToCrystal.y = 0; // Игнорировать вертикальную составляющую
            
            if (directionToCrystal != Vector3.zero)
            {
                // Вычислить поворот к кристаллу
                Quaternion targetRotation = Quaternion.LookRotation(directionToCrystal.normalized);
                
                // Сохранить текущий поворот по X и Z, изменить только Y
                Vector3 currentEuler = transform.rotation.eulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;
                
                // Плавно повернуть к кристаллу
                float yRotation = Mathf.LerpAngle(currentEuler.y, targetEuler.y, Time.deltaTime * 5f);
                
                // Применить вращение к визуальной модели, а не к корневому объекту
                if (visualModelTransform != null && visualModelTransform != transform)
                {
                    visualModelTransform.localRotation = Quaternion.Euler(currentEuler.x, yRotation, currentEuler.z);
                    visualModelTransform.localPosition = visualModelOffset;
                    transform.rotation = Quaternion.identity;
                }
                else
                {
                transform.rotation = Quaternion.Euler(currentEuler.x, yRotation, currentEuler.z);
                }
            }
        }
        
        // Добавлять монеты каждую секунду добычи
        if (Time.time - lastCoinTime >= coinInterval)
        {
            int coinsToAdd = 1; // Базовое количество монет
            switch (petData.rarity)
            {
                case PetRarity.Epic:
                    coinsToAdd = 2;
                    break;
                case PetRarity.Legendary:
                    coinsToAdd = 3;
                    break;
            }
            
            CoinManager.AddCoins(coinsToAdd);
            lastCoinTime = Time.time;
        }
    }
    
    /// <summary>
    /// Получить множитель скорости добычи по редкости
    /// </summary>
    private float GetRarityMiningMultiplier(PetRarity rarity)
    {
        switch (rarity)
        {
            case PetRarity.Common:
                return 1f; // 1x
            case PetRarity.Epic:
                return 2f; // 2x
            case PetRarity.Legendary:
                return 3f; // 3x
            default:
                return 1f;
        }
    }
    
    private void OnDestroy()
    {
        // Освободить кристалл при уничтожении
        if (targetCrystal != null)
        {
            CrystalManager.UnregisterPetMining(targetCrystal, this);
        }
        
        // Уничтожить эффект добычи при уничтожении питомца
        DestroyMiningEffect();
        
        // Уничтожить эффект ускорения при уничтожении питомца
        if (speedBoostEffect != null)
        {
            Destroy(speedBoostEffect);
            speedBoostEffect = null;
        }
        
        // Если питомец был ускорен, уведомить менеджер
        if (isBoosted && PetSpeedBoostManager.Instance != null)
        {
            PetSpeedBoostManager.Instance.OnBoostEffectEnded();
        }
        
        // Скрыть эмоцию при уничтожении
        PetEmotionUI emotionUI = GetComponent<PetEmotionUI>();
        if (emotionUI != null)
        {
            emotionUI.HideEmotion();
        }
        
        // Очистка при уничтожении
        targetCrystal = null;
        isMining = false;
    }
    
    /// <summary>
    /// Показать эмоцию окончания добычи с задержкой
    /// </summary>
    private IEnumerator ShowMiningCompleteEmotionDelayed()
    {
        // Подождать один кадр, чтобы убедиться, что кристалл действительно уничтожен
        yield return null;
        
        PetEmotionUI emotionUI = GetComponent<PetEmotionUI>();
        if (emotionUI != null)
        {
            emotionUI.ShowMiningCompleteEmotion();
        }
    }
    
    /// <summary>
    /// Проверить, ускорен ли питомец
    /// </summary>
    public bool IsBoosted()
    {
        return isBoosted;
    }
    
    /// <summary>
    /// Применить ускорение питомцу
    /// </summary>
    public void ApplySpeedBoost()
    {
        if (isBoosted) return; // Уже ускорен
        
        isBoosted = true;
        boostEndTime = Time.time + 5f; // Эффект длится 5 секунд
        
        // Сохранить флаг использования ускорения
        PlayerPrefs.SetInt("HasUsedSpeedBoost", 1);
        PlayerPrefs.Save();
        
        // Создать VFX эффект
        CreateSpeedBoostEffect();
        
        // Запустить анимацию увеличения
        StartCoroutine(ScaleUpAnimation());
        
        Debug.Log($"[PetBehavior] Питомец {petData?.petName} ускорен! Эффект продлится 5 секунд.");
    }
    
    /// <summary>
    /// Создать VFX эффект ускорения
    /// </summary>
    private void CreateSpeedBoostEffect()
    {
        // Загрузить префаб эффекта
        GameObject effectPrefab = null;
        
        // Попробовать загрузить из Resources
        effectPrefab = Resources.Load<GameObject>("vfx_Implosion_01");
        
        // Если не найдено в Resources, попробовать загрузить через AssetDatabase (только в редакторе)
        #if UNITY_EDITOR
        if (effectPrefab == null)
        {
            // Попробовать разные пути
            string[] possiblePaths = {
                "Assets/vfx_Implosion_01.prefab",
                "Assets/Assets/vfx_Implosion_01.prefab",
                "Assets/Effects/vfx_Implosion_01.prefab",
                "Assets/VFX/vfx_Implosion_01.prefab"
            };
            
            foreach (string path in possiblePaths)
            {
                effectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (effectPrefab != null) break;
            }
        }
        #endif
        
        if (effectPrefab != null)
        {
            // Создать эффект на позиции питомца
            speedBoostEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            
            // Сделать эффект дочерним объектом питомца
            speedBoostEffect.transform.SetParent(transform);
        }
        else
        {
            Debug.LogWarning("[PetBehavior] Не удалось загрузить эффект vfx_Implosion_01");
        }
    }
    
    /// <summary>
    /// Анимация увеличения питомца на 30% за 1 секунду
    /// </summary>
    private IEnumerator ScaleUpAnimation()
    {
        Transform targetTransform = visualModelTransform != null ? visualModelTransform : transform;
        
        // Если originalScale еще не был сохранен, сохранить текущий размер
        if (originalScale == Vector3.zero)
        {
            originalScale = targetTransform.localScale;
        }
        
        Vector3 startScale = targetTransform.localScale;
        Vector3 targetScale = startScale * 1.3f; // Увеличить на 30%
        
        float duration = 1f; // 1 секунда
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Плавная интерполяция
            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null;
        }
        
        // Убедиться, что достигли целевого размера
        targetTransform.localScale = targetScale;
    }
    
    /// <summary>
    /// Завершить эффект ускорения
    /// </summary>
    private void EndSpeedBoost()
    {
        if (!isBoosted) return;
        
        isBoosted = false;
        
        // Уничтожить VFX эффект
        if (speedBoostEffect != null)
        {
            Destroy(speedBoostEffect);
            speedBoostEffect = null;
        }
        
        // Вернуть исходный размер
        StartCoroutine(ScaleDownAnimation());
        
        // Уведомить менеджер, что эффект закончился
        if (PetSpeedBoostManager.Instance != null)
        {
            PetSpeedBoostManager.Instance.OnBoostEffectEnded();
        }
        
        Debug.Log($"[PetBehavior] Эффект ускорения питомца {petData?.petName} закончился.");
    }
    
    /// <summary>
    /// Анимация уменьшения питомца до исходного размера
    /// </summary>
    private IEnumerator ScaleDownAnimation()
    {
        Transform targetTransform = visualModelTransform != null ? visualModelTransform : transform;
        Vector3 currentScale = targetTransform.localScale;
        
        float duration = 0.5f; // 0.5 секунды для уменьшения
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Плавная интерполяция обратно к исходному размеру
            targetTransform.localScale = Vector3.Lerp(currentScale, originalScale, t);
            
            yield return null;
        }
        
        // Убедиться, что вернулись к исходному размеру
        targetTransform.localScale = originalScale;
    }
}

