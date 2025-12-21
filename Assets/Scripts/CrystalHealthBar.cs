using UnityEngine;
using UnityEngine.UI;

public class CrystalHealthBar : MonoBehaviour
{
    [Header("Настройки UI")]
    [SerializeField] private Canvas healthBarCanvas;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarBackground;
    [SerializeField] private Image healthBarOutline;
    [SerializeField] private Text healthBarText;
    [SerializeField] private float offsetY = 50f; // Высота над кристаллом (увеличено пропорционально масштабу 25x)
    [SerializeField] private float healthBarWidth = 150f; // Ширина health bar
    [SerializeField] private float healthBarHeight = 10f; // Высота health bar
    [SerializeField] private float maxDistanceToShow = 15f; // Максимальное расстояние от игрока для отображения UI
    
    private Transform targetCrystal; // Кристалл, над которым висит health bar
    private Camera mainCamera;
    private RectTransform canvasRect;
    private Transform playerTransform; // Трансформ игрока
    
    private float targetHealthPercent = 1f; // Целевой процент HP для анимации
    private float currentDisplayPercent = 1f; // Текущий отображаемый процент HP
    private Coroutine healthAnimationCoroutine; // Корутина для анимации HP
    
    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Найти игрока
        FindPlayer();
        
        // Если Canvas не назначен, создать его
        if (healthBarCanvas == null)
        {
            CreateHealthBarCanvas();
        }
        
