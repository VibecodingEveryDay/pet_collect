using UnityEngine;

/// <summary>
/// Скрипт для исправления настроек коллайдера платформы
/// Убедитесь, что Box Collider НЕ является триггером
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class PlatformColliderFix : MonoBehaviour
{
    [Header("Автоматическая настройка размера")]
    [SerializeField] private bool autoSizeCollider = true; // Автоматически подогнать размер коллайдера под меш
    [SerializeField] private float colliderHeight = 0.2f; // Высота коллайдера (толщина платформы)
    
    [Header("Ручная настройка (если автоматическая не работает)")]
    [SerializeField] private Vector3 manualSize = Vector3.zero; // Ручной размер коллайдера (если не 0, будет использован вместо автоматического)
    [SerializeField] private Vector3 manualCenter = Vector3.zero; // Ручной центр коллайдера
    
    private BoxCollider boxCollider;
    private Rigidbody rb;
    
    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        
        if (boxCollider != null)
        {
            // Убедиться, что коллайдер НЕ является триггером
            if (boxCollider.isTrigger)
            {
                Debug.LogWarning($"[PlatformColliderFix] Коллайдер на {gameObject.name} был триггером! Исправлено.");
                boxCollider.isTrigger = false;
            }
            
            // Убедиться, что коллайдер включен
            if (!boxCollider.enabled)
            {
                Debug.LogWarning($"[PlatformColliderFix] Коллайдер на {gameObject.name} был отключен! Включено.");
                boxCollider.enabled = true;
            }
            
            // Автоматически подогнать размер коллайдера под меш
            if (autoSizeCollider)
            {
                if (manualSize != Vector3.zero)
                {
                    // Использовать ручной размер
                    boxCollider.size = manualSize;
                    if (manualCenter != Vector3.zero)
                    {
                        boxCollider.center = manualCenter;
                    }
                    Debug.Log($"[PlatformColliderFix] Использован ручной размер коллайдера: {manualSize}");
                }
                else
                {
                    // Попытаться автоматически подогнать
                    bool adjusted = AdjustColliderToMesh();
                    if (!adjusted)
                    {
                        Debug.LogWarning($"[PlatformColliderFix] Не удалось автоматически подогнать размер коллайдера! " +
                                       $"Установите manualSize вручную в Inspector.");
                    }
                }
            }
            
            // Проверить и исправить Rigidbody (если есть)
            if (rb != null)
            {
                // Для статичной платформы Rigidbody должен быть Kinematic
                if (!rb.isKinematic)
                {
                    Debug.LogWarning($"[PlatformColliderFix] Rigidbody на {gameObject.name} не был Kinematic! Исправлено.");
                    rb.isKinematic = true;
                }
            }
            
            // Полная диагностика
            Debug.Log($"[PlatformColliderFix] === ДИАГНОСТИКА ПЛАТФОРМЫ {gameObject.name} ===");
            Debug.Log($"  Позиция: {transform.position}");
            Debug.Log($"  Размер коллайдера: {boxCollider.size}");
            Debug.Log($"  Центр коллайдера: {boxCollider.center}");
            Debug.Log($"  Мир. границы коллайдера: Min={GetWorldBounds().min}, Max={GetWorldBounds().max}");
            Debug.Log($"  IsTrigger: {boxCollider.isTrigger}");
            Debug.Log($"  Enabled: {boxCollider.enabled}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
            Debug.Log($"  Rigidbody: {(rb != null ? $"Kinematic={rb.isKinematic}, UseGravity={rb.useGravity}" : "Нет")}");
            
            // Проверить взаимодействие с персонажем
            CheckPlayerCollision();
        }
        else
        {
            Debug.LogError($"[PlatformColliderFix] BoxCollider не найден на {gameObject.name}!");
        }
    }
    
    /// <summary>
    /// Автоматически подогнать размер коллайдера под меш
    /// </summary>
    private bool AdjustColliderToMesh()
    {
        // Сначала попробовать найти Renderer на самом объекте
        Renderer renderer = GetComponent<Renderer>();
        
        // Если не найден, искать во всех дочерних объектах
        if (renderer == null)
        {
            renderer = GetComponentInChildren<Renderer>();
        }
        
        // Если все еще не найден, попробовать найти MeshFilter (для мешей без Renderer)
        if (renderer == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = GetComponentInChildren<MeshFilter>();
            }
            
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Вычислить границы на основе меша
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 worldSize = transform.TransformVector(meshBounds.size);
                Vector3 worldCenter = transform.TransformPoint(meshBounds.center);
                
                Vector3 localSize = transform.InverseTransformVector(worldSize);
                Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
                
                // Установить размер коллайдера
                boxCollider.size = new Vector3(
                    Mathf.Abs(localSize.x),
                    colliderHeight,
                    Mathf.Abs(localSize.z)
                );
                
                // Центрировать коллайдер по Y относительно меша
                boxCollider.center = new Vector3(
                    localCenter.x,
                    localCenter.y - (localSize.y / 2f) + (colliderHeight / 2f),
                    localCenter.z
                );
                
                Debug.Log($"[PlatformColliderFix] Размер коллайдера подогнан по MeshFilter: {boxCollider.size}");
                return true;
            }
        }
        
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 localSize = transform.InverseTransformVector(bounds.size);
            
            // Установить размер коллайдера на основе размера меша
            boxCollider.size = new Vector3(
                Mathf.Abs(localSize.x),
                colliderHeight, // Использовать заданную высоту
                Mathf.Abs(localSize.z)
            );
            
            // Центрировать коллайдер по Y относительно меша
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            boxCollider.center = new Vector3(
                localCenter.x,
                localCenter.y - (bounds.size.y / 2f) + (colliderHeight / 2f),
                localCenter.z
            );
            
            Debug.Log($"[PlatformColliderFix] Размер коллайдера автоматически подогнан под меш: {boxCollider.size}");
            return true;
        }
        
        // Если ничего не найдено, попробовать вычислить границы всех дочерних объектов
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        if (allRenderers.Length > 0)
        {
            Bounds combinedBounds = allRenderers[0].bounds;
            foreach (Renderer r in allRenderers)
            {
                if (r.enabled)
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }
            
            Vector3 localSize = transform.InverseTransformVector(combinedBounds.size);
            Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
            
            boxCollider.size = new Vector3(
                Mathf.Abs(localSize.x),
                colliderHeight,
                Mathf.Abs(localSize.z)
            );
            
            boxCollider.center = new Vector3(
                localCenter.x,
                localCenter.y - (combinedBounds.size.y / 2f) + (colliderHeight / 2f),
                localCenter.z
            );
            
            Debug.Log($"[PlatformColliderFix] Размер коллайдера подогнан по всем дочерним Renderer: {boxCollider.size}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Получить мировые границы коллайдера
    /// </summary>
    private Bounds GetWorldBounds()
    {
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 size = transform.TransformVector(boxCollider.size);
        return new Bounds(center, size);
    }
    
    /// <summary>
    /// Проверить возможность коллизии с персонажем
    /// </summary>
    private void CheckPlayerCollision()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Попробовать найти по имени
            player = GameObject.Find("Player");
        }
        
        if (player != null)
        {
            CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
            if (playerCollider != null)
            {
                Bounds platformBounds = GetWorldBounds();
                Bounds playerBounds = new Bounds(
                    playerCollider.bounds.center,
                    playerCollider.bounds.size
                );
                
                bool intersects = platformBounds.Intersects(playerBounds);
                float distance = Vector3.Distance(platformBounds.center, playerBounds.center);
                
                Debug.Log($"[PlatformColliderFix] Проверка коллизии с персонажем:");
                Debug.Log($"  Персонаж найден: {player.name}");
                Debug.Log($"  Позиция персонажа: {player.transform.position}");
                Debug.Log($"  Границы персонажа: {playerBounds.min} - {playerBounds.max}");
                Debug.Log($"  Расстояние между центрами: {distance:F2}");
                Debug.Log($"  Коллизия возможна: {intersects}");
                
                if (!intersects && distance < 10f)
                {
                    Debug.LogWarning($"[PlatformColliderFix] ВНИМАНИЕ: Коллизия не обнаружена, но объекты близко! " +
                                   $"Возможно, коллайдер платформы слишком маленький или находится не в том месте.");
                }
            }
            else
            {
                Debug.LogWarning($"[PlatformColliderFix] У персонажа не найден CapsuleCollider!");
            }
        }
        else
        {
            Debug.LogWarning($"[PlatformColliderFix] Персонаж не найден в сцене!");
        }
    }
    
    /// <summary>
    /// Визуализация коллайдера в редакторе (для отладки)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();
            
        if (boxCollider != null && boxCollider.enabled)
        {
            Gizmos.color = boxCollider.isTrigger ? Color.red : Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            
            // Также показать мировые границы
            Bounds bounds = GetWorldBounds();
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
    
    /// <summary>
    /// Тест коллизии в реальном времени (для отладки)
    /// </summary>
    private void Update()
    {
        // Периодически проверять коллизию с персонажем (раз в секунду)
        if (Time.frameCount % 60 == 0)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");
                
            if (player != null && boxCollider != null)
            {
                Bounds platformBounds = GetWorldBounds();
                CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
                if (playerCollider != null)
                {
                    bool isColliding = platformBounds.Intersects(playerCollider.bounds);
                    if (isColliding)
                    {
                        Debug.Log($"[PlatformColliderFix] КОЛЛИЗИЯ ОБНАРУЖЕНА с {player.name}!");
                    }
                }
            }
        }
    }
}


