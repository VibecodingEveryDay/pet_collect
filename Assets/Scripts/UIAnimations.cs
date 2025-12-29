using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс для анимаций UI элементов в стиле Roblox
/// </summary>
public static class UIAnimations
{
    // Словарь для отслеживания активных анимаций bounce
    private static Dictionary<VisualElement, Coroutine> activeBounceAnimations = new Dictionary<VisualElement, Coroutine>();
    // Словарь для сохранения оригинальных размеров элементов
    private static Dictionary<VisualElement, Vector2> originalScales = new Dictionary<VisualElement, Vector2>();
    /// <summary>
    /// Анимация появления модального окна (масштаб + прозрачность)
    /// </summary>
    public static void AnimateModalAppear(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return;
        
        element.style.scale = new Scale(new Vector2(0.8f, 0.8f));
        element.style.opacity = 0f;
        
        coroutineRunner.StartCoroutine(AnimateScaleAndOpacity(element, Vector2.one, 1f, 0.3f));
    }
    
    /// <summary>
    /// Анимация исчезновения модального окна
    /// </summary>
    public static void AnimateModalDisappear(VisualElement element, MonoBehaviour coroutineRunner, System.Action onComplete = null)
    {
        if (element == null || coroutineRunner == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        coroutineRunner.StartCoroutine(AnimateScaleAndOpacity(element, new Vector2(0.8f, 0.8f), 0f, 0.2f, onComplete));
    }
    
    /// <summary>
    /// Анимация пульсации (для важных элементов)
    /// </summary>
    public static void AnimatePulse(VisualElement element, MonoBehaviour coroutineRunner, float duration = 1f)
    {
        if (element == null || coroutineRunner == null) return;
        
        coroutineRunner.StartCoroutine(PulseAnimation(element, duration));
    }
    
    // Словарь для отслеживания активных непрерывных анимаций пульсации
    private static Dictionary<VisualElement, Coroutine> activePulseAnimations = new Dictionary<VisualElement, Coroutine>();
    
    /// <summary>
    /// Запустить непрерывную анимацию пульсации (для подсказок)
    /// </summary>
    public static Coroutine StartContinuousPulse(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return null;
        
        // Остановить предыдущую анимацию, если она есть
        StopContinuousPulse(element, coroutineRunner);
        
        // Запустить новую непрерывную анимацию
        Coroutine pulseCoroutine = coroutineRunner.StartCoroutine(ContinuousPulseAnimation(element));
        activePulseAnimations[element] = pulseCoroutine;
        return pulseCoroutine;
    }
    
    /// <summary>
    /// Остановить непрерывную анимацию пульсации
    /// </summary>
    public static void StopContinuousPulse(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return;
        
        if (activePulseAnimations.ContainsKey(element) && activePulseAnimations[element] != null)
        {
            coroutineRunner.StopCoroutine(activePulseAnimations[element]);
            activePulseAnimations.Remove(element);
            
            // Восстановить оригинальный размер
            if (originalScales.ContainsKey(element))
            {
                element.style.scale = new Scale(originalScales[element]);
            }
            else
            {
                element.style.scale = new Scale(Vector2.one);
            }
        }
    }
    
    /// <summary>
    /// Анимация изменения числа (для счетчика монет)
    /// </summary>
    public static void AnimateNumberChange(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return;
        
        coroutineRunner.StartCoroutine(NumberChangeAnimation(element));
    }
    
    /// <summary>
    /// Инициализировать оригинальный размер элемента для bounce анимации
    /// </summary>
    public static void InitializeBounceAnimation(VisualElement element)
    {
        if (element == null) return;
        
        // Сохраняем оригинальный размер при первом использовании
        if (!originalScales.ContainsKey(element))
        {
            Vector3 currentScaleValue = element.style.scale.value.value;
            Vector2 currentScale = new Vector2(currentScaleValue.x, currentScaleValue.y);
            // Если scale не установлен, использовать Vector2.one
            if (currentScaleValue.x == 0f && currentScaleValue.y == 0f)
            {
                currentScale = Vector2.one;
                element.style.scale = new Scale(currentScale);
            }
            originalScales[element] = currentScale;
        }
    }
    
    /// <summary>
    /// Анимация "прыжка" элемента (bounce effect)
    /// </summary>
    public static void AnimateBounce(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return;
        
        // Инициализируем, если еще не инициализировано
        InitializeBounceAnimation(element);
        
        // Если анимация уже идет, останавливаем её
        if (activeBounceAnimations.ContainsKey(element) && activeBounceAnimations[element] != null)
        {
            coroutineRunner.StopCoroutine(activeBounceAnimations[element]);
            activeBounceAnimations.Remove(element);
            
            // Восстанавливаем оригинальный размер перед запуском новой анимации
            if (originalScales.ContainsKey(element))
            {
                element.style.scale = new Scale(originalScales[element]);
            }
        }
        
        // Запускаем новую анимацию
        Coroutine bounceCoroutine = coroutineRunner.StartCoroutine(BounceAnimation(element, coroutineRunner));
        activeBounceAnimations[element] = bounceCoroutine;
    }
    
    /// <summary>
    /// Анимация заполнения ячейки (для активных питомцев)
    /// </summary>
    public static void AnimateSlotFill(VisualElement element, MonoBehaviour coroutineRunner)
    {
        if (element == null || coroutineRunner == null) return;
        
        coroutineRunner.StartCoroutine(SlotFillAnimation(element));
    }
    
    /// <summary>
    /// Анимация масштаба и прозрачности
    /// </summary>
    private static IEnumerator AnimateScaleAndOpacity(VisualElement element, Vector2 targetScale, float targetOpacity, float duration, System.Action onComplete = null)
    {
        Vector2 startScale = element.style.scale.value.value;
        float startOpacity = element.style.opacity.value;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Используем easing функцию для плавности
            t = EaseOutBack(t);
            
            Vector2 currentScale = Vector2.Lerp(startScale, targetScale, t);
            float currentOpacity = Mathf.Lerp(startOpacity, targetOpacity, t);
            
            element.style.scale = new Scale(currentScale);
            element.style.opacity = currentOpacity;
            
            yield return null;
        }
        
        element.style.scale = new Scale(targetScale);
        element.style.opacity = targetOpacity;
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Анимация пульсации
    /// </summary>
    private static IEnumerator PulseAnimation(VisualElement element, float duration)
    {
        Vector2 originalScale = element.style.scale.value.value;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Синусоидальная пульсация
            float scale = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.1f;
            element.style.scale = new Scale(originalScale * scale);
            
            yield return null;
        }
        
        element.style.scale = new Scale(originalScale);
    }
    
