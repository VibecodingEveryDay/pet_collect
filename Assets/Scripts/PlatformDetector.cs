using UnityEngine;
#if EnvirData_yg
using YG;
#endif

/// <summary>
/// Определяет платформу устройства с использованием YG2 SDK (EnvirData модуль)
/// Все данные о типе устройства получаются из YG2.envir
/// </summary>
public static class PlatformDetector
{
    private static bool? _isMobile = null;
    private static bool? _isTablet = null;
    private static bool? _isDesktop = null;
    private static bool? _isTV = null;
    
    /// <summary>
    /// Проверить, является ли устройство мобильным
    /// Использует YG2.envir.isMobile
    /// </summary>
    public static bool IsMobile()
    {
        if (_isMobile.HasValue)
        {
            return _isMobile.Value;
        }
        
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            _isMobile = YG2.envir.isMobile;
            
            float screenWidth = GetScreenWidth();
            float screenHeight = GetScreenHeight();
            string deviceInfo = $"deviceType={YG2.envir.deviceType}, device={YG2.envir.device}";
            string platformInfo = $"platform={YG2.envir.platform}, browser={YG2.envir.browser}";
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            int realWidth = GetRealScreenWidth();
            int realHeight = GetRealScreenHeight();
            float canvasWidth = Screen.width;
            float canvasHeight = Screen.height;
            Debug.Log($"[PlatformDetector] IsMobile: {_isMobile.Value} (YG2 SDK), {deviceInfo}, {platformInfo}, canvas={canvasWidth}x{canvasHeight}, realScreen={realWidth}x{realHeight}, UI={screenWidth}x{screenHeight}");
            #else
            Debug.Log($"[PlatformDetector] IsMobile: {_isMobile.Value} (YG2 SDK), {deviceInfo}, {platformInfo}, resolution={screenWidth}x{screenHeight}");
            #endif
            
            return _isMobile.Value;
        }
        #endif
        
