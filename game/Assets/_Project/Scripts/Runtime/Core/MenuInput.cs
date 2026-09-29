using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    // The controls in a menu (the title screen, the pause menu): up/down/left/right (keys, d-pad or stick, repeating
    // while held), confirm, back, and pause. Works on real time, so it runs while the game is paused.
    public sealed class MenuInput : System.IDisposable
    {
        readonly InputAction move, confirm, back, pause, delete;
        Vector2Int held;
        float nextRepeat;

        public Vector2Int Step { get; private set; } // this frame's step: (x, y), y +1 up
        public bool Confirm => confirm.WasPressedThisFrame();
        public bool Back => back.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool Delete => delete.WasPressedThisFrame();

        public MenuInput()
        {
            move = new InputAction("MenuMove", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
            confirm = Button("<Keyboard>/e", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Gamepad>/buttonSouth");
            back = Button("<Keyboard>/escape", "<Keyboard>/backspace", "<Gamepad>/buttonEast");
            pause = Button("<Keyboard>/escape", "<Gamepad>/start");
            delete = Button("<Keyboard>/delete", "<Keyboard>/x", "<Gamepad>/buttonWest");
            foreach (var a in new[] { move, confirm, back, pause, delete })
                a.Enable();
        }

        static InputAction Button(params string[] bindings)
        {
            var a = new InputAction(type: InputActionType.Button);
            foreach (var b in bindings)
                a.AddBinding(b);
            return a;
        }

        // Once a frame: turns the held direction into steps (the first at once, then repeating while held).
        public void Tick()
        {
            Vector2 v = move.ReadValue<Vector2>();
            var dir = new Vector2Int(Mathf.Abs(v.x) > 0.5f ? (int)Mathf.Sign(v.x) : 0, Mathf.Abs(v.y) > 0.5f ? (int)Mathf.Sign(v.y) : 0);
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) dir.y = 0; else dir.x = 0;
            Step = Vector2Int.zero;
            if (dir == Vector2Int.zero)
                held = Vector2Int.zero;
            else if (dir != held)
            {
                held = dir;
                Step = dir;
                nextRepeat = Time.unscaledTime + 0.4f;
            }
            else if (Time.unscaledTime >= nextRepeat)
            {
                Step = dir;
                nextRepeat = Time.unscaledTime + 0.12f;
            }
        }

        public void Dispose()
        {
            foreach (var a in new[] { move, confirm, back, pause, delete })
                a.Dispose();
        }
    }
}
