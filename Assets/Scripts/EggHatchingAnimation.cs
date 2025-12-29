using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EggHatchingAnimation : MonoBehaviour
{
    [Header("Настройки анимации")]
    [SerializeField] private float hatchingDuration = 7f;
    [SerializeField] private float colorChangeStartTime = 4f; // С 4 секунды начинается изменение цвета лучей
    
    [Header("Мерцание")]
    [SerializeField] private float flickerSpeed = 2f;
    [SerializeField] private float flickerIntensity = 0.3f;
    
    [Header("Покачивание")]
    [SerializeField] private float wobbleSpeed = 1.5f;
    [SerializeField] private float wobbleAngle = 5f;
    
    [Header("Эффект молнии")]
    [SerializeField] private GameObject lightningEffectPrefab; // Префаб эффекта молнии
    
    private PetRarity petRarity;
    private System.Action<PetRarity> onComplete;
    private Material eggMaterial;
    private Light eggLight;
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
        // Найти Material для мерцания
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            eggMaterial = renderer.material;
            if (eggMaterial == null)
            {
                eggMaterial = renderer.material = new Material(Shader.Find("Standard"));
            }
        }
        
        // Найти или создать Light для мерцания
        eggLight = GetComponentInChildren<Light>();
        if (eggLight == null)
        {
            GameObject lightObj = new GameObject("EggLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            eggLight = lightObj.AddComponent<Light>();
            eggLight.type = LightType.Point;
            eggLight.range = 5f;
            eggLight.intensity = 1f;
            eggLight.color = Color.white;
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
            
            // Мерцание
            UpdateFlicker(elapsedTime);
            
            // Покачивание
            UpdateWobble(elapsedTime);
            
            // Изменение цвета света с 4 секунды
            if (elapsedTime >= colorChangeStartTime)
            {
                UpdateLightColor(elapsedTime);
            }
            
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
        
        // Создать экземпляр эффекта на 1 единицу выше яйца
        Vector3 effectPosition = transform.position + Vector3.up * 1.25f;
        lightningEffectInstance = Instantiate(lightningEffectPrefab, effectPosition, Quaternion.identity);
        lightningEffectInstance.name = "LightningEffect_Egg";
        
        // Разместить эффект на позиции яйца + 1 единица вверх
        lightningEffectInstance.transform.position = effectPosition;
        
        // Уменьшить масштаб эффекта на 20% (до 80% от исходного размера)
        lightningEffectInstance.transform.localScale = Vector3.one * 0.8f;
        
        // Можно сделать эффект дочерним объектом яйца, чтобы он двигался вместе с ним
        lightningEffectInstance.transform.SetParent(transform);
    }
    
    /// <summary>
    /// Обновить мерцание яйца
    /// </summary>
    private void UpdateFlicker(float time)
    {
        float flickerValue = Mathf.Sin(time * flickerSpeed) * flickerIntensity + (1f - flickerIntensity);
        
        // Мерцание через Light
        if (eggLight != null)
        {
            eggLight.intensity = flickerValue;
        }
        
        // Мерцание через Material (альфа-канал)
        if (eggMaterial != null)
        {
            Color color = eggMaterial.color;
            color.a = flickerValue;
            eggMaterial.color = color;
        }
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
    /// Обновить цвет света (с 4 секунды)
    /// </summary>
    private void UpdateLightColor(float time)
    {
        if (eggLight != null)
        {
            float colorProgress = (time - colorChangeStartTime) / (hatchingDuration - colorChangeStartTime);
            colorProgress = Mathf.Clamp01(colorProgress);
            Color currentColor = Color.Lerp(Color.white, rarityColor, colorProgress);
            eggLight.color = Color.Lerp(Color.white, currentColor, colorProgress * 0.5f);
        }
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