        if (healthBarCanvas != null)
        {
            canvasRect = healthBarCanvas.GetComponent<RectTransform>();
            // Убедиться, что Canvas активен
            healthBarCanvas.gameObject.SetActive(true);
        }
    }
    
    private void Start()
    {
        // Дополнительная проверка после Start
        if (healthBarCanvas != null && !healthBarCanvas.gameObject.activeSelf)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }
        
        // Повторная попытка найти игрока, если не найден в Awake
        if (playerTransform == null)
        {
            FindPlayer();
        }
    }
    
    /// <summary>
    /// Найти игрока в сцене
    /// </summary>
    private void FindPlayer()
    {
        // Попытка 1: найти по тегу "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }
        
        // Попытка 2: найти по имени "Player"
        GameObject playerByName = GameObject.Find("Player");
        if (playerByName != null)
        {
            playerTransform = playerByName.transform;
            return;
        }
        
        // Попытка 3: найти по компоненту PlayerController
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }
    }
    
    /// <summary>
    /// Создать Canvas для health bar в стиле Roblox
    /// </summary>
    private void CreateHealthBarCanvas()
    {
        // Создать Canvas в World Space для позиционирования над кристаллом
        // НЕ делаем его дочерним объектом, чтобы позиция была в мировых координатах
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        // canvasObj.transform.SetParent(transform); // Убрано для правильного позиционирования
        canvasObj.transform.position = Vector3.zero; // Будет обновляться в UpdatePosition
        canvasObj.transform.rotation = Quaternion.identity;
        
        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f; // Уменьшено для большего размера
        scaler.scaleFactor = 1f; // Фактор масштабирования для лучшего качества
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Настроить размер Canvas
        RectTransform canvasRectTransform = canvasObj.GetComponent<RectTransform>();
        canvasRectTransform.sizeDelta = new Vector2(healthBarWidth, healthBarHeight + 50f); // Дополнительное место для текста сверху
        // Увеличиваем масштаб для лучшей видимости (для кристаллов масштаб 25x)
        // Если Canvas width = 200, и мы хотим 2 метра в мире, то scale = 2/200 = 0.01
        // Но для лучшей видимости увеличим до 0.05 (5 метров в мире)
        canvasRectTransform.localScale = Vector3.one * 0.05f; // Увеличенный масштаб для видимости
        
        // Создать обводку (outline) - внешняя рамка в стиле Roblox
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(canvasObj.transform, false);
        healthBarOutline = outlineObj.AddComponent<Image>();
        healthBarOutline.color = new Color(0f, 0f, 0f, 1f); // Черная обводка
        
        RectTransform outlineRect = outlineObj.GetComponent<RectTransform>();
        outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRect.sizeDelta = new Vector2(healthBarWidth + 4f, healthBarHeight + 4f); // Немного больше для обводки
        outlineRect.anchoredPosition = new Vector2(0f, 0f); // Центр Canvas
        
        // Создать фон для health bar (белый/светлый в стиле Roblox)
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform, false);
        healthBarBackground = backgroundObj.AddComponent<Image>();
        healthBarBackground.color = new Color(1f, 1f, 1f, 1f); // Белый фон
        
        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        bgRect.anchoredPosition = new Vector2(0f, 0f); // Центр Canvas
        
        // Создать fill для health bar (яркий цвет в стиле Roblox)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(backgroundObj.transform, false);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = new Color(0f, 1f, 0.2f, 1f); // Яркий зеленый в стиле Roblox
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // Создать текст для отображения HP (100/100) - над health bar
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(canvasObj.transform, false);
        healthBarText = textObj.AddComponent<Text>();
        healthBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthBarText.fontSize = 14; // Уменьшен размер текста
        healthBarText.fontStyle = FontStyle.Bold;
        healthBarText.color = Color.white;
        healthBarText.alignment = TextAnchor.MiddleCenter;
        healthBarText.text = "100 / 100";
        healthBarText.resizeTextForBestFit = false; // Отключить авто-изменение размера для лучшего контроля
        healthBarText.horizontalOverflow = HorizontalWrapMode.Overflow;
        healthBarText.verticalOverflow = VerticalWrapMode.Overflow;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(healthBarWidth, 25f);
        textRect.anchoredPosition = new Vector2(0f, 18f); // Над health bar (было -5f под баром)
        
        // Добавить обводку тексту через Outline компонент для лучшей читаемости
        Outline textOutline = textObj.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(1.5f, 1.5f); // Немного уменьшена для более четкого текста
        
        canvasRect = canvasObj.GetComponent<RectTransform>();
        
    }
    
    /// <summary>
    /// Инициализировать health bar для конкретного кристалла
    /// </summary>
    public void Initialize(Transform crystalTransform)
    {
        targetCrystal = crystalTransform;
        currentDisplayPercent = 1f; // Инициализировать с полным HP
    }
    
    /// <summary>
    /// Обновить отображение HP с анимацией уменьшения
    /// </summary>
    public void UpdateHealthBar(float health, float maxHealth)
    {
        float healthPercent = Mathf.Clamp01(health / maxHealth);
        
        // Установить целевой процент для анимации
        targetHealthPercent = healthPercent;
        
        // Если анимация уже идет, остановить её
        if (healthAnimationCoroutine != null)
        {
            StopCoroutine(healthAnimationCoroutine);
        }
        
        // Запустить анимацию уменьшения HP бара (10 шагов)
        healthAnimationCoroutine = StartCoroutine(AnimateHealthBar(health, maxHealth));
    }
    
    /// <summary>
    /// Анимация уменьшения HP бара в 10 шагов
    /// </summary>
    private System.Collections.IEnumerator AnimateHealthBar(float targetHealth, float maxHealth)
    {
        float startPercent = currentDisplayPercent;
        float targetPercent = Mathf.Clamp01(targetHealth / maxHealth);
        
        // Если изменение очень маленькое, обновить сразу без анимации
        if (Mathf.Abs(startPercent - targetPercent) < 0.01f)
        {
            currentDisplayPercent = targetPercent;
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentDisplayPercent;
            }
            if (healthBarText != null)
            {
                healthBarText.text = $"{Mathf.CeilToInt(targetHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }
            healthAnimationCoroutine = null;
            yield break;
        }
        
        int steps = 10;
        float stepDuration = 0.05f; // Длительность одного шага (50ms)
        
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            currentDisplayPercent = Mathf.Lerp(startPercent, targetPercent, t);
            
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentDisplayPercent;
                
                // Яркие цвета в стиле Roblox в зависимости от HP
                if (currentDisplayPercent > 0.6f)
                {
                    healthBarFill.color = new Color(0f, 1f, 0.2f, 1f); // Яркий зеленый
                }
                else if (currentDisplayPercent > 0.3f)
                {
                    healthBarFill.color = new Color(1f, 0.8f, 0f, 1f); // Яркий желтый/оранжевый
                }
                else
                {
                    healthBarFill.color = new Color(1f, 0.2f, 0.2f, 1f); // Яркий красный
                }
            }
            
            // Обновить текст с текущим отображаемым HP
            if (healthBarText != null)
            {
                float displayHealth = currentDisplayPercent * maxHealth;
                healthBarText.text = $"{Mathf.CeilToInt(displayHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }
            
            yield return new WaitForSeconds(stepDuration);
        }
        
        // Убедиться, что финальное значение установлено точно
        currentDisplayPercent = targetPercent;
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentDisplayPercent;
        }
        if (healthBarText != null)
        {
            healthBarText.text = $"{Mathf.CeilToInt(targetHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
        
        healthAnimationCoroutine = null;
    }
    
    private void LateUpdate()
    {
        if (targetCrystal != null && mainCamera != null && canvasRect != null)
        {
            UpdatePosition();
        }
    }
    
    /// <summary>
    /// Обновить позицию health bar над кристаллом
    /// </summary>
    private void UpdatePosition()
    {
        if (targetCrystal == null || canvasRect == null) return;
        
        // Проверить расстояние до игрока и скрыть UI, если слишком далеко
        bool shouldShow = true;
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(targetCrystal.position, playerTransform.position);
            shouldShow = distanceToPlayer <= maxDistanceToShow;
        }
        
        // Показать/скрыть health bar в зависимости от расстояния
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(shouldShow);
        }
        
        // Если UI скрыт, не обновлять позицию
        if (!shouldShow) return;
        
        // Найти верхнюю точку кристалла через Renderer bounds
        Vector3 crystalTopPosition = targetCrystal.position;
        Renderer crystalRenderer = targetCrystal.GetComponentInChildren<Renderer>();
        if (crystalRenderer != null)
        {
            Bounds bounds = crystalRenderer.bounds;
            crystalTopPosition = new Vector3(
                bounds.center.x,
                bounds.max.y, // Верхняя точка кристалла
                bounds.center.z
            );
        }
        else
        {
            // Если нет Renderer, используем позицию + offsetY
            crystalTopPosition = targetCrystal.position + Vector3.up * offsetY;
        }
        
        // Обновить позицию Canvas над кристаллом
        canvasRect.position = crystalTopPosition + Vector3.up * 5f; // Небольшое смещение вверх от верхней точки
        
        // Повернуть health bar лицом к камере
        if (mainCamera != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - canvasRect.position;
            directionToCamera.y = 0; // Только горизонтальный поворот
            if (directionToCamera != Vector3.zero)
            {
                canvasRect.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
    
    /// <summary>
    /// Установить видимость health bar
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }
    }
    
    private void OnDestroy()
    {
        // Уничтожить Canvas при уничтожении компонента
        if (healthBarCanvas != null)
        {
            Destroy(healthBarCanvas.gameObject);
        }
    }
}

