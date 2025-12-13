using UnityEngine;
using System.Collections.Generic;

public class SwarmManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyPoolManager enemyPoolManager;

    [Header("Initial Spawn")]
    [Tooltip("Cantidad de enemigos en la masa inicial")]
    [SerializeField] private int initialSwarmSize;

    [Tooltip("Radio del círculo de spawn alrededor del jugador")]
    [SerializeField] private float spawnRadius;

    [Tooltip("Offset hacia atrás del jugador")]
    [SerializeField] private float spawnBehindOffset = 15f;

    [Header("Ground Height (SIMPLE METHOD)")]
    [Tooltip("Altura Y del suelo")]
    [SerializeField] private float groundHeight;

    [Tooltip("Altura SOBRE el suelo para spawnear")]
    [SerializeField] private float spawnHeightAboveGround = 0.5f;

    [Tooltip("¿Usar raycast o altura fija?")]
    [SerializeField] private bool useRaycast = false;

    [Header("Raycast Settings (si useRaycast = true)")]
    [SerializeField] private float raycastStartHeight = 100f;
    [SerializeField] private float raycastDistance = 200f;
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Debug")]
    [SerializeField] private bool autoSpawnOnStart = true;

    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private List<Vector3> lastSpawnPositions = new List<Vector3>();

    public int ActiveEnemiesCount => activeEnemies.Count;

    public void SpawnInitialSwarm()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned!");
            return;
        }

        if (enemyPoolManager == null)
        {
            Debug.LogError("Enemy Pool Manager not assigned!");
            return;
        }

        lastSpawnPositions.Clear();

        Vector3 spawnCenter = playerTransform.position - playerTransform.forward * spawnBehindOffset;

        int successfulSpawns = 0;
        int raycastHits = 0;
        int fixedHeightUsed = 0;

        for (int i = 0; i < initialSwarmSize; i++)
        {
            float angle = (float)i / initialSwarmSize * Mathf.PI * 2f;
            float randomRadius = Random.Range(spawnRadius * 0.5f, spawnRadius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * randomRadius,
                0f,
                Mathf.Sin(angle) * randomRadius
            );

            Vector3 horizontalPosition = spawnCenter + offset;
            Vector3 spawnPosition;

            if (useRaycast)
            {
                spawnPosition = DetectGroundWithRaycast(horizontalPosition, out bool hit);
                if (hit) raycastHits++;
                else fixedHeightUsed++;
            }
            else
            {
                spawnPosition = new Vector3(
                    horizontalPosition.x,
                    groundHeight + spawnHeightAboveGround + 2.0f,
                    horizontalPosition.z
                );
                fixedHeightUsed++;
            }

            lastSpawnPositions.Add(spawnPosition);

            EnemyBase enemy = enemyPoolManager.GetEnemy(
                spawnPosition,
                playerTransform,
                EnemyBase.EnemyState.Pursuing
            );

            activeEnemies.Add(enemy);
            successfulSpawns++;
        }
    }

    private Vector3 DetectGroundWithRaycast(Vector3 horizontalPosition, out bool hit)
    {
        Vector3 rayStart = new Vector3(
            horizontalPosition.x,
            raycastStartHeight,
            horizontalPosition.z
        );

        RaycastHit hitInfo;
        hit = false;

        if (Physics.Raycast(rayStart, Vector3.down, out hitInfo, raycastDistance, groundLayerMask))
        {
            hit = true;

            return hitInfo.point + Vector3.up * spawnHeightAboveGround;
        }

        return new Vector3(
            horizontalPosition.x,
            groundHeight + spawnHeightAboveGround,
            horizontalPosition.z
        );
    }

    public void ClearSwarm()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemyPoolManager.ReturnEnemy(enemy);
            }
        }

        activeEnemies.Clear();
        lastSpawnPositions.Clear();
    }

    public void RestartSwarm()
    {
        ClearSwarm();
        SpawnInitialSwarm();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && playerTransform != null)
        {
            Vector3 spawnCenter = playerTransform.position - playerTransform.forward * spawnBehindOffset;

            // Área de spawn
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

            // Altura de spawn
            Vector3 spawnHeight = new Vector3(spawnCenter.x, groundHeight + spawnHeightAboveGround, spawnCenter.z);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnHeight, 2f);
            Gizmos.DrawLine(spawnCenter, spawnHeight);
        }

        if (Application.isPlaying && playerTransform != null)
        {
            Vector3 spawnCenter = playerTransform.position - playerTransform.forward * spawnBehindOffset;

            // Área de spawn
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

            // Centro de spawn
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerTransform.position, spawnCenter);

            // Enemigos activos
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(enemy.Position, 0.5f);

                    // Línea hasta la altura del suelo
                    Gizmos.color = Color.cyan;
                    Vector3 groundPos = new Vector3(enemy.Position.x, groundHeight, enemy.Position.z);
                    Gizmos.DrawLine(enemy.Position, groundPos);
                }
            }

            // Posiciones de spawn
            if (lastSpawnPositions.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (var pos in lastSpawnPositions)
                {
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                }
            }
        }
    }

    private void OnGUI()
    {
        float x = 10f;
        float y = 610f;
        float w = 400f;
        float h = 25f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 15;
        style.fontStyle = FontStyle.Bold;

        GUI.Box(new Rect(x, y, w, 180f), "");

        y += 10f;

        style.normal.textColor = Color.red;
        GUI.Label(new Rect(x + 10, y, w - 20, h), "THE SWARM", style);
        y += h + 5;

        style.normal.textColor = Color.white;
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Active Enemies: {ActiveEnemiesCount}", style);
        y += h;

        style.fontSize = 12;
        style.normal.textColor = Color.gray;

        string method = useRaycast ? "RAYCAST" : "FIXED HEIGHT";
        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Method: {method}", style);
        y += h;

        GUI.Label(new Rect(x + 10, y, w - 20, h), $"Spawn Height: {groundHeight + spawnHeightAboveGround:F1}", style);
        y += h;

        style.normal.textColor = new Color(1f, 1f, 0.5f);
        GUI.Label(new Rect(x + 10, y, w - 20, h), "R: Restart | C: Clear | M: Toggle Method", style);
    }
}