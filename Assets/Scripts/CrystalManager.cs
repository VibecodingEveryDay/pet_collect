using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер для управления кристаллами на сцене
/// </summary>
public class CrystalManager : MonoBehaviour
{
    private static CrystalManager _instance;
    private static List<Crystal> allCrystals = new List<Crystal>();
    
    /// <summary>
    /// Singleton экземпляр
    /// </summary>
    public static CrystalManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CrystalManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("CrystalManager");
                    _instance = managerObject.AddComponent<CrystalManager>();
                    DontDestroyOnLoad(managerObject);
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
    }
    
    /// <summary>
    /// Зарегистрировать кристалл
    /// </summary>
    public static void RegisterCrystal(Crystal crystal)
    {
        if (crystal != null && !allCrystals.Contains(crystal))
        {
            allCrystals.Add(crystal);
        }
    }
    
    /// <summary>
    /// Отменить регистрацию кристалла
    /// </summary>
    public static void UnregisterCrystal(Crystal crystal)
    {
        if (crystal != null)
        {
            allCrystals.Remove(crystal);
        }
    }
    
    /// <summary>
    /// Получить все живые кристаллы
    /// </summary>
    public static List<Crystal> GetAllCrystals()
    {
        // Удалить null ссылки и неживые кристаллы
        allCrystals.RemoveAll(c => c == null || !c.IsAlive());
        return new List<Crystal>(allCrystals);
    }
    
    // Словарь для отслеживания занятых кристаллов (кристалл -> питомец)
    private static Dictionary<Crystal, PetBehavior> occupiedCrystals = new Dictionary<Crystal, PetBehavior>();
    
    /// <summary>
    /// Зарегистрировать питомца, добывающего кристалл
    /// </summary>
    public static void RegisterPetMining(Crystal crystal, PetBehavior pet)
    {
        if (crystal != null && pet != null)
        {
            occupiedCrystals[crystal] = pet;
        }
    }
    
    /// <summary>
    /// Отменить регистрацию питомца, добывающего кристалл
    /// </summary>
    public static void UnregisterPetMining(Crystal crystal, PetBehavior pet)
    {
        if (crystal != null && occupiedCrystals.ContainsKey(crystal))
        {
            // Проверить, что это тот же питомец
            if (occupiedCrystals[crystal] == pet)
            {
                occupiedCrystals.Remove(crystal);
            }
        }
    }
    
    /// <summary>
    /// Проверить, занят ли кристалл
    /// </summary>
    public static bool IsCrystalOccupied(Crystal crystal)
    {
        if (crystal == null)
        {
            return false;
        }
        
        // Очистить ссылки на уничтоженные питомцы
        if (occupiedCrystals.ContainsKey(crystal))
        {
            PetBehavior pet = occupiedCrystals[crystal];
            if (pet == null)
            {
                occupiedCrystals.Remove(crystal);
                return false;
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Получить питомца, который добывает указанный кристалл
    /// </summary>
    public static PetBehavior GetPetMiningCrystal(Crystal crystal)
    {
        if (crystal == null || !occupiedCrystals.ContainsKey(crystal))
        {
            return null;
        }
        
        PetBehavior pet = occupiedCrystals[crystal];
        
        // Очистить ссылку, если питомец уничтожен
        if (pet == null)
        {
            occupiedCrystals.Remove(crystal);
            return null;
        }
        
        return pet;
    }
    
    /// <summary>
    /// Найти ближайший кристалл к указанной позиции, который не занят другими питомцами
    /// </summary>
    public static Crystal GetNearestCrystal(Vector3 position, PetBehavior requestingPet = null)
    {
        List<Crystal> aliveCrystals = GetAllCrystals();
        
        if (aliveCrystals.Count == 0)
        {
            return null;
        }
        
        Crystal nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Crystal crystal in aliveCrystals)
        {
            if (crystal == null || !crystal.IsAlive())
            {
                continue;
            }
            
            // Пропустить кристалл, если он занят другим питомцем
            if (IsCrystalOccupied(crystal))
            {
                // Если это тот же питомец, который уже добывает этот кристалл, разрешить
                if (requestingPet != null && occupiedCrystals.ContainsKey(crystal) && occupiedCrystals[crystal] == requestingPet)
                {
                    // Разрешить - это тот же питомец
                }
                else
                {
                    // Пропустить - кристалл занят другим питомцем
                    continue;
                }
            }
            
            float distance = Vector3.Distance(position, crystal.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = crystal;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Получить количество живых кристаллов
    /// </summary>
    public static int GetCrystalCount()
    {
        GetAllCrystals(); // Очистить список от null
        return allCrystals.Count;
    }
}

