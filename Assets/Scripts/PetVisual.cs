using UnityEngine;

/// <summary>
/// Компонент для визуализации питомца в мире (примитив по редкости)
/// </summary>
public class PetVisual : MonoBehaviour
{
    [Header("Настройки визуализации")]
    [SerializeField] private PetRarity rarity;
    [SerializeField] private float petSize = 0.5f;
    [SerializeField] private PrimitiveType visualType = PrimitiveType.Sphere;
    
    private GameObject visualObject;
    private PetData petData;
    
    /// <summary>
    /// Создать визуализацию питомца
    /// </summary>
    public void CreateVisual(PetRarity petRarity, PetData data = null)
    {
        rarity = petRarity;
        petData = data;
        
        // Удалить старую визуализацию, если есть
        if (visualObject != null)
        {
            Destroy(visualObject);
        }
        
        // Создать примитив
        visualObject = GameObject.CreatePrimitive(visualType);
        visualObject.name = $"PetVisual_{rarity}";
        visualObject.transform.SetParent(transform);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one * petSize;
        
        // Установить цвет по редкости
        Renderer renderer = visualObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = PetHatchingManager.GetRarityColor(rarity);
            material.SetFloat("_Metallic", 0.5f);
            material.SetFloat("_Glossiness", 0.7f);
            renderer.material = material;
        }
        
        // Удалить коллайдер (не нужен для визуализации)
        Collider collider = visualObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
    }
    
    /// <summary>
    /// Установить размер питомца
    /// </summary>
    public void SetSize(float size)
    {
        petSize = size;
        if (visualObject != null)
        {
            visualObject.transform.localScale = Vector3.one * petSize;
        }
    }
    
    /// <summary>
    /// Получить редкость питомца
    /// </summary>
    public PetRarity GetRarity()
    {
        return rarity;
    }
    
    /// <summary>
    /// Получить данные питомца
    /// </summary>
    public PetData GetPetData()
    {
        return petData;
    }
    
    private void OnDestroy()
    {
        if (visualObject != null)
        {
            Destroy(visualObject);
        }
    }
}