    /// <summary>
    /// Непрерывная анимация пульсации (для подсказок)
    /// </summary>
    private static IEnumerator ContinuousPulseAnimation(VisualElement element)
    {
        // Инициализировать оригинальный размер, если еще не инициализирован
        InitializeBounceAnimation(element);
        
        // Получить оригинальный размер из сохраненных значений
        Vector2 originalScale = originalScales.ContainsKey(element) ? originalScales[element] : Vector2.one;
        float time = 0f;
        
        while (true)
        {
            time += Time.deltaTime;
            
            // Синусоидальная пульсация (1.0 - 1.15 масштаб)
            float scale = 1f + Mathf.Sin(time * 2f) * 0.15f;
            element.style.scale = new Scale(originalScale * scale);
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Анимация изменения числа
    /// </summary>
    private static IEnumerator NumberChangeAnimation(VisualElement element)
    {
        Vector2 originalScale = element.style.scale.value.value;
        float elapsed = 0f;
        float duration = 0.3f;
        
        // Увеличиваем и возвращаем
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            element.style.scale = new Scale(originalScale * scale);
            
            yield return null;
        }
        
        element.style.scale = new Scale(originalScale);
    }
    
    /// <summary>
    /// Анимация прыжка (bounce)
    /// </summary>
    private static IEnumerator BounceAnimation(VisualElement element, MonoBehaviour coroutineRunner)
    {
        // Используем сохраненный оригинальный размер
        Vector2 originalScale = originalScales.ContainsKey(element) ? originalScales[element] : Vector2.one;
        
        // Если scale не был установлен, установить его
        Vector3 currentScaleValue = element.style.scale.value.value;
        if (currentScaleValue.x == 0f && currentScaleValue.y == 0f)
        {
            element.style.scale = new Scale(originalScale);
        }
        
        float elapsed = 0f;
        float duration = 0.4f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Bounce эффект
            float scale = EaseOutBounce(t);
            element.style.scale = new Scale(originalScale * (1f + scale * 0.2f));
            
            yield return null;
        }
        
        // Восстанавливаем оригинальный размер
        element.style.scale = new Scale(originalScale);
        
        // Удаляем из словаря активных анимаций
        if (activeBounceAnimations.ContainsKey(element))
        {
            activeBounceAnimations.Remove(element);
        }
    }
    
    /// <summary>
    /// Easing функция для плавного появления
    /// </summary>
    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    /// <summary>
    /// Easing функция для bounce эффекта
    /// </summary>
    private static float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        }
        else
        {
            return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }
    }
    
    /// <summary>
    /// Анимация заполнения ячейки
    /// </summary>
    private static IEnumerator SlotFillAnimation(VisualElement element)
    {
        if (element == null) yield break;
        
        // Финальные значения всегда должны быть нормальными
        Vector2 targetScale = Vector2.one;
        float targetOpacity = 1f;
        
        float elapsed = 0f;
        float duration = 0.4f;
        
        // Начальное состояние: маленький и прозрачный
        element.style.scale = new Scale(new Vector2(0.5f, 0.5f));
        element.style.opacity = 0f;
        
        // Анимация появления с bounce эффектом
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Используем EaseOutBack для плавного появления с отскоком
            float scaleT = EaseOutBack(t);
            float opacityT = t;
            
            Vector2 currentScale = Vector2.Lerp(new Vector2(0.5f, 0.5f), targetScale, scaleT);
            float currentOpacity = Mathf.Lerp(0f, targetOpacity, opacityT);
            
            element.style.scale = new Scale(currentScale);
            element.style.opacity = currentOpacity;
            
            yield return null;
        }
        
        // Убедиться, что финальное состояние установлено правильно
        element.style.scale = new Scale(targetScale);
        element.style.opacity = targetOpacity;
        
        // Убедиться, что элемент видим
        element.style.visibility = Visibility.Visible;
        element.style.display = DisplayStyle.Flex;
    }
}

