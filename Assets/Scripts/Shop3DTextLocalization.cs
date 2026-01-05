using UnityEngine;
using TMPro;

/// <summary>
/// Скрипт для локализации 3D текста над магазином
/// Автоматически обновляет текст при изменении языка
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class Shop3DTextLocalization : MonoBehaviour
{
    [Header("Настройки смещения")]
    [Tooltip("Смещение по оси Z для английского текста 'Shop' (применяется только для английского языка)")]
    [SerializeField] private float shopZOffset = 0f;
    
    private TextMeshPro textMeshPro;
    private Vector3 originalPosition;
    private bool isEnglish = false;
    
    private void Awake()
    {
        textMeshPro = GetComponent<TextMeshPro>();
        if (textMeshPro == null)
        {
            Debug.LogError($"[Shop3DTextLocalization] TextMeshPro компонент не найден на {gameObject.name}!");
            return;
        }
        
        // Сохранить исходную позицию
        originalPosition = transform.localPosition;
    }
    
    private void Start()
    {
        UpdateText();
        
        // Подписаться на изменение языка
        LocalizationManager.OnLanguageChangedEvent += OnLanguageChanged;
    }
    
    private void OnDestroy()
    {
        // Отписаться от изменения языка
        LocalizationManager.OnLanguageChangedEvent -= OnLanguageChanged;
    }
    
    /// <summary>
    /// Обработчик изменения языка
    /// </summary>
    private void OnLanguageChanged(string lang)
    {
        UpdateText();
    }
    
    /// <summary>
    /// Обновить текст в зависимости от текущего языка
    /// </summary>
    private void UpdateText()
    {
        if (textMeshPro != null)
        {
            string currentText = LocalizationManager.GetShop3DText();
            textMeshPro.text = currentText;
            
            // Проверить, является ли текст английским "Shop"
            bool wasEnglish = isEnglish;
            isEnglish = (LocalizationManager.GetCurrentLanguage() == "en" && currentText == "Shop");
            
            // Применить смещение по Z только для английского текста "Shop"
            if (isEnglish)
            {
                // Применить смещение
                Vector3 newPosition = originalPosition;
                newPosition.z += shopZOffset;
                transform.localPosition = newPosition;
            }
            else
            {
                // Вернуть исходную позицию
                transform.localPosition = originalPosition;
            }
        }
    }
}

