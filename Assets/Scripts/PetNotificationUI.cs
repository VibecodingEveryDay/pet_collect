using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// UI компонент для отображения уведомлений о выигрыше питомца
/// </summary>
public class PetNotificationUI : MonoBehaviour
{
    private static PetNotificationUI _instance;
    
    [Header("UI Assets")]
    [SerializeField] public VisualTreeAsset notificationAsset;
    [SerializeField] public StyleSheet robloxStyleSheet;
    
    private UIDocument uiDocument;
    private VisualElement root;
    
    public static PetNotificationUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetNotificationUI>();
                if (_instance == null)
                {
                    GameObject uiObject = new GameObject("PetNotificationUI");
                    _instance = uiObject.AddComponent<PetNotificationUI>();
                    DontDestroyOnLoad(uiObject);
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
        
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Получить или создать UIDocument
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
                Debug.Log("UIDocument создан для PetNotificationUI");
            }
        }
        
        // Установить высокий sort order, чтобы уведомление было поверх всего
        uiDocument.sortingOrder = 1000;
        
        // Загрузить assets, если не назначены
        if (notificationAsset == null)
        {
            #if UNITY_EDITOR
            notificationAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/PetNotification.uxml");
            if (notificationAsset != null)
            {
                Debug.Log("PetNotification.uxml загружен автоматически");
            }
            else
            {
                Debug.LogError("PetNotification.uxml не найден по пути: Assets/UI Toolkit/PetNotification.uxml");
            }
            #endif
        }
        
        if (robloxStyleSheet == null)
        {
            #if UNITY_EDITOR
            robloxStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/RobloxStyle.uss");
            if (robloxStyleSheet != null)
            {
                Debug.Log("RobloxStyle.uss загружен автоматически");
            }
            else
            {
                Debug.LogError("RobloxStyle.uss не найден по пути: Assets/UI Toolkit/RobloxStyle.uss");
            }
            #endif
        }
        
        // Получить root
        root = uiDocument.rootVisualElement;
        
        if (root == null)
        {
            Debug.LogError("Не удалось получить rootVisualElement!");
            return;
        }
        
        // Настроить root для полного экрана и поверх всего
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.style.right = 0;
        root.style.bottom = 0;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.pickingMode = PickingMode.Ignore;
        root.style.display = DisplayStyle.Flex;
        root.style.visibility = Visibility.Visible;
        
        // Применить стили
        if (robloxStyleSheet != null)
        {
            root.styleSheets.Add(robloxStyleSheet);
            Debug.Log("RobloxStyleSheet применен к root");
        }
        else
        {
            Debug.LogWarning("RobloxStyleSheet не найден!");
        }
        
        Debug.Log($"PetNotificationUI инициализирован. Root: {root != null}, NotificationAsset: {notificationAsset != null}, StyleSheet: {robloxStyleSheet != null}, SortOrder: {uiDocument.sortingOrder}");
    }
    
    /// <summary>
    /// Показать уведомление (статический метод)
    /// </summary>
    public static void ShowNotification(PetRarity rarity)
    {
        if (Instance != null)
        {
            Instance.ShowNotificationInternal(rarity);
        }
        else
        {
            Debug.LogError("PetNotificationUI.Instance не найден!");
        }
    }
    
    /// <summary>
    /// Внутренний метод для показа уведомления
    /// </summary>
    private void ShowNotificationInternal(PetRarity rarity)
    {
        Debug.Log($"ShowNotificationInternal вызван для редкости: {rarity}");
        
        if (root == null)
        {
            Debug.LogWarning("Root не инициализирован! Повторная инициализация...");
            InitializeUI();
            if (root == null)
            {
                Debug.LogError("Не удалось инициализировать root!");
                return;
            }
        }
        
        if (notificationAsset == null)
        {
            Debug.LogError("NotificationAsset не назначен! Попытка загрузки...");
            #if UNITY_EDITOR
            notificationAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/PetNotification.uxml");
            #endif
            if (notificationAsset == null)
            {
                Debug.LogError("Не удалось загрузить NotificationAsset!");
                return;
            }
        }
        
        Debug.Log($"Создаю уведомление для редкости: {rarity}");
        
        // Создать уведомление
        TemplateContainer notification = notificationAsset.Instantiate();
        
        if (notification == null)
        {
            Debug.LogError("Не удалось создать уведомление из notificationAsset!");
            return;
        }
        
        // Найти контейнер и текст
        VisualElement container = notification.Q<VisualElement>("notification-container");
        Label notificationText = notification.Q<Label>("notification-text");
        
        if (container == null)
        {
            Debug.LogError("Не найден notification-container в UXML!");
            return;
        }
        
        if (notificationText == null)
        {
            Debug.LogError("Не найден notification-text в UXML!");
            return;
        }
        
        // Установить текст и цвет по редкости
        string rarityName = PetHatchingManager.GetRarityName(rarity);
        notificationText.text = $"Вы выиграли {rarityName} питомца!";
        
        // Получить цвет редкости
        Color rarityColor = PetHatchingManager.GetRarityColor(rarity);
        notificationText.style.color = new StyleColor(rarityColor);
        
        Debug.Log($"Текст уведомления установлен: {notificationText.text}, цвет: {rarityColor}");
        
        // Настроить позиционирование уведомления (полный экран с центрированием)
        notification.style.position = Position.Absolute;
        notification.style.left = 0;
        notification.style.top = 0;
        notification.style.right = 0;
        notification.style.bottom = 0;
        notification.style.width = Length.Percent(100);
        notification.style.height = Length.Percent(100);
        notification.style.display = DisplayStyle.Flex;
        notification.style.visibility = Visibility.Visible;
        notification.style.justifyContent = Justify.Center;
        notification.style.alignItems = Align.Center;
        
        // Настроить позиционирование контейнера для центрирования
        container.style.position = Position.Relative;
        container.style.left = StyleKeyword.Auto;
        container.style.top = StyleKeyword.Auto;
        container.style.right = StyleKeyword.Auto;
        container.style.bottom = StyleKeyword.Auto;
        container.style.width = StyleKeyword.Auto;
        container.style.height = StyleKeyword.Auto;
        container.style.display = DisplayStyle.Flex;
        container.style.visibility = Visibility.Visible;
        container.style.opacity = 0f; // Начальная непрозрачность для предотвращения мерцания
        container.style.marginLeft = 0;
        container.style.marginTop = 0;
        container.style.marginRight = 0;
        container.style.marginBottom = 0;
        
        // Добавить в root
        root.Add(notification);
        
        Debug.Log($"Уведомление добавлено в root. Root children count: {root.childCount}, Container visible: {container.style.visibility == Visibility.Visible}, Notification visible: {notification.style.visibility == Visibility.Visible}");
        
        // Небольшая задержка перед началом анимации, чтобы элемент успел отрендериться
        root.schedule.Execute(() => {
            StartCoroutine(AnimateNotification(container));
        }).ExecuteLater(10); // 10ms задержка
    }
    
    /// <summary>
    /// Анимация уведомления (анимируем контейнер, а не весь notification)
    /// </summary>
    private IEnumerator AnimateNotification(VisualElement container)
    {
        if (container == null) 
        {
            Debug.LogError("Container is null in AnimateNotification!");
            yield break;
        }
        
        Debug.Log("Начинаю анимацию уведомления");
        
        // Убедиться, что элемент видим
        container.style.display = DisplayStyle.Flex;
        container.style.visibility = Visibility.Visible;
        
        // Начальное состояние уже установлено (opacity = 0f), устанавливаем scale
        container.style.scale = new Scale(new Vector2(0.3f, 0.3f));
        
        // Небольшая задержка перед началом анимации
        yield return new WaitForSeconds(0.05f);
        
        // Анимация появления
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (container == null || container.parent == null)
            {
                Debug.LogWarning("Контейнер был удален во время анимации!");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Ease out back
            float scale = EaseOutBack(t);
            float opacity = Mathf.Lerp(0f, 1f, t);
            
            container.style.opacity = opacity;
            container.style.scale = new Scale(new Vector2(scale, scale));
            
            yield return null;
        }
        
        // Финальное состояние
        container.style.opacity = 1f;
        container.style.scale = new Scale(Vector2.one);
        
        Debug.Log("Анимация появления завершена, жду 1.7 секунды");
        
        // Ждать 1.7 секунды
        yield return new WaitForSeconds(1.7f);
        
        Debug.Log("Начинаю анимацию исчезновения");
        
        // Анимация исчезновения
        elapsed = 0f;
        duration = 0.3f;
        
        while (elapsed < duration)
        {
            if (container == null || container.parent == null)
            {
                Debug.LogWarning("Контейнер был удален во время анимации исчезновения!");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float opacity = Mathf.Lerp(1f, 0f, t);
            
            container.style.opacity = opacity;
            
            yield return null;
        }
        
        // Удалить уведомление (найти родительский notification)
        VisualElement notification = container.parent;
        if (notification != null && notification.parent != null)
        {
            notification.RemoveFromHierarchy();
            Debug.Log("Уведомление удалено");
        }
    }
    
    /// <summary>
    /// Ease out back функция для плавной анимации
    /// </summary>
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

