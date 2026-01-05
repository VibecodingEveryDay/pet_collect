using UnityEngine;

/// <summary>
/// Скрипт для кнопки магазина (redbutton)
/// Открывает магазин при наступлении персонажа на кнопку
/// Не открывает повторно, если персонаж все еще стоит на кнопке после закрытия магазина
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopButtonTrigger : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool isTrigger = true; // Коллайдер должен быть триггером
    
    private Collider buttonCollider;
    private bool playerOnButton = false; // Флаг, что персонаж находится на кнопке
    private bool shopWasOpened = false; // Флаг, что магазин был открыт, пока персонаж на кнопке
    private bool canOpenShop = true; // Флаг, можно ли открыть магазин (сбрасывается при выходе с кнопки)
    
    private void Start()
    {
        buttonCollider = GetComponent<Collider>();
        
        if (buttonCollider != null)
        {
            // Убедиться, что коллайдер является триггером
            if (!buttonCollider.isTrigger)
            {
                Debug.LogWarning($"[ShopButtonTrigger] Коллайдер на {gameObject.name} не был триггером! Исправлено.");
                buttonCollider.isTrigger = true;
            }
            
            Debug.Log($"[ShopButtonTrigger] Кнопка магазина {gameObject.name} инициализирована. IsTrigger: {buttonCollider.isTrigger}");
        }
        else
        {
            Debug.LogError($"[ShopButtonTrigger] Collider не найден на {gameObject.name}!");
        }
    }
    
    /// <summary>
    /// Обработка входа персонажа в триггер
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerOnButton = true;
            
            // Проверить, открыт ли магазин
            if (!IsShopOpen() && canOpenShop)
            {
                OpenShop();
                shopWasOpened = true;
                canOpenShop = false; // Заблокировать повторное открытие, пока персонаж на кнопке
                Debug.Log($"[ShopButtonTrigger] Персонаж наступил на кнопку, магазин открыт");
            }
            else if (IsShopOpen())
            {
                shopWasOpened = true;
                canOpenShop = false; // Заблокировать повторное открытие
                Debug.Log($"[ShopButtonTrigger] Персонаж наступил на кнопку, но магазин уже открыт");
            }
        }
    }
    
    /// <summary>
    /// Обработка выхода персонажа из триггера
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerOnButton = false;
            shopWasOpened = false;
            canOpenShop = true; // Разблокировать открытие магазина при следующем входе
            Debug.Log($"[ShopButtonTrigger] Персонаж ушел с кнопки, можно открыть магазин снова");
        }
    }
    
    /// <summary>
    /// Проверка в Update для отслеживания закрытия магазина
    /// </summary>
    private void Update()
    {
        // Если персонаж на кнопке, но магазин закрылся - не открывать снова автоматически
        if (playerOnButton && shopWasOpened && !IsShopOpen())
        {
            // Магазин был закрыт, пока персонаж на кнопке
            // Не открываем снова, пока персонаж не уйдет с кнопки
            // canOpenShop уже false, поэтому ничего не делаем
        }
    }
    
    /// <summary>
    /// Проверить, является ли коллайдер персонажем
    /// </summary>
    private bool IsPlayer(Collider collider)
    {
        // Проверить по тегу
        if (collider.CompareTag("Player"))
        {
            return true;
        }
        
        // Проверить по компоненту PlayerController
        if (collider.GetComponent<PlayerController>() != null)
        {
            return true;
        }
        
        // Проверить по имени
        if (collider.gameObject.name == "Player" || collider.gameObject.name.Contains("Player"))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Проверить, открыт ли магазин
    /// </summary>
    private bool IsShopOpen()
    {
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            return false;
        }
        
        return inventoryUI.IsShopModalOpen();
    }
    
    /// <summary>
    /// Открыть магазин
    /// </summary>
    private void OpenShop()
    {
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.OpenShopModal();
        }
        else
        {
            Debug.LogError($"[ShopButtonTrigger] InventoryUI не найден в сцене!");
        }
    }
}

