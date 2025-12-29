using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент для отображения health bar кристалла
/// </summary>
public class CrystalHealthBar : MonoBehaviour
{
    [Header("UI Компоненты")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Настройки")]
    [SerializeField] private float showDistance = 20f; // Расстояние для показа health bar
    
    private Transform crystalTransform;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isVisible = false;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        FindPlayer();
        
        // Найти Canvas в дочерних объектах, если не назначен
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>(true);
        }
        
        // Найти UI компоненты, если не назначены
        if (canvas != null)
        {
            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                string imgName = img.name.ToLower();
                if (fillImage == null && (imgName.Contains("fill")))
                {
                    fillImage = img;
                }
                else if (backgroundImage == null && (imgName.Contains("background") || imgName.Contains("bg")))
                {
                    backgroundImage = img;
                }
                else if (outlineImage == null && (imgName.Contains("outline")))
                {
                    outlineImage = img;
                }
            }
            
            if (healthText == null)
                healthText = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        
        // Изначально скрыть Canvas
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Найти кристалл в родителе
        if (crystalTransform == null)
        {
            crystalTransform = transform.parent;
        }
    }
    
    private void LateUpdate()
    {
        if (crystalTransform == null || canvas == null) return;
        
        UpdateVisibility();
        
        // Позиция health bar берется из префаба, обновляем только поворот к камере
        if (isVisible)
        {
            UpdateRotation();
        }
    }
    
    /// <summary>
    /// Инициализировать health bar для кристалла
    /// </summary>
    public void Initialize(Transform crystal)
    {
        crystalTransform = crystal;
        // Позиция health bar берется из префаба, не устанавливается программно
    }
    
    /// <summary>
    /// Обновить health bar с новыми значениями HP
    /// </summary>
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (fillImage != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            fillImage.fillAmount = healthPercent;
            
            // Обновить цвет в зависимости от процента HP
            UpdateFillColor(healthPercent);
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
    
    /// <summary>
    /// Обновить цвет fill в зависимости от процента HP
    /// </summary>
    private void UpdateFillColor(float healthPercent)
    {
        if (fillImage == null) return;
        
        if (healthPercent > 0.6f)
        {
            fillImage.color = new Color(0f, 1f, 0.2f, 1f); // Зеленый
        }
        else if (healthPercent > 0.3f)
        {
            fillImage.color = new Color(1f, 0.8f, 0f, 1f); // Желтый
        }
        else
        {
            fillImage.color = new Color(1f, 0.2f, 0.2f, 1f); // Красный
        }
    }
    
    /// <summary>
    /// Обновить видимость health bar в зависимости от расстояния до игрока
    /// </summary>
    private void UpdateVisibility()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                SetVisible(false);
                return;
            }
        }
        
        float distance = Vector3.Distance(crystalTransform.position, playerTransform.position);
        
        // Показывать health bar если игрок в пределах расстояния
        SetVisible(distance <= showDistance);
    }
    
    /// <summary>
    /// Установить видимость health bar
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (isVisible == visible || canvas == null) return;
        
        isVisible = visible;
        canvas.gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Обновить поворот health bar к камере (позиция берется из префаба)
    /// </summary>
    private void UpdateRotation()
    {
        if (canvas == null || mainCamera == null) return;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            // Повернуть к камере
            Vector3 directionToCamera = mainCamera.transform.position - canvasRect.position;
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
            {
                canvasRect.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
    
    /// <summary>
    /// Найти игрока в сцене
    /// </summary>
    private void FindPlayer()
    {
        if (playerTransform != null) return;
        
        // Попытка 1: по тегу
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }
        
        // Попытка 2: по имени
        player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }
        
        // Попытка 3: по компоненту
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }
    }
}

