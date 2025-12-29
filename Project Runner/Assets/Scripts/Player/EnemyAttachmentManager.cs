using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Maneja el sistema de enemigos pegados al jugador (lastre).
/// Responsabilidades: attachment, detachment, física del lastre, muerte por estacionario.
/// </summary>
public class EnemyAttachmentManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxAttachedEnemies = 10;
    [SerializeField] private float massPerEnemy = 10f;
    [SerializeField] private float dragPerEnemy = 0.15f;
    [SerializeField] private float stationaryTimeToDeathWithBallast = 1f;
    [SerializeField] private float stationarySpeedThreshold = 0.5f;
    
    [Header("References")]
    private PlayerHealth playerHealth;
    private PlayerLocomotion locomotion;
    private Rigidbody rb;
    
    private List<Enemy> attachedEnemies = new List<Enemy>();
    private float baseMass;
    private float baseDrag;
    private float stationaryTimer;
    
    public int AttachedCount => attachedEnemies.Count;
    public bool HasAttachedEnemies => attachedEnemies.Count > 0;
    
    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        locomotion = GetComponent<PlayerLocomotion>();
        rb = GetComponent<Rigidbody>();
        
        baseMass = rb.mass;
        baseDrag = rb.linearDamping;
    }
    
    private void Update()
    {
        if (playerHealth.IsDead) return;
        
        CheckStationaryDeath();
    }
    
    /// <summary>
    /// Intenta pegar un enemigo al jugador.
    /// </summary>
    public bool TryAttachEnemy(Enemy enemy)
    {
        if (attachedEnemies.Count >= maxAttachedEnemies)
        {
            Debug.LogWarning("Máximo de enemigos pegados alcanzado!");
            return false;
        }
        
        if (attachedEnemies.Contains(enemy))
        {
            Debug.LogWarning("Este enemigo ya está pegado!");
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
        
        // Cambiar color visual
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.magenta;
        }
        
        // Agregar a lista y actualizar física
        attachedEnemies.Add(enemy);
        UpdateBallastPhysics();
        
        Debug.Log($"Enemigo pegado! Total: {attachedEnemies.Count}");
        
        return true;
    }
    
    /// <summary>
    /// Despega un enemigo del jugador.
    /// </summary>
    public void DetachEnemy(Enemy enemy)
    {
        if (!attachedEnemies.Contains(enemy))
        {
            return;
        }
        
        // Despegar físicamente
        enemy.transform.SetParent(null);
        
        // Reactivar física
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
        
        // Remover de lista y actualizar física
        attachedEnemies.Remove(enemy);
        UpdateBallastPhysics();
        
        Debug.Log($"Enemigo despegado! Total: {attachedEnemies.Count}");
    }
    
    /// <summary>
    /// Despega todos los enemigos (útil para habilidades como Stomp).
    /// </summary>
    public void DetachAllEnemies()
    {
        List<Enemy> enemiesToDetach = new List<Enemy>(attachedEnemies);
        
        foreach (var enemy in enemiesToDetach)
        {
            DetachEnemy(enemy);
        }
    }
    
    private void UpdateBallastPhysics()
    {
        int count = attachedEnemies.Count;
        rb.mass = baseMass + (count * massPerEnemy);
        rb.linearDamping = baseDrag + (count * dragPerEnemy);
    }
    
    private void CheckStationaryDeath()
    {
        if (attachedEnemies.Count == 0)
        {
            stationaryTimer = 0f;
            return;
        }
        
        float currentSpeed = locomotion.GetCurrentSpeed();
        
        if (currentSpeed < stationarySpeedThreshold)
        {
            stationaryTimer += Time.deltaTime;
            
            if (stationaryTimer >= stationaryTimeToDeathWithBallast)
            {
                playerHealth.Die("DEVORADO POR LA HORDA (velocidad 0 con lastre)");
            }
        }
        else
        {
            stationaryTimer = 0f;
        }
    }
    
    /// <summary>
    /// Obtiene el decay rate actual de shield por lastre.
    /// </summary>
    public float GetShieldDecayRate(float decayPerEnemy)
    {
        return attachedEnemies.Count * decayPerEnemy;
    }
    
    /// <summary>
    /// Información de debug para OnGUI.
    /// </summary>
    public (int count, float mass, float drag, float stationaryTime) GetDebugInfo()
    {
        return (
            attachedEnemies.Count,
            rb.mass - baseMass,
            rb.linearDamping - baseDrag,
            stationaryTimer
        );
    }
}