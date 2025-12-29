using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Менеджер для управления системой ускорения питомцев
/// </summary>
public class PetSpeedBoostManager : MonoBehaviour
{
    private static PetSpeedBoostManager _instance;
    public static PetSpeedBoostManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetSpeedBoostManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PetSpeedBoostManager");
                    _instance = go.AddComponent<PetSpeedBoostManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    [Header("Настройки")]
    [SerializeField] private float detectionRange = 3f; // Радиус обнаружения питомцев рядом с игроком
    
    private PlayerController playerController;
    private Camera mainCamera;
    private bool isAnyPetBoosted = false; // Флаг, что какой-то питомец ускорен
    
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
    
    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    private void Update()
    {
        // Обработка кликов/тапов по питомцам
        if (isAnyPetBoosted) return; // Не обрабатывать клики, если питомец уже ускорен
        
        // Проверка клика мышью (десктоп)
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick(Input.mousePosition);
        }
        
        // Проверка тапов (мобильные устройства)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleClick(touch.position);
            }
        }
    }
    
    /// <summary>
    /// Обработать клик/тап
    /// </summary>
    private void HandleClick(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[PetSpeedBoostManager] Камера не найдена!");
            return;
        }
        
        if (playerController == null)
        {
            Debug.LogWarning("[PetSpeedBoostManager] PlayerController не найден!");
            return;
        }
        
        // Сначала проверить, попадает ли луч в питомца (3D объект)
        // Если попал в питомца, не проверять UI
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);
        
        Debug.Log($"[PetSpeedBoostManager] Raycast нашел {hits.Length} объектов");
        
        // Проверить все попадания
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            
            Debug.Log($"[PetSpeedBoostManager] Попадание в: {hit.collider.gameObject.name}");
            
            // Попробовать найти PetBehavior на объекте с коллайдером или его родителях
            PetBehavior pet = hit.collider.GetComponent<PetBehavior>();
            if (pet == null)
            {
                pet = hit.collider.GetComponentInParent<PetBehavior>();
            }
            
            if (pet != null)
            {
                Debug.Log($"[PetSpeedBoostManager] Найден питомец: {pet.gameObject.name}");
                
                // Если попали в питомца, проверяем UI только через UI Toolkit PanelRaycaster
                // чтобы убедиться, что UI элемент не перекрывает питомца
                if (IsPointerOverUIToolkit(screenPosition))
                {
                    Debug.Log("[PetSpeedBoostManager] Тап попал в UI Toolkit элемент поверх питомца, игнорируем");
                    return; // UI элемент перекрывает питомца
                }
                
                // Проверить, не ускорен ли уже этот питомец
                if (pet.IsBoosted())
                {
                    Debug.Log("[PetSpeedBoostManager] Питомец уже ускорен");
                    return;
                }
                
                // Проверить, находится ли питомец рядом с игроком
                float distance = Vector3.Distance(playerController.transform.position, pet.transform.position);
                Debug.Log($"[PetSpeedBoostManager] Расстояние до питомца: {distance}, detectionRange: {detectionRange}");
                
                if (distance <= detectionRange)
                {
                    // Применить ускорение
                    Debug.Log("[PetSpeedBoostManager] Применяю ускорение питомцу!");
                    pet.ApplySpeedBoost();
                    isAnyPetBoosted = true;
                    return; // Успешно применили ускорение
                }
                else
                {
                    Debug.Log($"[PetSpeedBoostManager] Питомец слишком далеко: {distance} > {detectionRange}");
                }
            }
        }
        
        // Если не попали в питомца, проверить UI через стандартный EventSystem
        // (но только если не нашли питомца в Raycast)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[PetSpeedBoostManager] Клик/тап по UI элементу (не попал в питомца), игнорируем");
        }
    }
    
    /// <summary>
    /// Проверить, находится ли указатель над UI Toolkit элементом
    /// </summary>
    private bool IsPointerOverUIToolkit(Vector2 screenPosition)
    {
        // Найти все UIDocument компоненты в сцене
        UIDocument[] uiDocuments = FindObjectsOfType<UIDocument>();
        
        foreach (UIDocument uiDoc in uiDocuments)
        {
            if (uiDoc == null || uiDoc.rootVisualElement == null) continue;
            
            // Использовать Panel.Pick для проверки попадания в UI Toolkit элементы
            var panel = uiDoc.rootVisualElement.panel;
            if (panel != null)
            {
                VisualElement pickedElement = panel.Pick(screenPosition);
                
                if (pickedElement != null)
                {
                    // Проверить, является ли элемент интерактивным (Button и т.д.)
                    if (pickedElement is Button || pickedElement is Toggle || pickedElement is Slider || pickedElement is TextField)
                    {
                        Debug.Log($"[PetSpeedBoostManager] Найден интерактивный UI элемент: {pickedElement.name}");
                        return true; // Это интерактивный элемент
                    }
                    
                    // Проверить, есть ли у элемента обработчики кликов или это элемент с классами кнопок
                    if (pickedElement.ClassListContains("shop-button") || 
                        pickedElement.ClassListContains("backpack-button") || 
                        pickedElement.ClassListContains("jump-button") ||
                        pickedElement.ClassListContains("shop-item-button"))
                    {
                        Debug.Log($"[PetSpeedBoostManager] Найден UI элемент с классом кнопки: {pickedElement.name}");
                        return true;
                    }
                    
                    // Проверить родительские элементы (могут быть кнопками)
                    VisualElement parent = pickedElement.parent;
                    while (parent != null)
                    {
                        if (parent is Button || parent is Toggle || parent is Slider)
                        {
                            Debug.Log($"[PetSpeedBoostManager] Найден интерактивный родительский UI элемент: {parent.name}");
                            return true;
                        }
                        parent = parent.parent;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Уведомить, что эффект ускорения закончился
    /// </summary>
    public void OnBoostEffectEnded()
    {
        isAnyPetBoosted = false;
    }
}

