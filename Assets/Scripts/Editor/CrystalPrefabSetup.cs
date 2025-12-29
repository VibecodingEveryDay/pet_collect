#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Скрипт для настройки префаба кристалла с UI компонентами
/// </summary>
public class CrystalPrefabSetup : EditorWindow
{
    [MenuItem("Tools/Setup Crystal Prefab")]
    public static void SetupCrystalPrefab()
    {
        // Найти префаб Crystal1
        string prefabPath = "Assets/Assets/Crystals/Crystal1.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Префаб не найден по пути: {prefabPath}");
            return;
        }
        
        // Открыть префаб для редактирования
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Найти корневой объект кристалла
        GameObject crystalRoot = prefabInstance;
        if (crystalRoot == null)
        {
            Debug.LogError("Не удалось найти корневой объект префаба");
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            return;
        }
        
        // Добавить компонент Crystal, если его нет
        Crystal crystalComponent = crystalRoot.GetComponent<Crystal>();
        if (crystalComponent == null)
        {
            crystalComponent = crystalRoot.AddComponent<Crystal>();
        }
        
        // Найти или создать Canvas для health bar
        Canvas healthBarCanvas = crystalRoot.GetComponentInChildren<Canvas>();
        GameObject canvasObj = null;
        
        if (healthBarCanvas == null)
        {
            // Создать Canvas для health bar
            canvasObj = new GameObject("HealthBarCanvas");
            canvasObj.transform.SetParent(crystalRoot.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            
            healthBarCanvas = canvasObj.AddComponent<Canvas>();
            healthBarCanvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1f;
            scaler.scaleFactor = 1f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200f, 60f);
            canvasRect.localScale = Vector3.one * 0.05f;
        }
        else
        {
            canvasObj = healthBarCanvas.gameObject;
            // Убедиться, что Canvas в World Space
            healthBarCanvas.renderMode = RenderMode.WorldSpace;
        }
        
        // Найти или создать Outline
        Image outlineImage = canvasObj.transform.Find("Outline")?.GetComponent<Image>();
        GameObject outlineObj = null;
        if (outlineImage == null)
        {
            outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(canvasObj.transform, false);
            outlineImage = outlineObj.AddComponent<Image>();
            outlineImage.color = new Color(0f, 0f, 0f, 1f);
            
            RectTransform outlineRect = outlineObj.GetComponent<RectTransform>();
            outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
            outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
            outlineRect.sizeDelta = new Vector2(154f, 14f);
            outlineRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            outlineObj = outlineImage.gameObject;
        }
        
        // Найти или создать Background
        Image backgroundImage = canvasObj.transform.Find("Background")?.GetComponent<Image>();
        GameObject backgroundObj = null;
        if (backgroundImage == null)
        {
            backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(canvasObj.transform, false);
            backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 1f);
            
            RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(150f, 10f);
            bgRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            backgroundObj = backgroundImage.gameObject;
        }
        
        // Найти или создать Fill
        Image fillImage = backgroundObj.transform.Find("Fill")?.GetComponent<Image>();
        GameObject fillObj = null;
        if (fillImage == null)
        {
            fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(backgroundObj.transform, false);
            fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0f, 1f, 0.2f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            fillObj = fillImage.gameObject;
        }
        
        // Найти или создать Text
        Text healthText = canvasObj.transform.Find("HealthText")?.GetComponent<Text>();
        GameObject textObj = null;
        if (healthText == null)
        {
            textObj = new GameObject("HealthText");
            textObj.transform.SetParent(backgroundObj.transform, false);
            healthText = textObj.AddComponent<Text>();
            healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthText.fontSize = 12;
            healthText.fontStyle = FontStyle.Bold;
            healthText.color = Color.white;
            healthText.alignment = TextAnchor.MiddleCenter;
            healthText.text = "100 / 100";
            healthText.resizeTextForBestFit = false;
            healthText.horizontalOverflow = HorizontalWrapMode.Overflow;
            healthText.verticalOverflow = VerticalWrapMode.Overflow;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            Outline textOutline = textObj.AddComponent<Outline>();
            textOutline.effectColor = Color.black;
            textOutline.effectDistance = new Vector2(1.5f, 1.5f);
        }
        else
        {
            textObj = healthText.gameObject;
        }
        
        // Добавить компонент CrystalHealthBar
        CrystalHealthBar healthBarComponent = canvasObj.GetComponent<CrystalHealthBar>();
        if (healthBarComponent == null)
        {
            healthBarComponent = canvasObj.AddComponent<CrystalHealthBar>();
        }
        
        // Назначить ссылки в компоненте Crystal через SerializedObject
        SerializedObject crystalSO = new SerializedObject(crystalComponent);
        
        SerializedProperty healthBarProp = crystalSO.FindProperty("healthBar");
        if (healthBarProp != null) healthBarProp.objectReferenceValue = healthBarComponent;
        
        SerializedProperty canvasProp = crystalSO.FindProperty("healthBarCanvas");
        if (canvasProp != null) canvasProp.objectReferenceValue = healthBarCanvas;
        
        SerializedProperty fillProp = crystalSO.FindProperty("healthBarFill");
        if (fillProp != null) fillProp.objectReferenceValue = fillImage;
        
        SerializedProperty bgProp = crystalSO.FindProperty("healthBarBackground");
        if (bgProp != null) bgProp.objectReferenceValue = backgroundImage;
        
        SerializedProperty outlineProp = crystalSO.FindProperty("healthBarOutline");
        if (outlineProp != null) outlineProp.objectReferenceValue = outlineImage;
        
        SerializedProperty textProp = crystalSO.FindProperty("healthBarText");
        if (textProp != null) textProp.objectReferenceValue = healthText;
        
        crystalSO.ApplyModifiedProperties();
        
        // Также назначить ссылки в CrystalHealthBar компоненте
        SerializedObject healthBarSO = new SerializedObject(healthBarComponent);
        SerializedProperty canvasPropHB = healthBarSO.FindProperty("healthBarCanvas");
        if (canvasPropHB != null) canvasPropHB.objectReferenceValue = healthBarCanvas;
        SerializedProperty fillPropHB = healthBarSO.FindProperty("healthBarFill");
        if (fillPropHB != null) fillPropHB.objectReferenceValue = fillImage;
        SerializedProperty bgPropHB = healthBarSO.FindProperty("healthBarBackground");
        if (bgPropHB != null) bgPropHB.objectReferenceValue = backgroundImage;
        SerializedProperty outlinePropHB = healthBarSO.FindProperty("healthBarOutline");
        if (outlinePropHB != null) outlinePropHB.objectReferenceValue = outlineImage;
        SerializedProperty textPropHB = healthBarSO.FindProperty("healthBarText");
        if (textPropHB != null) textPropHB.objectReferenceValue = healthText;
        healthBarSO.ApplyModifiedProperties();
        
        // Сохранить изменения в префаб
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);
        
        Debug.Log("Префаб Crystal1 успешно настроен с UI компонентами!");
    }
}
#endif

