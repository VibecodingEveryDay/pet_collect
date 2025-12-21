    using UnityEngine;

/// <summary>
/// Компонент для поведения питомца в мире
/// </summary>
public class PetBehavior : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float searchInterval = 1f; // Интервал поиска кристаллов в секундах
    [SerializeField] private float miningDistance = 1f; // Расстояние для начала добычи (уменьшено в два раза)
    [SerializeField] private float walkSwayAngle = 12f; // Максимальный угол поворота влево-вправо при ходьбе (градусы)
    [SerializeField] private float walkSwaySpeed = 6f; // Скорость изменения поворота при ходьбе
    
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
    
    // Для имитации естественной ходьбы (повороты влево-вправо)
    private float walkSwayOffset = 0f; // Текущее смещение поворота
    private float walkSwayTarget = 0f; // Целевое смещение поворота
    
    // Эффект добычи
    private GameObject miningEffect; // Эффект электричества во время добычи
    
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
        
        // Сбросить случайный поворот при остановке для добычи
        walkSwayOffset = 0f;
        walkSwayTarget = 0f;
        
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
    }
    
    /// <summary>
    /// Остановить добычу
    /// </summary>
    private void StopMining()
    {
        positionLocked = false;
        isMining = false;
        
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
        
        // Если не назначен в инспекторе, попробовать загрузить из Resources
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
            Debug.LogWarning("[PetBehavior] Не удалось загрузить эффект vfx_Electricity_01. Назначьте префаб в инспекторе или поместите его в папку Resources.");
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
        
        // Обновить случайный поворот для имитации ходьбы
        UpdateWalkSway();
        
        // Применить поворот с учетом случайного отклонения
        float finalYRotation = baseYRotation + walkSwayOffset;
        transform.rotation = Quaternion.Euler(90, finalYRotation, 0);
        
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
    /// Обновить случайный поворот для имитации естественной ходьбы
    /// </summary>
    private void UpdateWalkSway()
    {
        // Если достигли целевого смещения, выбрать новое случайное значение
        if (Mathf.Abs(walkSwayOffset - walkSwayTarget) < 0.1f)
        {
            // Выбрать новое случайное целевое смещение от -walkSwayAngle до +walkSwayAngle
            walkSwayTarget = Random.Range(-walkSwayAngle, walkSwayAngle);
        }
        
        // Плавно переходить к целевому смещению
        walkSwayOffset = Mathf.Lerp(walkSwayOffset, walkSwayTarget, Time.deltaTime * walkSwaySpeed);
    }
    
    /// <summary>
    /// Добывать кристалл
    /// </summary>
    private void MineCrystal()
    {
        if (targetCrystal == null || !targetCrystal.IsAlive())
        {
            if (targetCrystal != null)
            {
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
        float damage = baseMiningRate * rarityMultiplier * Time.deltaTime;
        
        // Нанести урон кристаллу
        targetCrystal.TakeDamage(damage);
        
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
                transform.rotation = Quaternion.Euler(currentEuler.x, yRotation, currentEuler.z);
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
        
        // Очистка при уничтожении
        targetCrystal = null;
        isMining = false;
    }
}

