mergeInto(LibraryManager.library, {
    SetCursorToCanvasCenter: function () {
        // В браузере нельзя программно установить позицию курсора из-за ограничений безопасности
        // При выходе из Pointer Lock API браузер автоматически возвращает курсор в центр canvas
        // Это стандартное поведение браузера, мы просто обеспечиваем фокус на canvas
        var canvas = Module.canvas;
        if (!canvas) {
            return;
        }
        
        // Фокус на canvas помогает браузеру правильно обработать курсор
        if (canvas.focus) {
            canvas.focus();
        }
        
        // Примечание: в браузере курсор появится в центре canvas автоматически
        // при выходе из Pointer Lock (Cursor.lockState = CursorLockMode.None)
        // Это стандартное поведение Pointer Lock API в браузерах
    }
});

