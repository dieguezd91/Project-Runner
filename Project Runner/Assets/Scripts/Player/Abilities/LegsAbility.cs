using UnityEngine;

public class LegsAbility : BodyPartAbility
{
    [Header("Jump Settings")]
    private bool canJump = true;
    private float lastJumpTime;

    private InputReader inputReader;
    private PlayerLocomotion locomotion;

    [Header("Speed Bonus")]
    private float originalMaxSpeed;
    private bool speedBonusApplied = false;

    [Header("Weight Compensation")]
    [Tooltip("Reducción de fuerza por cada enemigo (0.15 = 15% menos por enemigo)")]
    [SerializeField] private float penaltyPerEnemy = 0.15f;
    [Tooltip("Altura mínima garantizada incluso con enemigos")]
    [SerializeField] private float minimumJumpHeight = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    private float jumpStartHeight = 0f;
    private float maxJumpHeightReached = 0f;
    private bool isTracking = false;
    private float lastJumpForceUsed = 0f;
    private int enemiesAttachedOnJump = 0;

    // Para calcular altura base de referencia
    private float baseMass = 1f;

    protected override void OnInitialize()
    {
        locomotion = GetComponent<PlayerLocomotion>();
        if (locomotion != null)
        {
            // Guardar masa base del rigidbody
            baseMass = rb.mass;

            var field = typeof(PlayerLocomotion).GetField("inputReader",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                inputReader = field.GetValue(locomotion) as InputReader;
            }

            // Guardar velocidad máxima original
            if (locomotion.config != null)
            {
                originalMaxSpeed = locomotion.config.maxSpeed;
                ApplySpeedBonus(locomotion);
            }

            // Desuscribir PlayerLocomotion del evento de salto
            if (inputReader != null)
            {
                inputReader.OnJumpPerformed -= locomotion.HandleJumpPerformed;
                inputReader.OnJumpCanceled -= locomotion.HandleJumpCanceled;
                Debug.Log("[LegsAbility] PlayerLocomotion desuscrito del salto base");
            }
        }

        if (inputReader == null)
        {
            Debug.LogError("[LegsAbility] No se pudo obtener InputReader");
            return;
        }

        // Suscribirse al evento de salto
        inputReader.OnJumpPerformed += TryJump;

        Debug.Log($"[LegsAbility] Hydraulic Legs habilitado | Masa base: {baseMass}kg | Salto mejorado activado");
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnJumpPerformed -= TryJump;

            // Re-suscribir PlayerLocomotion cuando se destruye LegsAbility
            if (locomotion != null)
            {
                inputReader.OnJumpPerformed += locomotion.HandleJumpPerformed;
                inputReader.OnJumpCanceled += locomotion.HandleJumpCanceled;
                Debug.Log("[LegsAbility] PlayerLocomotion re-suscrito al salto base");
            }
        }

