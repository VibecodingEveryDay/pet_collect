using UnityEngine;

/// <summary>
/// Автоматическая настройка освещения для предотвращения излишнего затемнения объектов
/// </summary>
[ExecuteInEditMode]
public class LightingSetup : MonoBehaviour
{
    [Header("Настройки Directional Light")]
    [SerializeField] private float lightIntensity = 0.8f; // Уменьшенная интенсивность для предотвращения затемнения
    [SerializeField] private Color lightColor = new Color(1f, 0.98f, 0.95f); // Светлый теплый белый
    [SerializeField] private LightShadows shadowType = LightShadows.Soft;
    [SerializeField] private float shadowStrength = 0.3f; // Уменьшенная сила теней
    
    [Header("Настройки Ambient Light")]
    [SerializeField] private Color ambientSkyColor = new Color(0.4f, 0.4f, 0.45f); // Более светлый ambient
    [SerializeField] private float ambientIntensity = 1.2f; // Увеличенный ambient для компенсации
    
    [Header("Автоматическая настройка")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool findLightAutomatically = true;
    
    private Light directionalLight;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupLighting();
        }
    }
    
    /// <summary>
    /// Настроить освещение
    /// </summary>
    [ContextMenu("Настроить освещение")]
    public void SetupLighting()
    {
        // Найти Directional Light
        if (findLightAutomatically)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }
        }
        else
        {
            directionalLight = GetComponent<Light>();
        }
        
        if (directionalLight == null)
        {
            return;
        }
        
        // Настроить параметры освещения
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = lightIntensity;
        directionalLight.color = lightColor;
        directionalLight.shadows = shadowType;
        
        // Настроить тени
        if (shadowType != LightShadows.None)
        {
            directionalLight.shadowStrength = shadowStrength;
            directionalLight.shadowBias = 0.05f;
            directionalLight.shadowNormalBias = 0.4f;
        }
        
        // Настроить диапазон теней
        directionalLight.shadowNearPlane = 0.1f;
        
        // Настроить Ambient Light для компенсации затемнения
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = new Color(ambientSkyColor.r * 0.7f, ambientSkyColor.g * 0.7f, ambientSkyColor.b * 0.7f);
        RenderSettings.ambientGroundColor = new Color(ambientSkyColor.r * 0.4f, ambientSkyColor.g * 0.4f, ambientSkyColor.b * 0.4f);
        RenderSettings.ambientIntensity = ambientIntensity;
    }
    
    /// <summary>
    /// Уменьшить интенсивность освещения
    /// </summary>
    public void ReduceIntensity(float multiplier = 0.7f)
    {
        if (directionalLight != null)
        {
            directionalLight.intensity *= multiplier;
            lightIntensity = directionalLight.intensity;
        }
    }
    
    /// <summary>
    /// Увеличить интенсивность освещения
    /// </summary>
    public void IncreaseIntensity(float multiplier = 1.3f)
    {
        if (directionalLight != null)
        {
            directionalLight.intensity *= multiplier;
            lightIntensity = directionalLight.intensity;
        }
    }
    
    /// <summary>
    /// Отключить тени
    /// </summary>
    public void DisableShadows()
    {
        if (directionalLight != null)
        {
            directionalLight.shadows = LightShadows.None;
            shadowType = LightShadows.None;
        }
    }
    
    /// <summary>
    /// Включить мягкие тени
    /// </summary>
    public void EnableSoftShadows()
    {
        if (directionalLight != null)
        {
            directionalLight.shadows = LightShadows.Soft;
            shadowType = LightShadows.Soft;
            directionalLight.shadowStrength = shadowStrength;
        }
    }
    
    private void OnValidate()
    {
        // При изменении значений в Inspector автоматически применять настройки
        if (directionalLight != null && Application.isPlaying)
        {
            SetupLighting();
        }
    }
}

