using UnityEngine;
using System.Collections.Generic;

public class EnemyAttachmentManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxAttachedEnemies = 4; // Límite reducido
    [SerializeField] private float massPerEnemy = 5f; // Reducido (menos impacto en velocidad)
    [SerializeField] private float dragPerEnemy = 0.05f; // Reducido

    [Header("Shield Drain (Exponencial)")]
    [SerializeField]
    private float[] shieldDrainPerEnemy = new float[]
    {
        0f,    // 0 enemigos
        8f,    // 1 enemigo: -8%/s
        20f,   // 2 enemigos: -20%/s
        40f,   // 3 enemigos: -40%/s
        100f   // 4 enemigos: MUERTE INSTANTÁNEA
    };

    [Header("Instant Death")]
    [SerializeField] private int overwhelmThreshold = 4; // Muerte instantánea con 4 enemigos

    [Header("References")]
    private PlayerHealth playerHealth;
    private Rigidbody rb;

    private List<Enemy> attachedEnemies = new List<Enemy>();
    private float baseMass;
    private float baseDrag;

    public int AttachedCount => attachedEnemies.Count;
    public bool HasAttachedEnemies => attachedEnemies.Count > 0;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody>();

        baseMass = rb.mass;
        baseDrag = rb.linearDamping;
    }

    private void Update()
    {
        if (playerHealth.IsDead) return;

        CheckOverwhelmed();
    }

    public bool TryAttachEnemy(Enemy enemy)
    {
        if (attachedEnemies.Count >= maxAttachedEnemies)
        {
            Debug.LogWarning("Máximo de enemigos pegados alcanzado!");
            return false;
        }

        if (attachedEnemies.Contains(enemy))
        {
            return false;
        }

        // Pegar físicamente
        enemy.transform.SetParent(transform);

        // Desactivar física del enemigo
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.isKinematic = true;
            enemyRb.linearVelocity = Vector3.zero;
        }

        Collider enemyCollider = enemy.GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Cambiar color visual (más rojo = más peligro)
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Color dangerColor = Color.Lerp(Color.magenta, Color.red, attachedEnemies.Count / (float)maxAttachedEnemies);
            renderer.material.color = dangerColor;
        }

        // Agregar a lista y actualizar física
        attachedEnemies.Add(enemy);
        UpdateBallastPhysics();

        Debug.Log($"⚠ Enemigo pegado! Total: {attachedEnemies.Count}/{maxAttachedEnemies}");

        // Verificar muerte instantánea
        if (attachedEnemies.Count >= overwhelmThreshold)
        {
            playerHealth.Die("OVERWHELMED BY THE SWARM!");
        }

        return true;
    }

    public void DetachEnemy(Enemy enemy)
    {
        if (!attachedEnemies.Contains(enemy))
        {
            return;
        }

        enemy.transform.SetParent(null);

        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.isKinematic = false;
        }

        Collider enemyCollider = enemy.GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        attachedEnemies.Remove(enemy);
        UpdateBallastPhysics();

        Debug.Log($"Enemigo despegado! Total: {attachedEnemies.Count}/{maxAttachedEnemies}");
    }

    public void DetachAllEnemies()
    {
        List<Enemy> enemiesToDetach = new List<Enemy>(attachedEnemies);

        foreach (var enemy in enemiesToDetach)
        {
            DetachEnemy(enemy);

            // Destruir enemigo al sacarlo
            Destroy(enemy.gameObject);
        }

        // REWARD: Recuperar shield por cada enemigo eliminado
        if (playerHealth != null && enemiesToDetach.Count > 0)
        {
            float shieldRecovery = enemiesToDetach.Count * 20f; // 20% por enemigo
            playerHealth.RecoverShield(shieldRecovery, "Enemies Eliminated");
        }
    }

    private void UpdateBallastPhysics()
    {
        int count = attachedEnemies.Count;
        rb.mass = baseMass + (count * massPerEnemy);
        rb.linearDamping = baseDrag + (count * dragPerEnemy);
    }

    private void CheckOverwhelmed()
    {
        if (attachedEnemies.Count >= overwhelmThreshold && !playerHealth.IsDead)
        {
            playerHealth.Die("OVERWHELMED BY THE SWARM!");
        }
    }

    /// <summary>
    /// Obtiene el drain rate EXPONENCIAL actual.
    /// </summary>
    public float GetShieldDrainRate()
    {
        int count = Mathf.Clamp(attachedEnemies.Count, 0, shieldDrainPerEnemy.Length - 1);
        return shieldDrainPerEnemy[count];
    }

    public (int count, float mass, float drag, float drainRate) GetDebugInfo()
    {
        return (
            attachedEnemies.Count,
            rb.mass - baseMass,
            rb.linearDamping - baseDrag,
            GetShieldDrainRate()
        );
    }
}