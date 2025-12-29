using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if EnvirData_yg
using YG;
#endif

/// <summary>
/// Управление UI инвентаря питомцев с использованием UI Toolkit
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI Documents")]
    [SerializeField] private UIDocument mainUIDocument;
    [SerializeField] private VisualTreeAsset mainUIAsset;
    [SerializeField] private VisualTreeAsset inventoryModalAsset;
    [SerializeField] private VisualTreeAsset shopModalAsset;
    [SerializeField] private StyleSheet robloxStyleSheet;
    
    [Header("Настройки")]
    [SerializeField] private int petsPerPage = 5;
    [SerializeField] private int maxActivePets = 5;
    
    private VisualElement root;
    private VisualElement modalOverlay;
    private VisualElement shopModalOverlay;
    private VisualElement petsGrid;
    private VisualElement activePetsGrid;
    private Button prevPageButton;
    private Button nextPageButton;
    private Label pageInfoLabel;
    private Label coinAmountLabel;
    
    private int currentPage = 0;
    private List<PetData> allPets = new List<PetData>();
    private List<PetData> activePets = new List<PetData>();
    
    private Button backpackButton; // Ссылка на кнопку рюкзака
    private Coroutine backpackPulseCoroutine; // Корутина анимации пульсации кнопки рюкзака
    
    /// <summary>
    /// Проверить, открыто ли хотя бы одно модальное окно
    /// </summary>
    public static bool IsAnyModalOpen()
    {
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            return inventoryUI.modalOverlay != null || inventoryUI.shopModalOverlay != null;
        }
        return false;
    }
    
    private void Start()
    {
        InitializeUI();
        UpdateUI();
        ApplyMobileStyles();
    }
    
    /// <summary>
    /// Инициализация UI элементов
    /// </summary>
    private void InitializeUI()
    {
        if (mainUIDocument == null)
        {
            mainUIDocument = GetComponent<UIDocument>();
            if (mainUIDocument == null)
            {
                GameObject uiObject = new GameObject("InventoryUI");
                mainUIDocument = uiObject.AddComponent<UIDocument>();
            }
        }
        
        // Загрузить главный UI
        if (mainUIDocument.visualTreeAsset == null && mainUIAsset != null)
        {
            mainUIDocument.visualTreeAsset = mainUIAsset;
        }
        
        root = mainUIDocument.rootVisualElement;
        
        // Убедиться, что root занимает весь экран
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.style.right = 0;
        root.style.bottom = 0;
        root.style.width = new StyleLength(StyleKeyword.Auto);
        root.style.height = new StyleLength(StyleKeyword.Auto);
        
        // Применить стили
        if (robloxStyleSheet != null)
        {
            root.styleSheets.Add(robloxStyleSheet);
        }
        
        // Найти кнопку магазина
        Button shopButton = root.Q<Button>("shop-button");
        if (shopButton != null)
        {
            // Отключить фокус и обработку клавиатуры для кнопки
            shopButton.focusable = false;
            
            shopButton.clicked += () =>
            {
                // Анимация нажатия
                UIAnimations.AnimateBounce(shopButton, this);
                OpenShopModal();
            };
            
            // Отключить обработку Submit (Space) для кнопки
            shopButton.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Space)
                {
                    evt.StopPropagation();
                }
            });
            
            // Установить иконку магазина
            VisualElement shopIcon = shopButton.Q<VisualElement>("shop-icon");
            if (shopIcon != null)
            {
                LoadShopIcon(shopIcon);
            }
        }
        else
        {
            Debug.LogWarning("Кнопка магазина не найдена!");
        }
        
        // Найти кнопку рюкзака
        backpackButton = root.Q<Button>("backpack-button");
        if (backpackButton != null)
        {
            // Отключить фокус и обработку клавиатуры для кнопки
            backpackButton.focusable = false;
            
            backpackButton.clicked += () =>
            {
                // Анимация нажатия
                UIAnimations.AnimateBounce(backpackButton, this);
                OpenInventoryModal();
            };
            
            // Отключить обработку Submit (Space) для кнопки
            backpackButton.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Space)
                {
                    evt.StopPropagation();
                }
            });
            
            // Установить иконку рюкзака
            VisualElement backpackIcon = backpackButton.Q<VisualElement>("backpack-icon");
            if (backpackIcon != null)
            {
                LoadBackpackIcon(backpackIcon);
            }
        }
        
        // Инициализировать кнопку прыжка для мобильных устройств
        InitializeJumpButton();
        
        // Инициализировать счетчик монет
        InitializeCoinCounter();
        
        // Инициализировать панель подсказок
        InitializeHintPanel();
        
        // Запустить обновление подсказок
        StartCoroutine(UpdateHintPanelCoroutine());
        
        // Инициализировать анимацию кнопки рюкзака при старте
        StartCoroutine(InitializeBackpackAnimationDelayed());
    }
    
    /// <summary>
    /// Инициализировать анимацию кнопки рюкзака с небольшой задержкой
    /// </summary>
    private IEnumerator InitializeBackpackAnimationDelayed()
    {
        yield return new WaitForSeconds(0.5f); // Подождать, пока все компоненты инициализируются
        
        // Проверить количество активных питомцев и обновить анимацию
        int totalPets = PetInventory.Instance != null ? PetInventory.Instance.GetTotalPetCount() : 0;
        List<PetData> activePets = new List<PetData>();
        if (PetSpawner.Instance != null)
        {
            activePets = PetSpawner.Instance.GetActivePetsList();
        }
        int activePetsCount = activePets.Count;
        
        // Проверить, идет ли процесс вылупления яйца
        PetHatchingManager hatchingManager = FindObjectOfType<PetHatchingManager>();
        bool isHatching = hatchingManager != null && hatchingManager.IsHatching();
        
        // Проверить, было ли куплено яйцо
        bool eggPurchased = PlayerPrefs.GetInt("EggPurchased", 0) == 1;
        
        // Проверить, показывается ли подсказка "купите яйцо"
        bool showBuyEggHint = totalPets == 0 && !isHatching && !eggPurchased;
        
        UpdateBackpackButtonAnimation(activePetsCount, showBuyEggHint);
    }
    
    /// <summary>
    /// Инициализировать кнопку прыжка для мобильных устройств
    /// </summary>
    private void InitializeJumpButton()
    {
        Button jumpButton = root.Q<Button>("jump-button");
        if (jumpButton == null)
        {
            // Создать кнопку программно, если её нет в UXML
            jumpButton = new Button();
            jumpButton.name = "jump-button";
            root.Add(jumpButton);
        }
        
        // Проверить, является ли устройство мобильным (планшет или телефон)
        bool isMobile = PlatformDetector.IsMobile() || PlatformDetector.IsTablet();
        
        if (isMobile)
        {
            // Показать кнопку и настроить базовые стили
            jumpButton.style.display = DisplayStyle.Flex;
            jumpButton.style.visibility = Visibility.Visible;
            
            // Позиционирование: справа внизу
            jumpButton.style.position = Position.Absolute;
            jumpButton.style.right = 20f;
            jumpButton.style.bottom = 20f;
            
            // Размеры кнопки (базовые, будут переопределены в ApplyMobileStylesToMainUI)
            jumpButton.style.width = 80f;
            jumpButton.style.height = 80f;
            
            // Стили кнопки (похожи на другие кнопки)
            jumpButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f, 1f)); // Зеленый цвет
            jumpButton.style.color = Color.white;
            jumpButton.style.fontSize = 36f;
            jumpButton.text = "▲"; // Стрелка вверх для прыжка
            
            // Border radius (базовый, будет переопределен в ApplyMobileStylesToMainUI)
            float borderRadius = 40f; // Круглая кнопка (50% от 80px)
            jumpButton.style.borderTopLeftRadius = borderRadius;
            jumpButton.style.borderTopRightRadius = borderRadius;
            jumpButton.style.borderBottomLeftRadius = borderRadius;
            jumpButton.style.borderBottomRightRadius = borderRadius;
            
            // Граница (установить для каждой стороны отдельно)
            float borderWidth = 4f;
            Color borderColor = new Color(0.1f, 0.6f, 0.1f, 1f);
            jumpButton.style.borderTopWidth = borderWidth;
            jumpButton.style.borderBottomWidth = borderWidth;
            jumpButton.style.borderLeftWidth = borderWidth;
            jumpButton.style.borderRightWidth = borderWidth;
            jumpButton.style.borderTopColor = new StyleColor(borderColor);
            jumpButton.style.borderBottomColor = new StyleColor(borderColor);
            jumpButton.style.borderLeftColor = new StyleColor(borderColor);
            jumpButton.style.borderRightColor = new StyleColor(borderColor);
            
            // Отключить фокус
            jumpButton.focusable = false;
            
            // Инициализировать оригинальный размер для анимации
            Vector3 currentScaleValue = jumpButton.style.scale.value.value;
            if (currentScaleValue.x == 0f && currentScaleValue.y == 0f)
            {
                jumpButton.style.scale = new StyleScale(new Scale(Vector2.one));
            }
            UIAnimations.InitializeBounceAnimation(jumpButton);
            
            // Обработчик клика
            jumpButton.clicked += () =>
            {
                // Анимация нажатия
                UIAnimations.AnimateBounce(jumpButton, this);
                
                // Вызвать прыжок
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    playerController.Jump();
                }
            };
        }
        else
        {
            // Скрыть кнопку на десктопе
            jumpButton.style.display = DisplayStyle.None;
            jumpButton.style.visibility = Visibility.Hidden;
        }
    }
    
    /// <summary>
    /// Инициализировать счетчик монет
    /// </summary>
    private void InitializeCoinCounter()
    {
        VisualElement coinCounter = root.Q<VisualElement>("coin-counter");
        if (coinCounter != null)
        {
            // Найти иконку монеты
            VisualElement coinIcon = coinCounter.Q<VisualElement>("coin-icon");
            if (coinIcon != null)
            {
                LoadCoinIcon(coinIcon);
            }
            
            // Найти label для количества монет
            coinAmountLabel = coinCounter.Q<Label>("coin-amount");
            if (coinAmountLabel != null)
            {
                UpdateCoinDisplay();
            }
        }
        
        // Подписаться на изменения монет
        CoinManager.OnCoinsChanged += OnCoinsChanged;
        
        // Подписаться на изменение языка (локализация)
#if Localization_yg
        LocalizationManager.OnLanguageChangedEvent += OnLanguageChanged;
        // Применить текущий язык при инициализации
        OnLanguageChanged(LocalizationManager.GetCurrentLanguage());
#endif
    }
    
    /// <summary>
    /// Загрузить иконку магазина
    /// </summary>
    private void LoadShopIcon(VisualElement iconElement)
    {
        Texture2D shopTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        shopTexture = Resources.Load<Texture2D>("Icons/shop");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (shopTexture == null)
        {
            shopTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/shop.png");
        }
        #endif
        
        if (shopTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(shopTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку магазина! Проверьте путь к файлу.");
        }
    }
    
    /// <summary>
    /// Загрузить иконку монеты
    /// </summary>
    private void LoadCoinIcon(VisualElement iconElement)
    {
        Texture2D coinTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        coinTexture = Resources.Load<Texture2D>("Icons/crystal");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (coinTexture == null)
        {
        coinTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/crystal.png");
        }
        #endif
        
        if (coinTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(coinTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку монеты!");
        }
    }
    
    // Флаг для предотвращения множественных анимаций
    private bool isAnimatingCoins = false;
    
    /// <summary>
    /// Обработчик изменения количества монет
    /// </summary>
    private void OnCoinsChanged(int newAmount)
    {
        UpdateCoinDisplay();
        
        // Анимация изменения числа (только если не анимируется уже)
        if (coinAmountLabel != null && !isAnimatingCoins)
        {
            isAnimatingCoins = true;
            UIAnimations.AnimateNumberChange(coinAmountLabel, this);
            // Сбросить флаг через время анимации
            StartCoroutine(ResetCoinAnimationFlag());
        }
        
        // Обновить состояние всех кнопок магазина, если модальное окно открыто
        if (shopModalOverlay != null)
        {
            Button buyEggButton = shopModalOverlay.Q<Button>("buy-egg-button");
            UpdateBuyEggButtonState(buyEggButton);
            UpdateUpgradeCrystalButtonState(shopModalOverlay);
            UpdateMapUpgradeButtonState(shopModalOverlay);
        }
    }
    
    /// <summary>
    /// Сбросить флаг анимации монет
    /// </summary>
    private System.Collections.IEnumerator ResetCoinAnimationFlag()
    {
        yield return new WaitForSeconds(0.35f); // Время анимации + небольшой запас
        isAnimatingCoins = false;
    }
    
    /// <summary>
    /// Обновить отображение количества монет
    /// </summary>
    private void UpdateCoinDisplay()
    {
        if (coinAmountLabel != null)
        {
            int coins = CoinManager.GetCoins();
            coinAmountLabel.text = coins.ToString();
        }
    }
    
    /// <summary>
    /// Открыть модальное окно инвентаря
    /// </summary>
    private void OpenInventoryModal()
    {
        if (inventoryModalAsset == null)
        {
            Debug.LogError("InventoryModal asset не найден!");
            return;
        }
        
        // Создать модальное окно
        modalOverlay = inventoryModalAsset.Instantiate();
        
        // modalOverlay сам является overlay элементом (корневой элемент из UXML)
        VisualElement overlay = modalOverlay;
        
        // Установить правильное позиционирование через код
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.top = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.width = Length.Percent(100);
        overlay.style.height = Length.Percent(100);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        
        root.Add(modalOverlay);
        
        // Добавить обработчик клавиатуры для закрытия модального окна
        overlay.RegisterCallback<KeyDownEvent>(OnKeyDown);
        // Также добавить на root для гарантии обработки
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        
        // Установить фокус на overlay для получения событий клавиатуры
        overlay.Focus();
        
        // Найти элементы UI внутри overlay
        VisualElement modalContainer = overlay.Q<VisualElement>("modal-container");
        petsGrid = overlay.Q<VisualElement>("pets-grid");
        activePetsGrid = overlay.Q<VisualElement>("active-pets-grid");
        prevPageButton = overlay.Q<Button>("prev-page-button");
        nextPageButton = overlay.Q<Button>("next-page-button");
        pageInfoLabel = overlay.Q<Label>("page-info");
        
        // Убедиться, что контейнер правильно центрируется
        if (modalContainer != null)
        {
            modalContainer.style.alignSelf = Align.Center;
            modalContainer.style.marginTop = Length.Auto();
            modalContainer.style.marginBottom = Length.Auto();
            
            // Установить max-height относительно высоты экрана
            float rootScreenHeight = root.resolvedStyle.height;
            if (rootScreenHeight > 0)
            {
                modalContainer.style.maxHeight = rootScreenHeight * 0.9f;
            }
            
            // Уменьшить ширину модального окна на мобильных устройствах
            if (PlatformDetector.IsMobile())
            {
                // Определить, телефон ли это (упрощенная логика)
                bool isPhone = IsPhoneDevice();
                
                // Для телефонов уменьшаем еще больше
                if (isPhone)
                {
                    modalContainer.style.maxWidth = Length.Percent(85); // Уже для телефонов
                }
                else
                {
                    modalContainer.style.maxWidth = Length.Percent(81); // Обычное уменьшение для планшетов
                }
                
                // Увеличить max-height для телефонов, чтобы больше контента уместилось
                if (isPhone)
                {
                    float screenHeightValueForModal = root.resolvedStyle.height;
                    if (screenHeightValueForModal > 0)
                    {
                        modalContainer.style.maxHeight = screenHeightValueForModal * 0.92f; // Больше высоты для телефонов
                    }
                }
            }
            
            // Применить мобильные стили
            ApplyMobileStylesToInventoryModal(modalContainer);
            
            // Анимация появления модального окна
            UIAnimations.AnimateModalAppear(modalContainer, this);
        }
        
        // Подписаться на события
        if (prevPageButton != null)
        {
            // Убедиться, что текст стрелки установлен
            if (string.IsNullOrEmpty(prevPageButton.text))
            {
                prevPageButton.text = "<";
            }
            prevPageButton.clicked += () =>
            {
                UIAnimations.AnimateBounce(prevPageButton, this);
                ChangePage(-1);
            };
        }
        if (nextPageButton != null)
        {
            // Убедиться, что текст стрелки установлен
            if (string.IsNullOrEmpty(nextPageButton.text))
            {
                nextPageButton.text = ">";
            }
            nextPageButton.clicked += () =>
            {
                UIAnimations.AnimateBounce(nextPageButton, this);
                ChangePage(1);
            };
        }
        
        // Закрытие при клике на overlay (но не на контейнер)
        if (overlay != null)
        {
            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                // Проверяем, что клик был именно на overlay, а не на modal-container
                VisualElement clickedElement = evt.target as VisualElement;
                
                // Проверяем, является ли кликнутый элемент или его родитель modal-container
                bool clickedOnContainer = false;
                VisualElement current = clickedElement;
                
                while (current != null && current != overlay)
                {
                    if (current.name == "modal-container" || current == modalContainer)
                    {
                        clickedOnContainer = true;
                        break;
                    }
                    current = current.parent;
                }
                
                // Если клик был не на контейнере, закрываем модальное окно
                if (!clickedOnContainer)
                {
                    CloseInventoryModal();
                }
            });
        }
        
        // Предотвратить закрытие при клике на контейнер и его дочерних элементах
        if (modalContainer != null)
        {
            modalContainer.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
            });
        }
        
        // Загрузить данные
        LoadPetsFromInventory();
        UpdateModalUI();
    }
    
    /// <summary>
    /// Обработчик нажатия клавиши
    /// </summary>
    private void OnKeyDown(KeyDownEvent evt)
    {
        // Игнорировать Space - он используется для прыжка
        if (evt.keyCode == KeyCode.Space)
        {
            return;
        }
        
        // Закрыть модальное окно при нажатии любой клавиши (кроме Space)
        if (modalOverlay != null)
        {
            CloseInventoryModal();
            evt.StopPropagation();
        }
    }
    
    /// <summary>
    /// Закрыть модальное окно
    /// </summary>
    private void CloseInventoryModal()
    {
        if (modalOverlay != null)
        {
            // modalOverlay сам является overlay элементом
            VisualElement overlay = modalOverlay;
            VisualElement modalContainer = modalOverlay.Q<VisualElement>("modal-container");
            
            // Убрать обработчики клавиатуры
            overlay.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            
            // Сразу скрыть затемнение (overlay)
            overlay.style.opacity = 0f;
            
            if (modalContainer != null)
            {
                // Анимация исчезновения только контейнера
                UIAnimations.AnimateModalDisappear(modalContainer, this, () =>
                {
                    if (modalOverlay != null)
                    {
                        modalOverlay.RemoveFromHierarchy();
                        modalOverlay = null;
                    }
                });
            }
            else
            {
                // Если контейнер не найден, сразу удаляем overlay
                if (modalOverlay != null)
                {
                    modalOverlay.RemoveFromHierarchy();
                    modalOverlay = null;
                }
            }
        }
    }
    
    /// <summary>
    /// Загрузить питомцев из инвентаря
    /// </summary>
    private void LoadPetsFromInventory()
    {
        if (PetInventory.Instance != null)
        {
            allPets = PetInventory.Instance.GetAllPets();
        }
        else
        {
            allPets = new List<PetData>();
        }
        
        // Загрузить активных питомцев из PetSpawner (те, что уже заспавнены в мире)
        activePets = new List<PetData>();
        if (PetSpawner.Instance != null)
        {
            activePets = PetSpawner.Instance.GetActivePetsList();
        }
    }
    
    /// <summary>
    /// Обновить UI модального окна
    /// </summary>
    private void UpdateModalUI()
    {
        if (petsGrid == null || activePetsGrid == null)
            return;
        
        // Очистить сетки
        petsGrid.Clear();
        activePetsGrid.Clear();
        
        // Отобразить питомцев текущей страницы
        int startIndex = currentPage * petsPerPage;
        int endIndex = Mathf.Min(startIndex + petsPerPage, allPets.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            PetData pet = allPets[i];
            // Проверить, является ли питомец активным
            bool isPetActive = activePets.Contains(pet);
            VisualElement petSlot = CreatePetSlot(pet, false, isPetActive);
            petsGrid.Add(petSlot);
        }
        
        // Заполнить пустые слоты на странице
        int slotsOnPage = endIndex - startIndex;
        for (int i = slotsOnPage; i < petsPerPage; i++)
        {
            VisualElement emptySlot = CreateEmptySlot();
            petsGrid.Add(emptySlot);
        }
        
        // Отобразить активных питомцев
        for (int i = 0; i < maxActivePets; i++)
        {
            if (i < activePets.Count)
            {
                VisualElement petSlot = CreatePetSlot(activePets[i], true);
                activePetsGrid.Add(petSlot);
            }
            else
            {
                VisualElement emptySlot = CreateEmptySlot(true);
                activePetsGrid.Add(emptySlot);
            }
        }
        
        // Обновить пагинацию
        UpdatePagination();
        
        // Применить стили после создания всех ячеек (с задержкой, чтобы перезаписать CSS)
        // Применяем для всех устройств (мобильных и десктопа)
        if (modalOverlay != null)
        {
            StartCoroutine(ApplyMobileStylesDelayed());
            // Применить выравнивание пагинации для всех устройств
            StartCoroutine(ApplyPaginationAlignmentDelayed());
        }
    }
    
    /// <summary>
    /// Применить мобильные стили с задержкой (чтобы перезаписать CSS)
    /// </summary>
    private IEnumerator ApplyMobileStylesDelayed()
    {
        // Подождать несколько кадров, чтобы CSS стили применились, затем перезаписать их
        yield return null;
        yield return null;
        yield return null;
        
        if (modalOverlay == null || petsGrid == null || activePetsGrid == null)
            yield break;
            
            VisualElement overlay = modalOverlay;
            VisualElement modalContainer = overlay.Q<VisualElement>("modal-container");
            if (modalContainer != null)
            {
                // Применить стили к контейнеру (для мобильных и десктопа)
                bool isMobile = PlatformDetector.IsMobile();
                bool isTablet = PlatformDetector.IsTablet();
                bool isDesktop = !isMobile && !isTablet;
                
                if (isMobile)
                {
                    ApplyMobileStylesToInventoryModal(modalContainer);
                }
                else if (isDesktop)
                {
                    // Для десктопа применить стили к заголовкам
                    ApplyDesktopSectionTitleStyles(modalContainer);
                }
                
                // Определить isPhone (только для мобильных)
                // Для планшетов isPhone = false, но планшеты обрабатываются отдельно в ApplyMobileStylesToPetSlot
                bool isPhone = isMobile ? IsPhoneDevice() : false;
                
                Debug.Log($"[InventoryUI] ApplyMobileStylesDelayed - isMobile: {isMobile}, isTablet: {isTablet}, isPhone: {isPhone}");
            Debug.Log($"[InventoryUI] ApplyMobileStylesDelayed - Applying mobile styles, isMobile: {PlatformDetector.IsMobile()}, isPhone: {isPhone}");
            
            // Применить стили ко всем уже созданным ячейкам инвентаря (ВКЛЮЧАЯ empty!)
            var allSlots = petsGrid.Query<VisualElement>(className: "pet-slot").ToList();
            Debug.Log($"[InventoryUI] Found {allSlots.Count} pet slots in inventory grid");
            int appliedCount = 0;
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    bool isEmpty = slot.ClassListContains("empty");
                    bool isActive = slot.ClassListContains("active");
                    float oldWidth = slot.resolvedStyle.width;
                    float oldHeight = slot.resolvedStyle.height;
                    Debug.Log($"[InventoryUI] Processing inventory slot: empty={isEmpty}, active={isActive}, size={oldWidth}x{oldHeight}");
                    // Применяем стили ко всем ячейкам, включая empty
                    // Для десктопа isPhone будет false
                    bool isPhoneForSlot = PlatformDetector.IsMobile() ? isPhone : false;
                    ApplyMobileStylesToPetSlot(slot, isActive, isPhoneForSlot);
                    // Проверить, применились ли стили
                    yield return null; // Подождать кадр
                    float newWidth = slot.resolvedStyle.width;
                    float newHeight = slot.resolvedStyle.height;
                    Debug.Log($"[InventoryUI] Slot inventory - Old: {oldWidth}x{oldHeight}, New: {newWidth}x{newHeight}, isActive: {isActive}, empty: {isEmpty}");
                    appliedCount++;
                }
            }
            Debug.Log($"[InventoryUI] Applied styles to {appliedCount} inventory slots");
            
            // Применить стили ко всем активным ячейкам (ВКЛЮЧАЯ empty!)
            var activeSlots = activePetsGrid.Query<VisualElement>(className: "pet-slot").ToList();
            Debug.Log($"[InventoryUI] Found {activeSlots.Count} pet slots in active pets grid");
            int appliedActiveCount = 0;
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    bool isEmpty = slot.ClassListContains("empty");
                    float oldWidth = slot.resolvedStyle.width;
                    float oldHeight = slot.resolvedStyle.height;
                    Debug.Log($"[InventoryUI] Processing active slot: empty={isEmpty}, size={oldWidth}x{oldHeight}");
                    // Применяем стили ко всем ячейкам, включая empty
                    // Для десктопа isPhone будет false
                    bool isPhoneForSlot = PlatformDetector.IsMobile() ? isPhone : false;
                    ApplyMobileStylesToPetSlot(slot, true, isPhoneForSlot);
                    // Проверить, применились ли стили
                    yield return null; // Подождать кадр
                    float newWidth = slot.resolvedStyle.width;
                    float newHeight = slot.resolvedStyle.height;
                    Debug.Log($"[InventoryUI] Slot active - Old: {oldWidth}x{oldHeight}, New: {newWidth}x{newHeight}, empty: {isEmpty}");
                    appliedActiveCount++;
                }
            }
            Debug.Log($"[InventoryUI] Applied styles to {appliedActiveCount} active slots");
            
            // Применить стили еще раз через несколько кадров для гарантии
            yield return null;
            yield return null;
            
            // Повторно применить стили ко всем ячейкам (ВКЛЮЧАЯ empty!) через несколько кадров
            // чтобы убедиться, что CSS уже загружен и стили применяются после него
            yield return null;
            yield return null;
            
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    bool isActive = slot.ClassListContains("active");
                    ApplyMobileStylesToPetSlot(slot, isActive, isPhone);
                    slot.MarkDirtyRepaint();
                }
            }
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    ApplyMobileStylesToPetSlot(slot, true, isPhone);
                    slot.MarkDirtyRepaint();
                }
            }
        }
    }
    
    /// <summary>
    /// Применить выравнивание пагинации с задержкой (чтобы перезаписать CSS)
    /// </summary>
    private IEnumerator ApplyPaginationAlignmentDelayed()
    {
        // Подождать несколько кадров, чтобы CSS стили применились
        yield return null;
        yield return null;
        yield return null;
        
        if (modalOverlay == null)
            yield break;
            
        VisualElement overlay = modalOverlay;
        VisualElement modalContainer = overlay.Q<VisualElement>("modal-container");
        if (modalContainer == null)
            yield break;
        
        VisualElement paginationControls = modalContainer.Q<VisualElement>("pagination-controls");
        if (paginationControls != null)
        {
            // Принудительно установить выравнивание по центру для всех элементов
            paginationControls.style.alignItems = Align.Center;
            paginationControls.style.justifyContent = Justify.Center;
            
            // Применить выравнивание ко всем дочерним элементам
            var prevButton = paginationControls.Q<Button>("prev-page-button");
            var nextButton = paginationControls.Q<Button>("next-page-button");
            var pageInfo = paginationControls.Q<Label>("page-info");
            
            if (prevButton != null)
            {
                prevButton.style.alignSelf = Align.Center;
            }
            if (nextButton != null)
            {
                nextButton.style.alignSelf = Align.Center;
            }
            if (pageInfo != null)
            {
                pageInfo.style.alignSelf = Align.Center;
                pageInfo.style.marginTop = new StyleLength(0f);
                pageInfo.style.marginBottom = new StyleLength(0f);
                pageInfo.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            
            Debug.Log("[InventoryUI] Applied pagination alignment");
        }
    }
    
    /// <summary>
    /// Определить, является ли устройство extra small (очень маленький экран, например iPhone SE)
    /// </summary>
    private bool IsExtraSmallDevice()
    {
        if (!PlatformDetector.IsMobile())
        {
            return false;
        }
        
        // Используем те же методы, что и PlatformDetector для консистентности
        float screenWidth = PlatformDetector.GetScreenWidth();
        float screenHeight = PlatformDetector.GetScreenHeight();
        float aspectRatio = PlatformDetector.GetAspectRatio();
        
        // Extra small устройства: aspectRatio ~1.78 и разрешение меньше 1400px по ширине
        // iPhone SE: 1334x750 = aspectRatio 1.78
        bool isExtraSmall = aspectRatio >= 1.7f && aspectRatio <= 1.85f && screenWidth < 1400;
        
        Debug.Log($"[InventoryUI] IsExtraSmallDevice - Screen: {screenWidth}x{screenHeight}, AspectRatio: {aspectRatio:F2}, Result: {isExtraSmall}");
        
        return isExtraSmall;
    }
    
    /// <summary>
    /// Определить, является ли устройство телефоном (не планшетом)
    /// Использует YG2 SDK, если доступен, иначе fallback на текущую логику
    /// </summary>
    private bool IsPhoneDevice()
    {
        if (!PlatformDetector.IsMobile())
        {
            return false;
        }
        
        #if EnvirData_yg
        // Используем YG2 SDK для определения типа устройства
        if (YG2.envir != null)
        {
            // Если это мобильное устройство, но не планшет - это телефон
            bool isPhone = YG2.envir.isMobile && !YG2.envir.isTablet;
            Debug.Log($"[InventoryUI] IsPhoneDevice - {isPhone} (YG2 SDK: isMobile={YG2.envir.isMobile}, isTablet={YG2.envir.isTablet})");
            return isPhone;
        }
        #endif
        
        // Если YG2 SDK не доступен, возвращаем false
        Debug.LogWarning("[InventoryUI] YG2.envir не доступен для определения IsPhoneDevice!");
        return false;
    }
    
    /// <summary>
    /// Создать ячейку питомца
    /// </summary>
    private VisualElement CreatePetSlot(PetData pet, bool isActive, bool isPetActiveInInventory = false)
    {
        VisualElement slot = new VisualElement();
        slot.AddToClassList("pet-slot");
        if (isActive)
        {
            slot.AddToClassList("active");
        }
        // Выделить питомца, если он активен (даже если он в инвентаре)
        if (isPetActiveInInventory)
        {
            slot.AddToClassList("active-pet");
        }
        
        if (isActive)
        {
            // Для активных питомцев: горизонтальная структура с двумя блоками
            // Блок 1: Аватарка с эмоджи
            VisualElement avatarBlock = new VisualElement();
            avatarBlock.AddToClassList("pet-avatar-block");
            
            VisualElement petIcon = new VisualElement();
            petIcon.AddToClassList("pet-emoji");
            LoadPetIcon(petIcon);
            avatarBlock.Add(petIcon);
            
            slot.Add(avatarBlock);
            
            // Блок 2: Информация (название и редкость)
            VisualElement infoBlock = new VisualElement();
            infoBlock.AddToClassList("pet-info-block");
            
            Label nameLabel = new Label(pet.petName);
            nameLabel.AddToClassList("pet-name");
            infoBlock.Add(nameLabel);
            
            VisualElement rarityBadge = new VisualElement();
            rarityBadge.AddToClassList("pet-rarity-badge");
            rarityBadge.AddToClassList(pet.rarity.ToString().ToLower());
            
            Label rarityLabel = new Label(GetRarityShortName(pet.rarity));
            rarityLabel.AddToClassList("pet-rarity-text");
            rarityBadge.Add(rarityLabel);
            infoBlock.Add(rarityBadge);
            
            slot.Add(infoBlock);
        }
        else
        {
            // Для инвентаря: проверить, мобильное ли устройство
            bool isMobile = PlatformDetector.IsMobile();
            
            if (isMobile)
            {
                // Для мобильных устройств: только emoji с фоном цвета редкости
                // Установить фон ячейки как круг цвета редкости
                Color rarityColor = PetHatchingManager.GetRarityColor(pet.rarity);
                
                // Создать контейнер для emoji (будет круглым)
                VisualElement emojiContainer = new VisualElement();
                emojiContainer.AddToClassList("pet-emoji-mobile");
                
                // Установить круглый фон цвета редкости
                emojiContainer.style.backgroundColor = new StyleColor(rarityColor);
                // Круглая форма через отдельные свойства для каждого угла
                float borderRadiusValue = 50f; // 50% от размера для круга
                emojiContainer.style.borderTopLeftRadius = new StyleLength(Length.Percent(borderRadiusValue));
                emojiContainer.style.borderTopRightRadius = new StyleLength(Length.Percent(borderRadiusValue));
                emojiContainer.style.borderBottomLeftRadius = new StyleLength(Length.Percent(borderRadiusValue));
                emojiContainer.style.borderBottomRightRadius = new StyleLength(Length.Percent(borderRadiusValue));
                // Размеры уменьшены еще на 15% (60% * 0.85 = 51%)
                emojiContainer.style.width = Length.Percent(51); 
                emojiContainer.style.height = Length.Percent(51);
                // Выравнивание по центру (горизонтально и вертикально)
                emojiContainer.style.alignSelf = Align.Center;
                emojiContainer.style.justifyContent = Justify.Center;
                emojiContainer.style.alignItems = Align.Center;
                emojiContainer.style.position = Position.Relative;
                emojiContainer.style.marginTop = Length.Auto();
                emojiContainer.style.marginBottom = Length.Auto();
                emojiContainer.style.marginLeft = Length.Auto();
                emojiContainer.style.marginRight = Length.Auto();
                
                // Добавить иконку питомца внутрь контейнера
                VisualElement petIcon = new VisualElement();
                petIcon.AddToClassList("pet-emoji");
                LoadPetIcon(petIcon);
                // Иконка размер уменьшен еще на 15% (52.5% * 0.85 = 44.625%), затем уменьшена на 10%
                float iconSizePercent = 44.625f * 0.9f;
                petIcon.style.width = Length.Percent(iconSizePercent); 
                petIcon.style.height = Length.Percent(iconSizePercent);
                petIcon.style.minWidth = Length.Percent(iconSizePercent);
                petIcon.style.minHeight = Length.Percent(iconSizePercent);
                petIcon.style.maxWidth = Length.Percent(iconSizePercent);
                petIcon.style.maxHeight = Length.Percent(iconSizePercent);
                
                // Выравнивание иконки по центру контейнера (горизонтально и вертикально)
                petIcon.style.alignSelf = Align.Center;
                petIcon.style.marginLeft = new StyleLength(0f);
                petIcon.style.marginRight = new StyleLength(0f);
                petIcon.style.marginTop = new StyleLength(0f);
                petIcon.style.marginBottom = new StyleLength(0f);
                petIcon.style.paddingLeft = new StyleLength(0f);
                petIcon.style.paddingRight = new StyleLength(0f);
                petIcon.style.paddingTop = new StyleLength(0f);
                petIcon.style.paddingBottom = new StyleLength(0f);
                petIcon.style.justifyContent = Justify.Center;
                petIcon.style.alignItems = Align.Center;
                petIcon.style.position = Position.Relative;
                emojiContainer.Add(petIcon);
                
                slot.Add(emojiContainer);
                
                // Пометить ячейку как мобильную для стилизации
                slot.AddToClassList("pet-slot-mobile");
            }
            else
            {
                // Для десктопа: стандартная вертикальная структура
            VisualElement petIcon = new VisualElement();
            petIcon.AddToClassList("pet-emoji");
            LoadPetIcon(petIcon);
            slot.Add(petIcon);
            
            Label nameLabel = new Label(pet.petName);
            nameLabel.AddToClassList("pet-name");
            slot.Add(nameLabel);
            
            VisualElement rarityBadge = new VisualElement();
            rarityBadge.AddToClassList("pet-rarity-badge");
            rarityBadge.AddToClassList(pet.rarity.ToString().ToLower());
            
            Label rarityLabel = new Label(GetRarityShortName(pet.rarity));
            rarityLabel.AddToClassList("pet-rarity-text");
            rarityBadge.Add(rarityLabel);
            slot.Add(rarityBadge);
            }
        }
        
        // Сохранить данные питомца для поиска при клике
        slot.userData = pet;
        
        // Сохранить данные питомца для поиска
        slot.userData = pet;
        
        // Обработчик клика
        slot.RegisterCallback<ClickEvent>(evt => 
        {
            // Анимация клика
            VisualElement clickedElement = evt.target as VisualElement;
            VisualElement slotElement = clickedElement;
            while (slotElement != null && !slotElement.ClassListContains("pet-slot"))
            {
                slotElement = slotElement.parent;
            }
            if (slotElement != null)
            {
                UIAnimations.AnimateBounce(slotElement, this);
            }
            
            OnPetSlotClicked(pet, isActive);
        });
        
        // Применить стили к ячейке питомца (после создания всех элементов)
        // Применяем для всех устройств (мобильных и десктопа)
        bool isPhone = PlatformDetector.IsMobile() ? IsPhoneDevice() : false;
        ApplyMobileStylesToPetSlot(slot, isActive, isPhone);
        
        return slot;
    }
    
    /// <summary>
    /// Создать пустую ячейку
    /// </summary>
    private VisualElement CreateEmptySlot(bool isActive = false)
    {
        VisualElement slot = new VisualElement();
        slot.AddToClassList("pet-slot");
        slot.AddToClassList("empty");
        
        if (isActive)
        {
            // Для активных питомцев: горизонтальная структура с двумя блоками
            // Блок 1: Аватарка (пустая)
            VisualElement avatarBlock = new VisualElement();
            avatarBlock.AddToClassList("pet-avatar-block");
            slot.Add(avatarBlock);
            
            // Блок 2: Информация с текстом "пусто"
            VisualElement infoBlock = new VisualElement();
            infoBlock.AddToClassList("pet-info-block");
            
            Label emptyLabel = new Label("пусто");
            emptyLabel.AddToClassList("pet-name");
            infoBlock.Add(emptyLabel);
            
            slot.Add(infoBlock);
        }
        
        return slot;
    }
    
    /// <summary>
    /// Обработчик клика на ячейку питомца
    /// </summary>
    private void OnPetSlotClicked(PetData pet, bool isActive)
    {
        if (isActive)
        {
            // Убрать из активных
            activePets.Remove(pet);
            
            // Удалить питомца из мира
            if (PetSpawner.Instance != null)
            {
                PetSpawner.Instance.DespawnPet(pet);
            }
            
            UpdateModalUI();
        }
        else
        {
            // Проверить, что PetSpawner доступен
            if (PetSpawner.Instance == null)
            {
                Debug.LogError("PetSpawner.Instance равен null! Невозможно заспавнить питомца.");
                return;
            }
            
            // Проверить, не заспавнен ли уже этот питомец
            bool alreadySpawned = PetSpawner.Instance.IsPetSpawned(pet);
            
            // Если уже заспавнен, просто добавить в список активных (если его там еще нет)
            if (alreadySpawned)
            {
                if (!activePets.Contains(pet))
                {
                    activePets.Add(pet);
                    
                    // Показать эмоцию при появлении в активных
                    if (pet.worldInstance != null)
                    {
                        PetEmotionUI emotionUI = pet.worldInstance.GetComponent<PetEmotionUI>();
                        if (emotionUI != null)
                        {
                            emotionUI.ShowActiveEmotion();
                        }
                    }
                }
                UpdateModalUI();
                return;
            }
            
            // Добавить в активные (если есть место)
            int addedIndex = -1;
            if (activePets.Count < maxActivePets)
            {
                if (!activePets.Contains(pet))
            {
                activePets.Add(pet);
                addedIndex = activePets.Count - 1;
                }
            }
            else
            {
                // Заменить последнего - удалить его из мира
                PetData removedPet = activePets[activePets.Count - 1];
                    PetSpawner.Instance.DespawnPet(removedPet);
                
                activePets.RemoveAt(activePets.Count - 1);
                if (!activePets.Contains(pet))
                {
                activePets.Add(pet);
                addedIndex = activePets.Count - 1;
                }
            }
            
            // Заспавнить питомца в мире
                PetSpawner.Instance.SpawnPetInWorld(pet);
            
            // Показать эмоцию при появлении в активных (после небольшой задержки, чтобы питомец успел заспавниться)
            StartCoroutine(ShowActiveEmotionDelayed(pet));
            
            // Проверить, что питомец действительно заспавнился
            if (PetSpawner.Instance.IsPetSpawned(pet))
            {
                Debug.Log($"Питомец {pet.petName} успешно заспавнен в мире");
            }
            else
            {
                Debug.LogError($"Питомец {pet.petName} не был заспавнен! Проверьте логи выше.");
            }
            
            // Сохранить ссылку на питомца для анимации
            PetData petToAnimate = pet;
            
            UpdateModalUI();
            
            // Анимация заполнения активной ячейки
            if (addedIndex >= 0 && activePetsGrid != null)
            {
                // Используем schedule для выполнения после обновления UI
                // Сначала устанавливаем начальное состояние, потом запускаем анимацию
                activePetsGrid.schedule.Execute(() =>
                {
                    // Найти ячейку по питомцу (userData содержит PetData)
                    VisualElement filledSlot = null;
                    
                    // Попробовать найти по индексу сначала
                    if (addedIndex < activePetsGrid.childCount)
                    {
                        VisualElement slot = activePetsGrid[addedIndex];
                        if (slot != null && !slot.ClassListContains("empty"))
                        {
                            filledSlot = slot;
                        }
                    }
                    
                    // Если не найден по индексу, искать по питомцу
                    if (filledSlot == null)
                    {
                        foreach (VisualElement child in activePetsGrid.Children())
                        {
                            if (child.userData == petToAnimate && !child.ClassListContains("empty"))
                            {
                                filledSlot = child;
                                break;
                            }
                        }
                    }
                    
                    if (filledSlot != null && !filledSlot.ClassListContains("empty"))
                    {
                        // Сразу устанавливаем начальное состояние анимации, чтобы избежать мерцания
                        filledSlot.style.scale = new Scale(new Vector2(0.5f, 0.5f));
                        filledSlot.style.opacity = 0f;
                        
                        // Небольшая задержка перед запуском анимации
                        activePetsGrid.schedule.Execute(() =>
                        {
                            UIAnimations.AnimateSlotFill(filledSlot, this);
                        }).ExecuteLater(10);
                    }
                }).ExecuteLater(0); // Нулевая задержка для немедленного выполнения
            }
        }
    }
    
    /// <summary>
    /// Изменить страницу
    /// </summary>
    private void ChangePage(int direction)
    {
        int totalPages = Mathf.CeilToInt((float)allPets.Count / petsPerPage);
        if (totalPages == 0) totalPages = 1;
        
        currentPage += direction;
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        
        UpdateModalUI();
    }
    
    /// <summary>
    /// Обновить информацию о пагинации
    /// </summary>
    private void UpdatePagination()
    {
        if (pageInfoLabel == null || prevPageButton == null || nextPageButton == null)
            return;
        
        int totalPages = Mathf.CeilToInt((float)allPets.Count / petsPerPage);
        if (totalPages == 0) totalPages = 1;
        
        pageInfoLabel.text = LocalizationManager.GetPaginationText(currentPage, totalPages);
        
        prevPageButton.SetEnabled(currentPage > 0);
        nextPageButton.SetEnabled(currentPage < totalPages - 1);
    }
    
    /// <summary>
    /// Получить эмоджи для питомца по редкости
    /// </summary>
    private string GetPetEmoji(PetRarity rarity)
    {
        switch (rarity)
        {
            case PetRarity.Common:
                return "🐱"; // Кот
            case PetRarity.Epic:
                return "🐉"; // Дракон
            case PetRarity.Legendary:
                return "🦄"; // Единорог
            default:
                return "🐾"; // Лапка
        }
    }
    
    /// <summary>
    /// Получить короткое название редкости
    /// </summary>
    private string GetRarityShortName(PetRarity rarity)
    {
        return LocalizationManager.GetRarityShortName(rarity);
    }
    
    /// <summary>
    /// Загрузить моковых питомцев для теста
    /// </summary>
    
    /// <summary>
    /// Загрузить иконку рюкзака
    /// </summary>
    private void LoadBackpackIcon(VisualElement iconElement)
    {
        Texture2D backpackTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        backpackTexture = Resources.Load<Texture2D>("Icons/backpack");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (backpackTexture == null)
        {
        backpackTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/backpack.png");
        }
        #endif
        
        if (backpackTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(backpackTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку рюкзака!");
        }
    }
    
    /// <summary>
    /// Загрузить иконку яйца
    /// </summary>
    private void LoadEggIcon(VisualElement iconElement)
    {
        Texture2D eggTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        eggTexture = Resources.Load<Texture2D>("Icons/egg");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (eggTexture == null)
        {
            eggTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Resources/Icons/egg.png");
        }
        #endif
        
        if (eggTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(eggTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку яйца! Проверьте путь к файлу.");
        }
    }
    
    /// <summary>
    /// Загрузить иконку карты
    /// </summary>
    private void LoadMapIcon(VisualElement iconElement)
    {
        Texture2D mapTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        mapTexture = Resources.Load<Texture2D>("Icons/map");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (mapTexture == null)
        {
            mapTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Resources/Icons/map.png");
        }
        #endif
        
        if (mapTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(mapTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку карты! Проверьте путь к файлу.");
        }
    }
    
    /// <summary>
    /// Загрузить иконку питомца
    /// </summary>
    private void LoadPetIcon(VisualElement iconElement)
    {
        Texture2D petTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        petTexture = Resources.Load<Texture2D>("Icons/pet");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (petTexture == null)
        {
            petTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Resources/Icons/pet.png");
        }
        #endif
        
        if (petTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(petTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку питомца! Проверьте путь к файлу.");
        }
    }
    
    /// <summary>
    /// Инициализировать панель подсказок
    /// </summary>
    private void InitializeHintPanel()
    {
        VisualElement hintPanel = root.Q<VisualElement>("hint-panel");
        if (hintPanel != null)
        {
            // Настроить базовые стили
            hintPanel.style.position = Position.Absolute;
            hintPanel.style.top = 20f;
            // Центрирование: left 50% и отрицательный margin-left (будет установлен после вычисления ширины)
            hintPanel.style.left = new StyleLength(Length.Percent(50));
            hintPanel.style.right = StyleKeyword.Auto;
            hintPanel.style.width = new StyleLength(StyleKeyword.Auto);
            hintPanel.style.height = 80f; // Такая же высота как у кнопок
            hintPanel.style.flexDirection = FlexDirection.Row;
            hintPanel.style.alignItems = Align.Center;
            hintPanel.style.justifyContent = Justify.Center;
            hintPanel.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.7f));
            hintPanel.style.borderTopLeftRadius = 18f;
            hintPanel.style.borderTopRightRadius = 18f;
            hintPanel.style.borderBottomLeftRadius = 18f;
            hintPanel.style.borderBottomRightRadius = 18f;
            hintPanel.style.paddingLeft = 10f;
            hintPanel.style.paddingRight = 10f;
            hintPanel.style.paddingTop = 6f;
            hintPanel.style.paddingBottom = 6f;
            // z-index задается через CSS, порядок в иерархии или через BringToFront()
            
            // Настроить текст подсказки
            Label hintText = hintPanel.Q<Label>("hint-text");
            if (hintText != null)
            {
                hintText.style.color = Color.white;
                hintText.style.fontSize = 14f;
                hintText.style.marginRight = 6f;
            }
            
            // Настроить контейнер для иконок
            VisualElement iconsContainer = hintPanel.Q<VisualElement>("hint-icons-container");
            if (iconsContainer != null)
            {
                iconsContainer.style.flexDirection = FlexDirection.Row;
                iconsContainer.style.alignItems = Align.Center;
                // gap задается через CSS, используем margin между элементами через дочерние стили
            }
        }
        
        // Применить адаптивные стили
        ApplyMobileStylesToHintPanel();
    }
    
    /// <summary>
    /// Корутина для обновления панели подсказок
    /// </summary>
    private IEnumerator UpdateHintPanelCoroutine()
    {
        while (true)
        {
            UpdateHintPanel();
            yield return new WaitForSeconds(0.5f); // Обновлять каждые 0.5 секунды
        }
    }
    
    /// <summary>
    /// Обновить подсказки после покупки яйца
    /// </summary>
    private IEnumerator UpdateHintPanelAfterPurchase()
    {
        yield return null; // Подождать один кадр, чтобы модальное окно успело закрыться
        UpdateHintPanel(); // Обновить подсказки
    }
    
    /// <summary>
    /// Обновить панель подсказок в зависимости от состояния игрока
    /// </summary>
    private void UpdateHintPanel()
    {
        VisualElement hintPanel = root.Q<VisualElement>("hint-panel");
        if (hintPanel == null) return;
        
        Label hintText = hintPanel.Q<Label>("hint-text");
        VisualElement iconsContainer = hintPanel.Q<VisualElement>("hint-icons-container");
        if (hintText == null || iconsContainer == null) return;
        
        // Очистить иконки
        iconsContainer.Clear();
        
        // Проверить состояние игрока
        int totalPets = PetInventory.Instance != null ? PetInventory.Instance.GetTotalPetCount() : 0;
        
        // Получить действительно активных питомцев (тех, что заспавнены в мире)
        List<PetData> activePets = new List<PetData>();
        if (PetSpawner.Instance != null)
        {
            activePets = PetSpawner.Instance.GetActivePetsList();
        }
        int activePetsCount = activePets.Count;
        
        // Проверить, идет ли процесс вылупления яйца
        PetHatchingManager hatchingManager = FindObjectOfType<PetHatchingManager>();
        bool isHatching = hatchingManager != null && hatchingManager.IsHatching();
        
        // Проверить, было ли куплено яйцо (даже если оно еще не вылупилось)
        bool eggPurchased = PlayerPrefs.GetInt("EggPurchased", 0) == 1;
        
        // Проверить, показывается ли подсказка "купите яйцо"
        bool showBuyEggHint = totalPets == 0 && !isHatching && !eggPurchased;
        
        // Обновить анимацию кнопки рюкзака (не пульсировать, если показывается подсказка "купите яйцо")
        UpdateBackpackButtonAnimation(activePetsCount, showBuyEggHint);
        
        // Условие 1: 0 питомцев (но только если не идет вылупление и яйцо не было куплено)
        if (totalPets == 0 && !isHatching && !eggPurchased)
        {
            hintPanel.style.display = DisplayStyle.Flex;
            hintText.text = LocalizationManager.GetHintBuyEgg();
            
            // Добавить иконки shop.png и egg.png
            VisualElement shopIcon = new VisualElement();
            shopIcon.name = "hint-shop-icon";
            shopIcon.AddToClassList("hint-icon");
            LoadShopIcon(shopIcon);
            iconsContainer.Add(shopIcon);
            
            VisualElement eggIcon = new VisualElement();
            eggIcon.name = "hint-egg-icon";
            eggIcon.AddToClassList("hint-icon");
            LoadEggIcon(eggIcon);
            iconsContainer.Add(eggIcon);
            
            // Применить размеры иконок
            ApplyHintIconSizes(iconsContainer);
            
            return;
        }
        
        // Условие 2: 1 неактивный питомец
        if (totalPets == 1 && activePetsCount == 0)
        {
            hintPanel.style.display = DisplayStyle.Flex;
            hintText.text = LocalizationManager.GetHintActivatePet();
            
            // Добавить иконку backpack.png
            VisualElement backpackIcon = new VisualElement();
            backpackIcon.name = "hint-backpack-icon";
            backpackIcon.AddToClassList("hint-icon");
            LoadBackpackIcon(backpackIcon);
            iconsContainer.Add(backpackIcon);
            
            // Применить размеры иконок
            ApplyHintIconSizes(iconsContainer);
            
            return;
        }
        
        // Условие 3: Игрок никогда не нажимал на ускорение и находится в радиусе питомца
        bool hasUsedBoost = PlayerPrefs.GetInt("HasUsedSpeedBoost", 0) == 1;
        if (!hasUsedBoost && activePetsCount > 0)
        {
            // Проверить, находится ли игрок рядом с питомцем
            if (IsPlayerNearPet())
            {
                hintPanel.style.display = DisplayStyle.Flex;
                hintText.text = LocalizationManager.GetHintSpeedUpPet();
                
                // Не добавляем иконки для этой подсказки
                return;
            }
        }
        
        // Если ни одно условие не выполнено, скрыть панель
        hintPanel.style.display = DisplayStyle.None;
        
        // Применить адаптивные стили после обновления
        ApplyMobileStylesToHintPanel();
        
        // Обновить центрирование после изменения содержимого
        if (hintPanel.style.display == DisplayStyle.Flex)
        {
            StartCoroutine(CenterHintPanelCoroutine(hintPanel));
        }
    }
    
    /// <summary>
    /// Обновить анимацию кнопки рюкзака в зависимости от количества активных питомцев
    /// </summary>
    /// <param name="activePetsCount">Количество активных питомцев</param>
    /// <param name="showBuyEggHint">Показывается ли подсказка "купите яйцо"</param>
    private void UpdateBackpackButtonAnimation(int activePetsCount, bool showBuyEggHint = false)
    {
        if (backpackButton == null)
        {
            Debug.LogWarning("[InventoryUI] backpackButton == null, невозможно обновить анимацию");
            return;
        }
        
        // Если показывается подсказка "купите яйцо", не пульсировать кнопку рюкзака
        if (showBuyEggHint)
        {
            if (backpackPulseCoroutine != null)
            {
                Debug.Log("[InventoryUI] Останавливаю анимацию пульсации кнопки рюкзака (показывается подсказка 'купите яйцо')");
                UIAnimations.StopContinuousPulse(backpackButton, this);
                backpackPulseCoroutine = null;
            }
            return;
        }
        
        // Если активных питомцев 0, запустить непрерывную пульсацию
        if (activePetsCount == 0)
        {
            // Если анимация еще не запущена, запустить её
            if (backpackPulseCoroutine == null)
            {
                Debug.Log("[InventoryUI] Запускаю анимацию пульсации кнопки рюкзака (активных питомцев: 0)");
                backpackPulseCoroutine = UIAnimations.StartContinuousPulse(backpackButton, this);
            }
        }
        else
        {
            // Если есть активные питомцы, остановить пульсацию
            if (backpackPulseCoroutine != null)
            {
                Debug.Log($"[InventoryUI] Останавливаю анимацию пульсации кнопки рюкзака (активных питомцев: {activePetsCount})");
                UIAnimations.StopContinuousPulse(backpackButton, this);
                backpackPulseCoroutine = null;
            }
        }
    }
    
    /// <summary>
    /// Проверить, находится ли игрок рядом с питомцем (в радиусе обнаружения)
    /// </summary>
    private bool IsPlayerNearPet()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController == null) return false;
        
        PetSpeedBoostManager boostManager = PetSpeedBoostManager.Instance;
        if (boostManager == null) return false;
        
        // Получить все активные питомцы
        List<PetData> activePets = PetInventory.Instance != null ? PetInventory.Instance.GetActivePets(maxActivePets) : new List<PetData>();
        
        foreach (PetData petData in activePets)
        {
            if (petData == null || petData.worldInstance == null) continue;
            
            PetBehavior petBehavior = petData.worldInstance.GetComponent<PetBehavior>();
            if (petBehavior == null || petBehavior.IsBoosted()) continue;
            
            // Получить detectionRange из PetSpeedBoostManager
            float detectionRange = 3f; // Значение по умолчанию
            System.Reflection.FieldInfo rangeField = typeof(PetSpeedBoostManager).GetField("detectionRange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rangeField != null)
            {
                detectionRange = (float)rangeField.GetValue(boostManager);
            }
            
            float distance = Vector3.Distance(playerController.transform.position, petData.worldInstance.transform.position);
            if (distance <= detectionRange)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Применить размеры к иконкам подсказок
    /// </summary>
    private void ApplyHintIconSizes(VisualElement iconsContainer)
    {
        if (iconsContainer == null) return;
        
        bool isTablet = PlatformDetector.IsTablet();
        float baseIconSize = 28f; // Уменьшено с 40f
        float iconSize = isTablet ? baseIconSize * 1.5f : baseIconSize;
        
        foreach (VisualElement icon in iconsContainer.Children())
        {
            icon.style.width = iconSize;
            icon.style.height = iconSize;
            // backgroundSize автоматически устанавливается при установке backgroundImage
        }
    }
    
    /// <summary>
    /// Применить адаптивные стили к панели подсказок
    /// </summary>
    private void ApplyMobileStylesToHintPanel()
    {
        VisualElement hintPanel = root.Q<VisualElement>("hint-panel");
        if (hintPanel == null) return;
        
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        bool isDesktop = PlatformDetector.IsDesktop();
        
        float baseHeight = 80f;
        float baseFontSize = 14f; // Уменьшено с 18f
        float baseIconSize = 28f; // Уменьшено с 40f
        
        // Получить высоту кнопок для синхронизации
        Button shopButton = root.Q<Button>("shop-button");
        float buttonHeight = baseHeight;
        if (shopButton != null)
        {
            float heightValue = shopButton.resolvedStyle.height;
            if (heightValue > 0)
            {
                buttonHeight = heightValue;
            }
        }
        
        // Также применить стили к иконкам, если они уже созданы
        VisualElement iconsContainer = hintPanel.Q<VisualElement>("hint-icons-container");
        
        if (isTablet)
        {
            // Для планшетов: увеличить размеры пропорционально кнопкам
            hintPanel.style.height = buttonHeight;
            hintPanel.style.fontSize = baseFontSize * 1.5f;
            
            // Увеличить размер иконок
            if (iconsContainer != null)
            {
                foreach (VisualElement icon in iconsContainer.Children())
                {
                    icon.style.width = baseIconSize * 1.5f;
                    icon.style.height = baseIconSize * 1.5f;
                }
            }
        }
        else if (isMobile)
        {
            // Для телефонов: стандартные размеры или немного меньше
            hintPanel.style.height = buttonHeight;
            hintPanel.style.fontSize = baseFontSize;
            
            if (iconsContainer != null)
            {
                foreach (VisualElement icon in iconsContainer.Children())
                {
                    icon.style.width = baseIconSize;
                    icon.style.height = baseIconSize;
                }
            }
        }
        else if (isDesktop)
        {
            // Для десктопа: стандартные размеры
            hintPanel.style.height = buttonHeight;
            hintPanel.style.fontSize = baseFontSize;
            
            if (iconsContainer != null)
            {
                foreach (VisualElement icon in iconsContainer.Children())
                {
                    icon.style.width = baseIconSize;
                    icon.style.height = baseIconSize;
                }
            }
        }
        
        // Применить размеры иконок через вспомогательный метод
        if (iconsContainer != null)
        {
            ApplyHintIconSizes(iconsContainer);
        }
        
        // Центрирование: вычислить ширину и установить отрицательный margin-left
        StartCoroutine(CenterHintPanelCoroutine(hintPanel));
    }
    
    /// <summary>
    /// Корутина для центрирования панели подсказок
    /// </summary>
    private IEnumerator CenterHintPanelCoroutine(VisualElement hintPanel)
    {
        // Подождать несколько кадров, чтобы панель отрендерилась и получила ширину
        yield return null;
        yield return null;
        
        if (hintPanel == null) yield break;
        
        // Получить ширину панели
        float panelWidth = hintPanel.resolvedStyle.width;
        
        if (panelWidth > 0)
        {
            // Установить отрицательный margin-left равный половине ширины для центрирования
            hintPanel.style.marginLeft = -panelWidth / 2f;
        }
    }
    
    /// <summary>
    /// Обновить главный UI
    /// </summary>
    private void UpdateUI()
    {
        // Здесь можно добавить обновление других элементов UI
    }
    
    /// <summary>
    /// Открыть модальное окно магазина
    /// </summary>
    private void OpenShopModal()
    {
        if (shopModalAsset == null)
        {
            Debug.LogError("ShopModal asset не найден!");
            return;
        }
        
        // Закрыть инвентарь, если открыт
        if (modalOverlay != null)
        {
            CloseInventoryModal();
        }
        
        // Создать модальное окно магазина
        shopModalOverlay = shopModalAsset.Instantiate();
        
        VisualElement overlay = shopModalOverlay;
        
        // Установить правильное позиционирование
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.top = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.width = Length.Percent(100);
        overlay.style.height = Length.Percent(100);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        
        root.Add(shopModalOverlay);
        
        // Добавить обработчик клавиатуры
        overlay.RegisterCallback<KeyDownEvent>(OnKeyDown);
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        
        // Установить фокус
        overlay.Focus();
        
        // Найти элементы UI
        VisualElement modalContainer = overlay.Q<VisualElement>("modal-container");
        Button buyEggButton = overlay.Q<Button>("buy-egg-button");
        Button upgradeCrystalButton = overlay.Q<Button>("upgrade-crystal-button");
        Button upgradeMapButton = overlay.Q<Button>("upgrade-map-button");
        
        // Уменьшить ширину модального окна на телефонах
        if (modalContainer != null && PlatformDetector.IsMobile())
        {
            // Определить, телефон ли это (упрощенная логика)
            bool isPhone = IsPhoneDevice();
            
            // Для телефонов уменьшаем еще больше
            if (isPhone)
            {
                modalContainer.style.maxWidth = Length.Percent(85); // Еще меньше для телефонов
            }
            else
            {
                modalContainer.style.maxWidth = Length.Percent(81); // Обычное уменьшение для планшетов
            }
            
            // Уменьшить max-height для телефонов
            if (isPhone)
            {
                float screenHeightValue = root.resolvedStyle.height;
                if (screenHeightValue > 0)
                {
                    modalContainer.style.maxHeight = screenHeightValue * 0.85f; // Уменьшено с 0.9f
                }
            }
        }
        
        // Добавить класс для золотой кнопки
        if (buyEggButton != null)
        {
            buyEggButton.AddToClassList("buy-egg-button");
        }
        
        // Загрузить иконки для магазина
        VisualElement eggEmoji = buyEggButton?.Q<VisualElement>("egg-emoji");
        if (eggEmoji != null)
        {
            LoadEggIcon(eggEmoji);
        }
        
        VisualElement mapEmoji = upgradeMapButton?.Q<VisualElement>("map-emoji");
        if (mapEmoji != null)
        {
            LoadMapIcon(mapEmoji);
        }
        
        // Загрузить иконку кристалла
        VisualElement crystalIcon = upgradeCrystalButton?.Q<VisualElement>("crystal-icon");
        if (crystalIcon != null)
        {
            LoadCrystalIcon(crystalIcon);
        }
        
        // Обновить тексты магазина (локализация)
        UpdateShopTexts(overlay);
        
        // Обновить цены в UI
        UpdateShopPrices(overlay);
        
        // Обновить состояние всех кнопок магазина
        UpdateBuyEggButtonState(buyEggButton);
        UpdateUpgradeCrystalButtonState(overlay);
        UpdateMapUpgradeButtonState(overlay);
        
        // Применить мобильные стили
        if (modalContainer != null)
        {
            // Применить мобильные стили (с небольшой задержкой, чтобы элементы успели загрузиться)
            // Ширина уже установлена выше
            StartCoroutine(ApplyShopModalStylesDelayed(modalContainer));
            
            // Анимация появления
            UIAnimations.AnimateModalAppear(modalContainer, this);
        }
        
        // Обработчики кнопок
        if (buyEggButton != null)
        {
            buyEggButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation(); // Остановить распространение события, чтобы не закрывалось модальное окно
                UIAnimations.AnimateBounce(buyEggButton, this);
                BuyEgg();
            });
        }
        
        if (upgradeCrystalButton != null)
        {
            upgradeCrystalButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation(); // Остановить распространение события, чтобы не закрывалось модальное окно
                UIAnimations.AnimateBounce(upgradeCrystalButton, this);
                UpgradeCrystal();
            });
        }
        
        if (upgradeMapButton != null)
        {
            upgradeMapButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation(); // Остановить распространение события, чтобы не закрывалось модальное окно
                UIAnimations.AnimateBounce(upgradeMapButton, this);
                BuyMapUpgrade();
            });
        }
        
        // Закрытие при клике на overlay
        if (overlay != null)
        {
            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                VisualElement clickedElement = evt.target as VisualElement;
                bool clickedOnContainer = false;
                VisualElement current = clickedElement;
                
                while (current != null && current != overlay)
                {
                    if (current.name == "modal-container" || current == modalContainer)
                    {
                        clickedOnContainer = true;
                        break;
                    }
                    current = current.parent;
                }
                
                if (!clickedOnContainer)
                {
                    CloseShopModal();
                }
            });
        }
        
        if (modalContainer != null)
        {
            modalContainer.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
            });
        }
    }
    
    /// <summary>
    /// Закрыть модальное окно магазина
    /// </summary>
    private void CloseShopModal()
    {
        if (shopModalOverlay != null)
        {
            VisualElement overlay = shopModalOverlay;
            VisualElement modalContainer = overlay.Q<VisualElement>("modal-container");
            
            // Убрать обработчики клавиатуры
            overlay.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            
            // Сразу скрыть затемнение
            overlay.style.opacity = 0f;
            
            if (modalContainer != null)
            {
                UIAnimations.AnimateModalDisappear(modalContainer, this, () =>
                {
                    if (shopModalOverlay != null)
                    {
                        shopModalOverlay.RemoveFromHierarchy();
                        shopModalOverlay = null;
                    }
                });
            }
            else
            {
                if (shopModalOverlay != null)
                {
                    shopModalOverlay.RemoveFromHierarchy();
                    shopModalOverlay = null;
                }
            }
        }
    }
    
    /// <summary>
    /// Загрузить иконку кристалла
    /// </summary>
    private void LoadCrystalIcon(VisualElement iconElement)
    {
        Texture2D crystalTexture = null;
        
        // Сначала пробуем Resources (работает и в редакторе, и в билде)
        crystalTexture = Resources.Load<Texture2D>("Icons/crystal");
        
        #if UNITY_EDITOR
        // В редакторе, если не нашли в Resources, пробуем AssetDatabase
        if (crystalTexture == null)
        {
        crystalTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/crystal.png");
        }
        #endif
        
        if (crystalTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(crystalTexture);
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить иконку кристалла!");
        }
    }
    
    /// <summary>
    /// Купить яйцо
    /// </summary>
    private void BuyEgg()
    {
        // Получить цену яйца (динамическая цена на основе количества питомцев)
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        int eggPrice = shopManager != null ? shopManager.GetEggPrice() : 100;
        int currentCoins = CoinManager.GetCoins();
        
        Debug.Log($"Попытка купить яйцо. Текущие монеты: {currentCoins}, цена: {eggPrice}");
        
        if (currentCoins >= eggPrice)
        {
            CoinManager.SpendCoins(eggPrice);
            Debug.Log($"Монеты потрачены. Осталось: {CoinManager.GetCoins()}");
            
            // Спавнить яйцо через PetHatchingManager
            PetHatchingManager hatchingManager = FindObjectOfType<PetHatchingManager>();
            if (hatchingManager != null)
            {
                hatchingManager.StartHatching();
                
                // Обновить состояние кнопки (заблокировать)
                if (shopModalOverlay != null)
                {
                    Button buyEggButton = shopModalOverlay.Q<Button>("buy-egg-button");
                    UpdateBuyEggButtonState(buyEggButton);
                }
                
                // Сохранить флаг покупки яйца (чтобы подсказка пропала сразу)
                PlayerPrefs.SetInt("EggPurchased", 1);
                PlayerPrefs.Save();
                
                // Закрыть модальное окно после успешной покупки
                CloseShopModal();
                
                // Обновить подсказки сразу (чтобы скрыть подсказку "Купите яйцо")
                UpdateHintPanel();
                
                // Обновить подсказки еще раз с задержкой, чтобы модальное окно успело закрыться
                StartCoroutine(UpdateHintPanelAfterPurchase());
            }
            else
            {
                Debug.LogError("PetHatchingManager не найден на сцене! Убедитесь, что объект с компонентом PetHatchingManager присутствует на сцене.");
            }
        }
        else
        {
            Debug.Log($"Недостаточно монет! Нужно: {eggPrice}, есть: {currentCoins}");
        }
    }
    
    /// <summary>
    /// Улучшить кристаллы
    /// </summary>
    private void UpgradeCrystal()
    {
        int upgradePrice = CrystalUpgradeSystem.GetUpgradePrice();
        int currentCoins = CoinManager.GetCoins();
        
        if (currentCoins >= upgradePrice)
        {
            CoinManager.SpendCoins(upgradePrice);
            // Улучшить HP кристаллов на 50%
            CrystalUpgradeSystem.UpgradeHP();
            
            // Закрыть модальное окно после успешной покупки
            CloseShopModal();
        }
    }
    
    /// <summary>
    /// Купить или переключить карту
    /// </summary>
    private void BuyMapUpgrade()
    {
        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager == null)
        {
            Debug.LogWarning("[InventoryUI] MapManager не найден на сцене!");
            return;
        }
        
        // Если карта уже куплена, переключаем между картами
        if (MapUpgradeSystem.IsMapUpgradePurchased())
        {
            mapManager.ToggleMap();
            
            // Обновить состояние кнопки
            if (shopModalOverlay != null)
            {
                UpdateMapUpgradeButtonState(shopModalOverlay);
            }
            
            // Закрыть модальное окно после переключения карты
            CloseShopModal();
            return;
        }
        
        // Если карта не куплена, покупаем её
        int mapPrice = MapUpgradeSystem.GetMapPrice();
        int currentCoins = CoinManager.GetCoins();
        
        if (currentCoins >= mapPrice)
        {
            CoinManager.SpendCoins(mapPrice);
            MapUpgradeSystem.PurchaseMapUpgrade();
            
            // Установить начальную карту на сумеречные долины после покупки
            MapUpgradeSystem.SetCurrentMap("Level2Map");
            
            // Обновить состояние кнопки
            if (shopModalOverlay != null)
            {
                UpdateMapUpgradeButtonState(shopModalOverlay);
            }
            
            // Обновить карту через MapManager
            mapManager.RefreshMap();
            
            Debug.Log("[InventoryUI] Улучшенная карта успешно куплена! Скорость фарма всех питомцев увеличена на 50%.");
            
            // Закрыть модальное окно после покупки карты
            CloseShopModal();
        }
        else
        {
            Debug.Log($"[InventoryUI] Недостаточно монет для покупки карты. Нужно: {mapPrice}, есть: {currentCoins}");
        }
    }
    
    /// <summary>
    /// Обновить состояние кнопки улучшения кристаллов
    /// </summary>
    private void UpdateUpgradeCrystalButtonState(VisualElement overlay)
    {
        if (overlay == null) return;
        
        Button upgradeCrystalButton = overlay.Q<Button>("upgrade-crystal-button");
        if (upgradeCrystalButton == null) return;
        
        // Получить цену улучшения
        int upgradePrice = CrystalUpgradeSystem.GetUpgradePrice();
        
        // Проверить наличие монет
        int currentCoins = CoinManager.GetCoins();
        bool hasEnoughCoins = currentCoins >= upgradePrice;
        
        // Блокировать кнопку, если недостаточно монет
        upgradeCrystalButton.SetEnabled(hasEnoughCoins);
        
        // Визуально показать, что кнопка заблокирована
        if (!hasEnoughCoins)
        {
            upgradeCrystalButton.AddToClassList("disabled-button");
            upgradeCrystalButton.style.opacity = 0.5f;
        }
        else
        {
            upgradeCrystalButton.RemoveFromClassList("disabled-button");
            upgradeCrystalButton.style.opacity = 1f;
        }
    }
    
    /// <summary>
    /// Обновить состояние кнопки улучшения карты
    /// </summary>
    private void UpdateMapUpgradeButtonState(VisualElement overlay)
    {
        if (overlay == null) return;
        
        Button upgradeMapButton = overlay.Q<Button>("upgrade-map-button");
        if (upgradeMapButton == null) return;
        
        Label mapPriceLabel = overlay.Q<Label>("map-upgrade-price");
        Label mapButtonLabel = upgradeMapButton.Q<Label>(className: "shop-item-label");
        
        // Если карта уже куплена, установить цену на 0 и изменить текст кнопки
        if (MapUpgradeSystem.IsMapUpgradePurchased())
        {
            // Кнопка всегда доступна для переключения
            upgradeMapButton.SetEnabled(true);
            upgradeMapButton.style.opacity = 1f;
            upgradeMapButton.RemoveFromClassList("disabled-button");
            
            // Установить цену на 0
            if (mapPriceLabel != null)
            {
                mapPriceLabel.text = "0 💎";
            }
            
            // Изменить текст кнопки в зависимости от текущей карты
            if (mapButtonLabel != null)
            {
                bool isTwilightValley = MapUpgradeSystem.IsTwilightValleyActive();
                mapButtonLabel.text = isTwilightValley 
                    ? LocalizationManager.GetShopGoToSunnyMeadows() 
                    : LocalizationManager.GetShopGoToTwilightValleys();
            }
        }
        else
        {
            // Карта не куплена, показать цену покупки
            int mapPriceValue = MapUpgradeSystem.GetMapPrice();
            
            if (mapPriceLabel != null)
            {
                mapPriceLabel.text = $"{mapPriceValue} 💎";
            }
            
            // Вернуть исходный текст кнопки
            if (mapButtonLabel != null)
            {
                mapButtonLabel.text = LocalizationManager.GetShopUpgradeMap();
            }
            
            // Проверить, достаточно ли монет
            int currentCoins = CoinManager.GetCoins();
            bool hasEnoughCoins = currentCoins >= mapPriceValue;
            
            // Блокировать кнопку, если недостаточно монет
            upgradeMapButton.SetEnabled(hasEnoughCoins);
            
            // Визуально показать, что кнопка заблокирована
            if (!hasEnoughCoins)
            {
                upgradeMapButton.AddToClassList("disabled-button");
                upgradeMapButton.style.opacity = 0.5f;
            }
            else
            {
                upgradeMapButton.RemoveFromClassList("disabled-button");
                upgradeMapButton.style.opacity = 1f;
            }
        }
    }
    
    /// <summary>
    /// Обновить состояние кнопки покупки яйца
    /// </summary>
    private void UpdateBuyEggButtonState(Button buyEggButton)
    {
        if (buyEggButton == null) return;
        
        PetHatchingManager hatchingManager = FindObjectOfType<PetHatchingManager>();
        bool isHatching = hatchingManager != null && hatchingManager.IsHatching();
        
        // Получить цену яйца (динамическая цена на основе количества питомцев)
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        int eggPrice = shopManager != null ? shopManager.GetEggPrice() : 100;
        
        // Проверить наличие монет
        int currentCoins = CoinManager.GetCoins();
        bool hasEnoughCoins = currentCoins >= eggPrice;
        
        // Блокировать кнопку, если идет вылупление или недостаточно монет
        bool shouldBeEnabled = !isHatching && hasEnoughCoins;
        buyEggButton.SetEnabled(shouldBeEnabled);
        
        // Визуально показать, что кнопка заблокирована
        if (!shouldBeEnabled)
        {
            buyEggButton.AddToClassList("disabled-button");
            buyEggButton.style.opacity = 0.5f;
        }
        else
        {
            buyEggButton.RemoveFromClassList("disabled-button");
            buyEggButton.style.opacity = 1f;
        }
    }
    
    /// <summary>
    /// Обновить состояние кнопки покупки яйца после завершения вылупления
    /// </summary>
    private void UpdateBuyEggButtonAfterHatching()
    {
        if (shopModalOverlay != null)
        {
            Button buyEggButton = shopModalOverlay.Q<Button>("buy-egg-button");
            UpdateBuyEggButtonState(buyEggButton);
        }
    }
    
    /// <summary>
    /// Обновить тексты магазина (локализация)
    /// </summary>
    private void UpdateShopTexts(VisualElement overlay)
    {
        if (overlay == null) return;
        
        // Обновить заголовок магазина
        VisualElement shopContent = overlay.Q<VisualElement>("shop-content");
        if (shopContent != null)
        {
            Label shopTitle = shopContent.Q<Label>(className: "shop-title");
            if (shopTitle != null)
            {
                shopTitle.text = LocalizationManager.GetShopTitle();
            }
        }
        
        // Обновить текст кнопки "Купить яйцо"
        Button buyEggButton = overlay.Q<Button>("buy-egg-button");
        if (buyEggButton != null)
        {
            Label buyEggLabel = buyEggButton.Q<Label>(className: "shop-item-label");
            if (buyEggLabel != null)
            {
                buyEggLabel.text = LocalizationManager.GetShopBuyEgg();
            }
        }
        
        // Обновить текст кнопки "Улучшить кристаллы"
        Button upgradeCrystalButton = overlay.Q<Button>("upgrade-crystal-button");
        if (upgradeCrystalButton != null)
        {
            Label upgradeCrystalLabel = upgradeCrystalButton.Q<Label>(className: "shop-item-label");
            if (upgradeCrystalLabel != null)
            {
                upgradeCrystalLabel.text = LocalizationManager.GetShopUpgradeCrystals();
            }
        }
        
        // Текст кнопки карты обновляется в UpdateMapUpgradeButtonState
    }
    
    /// <summary>
    /// Обновить цены в модальном окне магазина
    /// </summary>
    private void UpdateShopPrices(VisualElement overlay)
    {
        if (overlay == null) return;
        
        // Обновить цену яйца (динамическая цена на основе количества питомцев)
        Label eggPriceLabel = overlay.Q<Label>("egg-price");
        if (eggPriceLabel != null)
        {
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            int eggPrice = shopManager != null ? shopManager.GetEggPrice() : 100;
            eggPriceLabel.text = $"{eggPrice} 💎";
        }
        
        // Обновить цену улучшения кристаллов
        Label crystalPriceLabel = overlay.Q<Label>("crystal-upgrade-price");
        if (crystalPriceLabel != null)
        {
            int crystalPrice = CrystalUpgradeSystem.GetUpgradePrice();
            crystalPriceLabel.text = $"{crystalPrice} 💎";
        }
        
        // Обновить цену улучшения карты
        Label mapPriceLabel = overlay.Q<Label>("map-upgrade-price");
        if (mapPriceLabel != null)
        {
            int mapPrice = MapUpgradeSystem.GetMapPrice();
            mapPriceLabel.text = $"{mapPrice} 💎";
        }
        
        // Обновить состояние всех кнопок магазина
        Button buyEggButton = overlay.Q<Button>("buy-egg-button");
        UpdateBuyEggButtonState(buyEggButton);
        UpdateUpgradeCrystalButtonState(overlay);
        UpdateMapUpgradeButtonState(overlay);
    }
    
    /// <summary>
    /// Показать эмоцию при появлении питомца в активных (с задержкой)
    /// </summary>
    private IEnumerator ShowActiveEmotionDelayed(PetData pet)
    {
        // Подождать немного, чтобы питомец успел заспавниться
        yield return new WaitForSeconds(0.3f);
        
        // Попробовать несколько раз, если питомец еще не заспавнился
        int attempts = 0;
        while (attempts < 10 && (pet == null || pet.worldInstance == null))
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
        }
        
        if (pet != null && pet.worldInstance != null)
        {
            PetEmotionUI emotionUI = pet.worldInstance.GetComponent<PetEmotionUI>();
            if (emotionUI == null)
            {
                // Если компонент еще не добавлен, подождать еще немного
                yield return new WaitForSeconds(0.2f);
                emotionUI = pet.worldInstance.GetComponent<PetEmotionUI>();
            }
            
            if (emotionUI != null)
            {
                // Показать эмоцию при появлении в активных (анимация движения вверх и исчезновения)
                emotionUI.ShowActiveEmotion();
            }
        }
    }
    
    /// <summary>
    /// Применить адаптивные стили для мобильных устройств
    /// </summary>
    private void ApplyMobileStyles()
    {
        if (!PlatformDetector.IsMobile())
        {
            return; // На ПК не применяем мобильные стили
        }
        
        // Применить стили к главному UI
        ApplyMobileStylesToMainUI();
    }
    
    /// <summary>
    /// Применить мобильные стили к главному UI
    /// </summary>
    private void ApplyMobileStylesToMainUI()
    {
        if (root == null) return;
        
        // Определить тип устройства
        bool isExtraSmall = IsExtraSmallDevice();
        bool isPhone = IsPhoneDevice();
        
        // Уменьшить размеры кнопок на мобильных устройствах
        // Для extra small уменьшаем еще больше, но иконки увеличиваем
        float buttonSize = isExtraSmall ? 45f : (isPhone ? 55f : 60f); // Extra small: 45px, Phone: 55px, остальные: 60px
        float buttonTop = isExtraSmall ? 10f : 12f;
        float buttonRight = isExtraSmall ? 10f : 12f;
        
        Button backpackButton = root.Q<Button>("backpack-button");
        if (backpackButton != null)
        {
            backpackButton.style.width = buttonSize; // Было 80px
            backpackButton.style.height = buttonSize; // Было 80px
            backpackButton.style.top = buttonTop; // Было 20px
            backpackButton.style.right = buttonRight; // Было 20px
        }
        
        // Увеличить иконки внутри кнопок (не сами кнопки)
        // Для extra small иконки делаем больше относительно кнопки
        VisualElement backpackIcon = root.Q<VisualElement>("backpack-icon");
        if (backpackIcon != null)
        {
            // Для extra small иконки занимают 85% от размера кнопки (вместо 75%)
            float iconRatio = isExtraSmall ? 0.85f : 0.75f;
            float iconSize = buttonSize * iconRatio;
            backpackIcon.style.width = iconSize;
            backpackIcon.style.height = iconSize;
        }
        
        Button shopButton = root.Q<Button>("shop-button");
        if (shopButton != null)
        {
            shopButton.style.width = buttonSize; // Было 80px
            shopButton.style.height = buttonSize; // Было 80px
            shopButton.style.top = buttonTop; // Было 20px
            shopButton.style.right = buttonSize + buttonRight + 10f; // Позиция справа от первой кнопки
        }
        
        // Увеличить иконки внутри кнопок (не сами кнопки)
        // Для extra small иконки делаем больше относительно кнопки
        VisualElement shopIcon = root.Q<VisualElement>("shop-icon");
        if (shopIcon != null)
        {
            // Для extra small иконки занимают 85% от размера кнопки (вместо 75%)
            float iconRatio = isExtraSmall ? 0.85f : 0.75f;
            float iconSize = buttonSize * iconRatio;
            shopIcon.style.width = iconSize;
            shopIcon.style.height = iconSize;
        }
        
        // Стили для кнопки прыжка (только на мобильных устройствах)
        Button jumpButton = root.Q<Button>("jump-button");
        if (jumpButton != null)
        {
            bool isMobile = PlatformDetector.IsMobile() || PlatformDetector.IsTablet();
            
            if (isMobile)
            {
                // Показать кнопку
                jumpButton.style.display = DisplayStyle.Flex;
                jumpButton.style.visibility = Visibility.Visible;
                
                // Размеры кнопки (адаптивные)
                float jumpButtonSize = isExtraSmall ? 50f : (isPhone ? 60f : 80f);
                jumpButton.style.width = jumpButtonSize;
                jumpButton.style.height = jumpButtonSize;
                
                // Позиционирование: справа внизу
                float jumpButtonRight = isExtraSmall ? 10f : (isPhone ? 15f : 20f);
                float jumpButtonBottom = isExtraSmall ? 10f : (isPhone ? 15f : 20f);
                jumpButton.style.right = jumpButtonRight;
                jumpButton.style.bottom = jumpButtonBottom;
                jumpButton.style.top = StyleKeyword.Auto;
                jumpButton.style.left = StyleKeyword.Auto;
                
                // Border radius для круглой кнопки
                float jumpBorderRadius = jumpButtonSize / 2f; // 50% для круглой кнопки
                jumpButton.style.borderTopLeftRadius = jumpBorderRadius;
                jumpButton.style.borderTopRightRadius = jumpBorderRadius;
                jumpButton.style.borderBottomLeftRadius = jumpBorderRadius;
                jumpButton.style.borderBottomRightRadius = jumpBorderRadius;
                
                // Размер шрифта
                float jumpFontSize = isExtraSmall ? 24f : (isPhone ? 28f : 36f);
                jumpButton.style.fontSize = jumpFontSize;
            }
            else
            {
                // Скрыть на десктопе
                jumpButton.style.display = DisplayStyle.None;
                jumpButton.style.visibility = Visibility.Hidden;
            }
        }
        
        // Применить стили к панели подсказок
        ApplyMobileStylesToHintPanel();
        
        // Уменьшить счетчик монет
        VisualElement coinCounter = root.Q<VisualElement>("coin-counter");
        if (coinCounter != null)
        {
            coinCounter.style.top = isExtraSmall ? 8f : 10f; // Было 20px
            coinCounter.style.left = isExtraSmall ? 8f : 10f; // Было 20px
            coinCounter.style.paddingTop = isExtraSmall ? 4f : 5f; // Было 10px
            coinCounter.style.paddingBottom = isExtraSmall ? 4f : 5f; // Было 10px
            coinCounter.style.paddingLeft = isExtraSmall ? 6f : 8f; // Было 15px
            coinCounter.style.paddingRight = isExtraSmall ? 6f : 8f; // Было 15px
        }
        
        VisualElement coinIcon = root.Q<VisualElement>("coin-icon");
        if (coinIcon != null)
        {
            coinIcon.style.width = isExtraSmall ? 18f : 20f; // Было 32px
            coinIcon.style.height = isExtraSmall ? 18f : 20f; // Было 32px
            coinIcon.style.marginRight = isExtraSmall ? 4f : 5f; // Было 10px
        }
        
        Label coinAmount = root.Q<Label>("coin-amount");
        if (coinAmount != null)
        {
            coinAmount.style.fontSize = isExtraSmall ? 16f : 18f; // Было 28px
        }
    }
    
    /// <summary>
    /// Применить мобильные стили к модальному окну инвентаря
    /// </summary>
    private void ApplyMobileStylesToInventoryModal(VisualElement modalContainer)
    {
        if (modalContainer == null) 
        {
            Debug.Log("[InventoryUI] ApplyMobileStylesToInventoryModal - modalContainer is null!");
            return;
        }
        
        // Используем PlatformDetector (который использует YG2 SDK) для определения типа устройства
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        float screenWidth = PlatformDetector.GetScreenWidth();
        float screenHeight = PlatformDetector.GetScreenHeight();
        float aspectRatio = PlatformDetector.GetAspectRatio();
        
        Debug.Log($"[InventoryUI] ApplyMobileStylesToInventoryModal START - isMobile: {isMobile}, isTablet: {isTablet}, Screen: {screenWidth}x{screenHeight}, AspectRatio: {aspectRatio:F2}");
        
        // Применяем стили для мобильных устройств и планшетов
        // Для десктопа тоже применяем стили к заголовкам
        bool isDesktop = !isMobile && !isTablet;
        
        if (isDesktop)
        {
            // Для десктопа применяем только стили к заголовкам
            ApplyDesktopSectionTitleStyles(modalContainer);
            Debug.Log("[InventoryUI] Desktop detected, applying section title styles only");
            return;
        }
        
        // Ширина модального окна уже уменьшена на 10% при открытии
        
        // Уменьшить заголовки
        VisualElement inventorySection = modalContainer.Q<VisualElement>("inventory-section");
        if (inventorySection != null)
        {
            inventorySection.style.paddingTop = 8f; // Было 15px
            inventorySection.style.paddingBottom = 8f; // Было 15px
            inventorySection.style.paddingLeft = 8f; // Было 15px
            inventorySection.style.paddingRight = 8f; // Было 15px
            inventorySection.style.marginRight = 8f; // Было 15px
            // borderRadius устанавливается через отдельные свойства для каждого угла
            inventorySection.style.borderTopLeftRadius = 12f;
            inventorySection.style.borderTopRightRadius = 12f;
            inventorySection.style.borderBottomLeftRadius = 12f;
            inventorySection.style.borderBottomRightRadius = 12f;
        }
        
        VisualElement activePetsSection = modalContainer.Q<VisualElement>("active-pets-section");
        if (activePetsSection != null)
        {
            activePetsSection.style.paddingTop = 8f; // Было 15px
            activePetsSection.style.paddingBottom = 8f; // Было 15px
            activePetsSection.style.paddingLeft = 8f; // Было 15px
            activePetsSection.style.paddingRight = 8f; // Было 15px
            // borderRadius устанавливается через отдельные свойства для каждого угла
            activePetsSection.style.borderTopLeftRadius = 12f;
            activePetsSection.style.borderTopRightRadius = 12f;
            activePetsSection.style.borderBottomLeftRadius = 12f;
            activePetsSection.style.borderBottomRightRadius = 12f;
        }
        
        // Используем упрощенный метод для определения типа устройства
        // aspectRatio уже объявлен выше на строке 1517
        bool isPhone = IsPhoneDevice();
        Debug.Log($"[InventoryUI] ApplyMobileStylesToInventoryModal - isPhone: {isPhone}, isMobile: {PlatformDetector.IsMobile()}, aspectRatio: {aspectRatio:F2}, screenWidth: {screenWidth}, screenHeight: {screenHeight}");
        
        // Для телефонов применяем более агрессивные стили
        float sectionPadding = isPhone ? 3f : 8f;
        float sectionBorderRadius = isPhone ? 6f : 12f;
        float sectionMargin = isPhone ? 3f : 8f;
        
        // Применить уменьшенные отступы для секций на маленьких экранах
        if (inventorySection != null)
        {
            inventorySection.style.paddingTop = sectionPadding;
            inventorySection.style.paddingBottom = sectionPadding;
            inventorySection.style.paddingLeft = sectionPadding;
            inventorySection.style.paddingRight = sectionPadding;
            inventorySection.style.marginRight = sectionMargin;
            inventorySection.style.marginTop = sectionMargin;
            inventorySection.style.marginBottom = sectionMargin;
            inventorySection.style.borderTopLeftRadius = sectionBorderRadius;
            inventorySection.style.borderTopRightRadius = sectionBorderRadius;
            inventorySection.style.borderBottomLeftRadius = sectionBorderRadius;
            inventorySection.style.borderBottomRightRadius = sectionBorderRadius;
        }
        
        if (activePetsSection != null)
        {
            activePetsSection.style.paddingTop = sectionPadding;
            activePetsSection.style.paddingBottom = sectionPadding;
            activePetsSection.style.paddingLeft = sectionPadding;
            activePetsSection.style.paddingRight = sectionPadding;
            activePetsSection.style.marginTop = sectionMargin;
            activePetsSection.style.marginBottom = sectionMargin;
            activePetsSection.style.borderTopLeftRadius = sectionBorderRadius;
            activePetsSection.style.borderTopRightRadius = sectionBorderRadius;
            activePetsSection.style.borderBottomLeftRadius = sectionBorderRadius;
            activePetsSection.style.borderBottomRightRadius = sectionBorderRadius;
        }
        
        // Уменьшить сам модальный контейнер для телефонов
        if (modalContainer != null && isPhone)
        {
            modalContainer.style.paddingTop = 6f; // Было 20px
            modalContainer.style.paddingBottom = 6f;
            modalContainer.style.paddingLeft = 6f;
            modalContainer.style.paddingRight = 6f;
        }
        
        // Значительно уменьшить заголовки секций
        // В UXML заголовки находятся в inventory-section и active-pets-section с классом "section-title"
        var sectionTitles = new List<Label>();
        
        // Способ 1: Найти через секции (наиболее надежный)
        if (inventorySection != null)
        {
            var title = inventorySection.Q<Label>(className: "section-title");
            if (title != null) 
            {
                sectionTitles.Add(title);
                Debug.Log($"[InventoryUI] Found section title in inventorySection: '{title.text}'");
            }
        }
        if (activePetsSection != null)
        {
            var title = activePetsSection.Q<Label>(className: "section-title");
            if (title != null) 
            {
                sectionTitles.Add(title);
                Debug.Log($"[InventoryUI] Found section title in activePetsSection: '{title.text}'");
            }
        }
        
        // Способ 2: Найти по классу во всем modalContainer
        if (sectionTitles.Count == 0)
        {
            sectionTitles = modalContainer.Query<Label>(className: "section-title").ToList();
            Debug.Log($"[InventoryUI] Found {sectionTitles.Count} section titles by className in modalContainer");
        }
        
        // Способ 3: Найти по тексту
        if (sectionTitles.Count == 0)
        {
            var allLabels = modalContainer.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label != null && label.text != null)
                {
                    string text = label.text.Trim().ToLower();
                    if (text == "все питомцы" || text == "активные питомцы")
                    {
                        sectionTitles.Add(label);
                    }
                }
            }
            Debug.Log($"[InventoryUI] Found {sectionTitles.Count} section titles by text");
        }
        
        foreach (Label sectionTitle in sectionTitles)
        {
            if (sectionTitle != null)
            {
                string oldText = sectionTitle.text;
                string textLower = oldText != null ? oldText.Trim().ToLower() : "";
                
                // Для десктопа: показать заголовок "Активные питомцы" с уменьшенным размером на 15%
                if (isDesktop && textLower.Contains("активные"))
                {
                    sectionTitle.style.display = DisplayStyle.Flex;
                    sectionTitle.style.visibility = Visibility.Visible;
                    sectionTitle.style.opacity = 1f;
                    // Уменьшить размер шрифта на 15%: 22px * 0.85 = 18.7px
                    sectionTitle.style.fontSize = new StyleLength(22f * 0.85f);
                    sectionTitle.SetEnabled(true);
                    Debug.Log($"[InventoryUI] Section title '{oldText}' displayed for desktop with reduced font size (18.7px)");
                }
                else
                {
                    // Скрыть заголовки на мобильных устройствах и планшетах
                sectionTitle.style.display = DisplayStyle.None;
                sectionTitle.style.visibility = Visibility.Hidden;
                sectionTitle.style.opacity = 0f;
                sectionTitle.style.height = 0f;
                sectionTitle.style.width = 0f;
                sectionTitle.style.marginTop = 0f;
                sectionTitle.style.marginBottom = 0f;
                sectionTitle.style.paddingTop = 0f;
                sectionTitle.style.paddingBottom = 0f;
                sectionTitle.style.paddingLeft = 0f;
                sectionTitle.style.paddingRight = 0f;
                sectionTitle.text = ""; // Очистить текст
                sectionTitle.SetEnabled(false); // Отключить элемент
                
                // Удалить из иерархии для полного скрытия
                if (sectionTitle.parent != null)
                {
                    sectionTitle.parent.Remove(sectionTitle);
                    Debug.Log($"[InventoryUI] SectionTitle REMOVED from hierarchy (text was: '{oldText}')");
                }
                else
                {
                    Debug.Log($"[InventoryUI] SectionTitle HIDDEN (text was: '{oldText}')");
                    }
                }
            }
        }
        
        if (sectionTitles.Count == 0)
        {
            Debug.LogWarning("[InventoryUI] No section titles found! Listing all labels...");
            var allLabels = modalContainer.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label != null)
                {
                    Debug.Log($"[InventoryUI] Label: name='{label.name}', text='{label.text}', classes={string.Join(",", label.GetClasses())}");
                }
            }
        }
        
        // Уменьшить ячейки питомцев и сетку
        VisualElement petsGrid = modalContainer.Q<VisualElement>("pets-grid");
        if (petsGrid != null)
        {
            // Увеличить marginBottom, чтобы создать больше пространства перед пагинацией
            petsGrid.style.marginBottom = isPhone ? 15f : 20f; // Увеличено для всех устройств
            petsGrid.style.marginTop = isPhone ? 2f : 8f;
            petsGrid.style.paddingTop = isPhone ? 1f : 4f;
            petsGrid.style.paddingBottom = isPhone ? 1f : 4f;
            petsGrid.style.paddingLeft = isPhone ? 1f : 4f;
            petsGrid.style.paddingRight = isPhone ? 1f : 4f;
            // Gap между элементами сетки контролируется через margin самих элементов
        }
        
        // Уменьшить сетку активных питомцев
        VisualElement activePetsGrid = modalContainer.Q<VisualElement>("active-pets-grid");
        if (activePetsGrid != null)
        {
            activePetsGrid.style.marginTop = isPhone ? 2f : 8f;
            activePetsGrid.style.marginBottom = isPhone ? 2f : 8f;
            activePetsGrid.style.paddingTop = isPhone ? 1f : 4f;
            activePetsGrid.style.paddingBottom = isPhone ? 1f : 4f;
            // Gap между элементами сетки контролируется через margin самих элементов
        }
        
        // Применить более агрессивные стили для мобильных устройств
        // Для всех мобильных уменьшаем, для телефонов - еще больше
        float buttonSize = isPhone ? 28f : 32f; // Для всех мобильных уменьшаем до 32px
        float buttonFontSize = isPhone ? 12f : 14f; // Для всех мобильных уменьшаем до 14px
        float buttonMargin = isPhone ? 3f : 5f; // Для всех мобильных уменьшаем до 5px
        
        // Уменьшить кнопки пагинации
        Button prevPageButton = modalContainer.Q<Button>("prev-page-button");
        Button nextPageButton = modalContainer.Q<Button>("next-page-button");
        if (prevPageButton != null)
        {
            prevPageButton.style.width = buttonSize; // Было 55px
            prevPageButton.style.height = buttonSize; // Было 55px
            prevPageButton.style.fontSize = buttonFontSize; // Было 22px
            prevPageButton.style.marginLeft = buttonMargin; // Было 12px
            prevPageButton.style.marginRight = buttonMargin; // Было 12px
        }
        if (nextPageButton != null)
        {
            nextPageButton.style.width = buttonSize; // Было 55px
            nextPageButton.style.height = buttonSize; // Было 55px
            nextPageButton.style.fontSize = buttonFontSize; // Было 22px
            nextPageButton.style.marginLeft = buttonMargin; // Было 12px
            nextPageButton.style.marginRight = buttonMargin; // Было 12px
        }
        
        VisualElement paginationControls = modalContainer.Q<VisualElement>("pagination-controls");
        if (paginationControls != null)
        {
            // Увеличить отступы сверху для всех устройств, чтобы пагинация была выше
            // margin-top - отступ от сетки питомцев (линия над пагинацией будет выше)
            // padding-top - внутренний отступ (сама пагинация будет выше)
            paginationControls.style.marginTop = isPhone ? 20f : 25f; // Увеличено для всех устройств
            paginationControls.style.paddingTop = isPhone ? 15f : 20f; // Увеличено для всех устройств
            float paginationHeight = isPhone ? 28f : 35f; // Для всех мобильных уменьшаем до 35px
            paginationControls.style.minHeight = new StyleLength(paginationHeight); // Было 60px
            paginationControls.style.height = new StyleLength(paginationHeight); // Было 60px
            // Убедиться, что пагинация не прилипает к низу
            paginationControls.style.marginBottom = isPhone ? 10f : 15f; // Отступ снизу
            // Убедиться, что элементы выровнены по центру вертикально
            paginationControls.style.alignItems = Align.Center;
            paginationControls.style.justifyContent = Justify.Center;
            
            // Применить выравнивание ко всем дочерним элементам пагинации
            foreach (var child in paginationControls.Children())
            {
                if (child != null)
                {
                    child.style.alignSelf = Align.Center;
                }
            }
        }
        
        Label pageInfo = modalContainer.Q<VisualElement>("pagination-controls")?.Q<Label>("page-info");
        if (pageInfo == null)
        {
            pageInfo = modalContainer.Q<Label>("page-info");
        }
        if (pageInfo != null)
        {
            pageInfo.style.fontSize = isPhone ? 10f : 12f; // Для всех мобильных уменьшаем до 12px
            pageInfo.style.marginLeft = buttonMargin; // Было 15px
            pageInfo.style.marginTop = new StyleLength(0f); // Убрать отступ сверху
            pageInfo.style.marginBottom = new StyleLength(0f); // Убрать отступ снизу
            pageInfo.style.marginRight = buttonMargin; // Было 15px
            // Выровнять текст по центру вертикально - принудительно
            pageInfo.style.alignSelf = Align.Center;
            // Установить выравнивание текста по центру
            pageInfo.style.unityTextAlign = TextAnchor.MiddleCenter;
        }
        
        // Также применить выравнивание к кнопкам пагинации
        if (prevPageButton != null)
        {
            prevPageButton.style.alignSelf = Align.Center;
        }
        if (nextPageButton != null)
        {
            nextPageButton.style.alignSelf = Align.Center;
        }
        
        // Уменьшить кнопку закрытия
        Button closeButton = modalContainer.Q<Button>("close-button");
        if (closeButton != null)
        {
            float closeButtonSize = isPhone ? 22f : 28f; // Для всех мобильных уменьшаем до 28px
            closeButton.style.width = closeButtonSize; // Было 45px
            closeButton.style.height = closeButtonSize; // Было 45px
            closeButton.style.fontSize = isPhone ? 14f : 18f; // Было 26px
            // borderRadius устанавливается через отдельные свойства для каждого угла
            float borderRadius = closeButtonSize * 0.5f;
            closeButton.style.borderTopLeftRadius = borderRadius;
            closeButton.style.borderTopRightRadius = borderRadius;
            closeButton.style.borderBottomLeftRadius = borderRadius;
            closeButton.style.borderBottomRightRadius = borderRadius;
        }
    }
    
    /// <summary>
    /// Применить мобильные стили к ячейке питомца (также применяется для десктопа)
    /// </summary>
    private void ApplyMobileStylesToPetSlot(VisualElement slot, bool isActive, bool isPhone = false)
    {
        if (slot == null) 
        {
            Debug.Log($"[InventoryUI] ApplyMobileStylesToPetSlot - Skipping: slot is null");
            return;
        }
        
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        
        Debug.Log($"[InventoryUI] ApplyMobileStylesToPetSlot - Applying styles: isActive={isActive}, isPhone={isPhone}, isMobile={isMobile}, isTablet={isTablet}, isMobileLocal={PlatformDetector.IsMobile()}");
        
        // Принудительно перезаписать CSS стили, установив важные свойства
        // Устанавливаем стили напрямую через код, чтобы они имели приоритет над CSS
        
        if (isActive)
        {
            bool isMobileLocal = PlatformDetector.IsMobile();
            
            // Определить extra small устройство (для мобильных и планшетов)
            bool isExtraSmall = (isMobileLocal || isTablet) ? IsExtraSmallDevice() : false;
            
            // Для активных питомцев
            float baseHeight, baseMinHeight;
            
            // Проверяем планшеты отдельно, так как YG2 SDK может определить их как isMobile=false
            if (isMobileLocal || isTablet)
            {
                // Для мобильных устройств (телефоны и планшеты)
            if (isPhone)
            {
                // Телефоны - уменьшить высоту на 10% (от исходного значения 40f и 36f), затем ещё на 2%
                baseHeight = 40f * 0.9f * 0.9f * 0.98f; // 31.752f (уменьшено на 10% дважды, затем на 2%)
                baseMinHeight = 36f * 0.9f * 0.9f * 0.98f; // 28.5768f (уменьшено на 10% дважды, затем на 2%)
            }
                else if (isTablet)
                {
                    // Планшеты - размеры между телефоном и старым планшетом, увеличены на 15% и еще на 15%
                    // Базовый размер для планшетов: 52px (высота), увеличиваем на 15% и еще на 15%
                    baseHeight = 52f * 1.15f * 1.15f; // 68.77px (увеличено на 15% от предыдущего значения 59.8px)
                    baseMinHeight = 46f * 1.15f * 1.15f; // 60.835px (увеличено на 15% от предыдущего значения 52.9px)
                    Debug.Log($"[InventoryUI] Active pet slot - TABLET: baseHeight={baseHeight}, baseMinHeight={baseMinHeight}");
                }
                else
                {
                    // Старые планшеты или другие устройства
                    baseHeight = 64f;
                    baseMinHeight = 56f;
                }
            }
            else
            {
                // Для десктопа: уменьшить на 15%, затем увеличить на 7% (было 64px и 56px из CSS)
                // Сначала уменьшили на 15% = 54.4px и 47.6px
                // Теперь увеличиваем на 7%: 54.4 * 1.07 = 58.208px, 47.6 * 1.07 = 50.932px
                baseHeight = 64f * 0.85f * 1.07f; // 58.208f
                baseMinHeight = 56f * 0.85f * 1.07f; // 50.932f
            }
            
            // Для extra small уменьшаем на 10% (дополнительно к предыдущему уменьшению)
            float slotHeight = isExtraSmall ? baseHeight * 0.9f * 0.9f : baseHeight;
            float slotMinHeight = isExtraSmall ? baseMinHeight * 0.9f * 0.9f : baseMinHeight;
            
            Debug.Log($"[InventoryUI] Active pet slot - Calculated sizes: slotHeight={slotHeight}, slotMinHeight={slotMinHeight}, isExtraSmall={isExtraSmall}");
            
            // Padding и margin зависят от типа устройства
            float padding, margin;
            
            if (isPhone)
            {
                padding = 2f; // Увеличено в 10 раз: было 0.2px, стало 2px
                margin = 2f; // Увеличено в 10 раз
            }
            else if (isTablet)
            {
                padding = 3f; // Между телефоном 2f и старым планшетом 4f
                margin = 3f; // Между телефоном 2f и старым планшетом 4f
            }
            else
            {
                padding = 4f;
                margin = 4f;
            }
            
            // Принудительно установить размеры, перезаписав CSS
            // Для активных питомцев width должен быть 100%, но высоту ограничиваем
            slot.style.width = new StyleLength(Length.Percent(100)); // Оставить 100% ширины
            
            // Применить размеры с принудительным обновлением
            // Принудительно перезаписываем CSS значения
            // Убеждаемся, что height не переопределяется CSS правилом .active-pets-grid .pet-slot
            slot.style.height = new StyleLength(slotHeight);
            slot.style.minHeight = new StyleLength(slotMinHeight);
            slot.style.maxHeight = new StyleLength(slotHeight); // Максимальная высота = высота (не minHeight!)
            
            // Убедиться, что flex-shrink не мешает
            slot.style.flexShrink = 0f;
            slot.style.flexGrow = 0f; // Не давать ячейке расти
            
            // Принудительно перезаписать CSS важными свойствами
            slot.style.position = Position.Relative;
            
            // Убрать любые ограничения, которые могут переопределить размеры
            slot.style.overflow = Overflow.Visible;
            
            Debug.Log($"[InventoryUI] Applied active slot styles - height={slotHeight}, minHeight={slotMinHeight}, padding={padding}, margin={margin}");
            
            slot.style.paddingTop = new StyleLength(padding);
            slot.style.paddingBottom = new StyleLength(padding);
            slot.style.paddingLeft = new StyleLength(padding);
            slot.style.paddingRight = new StyleLength(padding);
            slot.style.marginBottom = new StyleLength(margin);
            slot.style.marginTop = new StyleLength(0f);
            slot.style.marginLeft = new StyleLength(0f);
            slot.style.marginRight = new StyleLength(0f);
            
            // Принудительно обновить отображение
            slot.MarkDirtyRepaint();
            
            // Уменьшить размер аватара (увеличено в 10 раз)
            VisualElement avatarBlock = slot.Q<VisualElement>(className: "pet-avatar-block");
            float avatarSize = 0f;
            if (avatarBlock != null)
            {
                float avatarMargin;
                
                if (isPhone)
                {
                    avatarSize = 24f * 0.75f; // Уменьшить на 25%: 18px
                    avatarMargin = 3f; // Увеличено в 10 раз
                }
                else if (isTablet)
                {
                    avatarSize = 32f * 0.75f; // Уменьшить на 25%: 24px
                    avatarMargin = 4.5f; // Между телефоном 3f и старым планшетом 6f
                }
                else
                {
                    avatarSize = 40f * 0.75f; // Уменьшить на 25%: 30px
                    avatarMargin = 6f; // Увеличено в 10 раз
                }
                
                avatarBlock.style.width = new StyleLength(avatarSize);
                avatarBlock.style.minWidth = new StyleLength(avatarSize);
                avatarBlock.style.maxWidth = new StyleLength(avatarSize);
                avatarBlock.style.height = new StyleLength(avatarSize);
                avatarBlock.style.minHeight = new StyleLength(avatarSize);
                avatarBlock.style.maxHeight = new StyleLength(avatarSize);
                avatarBlock.style.marginRight = new StyleLength(avatarMargin);
                
                // Уменьшить размер эмоджи внутри avatarBlock (VisualElement, не Label)
                // Иконка должна занимать большую часть размера avatarBlock
                VisualElement emojiElement = avatarBlock.Q<VisualElement>(className: "pet-emoji");
                if (emojiElement != null)
                {
                    // Иконка занимает 80% от размера avatarBlock
                    float emojiDim = avatarSize * 0.8f;
                    
                    emojiElement.style.width = new StyleLength(emojiDim);
                    emojiElement.style.height = new StyleLength(emojiDim);
                    emojiElement.style.minWidth = new StyleLength(emojiDim);
                    emojiElement.style.minHeight = new StyleLength(emojiDim);
                    emojiElement.style.maxWidth = new StyleLength(emojiDim);
                    emojiElement.style.maxHeight = new StyleLength(emojiDim);
                }
            }
            
            // Также проверить Label на случай, если используется где-то еще
            Label emojiLabel = slot.Q<Label>(className: "pet-emoji");
            if (emojiLabel != null)
            {
                float emojiSize, emojiDim;
                
                if (isPhone)
                {
                    emojiSize = 18f * 0.8f * 0.75f; // Уменьшить на 20%, затем на 25%: 10.8px
                    emojiDim = 20f * 0.8f * 0.75f; // Уменьшить на 20%, затем на 25%: 12px
                }
                else if (isTablet)
                {
                    // Для планшетов: уменьшить на 20%, затем на 25%
                    emojiSize = 23f * 1.3f * 0.8f * 0.75f; // 13.8px
                    emojiDim = 26f * 1.3f * 0.8f * 0.75f; // 20.28px
                }
                else
                {
                    emojiSize = 28f * 0.8f * 0.75f; // Уменьшить на 20%, затем на 25%: 16.8px
                    emojiDim = 32f * 0.8f * 0.75f; // Уменьшить на 20%, затем на 25%: 19.2px
                }
                
                emojiLabel.style.fontSize = new StyleLength(emojiSize);
                emojiLabel.style.width = new StyleLength(emojiDim);
                emojiLabel.style.height = new StyleLength(emojiDim);
                emojiLabel.style.minWidth = new StyleLength(emojiDim);
                emojiLabel.style.minHeight = new StyleLength(emojiDim);
                emojiLabel.style.maxWidth = new StyleLength(emojiDim);
                emojiLabel.style.maxHeight = new StyleLength(emojiDim);
            }
            
            // Скрыть название питомца на всех платформах
            Label nameLabel = slot.Q<Label>(className: "pet-name");
            if (nameLabel != null)
            {
                nameLabel.style.display = DisplayStyle.None;
                nameLabel.style.visibility = Visibility.Hidden;
            }
            
            // Скрыть название питомца на всех платформах (для активных питомцев тоже)
            // Название уже скрыто выше, но убедимся для активных питомцев
            Label nameLabelActive = slot.Q<Label>(className: "pet-name");
            if (nameLabelActive != null)
            {
                nameLabelActive.style.display = DisplayStyle.None;
                nameLabelActive.style.visibility = Visibility.Hidden;
            }
            
            // Уменьшить размер бейджа редкости (увеличено в 10 раз)
            VisualElement rarityBadge = slot.Q<VisualElement>(className: "pet-rarity-badge");
            if (rarityBadge != null)
            {
                float badgeHeight = isPhone ? 12f : (isTablet ? 16f : 20f); // Увеличено в 10 раз: было 1.2px/1.6px/2px, стало 12px/16px/20px
                rarityBadge.style.height = new StyleLength(badgeHeight);
                rarityBadge.style.minHeight = new StyleLength(badgeHeight);
                rarityBadge.style.maxHeight = new StyleLength(badgeHeight);
            }
            
            Label rarityText = slot.Q<Label>(className: "pet-rarity-text");
            if (rarityText != null)
            {
                float textSize = isPhone ? 9f : (isTablet ? 11.5f : 14f); // Увеличено в 10 раз: было 0.9px/1.15px/1.4px, стало 9px/11.5px/14px
                rarityText.style.fontSize = new StyleLength(textSize);
            }
        }
        else
        {
            // Для инвентаря: уменьшить ширину так, чтобы помещалось 5 ячеек в ряд (2 ряда по 5)
            // Это применяется для ВСЕХ устройств (mobile и desktop)
            bool isMobileLocal = PlatformDetector.IsMobile();
            float slotWidth, slotHeight, padding, margin;
            
            // Проверяем планшет отдельно, так как YG2 SDK может определить его как isMobile=false
            if (isMobileLocal || isTablet)
            {
                // Определить extra small устройство
                bool isExtraSmall = (isMobileLocal || isTablet) ? IsExtraSmallDevice() : false;
                
                // Для мобильных устройств (телефоны и планшеты)
                float baseWidth, baseHeight;
                
                if (isPhone)
                {
                    // Телефоны
                    baseWidth = 42f; // Немного увеличено: было 35px, стало 42px
                    baseHeight = 60f; // Уменьшено на 25%: было 80px, стало 60px
                }
                else if (isTablet)
                {
                    // Планшеты - размеры увеличены в 2 раза
                    // Базовый размер для планшетов: 50px (ширина), 71px (высота), увеличиваем в 2 раза
                    baseWidth = 50f * 2f; // 100px (увеличено в 2 раза от 50px)
                    baseHeight = 71f * 2f; // 142px (увеличено в 2 раза от 71px)
                    Debug.Log($"[InventoryUI] Inventory slot - TABLET: baseWidth={baseWidth}, baseHeight={baseHeight}");
                }
                else
                {
                    // Старые планшеты или другие устройства (isMobileLocal = true, но не телефон и не планшет)
                    baseWidth = 58f; // Немного увеличено: было 50px, стало 58px
                    baseHeight = 82.5f; // Уменьшено на 25%: было 110px, стало 82.5px
                }
                
                // Для extra small уменьшаем на 10% (дополнительно к предыдущему уменьшению)
                slotWidth = isExtraSmall ? baseWidth * 0.9f * 0.9f : baseWidth; // Дважды на 10% = 0.81 от базового
                slotHeight = isExtraSmall ? baseHeight * 0.9f * 0.9f : baseHeight; // Дважды на 10% = 0.81 от базового
                
                Debug.Log($"[InventoryUI] Inventory slot sizes - isTablet={isTablet}, isPhone={isPhone}, baseWidth={baseWidth}, baseHeight={baseHeight}, slotWidth={slotWidth}, slotHeight={slotHeight}, isExtraSmall={isExtraSmall}");
                
                // Padding и margin зависят от типа устройства
                if (isPhone)
                {
                    padding = 2f;
                    margin = 1.6f;
                }
                else if (isTablet)
                {
                    padding = 3f; // Между телефоном 2f и старым планшетом 4f
                    margin = 2.3f; // Между телефоном 1.6f и старым планшетом 3f
                }
                else
                {
                    padding = 4f;
                    margin = 3f;
                }
            }
            else
            {
                // Для desktop: увеличить на 5% (было уменьшено на 15%, теперь увеличиваем на 5% от базового)
                // Базовый размер: 75px и 110px, уменьшили на 15% = 63.75px и 93.5px
                // Теперь увеличиваем на 5%: 63.75 * 1.05 = 66.9375px, 93.5 * 1.05 = 98.175px
                slotWidth = 75f * 0.85f * 1.05f; // 66.9375f
                slotHeight = 110f * 0.85f * 1.05f; // 98.175f
                padding = 4f;
                margin = 2f;
                isPhone = false; // Для desktop не используем телефонные стили
                Debug.Log($"[InventoryUI] Inventory slot - DESKTOP: slotWidth={slotWidth}, slotHeight={slotHeight}");
            }
            
            // Принудительно установить размеры, перезаписав CSS
            slot.style.width = new StyleLength(slotWidth);
            slot.style.height = new StyleLength(slotHeight);
            slot.style.minWidth = new StyleLength(slotWidth);
            slot.style.minHeight = new StyleLength(slotHeight);
            slot.style.maxWidth = new StyleLength(slotWidth);
            slot.style.maxHeight = new StyleLength(slotHeight);
            
            // Убедиться, что flex-shrink не мешает
            slot.style.flexShrink = 0f;
            slot.style.flexGrow = 0f; // Не давать ячейке расти
            
            // Принудительно перезаписать CSS важными свойствами
            slot.style.position = Position.Relative;
            
            // Убрать любые ограничения, которые могут переопределить размеры
            slot.style.overflow = Overflow.Visible;
            
            Debug.Log($"[InventoryUI] Applied inventory slot styles - width={slotWidth}, height={slotHeight}, padding={padding}, margin={margin}");
            
            slot.style.paddingTop = new StyleLength(padding);
            slot.style.paddingBottom = new StyleLength(padding);
            slot.style.paddingLeft = new StyleLength(padding);
            slot.style.paddingRight = new StyleLength(padding);
            slot.style.marginTop = new StyleLength(margin);
            slot.style.marginBottom = new StyleLength(margin);
            slot.style.marginLeft = new StyleLength(margin);
            slot.style.marginRight = new StyleLength(margin);
            
            // Принудительно обновить отображение
            slot.MarkDirtyRepaint();
            
            // Для мобильных устройств в инвентаре: скрыть название и badge, показать только emoji с фоном
            if (slot.ClassListContains("pet-slot-mobile"))
            {
                // Скрыть название питомца
                Label nameLabel = slot.Q<Label>(className: "pet-name");
                if (nameLabel != null)
                {
                    nameLabel.style.display = DisplayStyle.None;
                    nameLabel.style.visibility = Visibility.Hidden;
                }
                
                // Для планшетов показываем редкость, для телефонов скрываем
                VisualElement rarityBadge = slot.Q<VisualElement>(className: "pet-rarity-badge");
                if (rarityBadge != null)
                {
                    if (isTablet)
                    {
                        // Для планшетов показываем редкость и увеличиваем на 20%
                        rarityBadge.style.display = DisplayStyle.Flex;
                        rarityBadge.style.visibility = Visibility.Visible;
                        
                        // Для планшетов: базовый размер 72px ширина, 22px высота
                        // Увеличиваем на 20%, затем уменьшаем на 10% (только для планшетов): 1.2 * 0.9 = 1.08
                        float baseBadgeWidth = 72f;
                        float baseBadgeHeight = 22f;
                        float badgeWidth = baseBadgeWidth * 1.08f; // 72px * 1.08 = 77.76px (только для планшетов)
                        float badgeHeight = baseBadgeHeight * 1.08f; // 22px * 1.08 = 23.76px (только для планшетов)
                        
                        rarityBadge.style.width = new StyleLength(badgeWidth);
                        rarityBadge.style.height = new StyleLength(badgeHeight);
                        rarityBadge.style.minWidth = new StyleLength(badgeWidth);
                        rarityBadge.style.minHeight = new StyleLength(badgeHeight);
                        rarityBadge.style.maxWidth = new StyleLength(badgeWidth);
                        rarityBadge.style.maxHeight = new StyleLength(badgeHeight);
                    }
                    else
                    {
                        rarityBadge.style.display = DisplayStyle.None;
                        rarityBadge.style.visibility = Visibility.Hidden;
                    }
                }
                
                // Стилизовать контейнер emoji с фоном
                VisualElement emojiContainer = slot.Q<VisualElement>(className: "pet-emoji-mobile");
                if (emojiContainer != null)
                {
                    // Для планшетов увеличиваем размер контейнера на 50%
                    float baseContainerSize = slotHeight * 0.478125f; // 0.5625 * 0.85 = 0.478125
                    float containerSize = isTablet ? baseContainerSize * 1.5f : baseContainerSize;
                    emojiContainer.style.width = new StyleLength(containerSize);
                    emojiContainer.style.height = new StyleLength(containerSize);
                    emojiContainer.style.minWidth = new StyleLength(containerSize);
                    emojiContainer.style.minHeight = new StyleLength(containerSize);
                    emojiContainer.style.maxWidth = new StyleLength(containerSize);
                    emojiContainer.style.maxHeight = new StyleLength(containerSize);
                    
                    // Центрировать контейнер вертикально и горизонтально в ячейке
                    emojiContainer.style.alignSelf = Align.Center;
                    emojiContainer.style.marginTop = Length.Auto();
                    emojiContainer.style.marginBottom = Length.Auto();
                    emojiContainer.style.marginLeft = Length.Auto();
                    emojiContainer.style.marginRight = Length.Auto();
                    emojiContainer.style.justifyContent = Justify.Center;
                    emojiContainer.style.alignItems = Align.Center;
                    
                    // Убедиться, что ячейка центрирует содержимое вертикально
                    slot.style.justifyContent = Justify.Center;
                    slot.style.alignItems = Align.Center;
            
                    // Настроить emoji внутри контейнера
                    Label emojiLabel = emojiContainer.Q<Label>(className: "pet-emoji");
                    if (emojiLabel != null)
                    {
                        // Emoji размер: для планшетов увеличиваем на 50% (0.44625 * 1.5 = 0.669375), затем увеличиваем на 30% для всех устройств, затем уменьшаем на 10%
                        float emojiSizeMultiplier = isTablet ? 0.44625f * 1.5f : 0.44625f;
                        emojiSizeMultiplier *= 1.3f; // Увеличить на 30% для инвентаря
                        emojiSizeMultiplier *= 0.9f; // Уменьшить на 10%
                        float emojiSize = containerSize * emojiSizeMultiplier; 
                        emojiLabel.style.fontSize = new StyleLength(emojiSize);
                        emojiLabel.style.width = new StyleLength(emojiSize);
                        emojiLabel.style.height = new StyleLength(emojiSize);
                        emojiLabel.style.minWidth = new StyleLength(emojiSize);
                        emojiLabel.style.minHeight = new StyleLength(emojiSize);
                        emojiLabel.style.maxWidth = new StyleLength(emojiSize);
                        emojiLabel.style.maxHeight = new StyleLength(emojiSize);
                        
                        // Выровнять emoji по центру контейнера (горизонтально и вертикально)
                        emojiLabel.style.alignSelf = Align.Center;
                        emojiLabel.style.marginLeft = new StyleLength(0f);
                        emojiLabel.style.marginRight = new StyleLength(0f);
                        emojiLabel.style.marginTop = new StyleLength(0f);
                        emojiLabel.style.marginBottom = new StyleLength(0f);
                        emojiLabel.style.paddingLeft = new StyleLength(0f);
                        emojiLabel.style.paddingRight = new StyleLength(0f);
                        emojiLabel.style.paddingTop = new StyleLength(0f);
                        emojiLabel.style.paddingBottom = new StyleLength(0f);
                        emojiLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        emojiLabel.style.justifyContent = Justify.Center;
                        emojiLabel.style.alignItems = Align.Center;
                        emojiLabel.style.position = Position.Relative;
                    }
                }
            }
            else
            {
                // Для десктопа: стандартная стилизация
                // Уменьшить размер эмоджи (увеличено в 10 раз)
            VisualElement emojiElement = slot.Q<VisualElement>(className: "pet-emoji");
            if (emojiElement != null)
            {
                    // Обводка убрана
                    // Уменьшить размер иконки на 10% для десктопа (если используется VisualElement с pet.png)
                    // Размеры задаются через CSS, но можно установить через width/height, если нужно
                    // Для VisualElement размеры могут задаваться через style.width/height, но здесь мы полагаемся на CSS
                }
                
                // Также проверить Label на случай, если используется где-то еще
            Label emojiLabel = slot.Q<Label>(className: "pet-emoji");
            if (emojiLabel != null)
            {
                    // Для планшетов увеличиваем эмоджи на 50%, затем на 30% для всех устройств (для инвентаря), затем уменьшаем на 10%
                    float baseEmojiSize = isPhone ? 20f : 28f;
                    float baseEmojiDim = isPhone ? 28f : 36f;
                    float emojiSize = isTablet ? baseEmojiSize * 1.5f * 1.3f * 0.9f : baseEmojiSize * 1.3f * 0.9f; // Увеличить на 30% для инвентаря, затем уменьшить на 10%
                    float emojiDim = isTablet ? baseEmojiDim * 1.5f * 1.3f * 0.9f : baseEmojiDim * 1.3f * 0.9f; // Увеличить на 30% для инвентаря, затем уменьшить на 10%
                    
                    emojiLabel.style.fontSize = new StyleLength(emojiSize);
                    emojiLabel.style.width = new StyleLength(emojiDim);
                    emojiLabel.style.height = new StyleLength(emojiDim);
                    emojiLabel.style.minWidth = new StyleLength(emojiDim);
                    emojiLabel.style.minHeight = new StyleLength(emojiDim);
                    emojiLabel.style.maxWidth = new StyleLength(emojiDim);
                    emojiLabel.style.maxHeight = new StyleLength(emojiDim);
                    emojiLabel.style.marginBottom = new StyleLength(margin);
            }
            
                // Скрыть название питомца на всех платформах
            Label nameLabel = slot.Q<Label>(className: "pet-name");
            if (nameLabel != null)
            {
                    nameLabel.style.display = DisplayStyle.None;
                    nameLabel.style.visibility = Visibility.Hidden;
            }
            
                // Для планшетов увеличиваем редкость на 20%, для десктопа уменьшаем на 30%
            VisualElement rarityBadge = slot.Q<VisualElement>(className: "pet-rarity-badge");
            if (rarityBadge != null)
            {
                    // Базовые размеры из CSS: 80px ширина, 22px высота
                    float badgeWidth, badgeHeight;
                    if (isPhone)
                    {
                        badgeWidth = 48f;
                        badgeHeight = 16f;
                    }
                    else if (isTablet)
                    {
                        // Для планшетов: увеличиваем на 20%, затем уменьшаем на 10% (только для планшетов): 1.2 * 0.9 = 1.08
                        badgeWidth = 80f * 1.08f; // 80px * 1.08 = 86.4px (только для планшетов)
                        badgeHeight = 22f * 1.08f; // 22px * 1.08 = 23.76px (только для планшетов)
                    }
                    else if (isMobileLocal)
                    {
                        badgeWidth = 72f;
                        badgeHeight = 22f;
                    }
                    else
                    {
                        // Для десктопа уменьшаем на 30%
                        badgeWidth = 56f; // 80px * 0.7 = 56px
                        badgeHeight = 15.4f; // 22px * 0.7 = 15.4px
                    }
                    
                    rarityBadge.style.width = new StyleLength(badgeWidth);
                    rarityBadge.style.height = new StyleLength(badgeHeight);
                    rarityBadge.style.minWidth = new StyleLength(badgeWidth);
                    rarityBadge.style.minHeight = new StyleLength(badgeHeight);
                    rarityBadge.style.maxWidth = new StyleLength(badgeWidth);
                    rarityBadge.style.maxHeight = new StyleLength(badgeHeight);
                    rarityBadge.style.marginTop = new StyleLength(margin);
                    
                    // Для планшетов центрируем эмоджи и редкость вертикально
                    if (isTablet)
                    {
                        slot.style.justifyContent = Justify.Center;
                        slot.style.alignItems = Align.Center;
                        emojiLabel.style.marginBottom = new StyleLength(0f);
                        rarityBadge.style.marginTop = new StyleLength(0f);
                    }
            }
            
            Label rarityText = slot.Q<Label>(className: "pet-rarity-text");
            if (rarityText != null)
            {
                    float textSize = isPhone ? 10f : (isMobileLocal ? 13f : 9.1f); // Для десктопа: 13px * 0.7 = 9.1px
                    rarityText.style.fontSize = new StyleLength(textSize);
                }
            }
        }
    }
    
    /// <summary>
    /// Проверить, является ли устройство iPhone (не iPad)
    /// Использует YG2 SDK для определения типа устройства
    /// </summary>
    private bool IsIPhone()
    {
        #if EnvirData_yg
        // Используем YG2 SDK для определения типа устройства
        if (YG2.envir != null)
        {
            // Если это мобильное устройство (телефон), но не планшет - это iPhone/Android телефон
            bool isPhone = YG2.envir.isMobile && !YG2.envir.isTablet;
            return isPhone;
        }
        #endif
        
        // Если YG2 SDK не доступен, возвращаем false
        return false;
    }
    
    /// <summary>
    /// Применить мобильные стили к модальному окну магазина
    /// </summary>
    private void ApplyMobileStylesToShopModal(VisualElement modalContainer)
    {
        if (modalContainer == null) return;
        
        // Используем PlatformDetector (который использует YG2 SDK) для определения типа устройства
        bool isMobile = PlatformDetector.IsMobile();
        bool isTablet = PlatformDetector.IsTablet();
        float screenWidth = PlatformDetector.GetScreenWidth();
        float screenHeight = PlatformDetector.GetScreenHeight();
        float aspectRatio = PlatformDetector.GetAspectRatio();
        
        // Для отладки
        string deviceType = PlatformDetector.GetDeviceTypeString();
        Debug.Log($"[InventoryUI] ApplyShopModalStyles START - isMobile: {isMobile}, isTablet: {isTablet}, deviceType: {deviceType}, Screen: {screenWidth}x{screenHeight}, AspectRatio: {aspectRatio:F2}");
        
        // Применяем стили для мобильных устройств и планшетов
        if (!isMobile && !isTablet) 
        {
            Debug.Log("[InventoryUI] Not mobile, skipping styles");
            return;
        }
        
        // Используем упрощенный метод для определения типа устройства
        bool isPhone = IsPhoneDevice();
        
        // Для отладки
        Debug.Log($"[InventoryUI] ApplyShopModalStyles - isPhone: {isPhone}, isTablet: {isTablet}, screenWidth: {screenWidth}, screenHeight: {screenHeight}, aspectRatio: {aspectRatio:F2}");
        
        // Для телефонов применяем более агрессивные стили
        // Также считаем маленьким экраном если это телефон
        bool isSmallScreen = isPhone;
        
        // Ширина модального окна уже уменьшена на 10% при открытии
        
        // ВАЖНО: На всех мобильных устройствах применяем адаптивные стили
        // Используем isSmallScreen только для определения степени уменьшения, но стили применяем всегда
        
        VisualElement shopContent = modalContainer.Q<VisualElement>("shop-content");
        if (shopContent == null)
        {
            shopContent = modalContainer.Query<VisualElement>(name: "shop-content").First();
        }
        if (shopContent == null)
        {
            shopContent = modalContainer.Query<VisualElement>(className: "shop-content").First();
        }
        
        if (shopContent != null)
        {
            // На всех мобильных устройствах уменьшаем padding
            // На телефонах делаем еще меньше
            float padding = isPhone ? 4f : (isSmallScreen ? 8f : 10f);
            shopContent.style.paddingTop = padding; // Было 20px
            shopContent.style.paddingBottom = padding; // Было 20px
            shopContent.style.paddingLeft = padding; // Было 20px
            shopContent.style.paddingRight = padding; // Было 20px
            Debug.Log($"[InventoryUI] ShopContent styled with padding: {padding}, isPhone: {isPhone}");
        }
        
        // Уменьшить сам модальный контейнер для телефонов
        if (modalContainer != null && isPhone)
        {
            modalContainer.style.paddingTop = 6f; // Было 20px
            modalContainer.style.paddingBottom = 6f;
            modalContainer.style.paddingLeft = 6f;
            modalContainer.style.paddingRight = 6f;
        }
        else
        {
            Debug.LogWarning("[InventoryUI] ShopContent NOT FOUND!");
        }
        
        // Значительно уменьшить заголовок магазина
        // В UXML заголовок находится в shop-content с классом "shop-title" (без name)
        Label shopTitle = null;
        
        // Способ 1: Найти через shop-content
        if (shopContent != null)
        {
            shopTitle = shopContent.Q<Label>(className: "shop-title");
        }
        
        // Способ 2: Найти по классу во всем modalContainer
        if (shopTitle == null)
        {
            var titles = modalContainer.Query<Label>(className: "shop-title").ToList();
            if (titles.Count > 0)
            {
                shopTitle = titles[0];
            }
        }
        
        // Способ 3: Найти все Label и проверить текст
        if (shopTitle == null)
        {
            var allLabels = modalContainer.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label != null && label.text != null && label.text.Trim().ToLower() == "магазин")
                {
                    shopTitle = label;
                    break;
                }
            }
        }
        
        if (shopTitle != null)
        {
            // Скрыть заголовок "Магазин" на всех устройствах
            string oldText = shopTitle.text;
            shopTitle.style.display = DisplayStyle.None;
            shopTitle.style.visibility = Visibility.Hidden;
            shopTitle.style.opacity = 0f;
            shopTitle.style.height = 0f;
            shopTitle.style.width = 0f;
            shopTitle.style.marginTop = 0f;
            shopTitle.style.marginBottom = 0f;
            shopTitle.style.paddingTop = 0f;
            shopTitle.style.paddingBottom = 0f;
            shopTitle.style.paddingLeft = 0f;
            shopTitle.style.paddingRight = 0f;
            shopTitle.text = ""; // Очистить текст
            shopTitle.SetEnabled(false); // Отключить элемент
            
            // Удалить из иерархии для полного скрытия
            if (shopTitle.parent != null)
            {
                shopTitle.parent.Remove(shopTitle);
                Debug.Log($"[InventoryUI] ShopTitle REMOVED from hierarchy (text was: '{oldText}')");
            }
            else
            {
                Debug.Log($"[InventoryUI] ShopTitle HIDDEN (text was: '{oldText}')");
            }
        }
        else
        {
            Debug.LogError("[InventoryUI] ShopTitle NOT FOUND! Listing all labels in shopContent...");
            if (shopContent != null)
            {
                var allLabels = shopContent.Query<Label>().ToList();
                Debug.Log($"[InventoryUI] Found {allLabels.Count} labels in shopContent");
                foreach (var label in allLabels)
                {
                    if (label != null)
                    {
                        Debug.Log($"[InventoryUI] Label: name='{label.name}', text='{label.text}', classes={string.Join(",", label.GetClasses())}");
                    }
                }
            }
        }
        
        // gap нельзя установить через код в UI Toolkit, он устанавливается через CSS
        // Вместо этого уменьшим marginBottom для всех кнопок товаров
        var shopItemButtons = modalContainer.Query<Button>(className: "shop-item-button").ToList();
        Debug.Log($"[InventoryUI] Found {shopItemButtons.Count} shop item buttons by className");
        
        // Если не нашли по классу, попробуем найти все кнопки
        if (shopItemButtons.Count == 0)
        {
            var allButtons = modalContainer.Query<Button>().ToList();
            Debug.Log($"[InventoryUI] Found {allButtons.Count} total buttons in modal");
            // Ищем кнопки по имени или по содержимому
            foreach (Button btn in allButtons)
            {
                if (btn != null)
                {
                    // Проверяем, содержит ли кнопка элементы магазина
                    var eggEmoji = btn.Q<VisualElement>("egg-emoji");
                    var crystalIcon = btn.Q<VisualElement>("crystal-icon");
                    var mapEmoji = btn.Q<VisualElement>("map-emoji");
                    var buyEggLabel = btn.Q<Label>(className: "shop-item-label");
                    
                    if (eggEmoji != null || crystalIcon != null || mapEmoji != null || buyEggLabel != null || 
                        btn.name.Contains("egg") || btn.name.Contains("crystal") || btn.name.Contains("upgrade") ||
                        btn.name.Contains("buy") || btn.name.Contains("map"))
                    {
                        shopItemButtons.Add(btn);
                        Debug.Log($"[InventoryUI] Added button to list: {btn.name}");
                    }
                }
            }
        }
        
        // Применить стили ко всем найденным кнопкам
        foreach (Button shopItemButton in shopItemButtons)
        {
            if (shopItemButton != null)
            {
                // Более агрессивные размеры для маленьких экранов
                // Для планшетов увеличиваем в 2 раза
                float minHeight = isPhone ? 50f : (isTablet ? 160f : (isSmallScreen ? 65f : 80f)); // Для планшетов: 80f * 2 = 160f
                float padding = isPhone ? 6f : (isTablet ? 24f : (isSmallScreen ? 8f : 12f)); // Для планшетов: 12f * 2 = 24f
                shopItemButton.style.minHeight = minHeight; // Было 140px
                shopItemButton.style.paddingTop = padding; // Было 25px
                shopItemButton.style.paddingBottom = padding; // Было 25px
                shopItemButton.style.paddingLeft = padding; // Было 25px
                shopItemButton.style.paddingRight = padding; // Было 25px
                // borderRadius устанавливается через отдельные свойства для каждого угла
                float borderRadius = isPhone ? 10f : (isTablet ? 30f : (isSmallScreen ? 12f : 15f)); // Для планшетов: 15f * 2 = 30f
                shopItemButton.style.borderTopLeftRadius = borderRadius;
                shopItemButton.style.borderTopRightRadius = borderRadius;
                shopItemButton.style.borderBottomLeftRadius = borderRadius;
                shopItemButton.style.borderBottomRightRadius = borderRadius;
                shopItemButton.style.marginBottom = isPhone ? 4f : (isTablet ? 16f : (isSmallScreen ? 6f : 8f)); // Для планшетов: 8f * 2 = 16f
                Debug.Log($"[InventoryUI] Styled button: {shopItemButton.name}, isPhone={isPhone}, isTablet={isTablet}, minHeight: {minHeight}, padding: {padding}");
            }
        }
        
        if (shopItemButtons.Count == 0)
        {
            Debug.LogError("[InventoryUI] No shop item buttons found at all!");
        }
        
        // Найти все иконки яиц (теперь VisualElement, а не Label)
        var eggEmojis = modalContainer.Query<VisualElement>("egg-emoji").ToList();
        foreach (VisualElement eggEmoji in eggEmojis)
        {
            if (eggEmoji != null)
            {
                float size = isPhone ? 25f : (isTablet ? 80f : (isSmallScreen ? 30f : 40f)); // Для планшетов: 40f * 2 = 80f
                eggEmoji.style.width = size; // Было 90px
                eggEmoji.style.height = size; // Было 90px
                eggEmoji.style.minWidth = size;
                eggEmoji.style.minHeight = size;
                eggEmoji.style.maxWidth = size;
                eggEmoji.style.maxHeight = size;
                eggEmoji.style.marginRight = isPhone ? 6f : (isTablet ? 20f : (isSmallScreen ? 8f : 10f)); // Для планшетов: 10f * 2 = 20f
                // Загрузить иконку
                LoadEggIcon(eggEmoji);
            }
        }
        
        // Найти все иконки карт (теперь VisualElement, а не Label)
        var mapEmojis = modalContainer.Query<VisualElement>("map-emoji").ToList();
        foreach (VisualElement mapEmoji in mapEmojis)
        {
            if (mapEmoji != null)
            {
                float size = isPhone ? 25f : (isTablet ? 80f : (isSmallScreen ? 30f : 40f)); // Для планшетов: 40f * 2 = 80f
                mapEmoji.style.width = size;
                mapEmoji.style.height = size;
                mapEmoji.style.minWidth = size;
                mapEmoji.style.minHeight = size;
                mapEmoji.style.maxWidth = size;
                mapEmoji.style.maxHeight = size;
                mapEmoji.style.marginRight = isPhone ? 6f : (isTablet ? 20f : (isSmallScreen ? 8f : 10f)); // Для планшетов: 10f * 2 = 20f
                // Загрузить иконку
                LoadMapIcon(mapEmoji);
            }
        }
        
        var crystalIcons = modalContainer.Query<VisualElement>("crystal-icon").ToList();
        foreach (VisualElement crystalIcon in crystalIcons)
        {
            if (crystalIcon != null)
            {
                float size = isPhone ? 25f : (isTablet ? 100f : (isSmallScreen ? 30f : 50f)); // Для планшетов: 50f * 2 = 100f
                crystalIcon.style.width = size; // Было 90px
                crystalIcon.style.height = size; // Было 90px
                crystalIcon.style.marginRight = isPhone ? 6f : (isTablet ? 20f : (isSmallScreen ? 8f : 10f)); // Для планшетов: 10f * 2 = 20f
            }
        }
        
        // Применить стили ко всем лейблам товаров
        var shopItemLabels = modalContainer.Query<Label>(className: "shop-item-label").ToList();
        foreach (Label shopItemLabel in shopItemLabels)
        {
            if (shopItemLabel != null)
            {
                shopItemLabel.style.fontSize = isPhone ? 12f : (isTablet ? 32f : (isSmallScreen ? 14f : 16f)); // Для планшетов: 16f * 2 = 32f
            }
        }
        
        var shopItemPrices = modalContainer.Query<Label>(className: "shop-item-price").ToList();
        foreach (Label shopItemPrice in shopItemPrices)
        {
            if (shopItemPrice != null)
            {
                shopItemPrice.style.fontSize = isPhone ? 10f : (isTablet ? 28f : (isSmallScreen ? 12f : 14f)); // Для планшетов: 14f * 2 = 28f
                shopItemPrice.style.marginLeft = isPhone ? 6f : (isTablet ? 20f : (isSmallScreen ? 8f : 10f)); // Для планшетов: 10f * 2 = 20f
            }
        }
        
        // Уменьшить кнопку закрытия
        Button closeButton = modalContainer.Q<Button>("close-button");
        if (closeButton != null)
        {
            float size = isPhone ? 22f : (isSmallScreen ? 25f : 30f);
            closeButton.style.width = size; // Было 45px
            closeButton.style.height = size; // Было 45px
            closeButton.style.fontSize = isPhone ? 14f : (isSmallScreen ? 16f : 18f); // Было 26px
            // borderRadius устанавливается через отдельные свойства для каждого угла
            float borderRadius = size * 0.5f;
            closeButton.style.borderTopLeftRadius = borderRadius;
            closeButton.style.borderTopRightRadius = borderRadius;
            closeButton.style.borderBottomLeftRadius = borderRadius;
            closeButton.style.borderBottomRightRadius = borderRadius;
        }
    }
    
    private IEnumerator ApplyShopModalStylesDelayed(VisualElement modalContainer)
    {
        // Подождать несколько кадров, чтобы все элементы UI успели загрузиться
        yield return null;
        yield return null;
        
        // Применить стили
        if (modalContainer != null)
        {
            bool isMobile = PlatformDetector.IsMobile();
            bool isTablet = PlatformDetector.IsTablet();
            
            if (isMobile || isTablet)
        {
            ApplyMobileStylesToShopModal(modalContainer);
            }
            else
            {
                // Для десктопа применяем десктопные стили
                ApplyDesktopStylesToShopModal(modalContainer);
            }
        }
    }
    
    /// <summary>
    /// Применить десктопные стили к модальному окну магазина
    /// </summary>
    private void ApplyDesktopStylesToShopModal(VisualElement modalContainer)
    {
        if (modalContainer == null) return;
        
        // Найти заголовок "Магазин"
        Label shopTitle = null;
        
        // Способ 1: Найти через shop-content
        VisualElement shopContent = modalContainer.Q<VisualElement>("shop-content");
        if (shopContent != null)
        {
            shopTitle = shopContent.Q<Label>(className: "shop-title");
        }
        
        // Способ 2: Найти по классу во всем modalContainer
        if (shopTitle == null)
        {
            var titles = modalContainer.Query<Label>(className: "shop-title").ToList();
            if (titles.Count > 0)
            {
                shopTitle = titles[0];
            }
        }
        
        // Способ 3: Найти все Label и проверить текст
        if (shopTitle == null)
        {
            var allLabels = modalContainer.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label != null && label.text != null && label.text.Trim().ToLower() == "магазин")
                {
                    shopTitle = label;
                    break;
                }
            }
        }
        
        if (shopTitle != null)
        {
            // Убедиться, что заголовок виден на десктопе (если был скрыт в мобильной версии)
            shopTitle.style.display = DisplayStyle.Flex;
            shopTitle.style.visibility = Visibility.Visible;
            shopTitle.style.opacity = 1f;
            shopTitle.SetEnabled(true);
            
            // Базовые значения из CSS: font-size: 36px, margin-bottom: 30px
            // Уменьшить отступы вверх и вниз (margin-bottom уменьшаем на 50%)
            float baseMarginBottom = 30f; // Из CSS
            shopTitle.style.marginTop = new StyleLength(0f); // Уменьшить на 50% (было 0, оставляем 0)
            shopTitle.style.marginBottom = new StyleLength(baseMarginBottom * 0.5f); // Уменьшить на 50%: 30px * 0.5 = 15px
            
            // Уменьшить размер шрифта на 20%
            float baseFontSize = 36f; // Из CSS
            shopTitle.style.fontSize = new StyleLength(baseFontSize * 0.8f); // 20% уменьшение: 36px * 0.8 = 28.8px
            
            Debug.Log($"[InventoryUI] Desktop: Shop title styled - fontSize: {baseFontSize * 0.8f}px, marginBottom: {baseMarginBottom * 0.5f}px");
        }
        else
        {
            Debug.LogWarning("[InventoryUI] Desktop: Shop title not found!");
        }
        
        // Найти все кнопки магазина
        var shopItemButtons = modalContainer.Query<Button>(className: "shop-item-button").ToList();
        
        // Если не нашли по классу, попробуем найти все кнопки
        if (shopItemButtons.Count == 0)
        {
            var allButtons = modalContainer.Query<Button>().ToList();
            foreach (Button btn in allButtons)
            {
                if (btn != null)
                {
                    var eggEmoji = btn.Q<VisualElement>("egg-emoji");
                    var crystalIcon = btn.Q<VisualElement>("crystal-icon");
                    var mapEmoji = btn.Q<VisualElement>("map-emoji");
                    var buyEggLabel = btn.Q<Label>(className: "shop-item-label");
                    
                    if (eggEmoji != null || crystalIcon != null || mapEmoji != null || buyEggLabel != null || 
                        btn.name.Contains("egg") || btn.name.Contains("crystal") || btn.name.Contains("upgrade") ||
                        btn.name.Contains("buy") || btn.name.Contains("map"))
                    {
                        shopItemButtons.Add(btn);
                    }
                }
            }
        }
        
        // Значительно уменьшить высоту кнопок для десктопа
        // Базовое значение из CSS: min-height: 140px, padding: 25px
        float baseMinHeight = 140f; // Из CSS
        float newMinHeight = 77f; // 70px * 1.1 = 77px (увеличено на 10%)
        float basePadding = 25f; // Из CSS
        float newPadding = 12f; // Уменьшить padding до 12px
        
        foreach (Button shopItemButton in shopItemButtons)
        {
            if (shopItemButton != null)
            {
                // Установить min-height и max-height, чтобы кнопка не растягивалась
                shopItemButton.style.minHeight = new StyleLength(newMinHeight);
                shopItemButton.style.maxHeight = new StyleLength(newMinHeight);
                shopItemButton.style.height = new StyleLength(newMinHeight);
                
                // Уменьшить padding
                shopItemButton.style.paddingTop = new StyleLength(newPadding);
                shopItemButton.style.paddingBottom = new StyleLength(newPadding);
                shopItemButton.style.paddingLeft = new StyleLength(newPadding);
                shopItemButton.style.paddingRight = new StyleLength(newPadding);
                
                // Убедиться, что flex-grow не растягивает кнопку
                shopItemButton.style.flexGrow = 0f;
                shopItemButton.style.flexShrink = 0f;
                
                // Уменьшить размеры эмоджи/иконок на 40%
                // Базовые размеры из CSS: font-size: 70px, width/height: 90px
                float baseEmojiSize = 70f; // Из CSS
                float baseEmojiDim = 90f; // Из CSS
                float newEmojiSize = baseEmojiSize * 0.6f; // Уменьшить на 40%: 70px * 0.6 = 42px
                float newEmojiDim = baseEmojiDim * 0.6f; // Уменьшить на 40%: 90px * 0.6 = 54px
                
                // Найти иконку яйца
                VisualElement eggEmoji = shopItemButton.Q<VisualElement>("egg-emoji");
                if (eggEmoji != null)
                {
                    eggEmoji.style.width = new StyleLength(newEmojiDim);
                    eggEmoji.style.height = new StyleLength(newEmojiDim);
                    eggEmoji.style.minWidth = new StyleLength(newEmojiDim);
                    eggEmoji.style.minHeight = new StyleLength(newEmojiDim);
                    eggEmoji.style.maxWidth = new StyleLength(newEmojiDim);
                    eggEmoji.style.maxHeight = new StyleLength(newEmojiDim);
                }
                
                // Найти иконку кристалла
                VisualElement crystalIcon = shopItemButton.Q<VisualElement>("crystal-icon");
                if (crystalIcon != null)
                {
                    crystalIcon.style.width = new StyleLength(newEmojiDim);
                    crystalIcon.style.height = new StyleLength(newEmojiDim);
                    crystalIcon.style.minWidth = new StyleLength(newEmojiDim);
                    crystalIcon.style.minHeight = new StyleLength(newEmojiDim);
                    crystalIcon.style.maxWidth = new StyleLength(newEmojiDim);
                    crystalIcon.style.maxHeight = new StyleLength(newEmojiDim);
                }
                
                // Найти иконку карты
                VisualElement mapEmoji = shopItemButton.Q<VisualElement>("map-emoji");
                if (mapEmoji != null)
                {
                    mapEmoji.style.width = new StyleLength(newEmojiDim);
                    mapEmoji.style.height = new StyleLength(newEmojiDim);
                    mapEmoji.style.minWidth = new StyleLength(newEmojiDim);
                    mapEmoji.style.minHeight = new StyleLength(newEmojiDim);
                    mapEmoji.style.maxWidth = new StyleLength(newEmojiDim);
                    mapEmoji.style.maxHeight = new StyleLength(newEmojiDim);
                }
                
                Debug.Log($"[InventoryUI] Desktop: Shop button styled - minHeight: {baseMinHeight}px -> {newMinHeight}px, padding: {basePadding}px -> {newPadding}px, emojiSize: {baseEmojiSize}px -> {newEmojiSize}px");
            }
        }
    }
    
    /// <summary>
    /// Применить стили к заголовкам секций для десктопа
    /// </summary>
    private void ApplyDesktopSectionTitleStyles(VisualElement modalContainer)
    {
        if (modalContainer == null) return;
        
        // Найти все заголовки секций
        var sectionTitles = new List<Label>();
        
        VisualElement inventorySection = modalContainer.Q<VisualElement>("inventory-section");
        VisualElement activePetsSection = modalContainer.Q<VisualElement>("active-pets-section");
        
        if (inventorySection != null)
        {
            var title = inventorySection.Q<Label>(className: "section-title");
            if (title != null) sectionTitles.Add(title);
        }
        
        if (activePetsSection != null)
        {
            var title = activePetsSection.Q<Label>(className: "section-title");
            if (title != null) sectionTitles.Add(title);
        }
        
        if (sectionTitles.Count == 0)
        {
            sectionTitles = modalContainer.Query<Label>(className: "section-title").ToList();
        }
        
        foreach (Label sectionTitle in sectionTitles)
        {
            if (sectionTitle != null)
            {
                string oldText = sectionTitle.text;
                string textLower = oldText != null ? oldText.Trim().ToLower() : "";
                
                // Для десктопа: показать заголовок "Активные питомцы" с уменьшенным размером на 15%
                if (textLower.Contains("активные"))
                {
                    sectionTitle.style.display = DisplayStyle.Flex;
                    sectionTitle.style.visibility = Visibility.Visible;
                    sectionTitle.style.opacity = 1f;
                    // Уменьшить размер шрифта на 15%: 22px * 0.85 = 18.7px
                    sectionTitle.style.fontSize = new StyleLength(22f * 0.85f);
                    sectionTitle.SetEnabled(true);
                    
                    // Уменьшить отступы вверх и вниз
                    // Базовые значения из CSS: margin-bottom: 15px
                    float baseMarginBottom = 15f; // Из CSS
                    sectionTitle.style.marginTop = new StyleLength(0f); // Уменьшить верхний отступ
                    sectionTitle.style.marginBottom = new StyleLength(baseMarginBottom * 0.5f); // Уменьшить нижний отступ на 50%: 15px * 0.5 = 7.5px
                    
                    Debug.Log($"[InventoryUI] Desktop: Section title '{oldText}' displayed with reduced font size (18.7px) and reduced margins");
                }
                else
                {
                    // Для "Все питомцы" оставить скрытым
                    sectionTitle.style.display = DisplayStyle.None;
                    sectionTitle.style.visibility = Visibility.Hidden;
                }
            }
        }
        
        // Уменьшить высоту ячеек активных питомцев на 5%
        VisualElement activePetsGrid = modalContainer.Q<VisualElement>("active-pets-grid");
        if (activePetsGrid != null)
        {
            var activeSlots = activePetsGrid.Query<VisualElement>(className: "pet-slot").ToList();
            // Базовые значения из CSS: height: 83px, min-height: 75px
            float baseHeight = 83f; // Из CSS
            float baseMinHeight = 75f; // Из CSS
            float newHeight = baseHeight * 0.95f; // Уменьшить на 5%: 83px * 0.95 = 78.85px
            float newMinHeight = baseMinHeight * 0.95f; // Уменьшить на 5%: 75px * 0.95 = 71.25px
            
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    slot.style.height = new StyleLength(newHeight);
                    slot.style.minHeight = new StyleLength(newMinHeight);
                    slot.style.maxHeight = new StyleLength(newHeight);
                    
                    Debug.Log($"[InventoryUI] Desktop: Active pet slot height reduced by 5% - height: {baseHeight}px -> {newHeight}px, minHeight: {baseMinHeight}px -> {newMinHeight}px");
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Отписаться от события изменения монет
        CoinManager.OnCoinsChanged -= OnCoinsChanged;
        
        // Отписаться от изменения языка
#if Localization_yg
        LocalizationManager.OnLanguageChangedEvent -= OnLanguageChanged;
#endif
    }
    
    /// <summary>
    /// Обработчик изменения языка
    /// </summary>
    private void OnLanguageChanged(string lang)
    {
        // Обновить подсказки
        UpdateHintPanel();
        
        // Обновить тексты магазина, если открыт
        if (shopModalOverlay != null)
        {
            UpdateShopTexts(shopModalOverlay);
            UpdateMapUpgradeButtonState(shopModalOverlay);
        }
        
        // Обновить тексты инвентаря (редкость питомцев, пагинация)
        if (modalOverlay != null)
        {
            UpdateModalUI();
            UpdatePagination();
        }
    }
}

