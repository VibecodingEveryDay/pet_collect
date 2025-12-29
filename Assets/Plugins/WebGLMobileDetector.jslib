mergeInto(LibraryManager.library, {
    GetUserAgent: function () {
        var userAgent = window.navigator.userAgent;
        var bufferSize = lengthBytesUTF8(userAgent) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(userAgent, buffer, bufferSize);
        return buffer;
    },
    
    GetWindowWidth: function () {
        // Получить ширину окна браузера (viewport) - размер контейнера, в котором рендерится игра
        // Это может быть canvas Unity или iframe
        // Сначала проверяем canvas Unity, затем window.innerWidth
        var canvas = Module.canvas;
        if (canvas) {
            var rect = canvas.getBoundingClientRect();
            if (rect && rect.width > 0) {
                return rect.width;
            }
        }
        // Если canvas не найден или его размеры некорректны, используем window.innerWidth
        return window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
    },
    
    GetWindowHeight: function () {
        // Получить высоту окна браузера (viewport) - размер контейнера, в котором рендерится игра
        var canvas = Module.canvas;
        if (canvas) {
            var rect = canvas.getBoundingClientRect();
            if (rect && rect.height > 0) {
                return rect.height;
            }
        }
        // Если canvas не найден или его размеры некорректны, используем window.innerHeight
        return window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
    },
    
    GetRealScreenWidthJS: function () {
        // Получить реальную ширину экрана устройства (не viewport)
        // Это может быть полезно для определения реального устройства
        // На 4К телевизорах это вернет 3840
        return window.screen.width || window.innerWidth || document.documentElement.clientWidth;
    },
    
    GetRealScreenHeightJS: function () {
        // Получить реальную высоту экрана устройства (не viewport)
        // На 4К телевизорах это вернет 2160
        return window.screen.height || window.innerHeight || document.documentElement.clientHeight;
    }
});







