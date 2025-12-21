using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система управления инвентарем питомцев (Singleton)
/// </summary>
public class PetInventory : MonoBehaviour
{
    private static PetInventory _instance;
    
    /// <summary>
    /// Событие добавления питомца в инвентарь
    /// </summary>
    public static System.Action<PetData> OnPetAdded;
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static PetInventory Instance
    {
        get
        {
            if (_instance == null)
            {
                // Попытаться найти существующий экземпляр
                _instance = FindObjectOfType<PetInventory>();
                
                // Если не найден, создать новый
                if (_instance == null)
                {
                    GameObject inventoryObject = new GameObject("PetInventory");
                    _instance = inventoryObject.AddComponent<PetInventory>();
                    DontDestroyOnLoad(inventoryObject);
                }
            }
            return _instance;
        }
    }
    
    [Header("Данные инвентаря")]
    [SerializeField] private List<PetData> pets = new List<PetData>();
    
    private void Awake()
    {
        // Убедиться, что только один экземпляр существует
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Инициализировать пустой список, если он null
        if (pets == null)
        {
            pets = new List<PetData>();
        }
    }
    
    /// <summary>
    /// Добавить питомца в инвентарь
    /// </summary>
    public void AddPet(PetData petData)
    {
        if (petData != null)
        {
            pets.Add(petData);
            Debug.Log($"Питомец добавлен в инвентарь: {petData.petName} ({petData.rarity})");
            OnPetAdded?.Invoke(petData);
        }
    }
    
    /// <summary>
    /// Получить активных питомцев (первые N из списка)
    /// </summary>
    public List<PetData> GetActivePets(int maxCount = 5)
    {
        List<PetData> activePets = new List<PetData>();
        int count = Mathf.Min(maxCount, pets.Count);
        
        for (int i = 0; i < count; i++)
        {
            if (pets[i] != null)
            {
                activePets.Add(pets[i]);
            }
        }
        
        return activePets;
    }
    
    /// <summary>
    /// Удалить питомца из инвентаря
    /// </summary>
    public void RemovePet(PetData petData)
    {
        if (petData != null && pets.Contains(petData))
        {
            pets.Remove(petData);
            Debug.Log($"Питомец удален из инвентаря: {petData.petName}");
        }
    }
    
    /// <summary>
    /// Получить список всех питомцев
    /// </summary>
    public List<PetData> GetAllPets()
    {
        return new List<PetData>(pets);
    }
    
    /// <summary>
    /// Получить общее количество питомцев
    /// </summary>
    public int GetTotalPetCount()
    {
        return pets != null ? pets.Count : 0;
    }
    
    /// <summary>
    /// Получить количество питомцев определенной редкости
    /// </summary>
    public int GetPetCountByRarity(PetRarity rarity)
    {
        if (pets == null)
        {
            return 0;
        }
        
        int count = 0;
        foreach (PetData pet in pets)
        {
            if (pet != null && pet.rarity == rarity)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Проверить, есть ли питомец в инвентаре
    /// </summary>
    public bool HasPet(PetData petData)
    {
        return pets != null && pets.Contains(petData);
    }
    
    /// <summary>
    /// Очистить инвентарь
    /// </summary>
    public void ClearInventory()
    {
        if (pets != null)
        {
            pets.Clear();
            Debug.Log("Инвентарь питомцев очищен");
        }
    }
}


