using System.Collections.Generic;
using UnityEngine;

namespace YG
{
    /// <summary>
    /// Расширение класса SavesYG для сохранения игровых данных
    /// </summary>
    public partial class SavesYG
    {
        // Монеты
        public int coins = 100;
        
        // Питомцы (сериализуемая версия)
        public List<PetDataSerializable> pets = new List<PetDataSerializable>();
        
        // Улучшения кристаллов
        public int crystalHPLevel = 1;
        
        // Текущая локация ("Map1" или "Level2Map")
        public string currentMap = "Map1";
        
        // Покупка улучшенной карты
        public bool mapUpgradePurchased = false;
    }
    
    /// <summary>
    /// Сериализуемая версия PetData (без GameObject)
    /// </summary>
    [System.Serializable]
    public class PetDataSerializable
    {
        public PetRarity rarity;
        public string petName;
        public int petID;
        public float size = 1f;
        public ColorSerializable petColor;
        public string petModelPath;
        
        public PetDataSerializable() { }
        
        public PetDataSerializable(PetData petData)
        {
            if (petData != null)
            {
                rarity = petData.rarity;
                petName = petData.petName;
                petID = petData.petID;
                size = petData.size;
                petColor = new ColorSerializable(petData.petColor);
                petModelPath = petData.petModelPath;
            }
        }
        
        public PetData ToPetData()
        {
            PetData petData = new PetData();
            petData.rarity = rarity;
            petData.petName = petName;
            petData.petID = petID;
            petData.size = size;
            petData.petColor = petColor.ToColor();
            petData.petModelPath = petModelPath;
            petData.worldInstance = null; // GameObject не сохраняется
            return petData;
        }
    }
    
    /// <summary>
    /// Сериализуемая версия Color
    /// </summary>
    [System.Serializable]
    public class ColorSerializable
    {
        public float r;
        public float g;
        public float b;
        public float a;
        
        public ColorSerializable() { }
        
        public ColorSerializable(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
        
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}

