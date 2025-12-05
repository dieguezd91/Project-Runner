using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Gestor de pooling para enemigos - optimización de performance
/// </summary>
public class EnemyPoolManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyBase enemyPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private int maxPoolSize = 200;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private ObjectPool<EnemyBase> enemyPool;
    private int activeEnemiesCount = 0;

    public int ActiveEnemiesCount => activeEnemiesCount;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        enemyPool = new ObjectPool<EnemyBase>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: initialPoolSize,
            maxSize: maxPoolSize
        );

        // Pre-calentamiento del pool
        EnemyBase[] prewarmedEnemies = new EnemyBase[initialPoolSize];
        for (int i = 0; i < initialPoolSize; i++)
        {
            prewarmedEnemies[i] = enemyPool.Get();
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            enemyPool.Release(prewarmedEnemies[i]);
        }

        if (showDebug)
        {
            Debug.Log($"Enemy Pool initialized with {initialPoolSize} enemies");
        }
    }

    private EnemyBase CreateEnemy()
    {
        EnemyBase enemy = Instantiate(enemyPrefab, transform);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    private void OnGetEnemy(EnemyBase enemy)
    {
        enemy.gameObject.SetActive(true);
        activeEnemiesCount++;
    }

    private void OnReleaseEnemy(EnemyBase enemy)
    {
        enemy.ReturnToPool();
        activeEnemiesCount--;
    }

    private void OnDestroyEnemy(EnemyBase enemy)
    {
        if (enemy != null && enemy.gameObject != null)
        {
            Destroy(enemy.gameObject);
        }
    }

    /// <summary>
    /// Obtiene un enemigo del pool
    /// </summary>
    public EnemyBase GetEnemy(Vector3 position, Transform target, EnemyBase.EnemyState initialState = EnemyBase.EnemyState.Dormido)
    {
        EnemyBase enemy = enemyPool.Get();
        enemy.Initialize(position, target, initialState);
        return enemy;
    }

    /// <summary>
    /// Devuelve un enemigo al pool
    /// </summary>
    public void ReturnEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            enemyPool.Release(enemy);
        }
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        float x = 10f;
        float y = 500f; // Debajo de otros paneles
        float w = 300f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;

        // Fondo
        GUI.Box(new Rect(x, y, w, 100f), "");

        y += 10f;

        // TITULO
        style.normal.textColor = Color.red;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "ENEMY POOL", style);
        y += h + 5;

        // ACTIVOS
        style.normal.textColor = activeEnemiesCount > 50 ? Color.red : Color.white;
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Active: {activeEnemiesCount}", style);
        y += h;

        // POOL INFO
        style.normal.textColor = Color.gray;
        style.fontSize = 12;
        GUI.Label(new Rect(x + 10, y, w - 20, h),
            $"Pool: {initialPoolSize} → {maxPoolSize} max", style);
    }
}