using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    // What the Apprentice reads each frame. Tests swap in a ScriptedInput to play the game headless.
    public interface IApprenticeInput
    {
        Vector2 Move { get; }
        bool DashPressed { get; }
        bool LightPressed { get; }
        bool HeavyPressed { get; }
        bool HealPressed { get; }
        bool InteractPressed { get; }
        bool LockPressed { get; }
        bool HookHeld { get; } // hold to aim the Hook, release to throw it
        bool BlockHeld { get; }
        int SwitchTarget { get; } // -1 left, +1 right, 0 none (a flick or a scroll, reported once)
        int BeltStep { get; }         // the belt: -1 / +1 to choose the one before / after (d-pad or arrow left, right), 0 none
        bool BeltUsePressed { get; }  // use the chosen one (d-pad or arrow up)
    }

    // Keyboard/mouse and gamepad bindings (gamepad-first).
    public sealed class GameInput : IApprenticeInput, System.IDisposable
    {
        readonly InputAction move, dash, lightAttack, heavyAttack, heal, interact, lockOn, block, hook, switchStick, switchScroll;
        readonly InputAction[] beltSlots;
        bool stickArmed = true;

        public GameInput()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick"); // (not the arrow keys: they're the belt)
            dash = Button("Dash", "<Keyboard>/space", "<Gamepad>/buttonEast");
            lightAttack = Button("Light", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            heavyAttack = Button("Heavy", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/buttonNorth", "<Gamepad>/rightTrigger");
            heal = Button("Heal", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            lockOn = Button("LockOn", "<Keyboard>/tab", "<Mouse>/middleButton", "<Gamepad>/rightStickPress");
            block = Button("Block", "<Keyboard>/l", "<Gamepad>/leftTrigger");
            hook = Button("Hook", "<Keyboard>/f", "<Gamepad>/rightShoulder");
            switchStick = new InputAction("SwitchStick", InputActionType.Value, "<Gamepad>/rightStick/x");
            switchScroll = new InputAction("SwitchScroll", InputActionType.Value, "<Mouse>/scroll/y");
            beltSlots = new[]
            {
                Button("Belt Use", "<Keyboard>/upArrow", "<Gamepad>/dpad/up"),
                Button("Belt Next", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right"),
                Button("Belt Before", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left"),
            };
            foreach (var action in All)
                action.Enable();
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var binding in bindings)
                action.AddBinding(binding);
            return action;
        }

        public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        public bool DashPressed => dash.WasPressedThisFrame();
        public bool LightPressed => lightAttack.WasPressedThisFrame();
        public bool HeavyPressed => heavyAttack.WasPressedThisFrame();
        public bool HealPressed => heal.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool LockPressed => lockOn.WasPressedThisFrame();
        public bool BlockHeld => block.IsPressed();
        public bool HookHeld => hook.IsPressed();

        public bool BeltUsePressed => beltSlots[0].WasPressedThisFrame();
        public int BeltStep => beltSlots[1].WasPressedThisFrame() ? 1 : beltSlots[2].WasPressedThisFrame() ? -1 : 0;

        public int SwitchTarget
        {
            get
            {
                // Right stick: a flick past 0.7 switches once; it must return near centre before the next.
                float x = switchStick.ReadValue<float>();
                if (stickArmed && Mathf.Abs(x) > 0.7f)
                {
                    stickArmed = false;
                    return x > 0f ? 1 : -1;
                }
                if (Mathf.Abs(x) < 0.3f)
                    stickArmed = true;
                float scroll = switchScroll.ReadValue<float>();
                return scroll > 0.1f ? 1 : scroll < -0.1f ? -1 : 0;
            }
        }

        InputAction[] All
        {
            get
            {
                var list = new System.Collections.Generic.List<InputAction> { move, dash, lightAttack, heavyAttack, heal, interact, lockOn, block, hook, switchStick, switchScroll };
                list.AddRange(beltSlots);
                return list.ToArray();
            }
        }

        public void Dispose()
        {
            foreach (var action in All)
                action.Dispose();
        }
    }

    // Input driven from code: set a value and it's read once (presses) or held (Move).
    public sealed class ScriptedInput : IApprenticeInput
    {
        public Vector2 Move { get; set; }
        bool dash, lightAttack, heavyAttack, heal, interact, lockOn;
        int switchTarget;

        public void PressDash() => dash = true;
        public void PressLight() => lightAttack = true;
        public void PressHeavy() => heavyAttack = true;
        public void PressHeal() => heal = true;
        public void PressInteract() => interact = true;
        public void PressLock() => lockOn = true;
        public void Switch(int direction) => switchTarget = direction;
        public void PressBeltUse() => beltUse = true;
        public void StepBelt(int by) => beltStep = by;
        bool beltUse;
        int beltStep;

        public bool BeltUsePressed => Take(ref beltUse);
        public int BeltStep
        {
            get
            {
                int value = beltStep;
                beltStep = 0;
                return value;
            }
        }

        public bool DashPressed => Take(ref dash);
        public bool LightPressed => Take(ref lightAttack);
        public bool HeavyPressed => Take(ref heavyAttack);
        public bool HealPressed => Take(ref heal);
        public bool InteractPressed => Take(ref interact);
        public bool LockPressed => Take(ref lockOn);
        public bool HookHeld { get; set; }
        public bool BlockHeld { get; set; }

        public int SwitchTarget
        {
            get
            {
                int value = switchTarget;
                switchTarget = 0;
                return value;
            }
        }

        static bool Take(ref bool flag)
        {
            bool value = flag;
            flag = false;
            return value;
        }
    }
}
