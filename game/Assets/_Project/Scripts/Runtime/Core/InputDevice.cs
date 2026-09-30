using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MyGame
{
    // Which controls the player last touched, a gamepad or the keyboard and mouse, so every hint shows only those keys
    // (the prompts, the menus, the character screen, the pause menu's controls). It starts on the gamepad if one is
    // plugged in. Anything drawing hints calls Track once a frame (it only looks once a frame, however many ask).
    public static class InputDevice
    {
        public static bool Gamepad { get; private set; } = UnityEngine.InputSystem.Gamepad.current != null;
        static int trackedFrame = -1;

        // (tests, and when nothing's been touched yet)
        public static void Set(bool gamepad) => Gamepad = gamepad;

        public static void Track()
        {
            if (trackedFrame == Time.frameCount)
                return;
            trackedFrame = Time.frameCount;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.rightStick.ReadValue().sqrMagnitude > 0.25f || AnyButtonPressed(pad)))
                Gamepad = true;
            else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                     || (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)))
                Gamepad = false;
        }

        static bool AnyButtonPressed(Gamepad pad)
        {
            foreach (var control in pad.allControls)
                if (control is ButtonControl button && button.wasPressedThisFrame)
                    return true;
            return false;
        }

        // The one for the controls in use: "E" or "A".
        public static string Pick(string keys, string pad) => Gamepad ? pad : keys;
    }
}
