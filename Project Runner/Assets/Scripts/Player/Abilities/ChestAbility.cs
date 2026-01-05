using UnityEngine;

public class ChestAbility : BodyPartAbility
{
    [Header("Double Jump Settings")]
    private int jumpsRemaining = 0;
    private int maxAirJumps = 1; // Solo 1 doble salto (total 2 saltos)
    private bool hasDoubleJumped = false;

    private InputReader inputReader;
    private PlayerLocomotion locomotion;

    [Header("Air Control")]
    private float originalAirDrag = 0f;
    private bool airControlActive = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugGUI = true;
    private float lastDoubleJumpTime = -999f;

    protected override void OnInitialize()
    {
        locomotion = GetComponent<PlayerLocomotion>();
        if (locomotion != null)
        {
            var field = typeof(PlayerLocomotion).GetField("inputReader",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                inputReader = field.GetValue(locomotion) as InputReader;
            }
        }

        if (inputReader == null)
        {
            Debug.LogError("[ChestAbility] No se pudo obtener InputReader");
            return;
        }

        // Guardar drag original para restaurar
        originalAirDrag = rb.linearDamping;

        // Suscribirse al evento de salto (comparte el mismo input)
        inputReader.OnJumpPerformed += TryDoubleJump;

        Debug.Log("[ChestAbility] Air Propellers habilitado - Double Jump + control aéreo mejorado activado");
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.OnJumpPerformed -= TryDoubleJump;
        }

        // Restaurar drag original
        if (airControlActive)
        {
            rb.linearDamping = originalAirDrag;
        }
    }

    private void Update()
    {
        // CRITICAL: Verificar que playerLocomotion existe
        if (playerLocomotion == null)
        {
            return;
        }

        // Resetear saltos disponibles cuando tocamos el suelo
        if (playerLocomotion.IsGrounded())
        {
            jumpsRemaining = maxAirJumps;
            hasDoubleJumped = false;

            // Restaurar drag al tocar suelo
            if (airControlActive)
            {
                rb.linearDamping = originalAirDrag;
                airControlActive = false;
            }
        }
        else
        {
            // Bonus pasivo: reducir drag en aire para mantener velocidad horizontal
            if (!airControlActive)
            {
                rb.linearDamping = originalAirDrag * (1f - partData.airVelocityConservation * 0.5f);
                airControlActive = true;
            }
        }
    }

    private void TryDoubleJump()
    {
        // Solo intentar double jump si estamos en el aire
        if (playerLocomotion.IsGrounded())
        {
            return; // El salto base lo maneja LegsAbility o PlayerLocomotion
        }

        if (!CanUseAbility())
        {
            if (showDebugGUI)
            {
                if (isOnCooldown)
                {
                    Debug.Log($"[ChestAbility] Double Jump en cooldown: {GetRemainingCooldown():F1}s");
                }
                else if (jumpsRemaining <= 0)
                {
                    Debug.Log("[ChestAbility] Sin saltos disponibles");
                }
            }
            return;
        }

        ExecuteDoubleJump();
    }

    protected override bool CheckCustomConditions()
    {
        // Verificar que estamos en el aire
        if (playerLocomotion.IsGrounded())
        {
            return false;
        }

        // Verificar que tenemos saltos disponibles
        if (jumpsRemaining <= 0)
        {
            return false;
        }

        return true;
    }

    private void ExecuteDoubleJump()
    {
        // Guardar velocidad horizontal actual (la conservamos 100%)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Cancelar velocidad vertical actual (permite saltar hacia arriba incluso si estamos cayendo)
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Aplicar impulso de double jump (menor que el salto base)
        rb.AddForce(Vector3.up * partData.doubleJumpForce, ForceMode.Impulse);

        // Consumir salto
        jumpsRemaining--;
        hasDoubleJumped = true;
        lastDoubleJumpTime = Time.time;

        // Iniciar cooldown
        StartCooldown();

        Debug.Log($"[ChestAbility] Double Jump ejecutado | " +
                 $"Fuerza: {partData.doubleJumpForce:F1} | " +
                 $"Velocidad horizontal conservada: {horizontalVelocity.magnitude:F2} m/s | " +
                 $"Saltos restantes: {jumpsRemaining}");
    }

    protected override float GetCooldownDuration()
    {
        return partData.doubleJumpCooldown;
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
    //    float panelHeight = 160f;
    //    float padding = 10f;

    //    GUILayout.BeginArea(new Rect(Screen.width - panelWidth - padding, 640, panelWidth, panelHeight), boxStyle);

    //    GUILayout.Label("=== CHEST ABILITY (DOUBLE JUMP) ===", labelStyle);
    //    GUILayout.Space(5);

    //    // Estado de aire
    //    bool inAir = !playerLocomotion.IsGrounded();
    //    labelStyle.normal.textColor = inAir ? Color.yellow : Color.white;
    //    GUILayout.Label($"In Air: {(inAir ? "YES" : "NO")}", labelStyle);

    //    // Bonus pasivo
    //    labelStyle.normal.textColor = Color.cyan;
    //    GUILayout.Label($"Air Control: {(partData.airVelocityConservation * 100f):F0}%", labelStyle);

    //    // Saltos disponibles
    //    if (inAir)
    //    {
    //        labelStyle.normal.textColor = jumpsRemaining > 0 ? Color.green : Color.red;
    //        GUILayout.Label($"Jumps Available: {jumpsRemaining}/{maxAirJumps}", labelStyle);
    //    }

    //    // Cooldown
    //    if (isOnCooldown)
    //    {
    //        labelStyle.normal.textColor = Color.red;
    //        GUILayout.Label($"Cooldown: {GetRemainingCooldown():F1}s", labelStyle);
    //        DrawProgressBar(GetCooldownProgress(), "Cooldown", Color.yellow);
    //    }
    //    else if (inAir && jumpsRemaining > 0)
    //    {
    //        labelStyle.normal.textColor = Color.green;
    //        GUILayout.Label("Double Jump: READY", labelStyle);
    //        DrawProgressBar(1f, "Ready", Color.green);
    //    }

    //    GUILayout.EndArea();
    //}

    //private void DrawProgressBar(float percent, string label, Color barColor)
    //{
    //    float barWidth = 250f;
    //    float barHeight = 20f;

    //    Rect backgroundRect = GUILayoutUtility.GetRect(barWidth, barHeight);

    //    GUI.DrawTexture(backgroundRect, MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f)));

    //    Rect fillRect = new Rect(
    //        backgroundRect.x,
    //        backgroundRect.y,
    //        backgroundRect.width * Mathf.Clamp01(percent),
    //        backgroundRect.height
    //    );
    //    GUI.DrawTexture(fillRect, MakeTex(2, 2, barColor));

    //    GUIStyle percentStyle = new GUIStyle(GUI.skin.label);
    //    percentStyle.alignment = TextAnchor.MiddleCenter;
    //    percentStyle.fontStyle = FontStyle.Bold;
    //    percentStyle.normal.textColor = Color.white;
    //    GUI.Label(backgroundRect, $"{label}: {(percent * 100f):F0}%", percentStyle);
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