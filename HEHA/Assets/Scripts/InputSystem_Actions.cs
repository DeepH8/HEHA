// Hand-maintained wrapper for Assets/InputSystem_Actions.inputactions.
// Assign the .inputactions asset on PlayerInput; controllers construct this from playerInput.actions.
using UnityEngine.InputSystem;

public class InputSystem_Actions
{
    public InputActionAsset Asset { get; }

    readonly InputActionMap m_Player;
    readonly InputAction m_Player_Move;
    readonly InputAction m_Player_Look;
    readonly InputAction m_Player_Jump;
    readonly InputAction m_Player_Sprint;

    public InputSystem_Actions(InputActionAsset asset)
    {
        Asset = asset;
        m_Player = asset.FindActionMap("Player", true);
        m_Player_Move = m_Player.FindAction("Move", true);
        m_Player_Look = m_Player.FindAction("Look", true);
        m_Player_Jump = m_Player.FindAction("Jump", true);
        m_Player_Sprint = m_Player.FindAction("Sprint", true);
    }

    public void Enable() => Asset.Enable();
    public void Disable() => Asset.Disable();

    public PlayerActions Player => new PlayerActions(this);

    public struct PlayerActions
    {
        readonly InputSystem_Actions m_Wrapper;

        public PlayerActions(InputSystem_Actions wrapper) => m_Wrapper = wrapper;

        public InputAction Move => m_Wrapper.m_Player_Move;
        public InputAction Look => m_Wrapper.m_Player_Look;
        public InputAction Jump => m_Wrapper.m_Player_Jump;
        public InputAction Sprint => m_Wrapper.m_Player_Sprint;

        public InputActionMap Get() => m_Wrapper.m_Player;

        public void Enable() => Get().Enable();
        public void Disable() => Get().Disable();
        public bool enabled => Get().enabled;
    }
}
