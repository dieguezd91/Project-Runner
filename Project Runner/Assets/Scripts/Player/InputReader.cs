using UnityEngine;
using UnityEngine.InputSystem;
using System;

[CreateAssetMenu(fileName = "InputReader", menuName = "Project Runner/Input Reader")]
public class InputReader : ScriptableObject, PlayerControls.IPlayerActions
{
    public event Action OnJumpPerformed;
    public event Action OnJumpCanceled;
    public event Action OnDriftPerformed;
    public event Action OnDriftCanceled;
    public event Action OnDashPerformed;
    public event Action OnStompPerformed;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private PlayerControls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Player.SetCallbacks(this);
        }
        controls.Player.Enable();
    }

    private void OnDisable() => controls.Player.Disable();

    public void OnMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookInput = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) OnJumpPerformed?.Invoke();
        if (context.canceled) OnJumpCanceled?.Invoke();
    }

    public void OnDrift(InputAction.CallbackContext context)
    {
        if (context.performed) OnDriftPerformed?.Invoke();
        if (context.canceled) OnDriftCanceled?.Invoke();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) OnDashPerformed?.Invoke();
    }

    public void OnStomp(InputAction.CallbackContext context)
    {
        if (context.performed) OnStompPerformed?.Invoke();
    }
}