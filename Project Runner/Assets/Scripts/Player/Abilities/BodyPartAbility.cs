using UnityEngine;

public abstract class BodyPartAbility : MonoBehaviour
{
    protected BodyPartData partData;
    protected PlayerLocomotion playerLocomotion;
    protected Rigidbody rb;

    protected float lastUseTime = -999f;
    protected bool isOnCooldown => Time.time < lastUseTime + GetCooldownDuration();

    protected bool isEnabled = true;

    public virtual void Initialize(BodyPartData data)
    {
        partData = data;
        playerLocomotion = GetComponent<PlayerLocomotion>();
        rb = GetComponent<Rigidbody>();

        OnInitialize();
    }

    protected virtual void OnInitialize() { }

    protected bool CanUseAbility()
    {
        return isEnabled && !isOnCooldown && CheckCustomConditions();
    }

    protected virtual bool CheckCustomConditions()
    {
        return true;
    }

    protected void StartCooldown()
    {
        lastUseTime = Time.time;
    }

    public float GetCooldownProgress()
    {
        if (!isOnCooldown)
            return 1f;

        float elapsed = Time.time - lastUseTime;
        return Mathf.Clamp01(elapsed / GetCooldownDuration());
    }

    public float GetRemainingCooldown()
    {
        if (!isOnCooldown)
            return 0f;

        return Mathf.Max(0f, GetCooldownDuration() - (Time.time - lastUseTime));
    }

    protected virtual float GetCooldownDuration()
    {
        return 2f; // Default 2 segundos
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }
}