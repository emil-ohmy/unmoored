using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame
{
    // What the Apprentice reads each frame. Tests swap in a ScriptedInput to play the game headless.
    public interface IApprenticeInput
    {
        Vector2 Move { get; }
        bool RollPressed { get; }
        bool LightPressed { get; }
        bool HeavyPressed { get; }
        bool HealPressed { get; }
        bool InteractPressed { get; }
    }

    // Keyboard/mouse and gamepad bindings (gamepad-first).
    public sealed class GameInput : IApprenticeInput, System.IDisposable
    {
        readonly InputAction move, roll, lightAttack, heavyAttack, heal, interact;

        public GameInput()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            roll = Button("Roll", "<Keyboard>/space", "<Gamepad>/buttonEast");
            lightAttack = Button("Light", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest", "<Gamepad>/rightShoulder");
            heavyAttack = Button("Heavy", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/buttonNorth", "<Gamepad>/rightTrigger");
            heal = Button("Heal", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            foreach (var action in new[] { move, roll, lightAttack, heavyAttack, heal, interact })
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
        public bool RollPressed => roll.WasPressedThisFrame();
        public bool LightPressed => lightAttack.WasPressedThisFrame();
        public bool HeavyPressed => heavyAttack.WasPressedThisFrame();
        public bool HealPressed => heal.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();

        public void Dispose()
        {
            foreach (var action in new[] { move, roll, lightAttack, heavyAttack, heal, interact })
                action.Dispose();
        }
    }

    // Input driven from code: set a value and it's read once (presses) or held (Move).
    public sealed class ScriptedInput : IApprenticeInput
    {
        public Vector2 Move { get; set; }
        bool roll, lightAttack, heavyAttack, heal, interact;

        public void PressRoll() => roll = true;
        public void PressLight() => lightAttack = true;
        public void PressHeavy() => heavyAttack = true;
        public void PressHeal() => heal = true;
        public void PressInteract() => interact = true;

        public bool RollPressed => Take(ref roll);
        public bool LightPressed => Take(ref lightAttack);
        public bool HeavyPressed => Take(ref heavyAttack);
        public bool HealPressed => Take(ref heal);
        public bool InteractPressed => Take(ref interact);

        static bool Take(ref bool flag)
        {
            bool value = flag;
            flag = false;
            return value;
        }
    }
}
