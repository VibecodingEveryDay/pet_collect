using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

/// <summary>
/// Компонент для отображения эмоций над питомцем
/// </summary>
public class PetEmotionUI : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float displayDuration = 1.7f; // Длительность отображения эмоции
    [SerializeField] private float floatSpeed = 2f; // Скорость всплытия
    [SerializeField] private float floatDistance = 3.5f; // Расстояние всплытия (увеличено для большей высоты)
    [SerializeField] private float heightOffset = 1.0f; // Дополнительное смещение вверх от верхней точки питомца
    
    [Header("Спрайты эмоций")]
    [SerializeField] private Sprite spawnEmotion; // Эмоция при спавне (02)
    [SerializeField] private Sprite miningStartEmotion; // Эмоция при начале добычи (23)
    [SerializeField] private Sprite miningCompleteEmotion; // Эмоция при окончании добычи (36)
    [SerializeField] private Sprite activeEmotion; // Эмоция при появлении в активных (41)
    
    private Canvas emotionCanvas;
    private Image emotionImage;
    private RectTransform canvasRect;
    private Camera mainCamera;
    private Coroutine currentEmotionCoroutine;
    private Sprite currentEmotionSprite;
    private bool isEmotionActive = false;
    
    private void Awake()
    {
        Debug.Log($"[PetEmotionUI] Awake вызван для {gameObject.name}");
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[PetEmotionUI] Main camera не найдена!");
            // Попробовать найти камеру по тегу
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObj != null)
            {
                mainCamera = cameraObj.GetComponent<Camera>();
                Debug.Log($"[PetEmotionUI] Камера найдена по тегу: {mainCamera != null}");
            }
        }
        else
        {
            Debug.Log($"[PetEmotionUI] Main camera найдена: {mainCamera.name}");
        }
        CreateEmotionUI();
        // Загрузить спрайты эмоций сразу в Awake, чтобы они были доступны при первом вызове
        LoadEmotionSprites();
        Debug.Log($"[PetEmotionUI] Спрайты загружены: spawn={spawnEmotion != null}, miningStart={miningStartEmotion != null}, miningComplete={miningCompleteEmotion != null}, active={activeEmotion != null}");
    }
    
    /// <summary>
    /// Создать UI для эмоций
    /// </summary>
    private void CreateEmotionUI()
    {
        // Создать Canvas для эмоций
        GameObject canvasObj = new GameObject("EmotionCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        
        emotionCanvas = canvasObj.AddComponent<Canvas>();
        emotionCanvas.renderMode = RenderMode.WorldSpace;
        emotionCanvas.worldCamera = mainCamera; // Установить камеру для WorldSpace
        emotionCanvas.sortingOrder = 200; // Высокий порядок для отображения поверх всего
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;
        scaler.scaleFactor = 1f; // Добавить scaleFactor
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Настроить размер Canvas
        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 100f);
        canvasRect.localScale = Vector3.one * 0.0183f; // Масштаб уменьшен в 1.8 раза (0.033f / 1.8)
        
        // Создать Image для эмоции
        GameObject imageObj = new GameObject("EmotionImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        emotionImage = imageObj.AddComponent<Image>();
        emotionImage.preserveAspect = true;
        
        RectTransform imageRect = imageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(100f, 100f);
        imageRect.anchoredPosition = Vector2.zero;
        
        // Изначально скрыть
        canvasObj.SetActive(false);
    }
    
    private void Start()
    {
        Debug.Log($"[PetEmotionUI] Start вызван для {gameObject.name}");
        // Убедиться, что спрайты загружены (на случай, если Awake не вызвался)
        if (spawnEmotion == null || miningStartEmotion == null || miningCompleteEmotion == null || activeEmotion == null)
        {
            Debug.LogWarning("[PetEmotionUI] В Start: некоторые спрайты не загружены, загружаю...");
            LoadEmotionSprites();
        }
        Debug.Log($"[PetEmotionUI] В Start: спрайты загружены: spawn={spawnEmotion != null}, miningStart={miningStartEmotion != null}, miningComplete={miningCompleteEmotion != null}, active={activeEmotion != null}");
    }
    
    private void LateUpdate()
    {
        // Позиция обновляется в корутине анимации, здесь только обновляем поворот если нужно
        // (но поворот тоже обновляется в корутине)
    }
    
    /// <summary>
    /// Загрузить спрайты эмоций из Assets
    /// Используются номера: 02 (спавн), 23 (начало добычи), 36 (конец добычи), 41 (появление в активных)
    /// </summary>
    private void LoadEmotionSprites()
    {
        if (spawnEmotion == null || miningStartEmotion == null || miningCompleteEmotion == null || activeEmotion == null)
        {
            Debug.Log("[PetEmotionUI] Загрузка спрайтов эмоций...");
            #if UNITY_EDITOR
            // Загрузить спрайт-лист
            string spritePath = "Assets/Downloads/2D Pixel Art Icons/2D Pixel Art Emotion  Icons/Sprite.png";
            Debug.Log($"[PetEmotionUI] Загружаю спрайты из: {spritePath}");
            Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spritePath)
                .OfType<Sprite>().ToArray();
            
            Debug.Log($"[PetEmotionUI] Загружено спрайтов: {sprites?.Length ?? 0}");
            
            if (sprites != null && sprites.Length > 0)
            {
                // Ищем спрайты по номерам: 02, 23, 36, 41
                foreach (Sprite sprite in sprites)
                {
                    string spriteName = sprite.name.ToLower();
                    
                    // Эмоция при спавне - номер 02
                    if (spawnEmotion == null && (spriteName.Contains("_02") || spriteName.Contains("02_")))
                    {
                        spawnEmotion = sprite;
                        Debug.Log($"[PetEmotionUI] Найден спрайт спавна: {sprite.name}");
                    }
                    // Эмоция при начале добычи - номер 23
                    else if (miningStartEmotion == null && (spriteName.Contains("_23") || spriteName.Contains("23_")))
                    {
                        miningStartEmotion = sprite;
                        Debug.Log($"[PetEmotionUI] Найден спрайт начала добычи: {sprite.name}");
                    }
                    // Эмоция при окончании добычи - номер 36
                    else if (miningCompleteEmotion == null && (spriteName.Contains("_36") || spriteName.Contains("36_")))
                    {
                        miningCompleteEmotion = sprite;
                        Debug.Log($"[PetEmotionUI] Найден спрайт окончания добычи: {sprite.name}");
                    }
                    // Эмоция при появлении в активных - номер 41
                    else if (activeEmotion == null && (spriteName.Contains("_41") || spriteName.Contains("41_")))
                    {
                        activeEmotion = sprite;
                        Debug.Log($"[PetEmotionUI] Найден спрайт активного: {sprite.name}");
                    }
                }
                
                // Если не нашли по номерам, попробуем найти по индексу в массиве
                // Предполагаем, что спрайты упорядочены по номерам
                // Номера: 02 (индекс 1), 23 (индекс 22), 36 (индекс 35), 41 (индекс 40)
                Debug.Log("[PetEmotionUI] Поиск спрайтов по индексу...");
                if (spawnEmotion == null && sprites.Length > 1)
                {
                    spawnEmotion = sprites[1]; // Индекс 1 для номера 02 (0-based: 02 = index 1)
                    Debug.Log($"[PetEmotionUI] Найден спрайт спавна по индексу 1: {spawnEmotion.name}");
                }
                if (miningStartEmotion == null && sprites.Length > 22)
                {
                    miningStartEmotion = sprites[22]; // Индекс 22 для номера 23 (0-based: 23 = index 22)
                    Debug.Log($"[PetEmotionUI] Найден спрайт начала добычи по индексу 22: {miningStartEmotion.name}");
                }
                if (miningCompleteEmotion == null && sprites.Length > 35)
                {
                    miningCompleteEmotion = sprites[35]; // Индекс 35 для номера 36 (0-based: 36 = index 35)
                    Debug.Log($"[PetEmotionUI] Найден спрайт окончания добычи по индексу 35: {miningCompleteEmotion.name}");
                }
                if (activeEmotion == null && sprites.Length > 40)
                {
                    activeEmotion = sprites[40]; // Индекс 40 для номера 41 (0-based: 41 = index 40)
                    Debug.Log($"[PetEmotionUI] Найден спрайт активного по индексу 40: {activeEmotion.name}");
                }
                
                // Если все еще не нашли, используем первые доступные спрайты
                if (spawnEmotion == null && sprites.Length > 0)
                {
                    spawnEmotion = sprites[0];
                    Debug.Log($"[PetEmotionUI] Использую первый доступный спрайт для спавна: {spawnEmotion.name}");
                }
                if (miningStartEmotion == null && sprites.Length > 1)
                {
                    miningStartEmotion = sprites[1];
                    Debug.Log($"[PetEmotionUI] Использую второй доступный спрайт для начала добычи: {miningStartEmotion.name}");
                }
                if (miningCompleteEmotion == null && sprites.Length > 2)
                {
                    miningCompleteEmotion = sprites[2];
                    Debug.Log($"[PetEmotionUI] Использую третий доступный спрайт для окончания добычи: {miningCompleteEmotion.name}");
                }
                if (activeEmotion == null && sprites.Length > 3)
                {
                    activeEmotion = sprites[3];
                    Debug.Log($"[PetEmotionUI] Использую четвертый доступный спрайт для активного: {activeEmotion.name}");
                }
            }
            else
            {
                Debug.LogError("[PetEmotionUI] Не удалось загрузить спрайты из файла!");
            }
            #else
            // В билде пытаемся загрузить из Resources
            LoadEmotionSpritesFromResources();
            #endif
        }
        else
        {
            Debug.Log("[PetEmotionUI] Все спрайты уже загружены");
        }
    }
    
    /// <summary>
    /// Загрузить спрайты эмоций из Resources (для билда)
    /// </summary>
    private void LoadEmotionSpritesFromResources()
    {
        // Пытаемся загрузить спрайты из Resources
        // Путь: Resources/Emotions/Sprite_02, Sprite_23, Sprite_36, Sprite_41
        if (spawnEmotion == null)
        {
            spawnEmotion = Resources.Load<Sprite>("Emotions/Sprite_02");
        }
        if (miningStartEmotion == null)
        {
            miningStartEmotion = Resources.Load<Sprite>("Emotions/Sprite_23");
        }
        if (miningCompleteEmotion == null)
        {
            miningCompleteEmotion = Resources.Load<Sprite>("Emotions/Sprite_36");
        }
        if (activeEmotion == null)
        {
            activeEmotion = Resources.Load<Sprite>("Emotions/Sprite_41");
        }
        
        // Если не нашли по именам, попробуем загрузить весь спрайт-лист
        if (spawnEmotion == null || miningStartEmotion == null || miningCompleteEmotion == null || activeEmotion == null)
        {
            Sprite[] allSprites = Resources.LoadAll<Sprite>("Emotions/Sprite");
            if (allSprites != null && allSprites.Length > 0)
            {
                if (spawnEmotion == null && allSprites.Length > 1)
                    spawnEmotion = allSprites[1];
                if (miningStartEmotion == null && allSprites.Length > 22)
                    miningStartEmotion = allSprites[22];
                if (miningCompleteEmotion == null && allSprites.Length > 35)
                    miningCompleteEmotion = allSprites[35];
                if (activeEmotion == null && allSprites.Length > 40)
                    activeEmotion = allSprites[40];
            }
        }
        
        // Предупреждение, если спрайты не загружены
        if (spawnEmotion == null || miningStartEmotion == null || miningCompleteEmotion == null || activeEmotion == null)
        {
            Debug.LogWarning("[PetEmotionUI] Не удалось загрузить спрайты эмоций из Resources! Убедитесь, что спрайты находятся в папке Resources/Emotions/ или назначены в Inspector.");
        }
    }
    
    /// <summary>
    /// Показать эмоцию при спавне
    /// </summary>
    public void ShowSpawnEmotion()
    {
        Debug.Log($"[PetEmotionUI] ShowSpawnEmotion вызван для {gameObject.name}, spawnEmotion: {spawnEmotion != null}");
        
        // Всегда использовать корутину с задержкой при спавне, чтобы гарантировать инициализацию
        StartCoroutine(ShowSpawnEmotionDelayed());
    }
    
    /// <summary>
    /// Показать эмоцию при спавне с задержкой (гарантирует инициализацию)
    /// </summary>
    private IEnumerator ShowSpawnEmotionDelayed()
    {
        Debug.Log("[PetEmotionUI] ShowSpawnEmotionDelayed начата");
        
        // Подождать, чтобы Awake и Start точно выполнились
        yield return new WaitForSeconds(0.3f);
        
        // Убедиться, что спрайт загружен
        if (spawnEmotion == null)
        {
            Debug.LogWarning("[PetEmotionUI] spawnEmotion null, загружаю спрайты...");
            LoadEmotionSprites();
        }
        
        // Если все еще не загружен, попробовать еще раз
        int attempts = 0;
        while (spawnEmotion == null && attempts < 10)
        {
            yield return new WaitForSeconds(0.1f);
            LoadEmotionSprites();
            attempts++;
            Debug.Log($"[PetEmotionUI] Попытка {attempts}/10 загрузки спрайта спавна...");
        }
        
        if (spawnEmotion != null)
        {
            Debug.Log($"[PetEmotionUI] Спрайт спавна загружен: {spawnEmotion.name}, показываю эмоцию");
            ShowEmotion(spawnEmotion);
        }
        else
        {
            Debug.LogError("[PetEmotionUI] Не удалось загрузить спрайт спавна после всех попыток!");
        }
    }
    
    /// <summary>
    /// Показать эмоцию при начале добычи
    /// </summary>
    public void ShowMiningStartEmotion()
    {
        if (miningStartEmotion == null)
        {
            LoadEmotionSprites();
        }
        ShowEmotion(miningStartEmotion);
    }
    
    /// <summary>
    /// Показать эмоцию при окончании добычи
    /// </summary>
    public void ShowMiningCompleteEmotion()
    {
        Debug.Log($"[PetEmotionUI] ShowMiningCompleteEmotion вызван для {gameObject.name}");
        if (miningCompleteEmotion == null)
        {
            Debug.LogWarning("[PetEmotionUI] miningCompleteEmotion null, загружаю спрайты...");
            LoadEmotionSprites();
        }
        if (miningCompleteEmotion == null)
        {
            Debug.LogError("[PetEmotionUI] miningCompleteEmotion все еще null после загрузки!");
            return;
        }
        Debug.Log($"[PetEmotionUI] Показываю эмоцию окончания добычи, спрайт: {miningCompleteEmotion.name}");
        ShowEmotion(miningCompleteEmotion);
    }
    
    /// <summary>
    /// Показать эмоцию при появлении в активных
    /// </summary>
    public void ShowActiveEmotion()
    {
        if (activeEmotion == null)
        {
            LoadEmotionSprites();
        }
        ShowEmotion(activeEmotion);
    }
    
    /// <summary>
    /// Показать эмоцию (с анимацией движения вверх и исчезновения)
    /// </summary>
    private void ShowEmotion(Sprite emotionSprite)
    {
        Debug.Log($"[PetEmotionUI] ShowEmotion вызван: sprite={emotionSprite != null}, image={emotionImage != null}, canvas={emotionCanvas != null}");
        
        if (emotionSprite == null)
        {
            Debug.LogError("[PetEmotionUI] emotionSprite is null!");
            return;
        }
        if (emotionImage == null)
        {
            Debug.LogError("[PetEmotionUI] emotionImage is null!");
            return;
        }
        if (emotionCanvas == null)
        {
            Debug.LogError("[PetEmotionUI] emotionCanvas is null!");
            return;
        }
        
        // Остановить предыдущую анимацию, если есть
        if (currentEmotionCoroutine != null)
        {
            StopCoroutine(currentEmotionCoroutine);
        }
        
        // Сохранить текущую эмоцию
        currentEmotionSprite = emotionSprite;
        
        // Установить спрайт
        emotionImage.sprite = emotionSprite;
        Debug.Log($"[PetEmotionUI] Спрайт установлен: {emotionSprite.name}");
        
        // Убедиться, что Canvas виден и правильно позиционирован
        if (canvasRect != null)
        {
            Vector3 emotionPos = GetEmotionPosition();
            canvasRect.position = emotionPos;
            Debug.Log($"[PetEmotionUI] Canvas позиция установлена: {emotionPos}, масштаб: {canvasRect.localScale}, размер: {canvasRect.sizeDelta}");
        }
        
        // Показать Canvas
        emotionCanvas.gameObject.SetActive(true);
        isEmotionActive = true;
        
        Debug.Log($"[PetEmotionUI] Canvas активирован, активен: {emotionCanvas.gameObject.activeSelf}, в иерархии: {emotionCanvas.gameObject.activeInHierarchy}");
        
        // Запустить анимацию всплытия и исчезновения
        currentEmotionCoroutine = StartCoroutine(AnimateEmotionFloatUp());
        Debug.Log("[PetEmotionUI] Анимация запущена");
    }
    
    /// <summary>
    /// Скрыть эмоцию
    /// </summary>
    public void HideEmotion()
    {
        if (currentEmotionCoroutine != null)
        {
            StopCoroutine(currentEmotionCoroutine);
            currentEmotionCoroutine = null;
        }
        
        isEmotionActive = false;
        
        if (emotionCanvas != null)
        {
            emotionCanvas.gameObject.SetActive(false);
        }
        
        // Сбросить прозрачность
        if (emotionImage != null)
        {
            Color color = emotionImage.color;
            color.a = 1f;
            emotionImage.color = color;
        }
    }
    
    /// <summary>
    /// Анимация всплытия эмоции вверх с исчезновением
    /// Эмоция привязана к питомцу и следует за ним по X и Z, двигаясь вверх по Y
    /// </summary>
    private IEnumerator AnimateEmotionFloatUp()
    {
        if (emotionCanvas == null || canvasRect == null || emotionImage == null)
        {
            Debug.LogError("[PetEmotionUI] AnimateEmotionFloatUp: компоненты не инициализированы!");
            yield break;
        }
        
        Debug.Log("[PetEmotionUI] AnimateEmotionFloatUp начата");
        
        // Начальная позиция (над питомцем)
        Vector3 initialPosition = GetEmotionPosition();
        float initialY = initialPosition.y;
        float targetY = initialY + floatDistance;
        
        Debug.Log($"[PetEmotionUI] Начальная позиция: {initialPosition}, initialY: {initialY}, targetY: {targetY}");
        
        float elapsedTime = 0f;
        float duration = displayDuration;
        
        // Анимация всплытия и исчезновения
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Получить текущую позицию питомца (X и Z следуют за питомцем)
            Vector3 currentPetPosition = GetEmotionPosition();
            
            // Вычислить текущую Y позицию (движение вверх от начальной позиции)
            float currentY = Mathf.Lerp(initialY, targetY, t);
            
            // Позиция: X и Z следуют за питомцем, Y движется вверх от начальной позиции
            Vector3 currentPos = new Vector3(currentPetPosition.x, currentY, currentPetPosition.z);
            canvasRect.position = currentPos;
            
            // Обновить поворот к камере
            if (mainCamera != null)
            {
                Vector3 directionToCamera = mainCamera.transform.position - currentPos;
                directionToCamera.y = 0;
                if (directionToCamera != Vector3.zero)
                {
                    canvasRect.rotation = Quaternion.LookRotation(-directionToCamera);
                }
            }
            
            // Прозрачность (появление в начале, исчезновение в конце)
            float alpha = 1f;
            if (t < 0.2f)
            {
                // Появление (первые 20%)
                alpha = t / 0.2f;
            }
            else if (t > 0.7f)
            {
                // Исчезновение (последние 30%)
                alpha = (1f - t) / 0.3f;
            }
            
            // Логирование каждые 0.5 секунды для отладки
            if (Mathf.FloorToInt(elapsedTime * 2) != Mathf.FloorToInt((elapsedTime - Time.deltaTime) * 2))
            {
                Debug.Log($"[PetEmotionUI] Анимация: t={t:F2}, позиция={currentPos}, alpha={alpha:F2}, canvas активен={emotionCanvas.gameObject.activeInHierarchy}");
            }
            
            if (emotionImage != null)
            {
                Color color = emotionImage.color;
                color.a = alpha;
                emotionImage.color = color;
            }
            
            yield return null;
        }
        
        // Скрыть Canvas
        emotionCanvas.gameObject.SetActive(false);
        isEmotionActive = false;
        
        // Сбросить прозрачность
        if (emotionImage != null)
        {
            Color color = emotionImage.color;
            color.a = 1f;
            emotionImage.color = color;
        }
        
        currentEmotionCoroutine = null;
    }
    
    /// <summary>
    /// Получить позицию для эмоции (над питомцем)
    /// </summary>
    private Vector3 GetEmotionPosition()
    {
        // Найти верхнюю точку питомца
        Vector3 petTop = transform.position;
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            petTop = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
        }
        else
        {
            petTop.y += heightOffset;
        }
        
        return petTop;
    }
    
}