        // Если YG2 SDK не доступен, возвращаем false (не должно произойти, если SDK настроен правильно)
        Debug.LogWarning("[PlatformDetector] YG2.envir не доступен! Убедитесь, что модуль EnvirData подключен.");
        _isMobile = false;
        return false;
            }
    
    /// <summary>
    /// Проверить, является ли устройство планшетом
    /// Использует YG2.envir.isTablet
    /// </summary>
    public static bool IsTablet()
    {
        if (_isTablet.HasValue)
        {
            return _isTablet.Value;
        }
        
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            _isTablet = YG2.envir.isTablet;
            return _isTablet.Value;
        }
        #endif
        
        _isTablet = false;
        return false;
    }
    
    /// <summary>
    /// Проверить, является ли устройство десктопом
    /// Использует YG2.envir.isDesktop
    /// </summary>
    public static bool IsDesktop()
            {
        if (_isDesktop.HasValue)
        {
            return _isDesktop.Value;
            }
        
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            _isDesktop = YG2.envir.isDesktop;
            return _isDesktop.Value;
        }
        #endif
        
        _isDesktop = false;
        return false;
    }
    
    /// <summary>
    /// Проверить, является ли устройство TV
    /// Использует YG2.envir.isTV
    /// </summary>
    public static bool IsTV()
    {
        if (_isTV.HasValue)
    {
            return _isTV.Value;
        }
        
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            _isTV = YG2.envir.isTV;
            return _isTV.Value;
        }
        #endif
        
        _isTV = false;
        return false;
    }
    
    /// <summary>
    /// Получить тип устройства как строку (для логов и отладки)
    /// </summary>
    public static string GetDeviceTypeString()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.deviceType;
        }
        #endif
        
        return "unknown";
            }
    
    /// <summary>
    /// Получить enum тип устройства
    /// </summary>
    #if EnvirData_yg
    public static YG2.Device GetDevice()
    {
        if (YG2.envir != null)
        {
            return YG2.envir.device;
        }
        
        return YG2.Device.Desktop;
    }
    #endif
    
    #if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int GetRealScreenWidthJS();
    
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int GetRealScreenHeightJS();
    #endif
    
    /// <summary>
    /// Получить ширину окна/экрана для адаптивности UI
    /// ВАЖНО: Для адаптивности на 4К, 2К и других мониторах используем реальные размеры экрана
    /// Это позволяет правильно адаптировать UI: на 4К мониторе UI адаптируется под 4К, на 2К - под 2К
    /// Для альбомной ориентации всегда больше высоты
    /// </summary>
    public static float GetScreenWidth()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // В WebGL для адаптивности используем реальные размеры экрана
        int realWidth = GetRealScreenWidth();
        float canvasWidth = Screen.width;
        
        // Используем реальные размеры экрана для адаптивности UI
        if (realWidth > 0)
            {
            return realWidth;
        }
        
        // Fallback на canvas, если реальные размеры не получены
        return canvasWidth;
        #else
        // В других платформах используем размеры экрана
        return Screen.width;
        #endif
    }
    
    /// <summary>
    /// Получить высоту окна/экрана для адаптивности UI
    /// ВАЖНО: Для адаптивности на 4К, 2К и других мониторах используем реальные размеры экрана
    /// Это позволяет правильно адаптировать UI: на 4К мониторе UI адаптируется под 4К, на 2К - под 2К
    /// Для альбомной ориентации всегда меньше ширины
    /// </summary>
    public static float GetScreenHeight()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // В WebGL для адаптивности используем реальные размеры экрана
        int realHeight = GetRealScreenHeight();
        float canvasHeight = Screen.height;
        
        // Используем реальные размеры экрана для адаптивности UI
        if (realHeight > 0)
        {
            return realHeight;
        }
        
        // Fallback на canvas, если реальные размеры не получены
        return canvasHeight;
        #else
        // В других платформах используем размеры экрана
        return Screen.height;
        #endif
    }
    
    /// <summary>
    /// Получить реальные размеры экрана устройства (для WebGL - screen.width/height)
    /// Используется для определения реального устройства и адаптивности UI
    /// На 4К телевизорах и больших мониторах это даст реальное разрешение экрана
    /// </summary>
    public static int GetRealScreenWidth()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        return GetRealScreenWidthJS();
        #else
        return Screen.width;
        #endif
    }
    
    /// <summary>
    /// Получить реальные размеры экрана устройства (для WebGL - screen.width/height)
    /// Используется для определения реального устройства и адаптивности UI
    /// На 4К телевизорах и больших мониторах это даст реальное разрешение экрана
    /// </summary>
    public static int GetRealScreenHeight()
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        return GetRealScreenHeightJS();
    #else
        return Screen.height;
        #endif
    }
    
    /// <summary>
    /// Получить соотношение сторон окна/экрана (для альбомной ориентации всегда >= 1.0)
    /// </summary>
    public static float GetAspectRatio()
    {
        // Игра ВСЕГДА в альбомной ориентации
        // Мобильные устройства в альбомной: обычно 1.5-2.5 (16:9 = 1.78, 21:9 = 2.33)
        return GetScreenWidth() / GetScreenHeight();
    }
    
    /// <summary>
    /// Получить язык от SDK платформы
    /// </summary>
    public static string GetLanguage()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.language;
        }
        #endif
        
        return "en";
    }
    
    /// <summary>
    /// Получить язык браузера
    /// </summary>
    public static string GetBrowserLanguage()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.browserLang;
        }
        #endif
        
        return "en";
    }
    
    /// <summary>
    /// Получить домен (ru, com и т.д.)
    /// </summary>
    public static string GetDomain()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.domain;
        }
        #endif
        
        return "com";
    }
    
    /// <summary>
    /// Получить ID приложения
    /// </summary>
    public static string GetAppID()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.appID;
        }
        #endif
        
        return "";
    }
    
    /// <summary>
    /// Получить платформу (операционную систему)
    /// </summary>
    public static string GetPlatform()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.platform;
        }
        #endif
        
        return Application.platform.ToString();
    }
    
    /// <summary>
    /// Получить браузер
    /// </summary>
    public static string GetBrowser()
    {
        #if EnvirData_yg
        if (YG2.envir != null)
        {
            return YG2.envir.browser;
    }
    #endif
        
        return "Other";
    }
    
    /// <summary>
    /// Сбросить кэш определения платформы (для тестирования)
    /// </summary>
    public static void ResetCache()
    {
        _isMobile = null;
        _isTablet = null;
        _isDesktop = null;
        _isTV = null;
    }
}
