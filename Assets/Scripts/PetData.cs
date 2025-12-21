using UnityEngine;

/// <summary>
/// Класс для хранения данных о питомце
/// </summary>
[System.Serializable]
public class PetData
{
    [Header("Основные данные")]
    public PetRarity rarity;
    public string petName;
    public int petID;
    
    [Header("Дополнительные данные")]
    public float size = 1f;
    public Color petColor;
    public string petModelPath; // Путь к GLB модели питомца
    public GameObject worldInstance; // Ссылка на GameObject питомца в мире
    
    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public PetData()
    {
        rarity = PetRarity.Common;
        petName = "Питомец";
        petID = 0;
        size = 1f;
        petColor = Color.white;
    }
    
    /// <summary>
    /// Конструктор с редкостью
    /// </summary>
    public PetData(PetRarity petRarity)
    {
        rarity = petRarity;
        petName = $"Питомец {petRarity}";
        petID = Random.Range(1000, 9999);
        size = 1f;
        petColor = PetHatchingManager.GetRarityColor(petRarity);
    }
    
    /// <summary>
    /// Конструктор с полными данными
    /// </summary>
    public PetData(PetRarity petRarity, string name, int id, float petSize, Color color)
    {
        rarity = petRarity;
        petName = name;
        petID = id;
        size = petSize;
        petColor = color;
    }
}

