using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

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
    
    private void Start()
    {
        InitializeUI();
        UpdateUI();
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
        Button backpackButton = root.Q<Button>("backpack-button");
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
            
            // Добавить анимацию пульсации при старте
            UIAnimations.AnimatePulse(backpackButton, this, 2f);
        }
        
        // Инициализировать счетчик монет
        InitializeCoinCounter();
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
    }
    
    /// <summary>
    /// Загрузить иконку магазина
    /// </summary>
    private void LoadShopIcon(VisualElement iconElement)
    {
        Texture2D shopTexture = null;
        
        #if UNITY_EDITOR
        // В редакторе используем AssetDatabase
        shopTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/shop.png");
        if (shopTexture == null)
        {
            Debug.LogWarning("Не удалось загрузить иконку магазина по пути: Assets/Assets/Icons/shop.png");
        }
        #else
        // В билде используем Resources
        shopTexture = Resources.Load<Texture2D>("Assets/Assets/Icons/shop");
        #endif
        
        if (shopTexture != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(shopTexture);
            Debug.Log("Иконка магазина успешно загружена!");
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
        
        #if UNITY_EDITOR
        // В редакторе используем AssetDatabase
        coinTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/crystal.png");
        #else
        // В билде используем Resources
        coinTexture = Resources.Load<Texture2D>("Assets/Assets/Icons/crystal");
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
            float screenHeight = root.resolvedStyle.height;
            if (screenHeight > 0)
            {
                modalContainer.style.maxHeight = screenHeight * 0.9f;
            }
            
            // Анимация появления модального окна
            UIAnimations.AnimateModalAppear(modalContainer, this);
        }
        
        // Подписаться на события
        if (prevPageButton != null)
            prevPageButton.clicked += () =>
            {
                UIAnimations.AnimateBounce(prevPageButton, this);
                ChangePage(-1);
            };
        if (nextPageButton != null)
            nextPageButton.clicked += () =>
            {
                UIAnimations.AnimateBounce(nextPageButton, this);
                ChangePage(1);
            };
        
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
            VisualElement petSlot = CreatePetSlot(pet, false);
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
    }
    
    /// <summary>
    /// Создать ячейку питомца
    /// </summary>
    private VisualElement CreatePetSlot(PetData pet, bool isActive)
    {
        VisualElement slot = new VisualElement();
        slot.AddToClassList("pet-slot");
        if (isActive)
        {
            slot.AddToClassList("active");
        }
        
        if (isActive)
        {
            // Для активных питомцев: горизонтальная структура с двумя блоками
            // Блок 1: Аватарка с эмоджи
            VisualElement avatarBlock = new VisualElement();
            avatarBlock.AddToClassList("pet-avatar-block");
            
            Label emojiLabel = new Label(GetPetEmoji(pet.rarity));
            emojiLabel.AddToClassList("pet-emoji");
            avatarBlock.Add(emojiLabel);
            
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
            // Для инвентаря: вертикальная структура
            Label emojiLabel = new Label(GetPetEmoji(pet.rarity));
            emojiLabel.AddToClassList("pet-emoji");
            slot.Add(emojiLabel);
            
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
            Debug.Log($"Попытка заспавнить питомца {pet.petName} в мире");
            PetSpawner.Instance.SpawnPetInWorld(pet);
            
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
        
        pageInfoLabel.text = $"Страница {currentPage + 1} из {totalPages}";
        
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
        switch (rarity)
        {
            case PetRarity.Common:
                return "Обычн.";
            case PetRarity.Epic:
                return "Эпик";
            case PetRarity.Legendary:
                return "Легенд.";
            default:
                return "?";
        }
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
        
        #if UNITY_EDITOR
        // В редакторе используем AssetDatabase
        backpackTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/backpack.png");
        #else
        // В билде используем Resources
        backpackTexture = Resources.Load<Texture2D>("Assets/Assets/Icons/backpack");
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
        
        // Добавить класс для золотой кнопки
        if (buyEggButton != null)
        {
            buyEggButton.AddToClassList("buy-egg-button");
        }
        
        // Загрузить иконку кристалла
        VisualElement crystalIcon = overlay.Q<VisualElement>("crystal-icon");
        if (crystalIcon != null)
        {
            LoadCrystalIcon(crystalIcon);
        }
        
        // Анимация появления
        if (modalContainer != null)
        {
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
        
        #if UNITY_EDITOR
        // В редакторе используем AssetDatabase
        crystalTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Icons/crystal.png");
        #else
        // В билде используем Resources
        crystalTexture = Resources.Load<Texture2D>("Assets/Assets/Icons/crystal");
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
        int eggPrice = 100;
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
                Debug.Log("PetHatchingManager найден, вызываю SpawnEgg()");
                hatchingManager.SpawnEgg();
                Debug.Log("Яйцо куплено и заспавнено!");
                
                // Закрыть модальное окно после успешной покупки
                CloseShopModal();
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
        int upgradePrice = 200;
        int currentCoins = CoinManager.GetCoins();
        
        if (currentCoins >= upgradePrice)
        {
            CoinManager.SpendCoins(upgradePrice);
            // TODO: Добавить логику улучшения кристаллов
            Debug.Log("Кристаллы улучшены!");
        }
        else
        {
            Debug.Log("Недостаточно монет!");
        }
    }
    
    private void OnDestroy()
    {
        // Отписаться от события изменения монет
        CoinManager.OnCoinsChanged -= OnCoinsChanged;
    }
}

