using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EggHatchingAnimation : MonoBehaviour
{
    [Header("Настройки анимации")]
    [SerializeField] private float hatchingDuration = 7f;
    
    [Header("Покачивание")]
    [SerializeField] private float wobbleSpeed = 1.5f;
    [SerializeField] private float wobbleAngle = 5f;
    
    [Header("Эффект молнии")]
    [SerializeField] private GameObject lightningEffectPrefab; // Префаб эффекта молнии
    
    private PetRarity petRarity;
    private System.Action<PetRarity> onComplete;
    private Material eggMaterial;
    private GameObject lightningEffectInstance;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Color rarityColor;
    
    /// <summary>
    /// Инициализировать анимацию вылупления
    /// </summary>
    public void Initialize(PetRarity rarity, System.Action<PetRarity> onHatchingComplete)
    {
        petRarity = rarity;
        onComplete = onHatchingComplete;
        rarityColor = PetHatchingManager.GetRarityColor(rarity);
        
        // Сохранить исходную позицию и поворот
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Найти или создать компоненты для анимации
        SetupAnimationComponents();
        
        // Запустить корутину анимации
        StartCoroutine(HatchingCoroutine());
    }
    
    /// <summary>
    /// Настроить компоненты для анимации
    /// </summary>
    private void SetupAnimationComponents()
    {
        // Найти Material яйца (если нужен для других эффектов)
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            eggMaterial = renderer.material;
            if (eggMaterial == null)
            {
                eggMaterial = renderer.material = new Material(Shader.Find("Standard"));
            }
        }
        
        // Загрузить префаб эффекта молнии, если не назначен вручную
        if (lightningEffectPrefab == null)
        {
            LoadLightningEffect();
        }
    }
    
    /// <summary>
    /// Загрузить префаб эффекта молнии
    /// </summary>
    private void LoadLightningEffect()
    {
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        lightningEffectPrefab = Resources.Load<GameObject>("vfx_Lightning_02");
        
#if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (lightningEffectPrefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("vfx_Lightning_02 t:GameObject");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                lightningEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
#endif
    }
    
    /// <summary>
    /// Корутина анимации вылупления
    /// </summary>
    private IEnumerator HatchingCoroutine()
    {
        float elapsedTime = 0f;
        
        // Создать эффект молнии
        CreateLightningEffect();
        
        while (elapsedTime < hatchingDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Покачивание
            UpdateWobble(elapsedTime);
            
            yield return null;
        }
        
        // Завершение анимации
        OnAnimationComplete();
    }
    
    /// <summary>
    /// Создать эффект молнии вокруг яйца
    /// </summary>
    private void CreateLightningEffect()
    {
        if (lightningEffectPrefab == null)
        {
            return;
        }
        
        // Создать экземпляр эффекта на 1.25 единицы выше яйца
        Vector3 effectPosition = transform.position + Vector3.up * 1.25f;
        lightningEffectInstance = Instantiate(lightningEffectPrefab, effectPosition, Quaternion.identity);
        lightningEffectInstance.name = "LightningEffect_Egg";
        
        // Разместить эффект на позиции яйца + 1.25 единицы вверх
        lightningEffectInstance.transform.position = effectPosition;
        
        // Увеличить масштаб эффекта в 2 раза (базовый размер уже увеличен в 2 раза в PetBehavior)
        lightningEffectInstance.transform.localScale = Vector3.one * 2f;
        
        // Сделать эффект дочерним объектом яйца, чтобы он двигался вместе с ним
        lightningEffectInstance.transform.SetParent(transform);
        
        // Убрать иконки Unity префаба (Gizmos) - скрыть все компоненты, которые показывают иконки
        HidePrefabIcons(lightningEffectInstance);
    }
    
    /// <summary>
    /// Скрыть иконки Unity префаба (Gizmos)
    /// </summary>
    private void HidePrefabIcons(GameObject effect)
    {
        if (effect == null) return;
        
        // Скрыть иконки для всех компонентов, которые могут их показывать
        Component[] allComponents = effect.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            if (component != null && component is MonoBehaviour)
            {
                // Скрыть иконки через hideFlags
                component.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }
        }
        
        // Скрыть иконки для ParticleSystem (часто показывает иконки)
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            if (particle != null)
            {
                particle.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }
        }
        
        // Скрыть иконки для Light компонентов
        Light[] lights = effect.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }
        }
        
        // Скрыть иконки для AudioSource
        AudioSource[] audioSources = effect.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audio in audioSources)
        {
            if (audio != null)
            {
                audio.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            }
        }
        
        // Также скрыть иконки для самого GameObject
        effect.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
    }
    
    /// <summary>
    /// Обновить покачивание яйца (усиливается со временем)
    /// </summary>
    private void UpdateWobble(float time)
    {
        // Усиление тряски по ходу анимации (от 1x до 3x)
        float normalizedTime = time / hatchingDuration;
        float intensityMultiplier = 1f + (normalizedTime * 2f); // От 1 до 3
        
        float currentWobbleAngle = wobbleAngle * intensityMultiplier;
        float currentWobbleSpeed = wobbleSpeed * (1f + normalizedTime * 0.5f); // Ускоряется со временем
        
        float wobbleX = Mathf.Sin(time * currentWobbleSpeed) * currentWobbleAngle;
        float wobbleZ = Mathf.Cos(time * currentWobbleSpeed * 1.3f) * currentWobbleAngle;
        
        transform.rotation = originalRotation * Quaternion.Euler(wobbleX, 0f, wobbleZ);
    }
    
    
    /// <summary>
    /// Завершение анимации
    /// </summary>
    private void OnAnimationComplete()
    {
        // Вызвать callback
        if (onComplete != null)
        {
            onComplete(petRarity);
        }
    }
    
    private void OnDestroy()
    {
        // Очистка ресурсов
        if (lightningEffectInstance != null)
        {
            Destroy(lightningEffectInstance);
        }
    }
}