        // Restaurar velocidad original
        RemoveSpeedBonus();
    }

    private void ApplySpeedBonus(PlayerLocomotion loco)
    {
        if (loco.config != null && !speedBonusApplied)
        {
            loco.config.maxSpeed = originalMaxSpeed * partData.speedBonus;
            speedBonusApplied = true;

            Debug.Log($"[LegsAbility] Speed bonus aplicado: {originalMaxSpeed} → {loco.config.maxSpeed}");
        }
    }

    private void RemoveSpeedBonus()
    {
        if (locomotion != null && locomotion.config != null && speedBonusApplied)
        {
            locomotion.config.maxSpeed = originalMaxSpeed;
            speedBonusApplied = false;

            Debug.Log($"[LegsAbility] Speed bonus removido");
        }
    }

    private void Update()
    {
        // CRITICAL: Verificar que playerLocomotion existe
        if (playerLocomotion == null)
        {
            return;
        }

        // Tracking de altura de salto
        if (isTracking)
        {
            float currentHeight = transform.position.y;

            // Actualizar altura máxima alcanzada
            if (currentHeight > maxJumpHeightReached)
            {
                maxJumpHeightReached = currentHeight;
            }

            // Si empezamos a caer o tocamos el suelo, terminar tracking
            if (rb.linearVelocity.y <= 0 || playerLocomotion.IsGrounded())
            {
                if (playerLocomotion.IsGrounded())
                {
                    // Calcular altura final alcanzada
                    float heightReached = maxJumpHeightReached - jumpStartHeight;

                    if (showDebugGUI && heightReached > 0.1f)
                    {
                        string enemyInfo = enemiesAttachedOnJump > 0 ? $" [{enemiesAttachedOnJump} enemigos]" : "";
                        Debug.Log($"[LegsAbility] ✓ Altura alcanzada: {heightReached:F2}m{enemyInfo}");
                    }

                    // Reset tracking
                    isTracking = false;
                    maxJumpHeightReached = 0f;
                    jumpStartHeight = 0f;
                    enemiesAttachedOnJump = 0;
                }
            }
        }

        // Permitir saltar de nuevo cuando estamos en el suelo
        if (playerLocomotion.IsGrounded())
        {
            canJump = true;
        }
    }

    private void TryJump()
    {
        if (!CanUseAbility())
        {
            return;
        }

        ExecuteJump();
    }

    protected override bool CheckCustomConditions()
    {
        // Solo permitir salto si estamos en el suelo
        if (playerLocomotion != null && !playerLocomotion.IsGrounded())
        {
            return false;
        }

        if (!canJump)
        {
            return false;
        }

        return true;
    }

    private int GetAttachedEnemiesCount()
    {
        EnemyAttachmentManager attachmentManager = GetComponent<EnemyAttachmentManager>();
        if (attachmentManager != null)
        {
            return attachmentManager.AttachedCount;
        }

        return 0;
    }

    private void ExecuteJump()
    {
        enemiesAttachedOnJump = GetAttachedEnemiesCount();

        // Sistema simplificado: penalización directa a la fuerza base
        float penaltyMultiplier = 1f - (enemiesAttachedOnJump * penaltyPerEnemy);
        penaltyMultiplier = Mathf.Max(penaltyMultiplier, 0.4f); // Mínimo 40% de fuerza

        // Calcular fuerza ajustada
        float adjustedJumpForce = partData.jumpForce * penaltyMultiplier;

        // IMPORTANTE: No usar altura esperada para ajustar, solo aplicar mínimo si es necesario
        // El mínimo se alcanza aumentando la fuerza proporcionalmente a la masa
        if (enemiesAttachedOnJump > 0)
        {
            // Calcular la fuerza mínima necesaria considerando la masa actual
            float massRatio = rb.mass / baseMass;
            float minimumForce = partData.jumpForce * 0.7f * massRatio; // 70% de la fuerza base escalada por masa

            if (adjustedJumpForce < minimumForce)
            {
                adjustedJumpForce = minimumForce;
                Debug.Log($"[LegsAbility] Aplicando fuerza mínima: {adjustedJumpForce:F1}");
            }
        }

        lastJumpForceUsed = adjustedJumpForce;

        // Cancelar velocidad vertical actual
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Aplicar impulso de salto
        rb.AddForce(Vector3.up * adjustedJumpForce, ForceMode.Impulse);

        // Iniciar tracking de altura
        jumpStartHeight = transform.position.y;
        maxJumpHeightReached = jumpStartHeight;
        isTracking = true;

        canJump = false;
        lastJumpTime = Time.time;

        // Logs mejorados
        if (enemiesAttachedOnJump > 0)
        {
            float penaltyPercent = (1f - penaltyMultiplier) * 100f;
            Debug.Log($"[LegsAbility] Salto con {enemiesAttachedOnJump} enemigos | " +
                     $"Masa: {rb.mass:F1}kg | " +
                     $"Penalización: -{penaltyPercent:F0}% | " +
                     $"Fuerza aplicada: {adjustedJumpForce:F1}");
        }
        else
        {
            Debug.Log($"[LegsAbility] Salto normal | Fuerza: {adjustedJumpForce:F1}");
        }
    }

    protected override float GetCooldownDuration()
    {
        return partData.cooldownDuration;
    }

    //private void OnGUI()
    //{
    //    if (!showDebugGUI) return;

    //    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
    //    boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));

    //    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
    //    labelStyle.fontSize = 14;
    //    labelStyle.normal.textColor = Color.white;
    //    labelStyle.fontStyle = FontStyle.Bold;

    //    float panelWidth = 270f;
    //    float panelHeight = 180f;
    //    float padding = 10f;

    //    GUILayout.BeginArea(new Rect(Screen.width - panelWidth - padding, 430, panelWidth, panelHeight), boxStyle);

    //    GUILayout.Label("=== LEGS ABILITY (JUMP) ===", labelStyle);
    //    GUILayout.Space(5);

    //    // Estado de salto
    //    bool inAir = !playerLocomotion.IsGrounded();
    //    labelStyle.normal.textColor = inAir ? Color.yellow : Color.white;
    //    GUILayout.Label($"In Air: {(inAir ? "YES" : "NO")}", labelStyle);

    //    // Speed bonus
    //    labelStyle.normal.textColor = Color.cyan;
    //    GUILayout.Label($"Speed Bonus: +{((partData.speedBonus - 1f) * 100f):F0}%", labelStyle);

    //    // Masa
    //    labelStyle.normal.textColor = Color.white;
    //    GUILayout.Label($"Mass: {rb.mass:F1}kg (base: {baseMass:F1}kg)", labelStyle);

    //    // Enemigos pegados y penalización
    //    int attachedEnemies = GetAttachedEnemiesCount();
    //    if (attachedEnemies > 0)
    //    {
    //        labelStyle.normal.textColor = Color.red;
    //        GUILayout.Label($"Attached Enemies: {attachedEnemies}", labelStyle);

    //        float penaltyMultiplier = 1f - (attachedEnemies * penaltyPerEnemy);
    //        penaltyMultiplier = Mathf.Max(penaltyMultiplier, 0.4f);
    //        float penalty = (1f - penaltyMultiplier) * 100f;

    //        labelStyle.normal.textColor = Color.yellow;
    //        GUILayout.Label($"Jump Power: {(penaltyMultiplier * 100f):F0}%", labelStyle);
    //    }

    //    // Can jump
    //    labelStyle.normal.textColor = canJump ? Color.green : Color.red;
    //    GUILayout.Label($"Can Jump: {(canJump ? "YES" : "NO")}", labelStyle);

    //    // Altura actual mientras está saltando
    //    if (isTracking && rb.linearVelocity.y > 0)
    //    {
    //        labelStyle.normal.textColor = Color.cyan;
    //        float currentHeight = transform.position.y - jumpStartHeight;
    //        GUILayout.Label($"Height: {currentHeight:F2}m", labelStyle);
    //    }

    //    GUILayout.EndArea();
    //}

    //private Texture2D MakeTex(int width, int height, Color col)
    //{
    //    Color[] pix = new Color[width * height];
    //    for (int i = 0; i < pix.Length; i++)
    //        pix[i] = col;

    //    Texture2D result = new Texture2D(width, height);
    //    result.SetPixels(pix);
    //    result.Apply();
    //    return result;
    //}
}