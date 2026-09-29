using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Input
{
    public enum MotorCityControlScheme
    {
        Keyboard = 0,
        Wheel = 1,
        Arrows = 2
    }

    public enum MotorCityInputAction
    {
        Interact = 0,
        Cancel = 1,
        Retry = 2,
        EliteModifier = 3,
        PreviousVehicle = 4,
        NextVehicle = 5,
        Upgrade1 = 6,
        Upgrade2 = 7,
        Upgrade3 = 8,
        CycleDriveMode = 9,
        Rescue = 10,
        AdminToggle = 11,
        Throttle = 12,
        Reverse = 13,
        SteerLeft = 14,
        SteerRight = 15,
        Handbrake = 16,
        // 17 intentionally unused: retired vehicle purchase action.
        // 18 intentionally unused: retired Pixie skin action.
        CycleBodyColor = 19,
        CycleSticker = 20,
        CycleVinyl = 21,
        CycleWheels = 22,
        CycleNeon = 23,
        CyclePlate = 24,
        TakePhoto = 27,
        ToggleVehiclePassport = 28,
        ToggleClub = 29,
        RewardedBonus = 30,
        ToggleStore = 31,
        ToggleNavigator = 32,
        Count = 33
    }

    public static class MotorCityInput
    {
        private static readonly bool[] VirtualHeld =
            new bool[(int)MotorCityInputAction.Count];

        private static readonly int[] VirtualPressedFrame =
            new int[(int)MotorCityInputAction.Count];

        private const string ControlSchemeKey =
            "MotorCity.Input.ControlScheme";

        private static bool preferTouchPrompts;
        private static float virtualSteering;
        private static MotorCityControlScheme controlScheme =
            MotorCityControlScheme.Keyboard;

        static MotorCityInput()
        {
            if (PlayerPrefs.HasKey(ControlSchemeKey))
            {
                controlScheme =
                    (MotorCityControlScheme)Mathf.Clamp(
                        PlayerPrefs.GetInt(
                            ControlSchemeKey,
                            0),
                        0,
                        2);

                preferTouchPrompts =
                    controlScheme !=
                    MotorCityControlScheme.Keyboard;
            }

            for (int i = 0;
                 i < VirtualPressedFrame.Length;
                 i++)
            {
                VirtualPressedFrame[i] =
                    int.MinValue;
            }
        }

        public static bool InteractPressed =>
            KeyPressed(
                Key.E) ||
            VirtualPressed(
                MotorCityInputAction.Interact);

        public static bool InteractHeld =>
            KeyHeld(
                Key.E) ||
            VirtualIsHeld(
                MotorCityInputAction.Interact);

        public static bool CancelPressed =>
            KeyPressed(
                Key.Escape) ||
            VirtualPressed(
                MotorCityInputAction.Cancel);

        public static bool RetryPressed =>
            KeyPressed(
                Key.Enter) ||
            KeyPressed(
                Key.NumpadEnter) ||
            VirtualPressed(
                MotorCityInputAction.Retry);

        public static bool EliteModifierHeld =>
            KeyHeld(
                Key.LeftShift) ||
            KeyHeld(
                Key.RightShift) ||
            VirtualIsHeld(
                MotorCityInputAction.EliteModifier) ||
            (PreferTouchPrompts &&
             VirtualIsHeld(
                 MotorCityInputAction.Handbrake));

        public static bool PreviousVehiclePressed =>
            VirtualPressed(
                MotorCityInputAction.PreviousVehicle);

        public static bool NextVehiclePressed =>
            VirtualPressed(
                MotorCityInputAction.NextVehicle);

        public static bool Upgrade1Pressed =>
            VirtualPressed(
                MotorCityInputAction.Upgrade1);

        public static bool Upgrade2Pressed =>
            VirtualPressed(
                MotorCityInputAction.Upgrade2);

        public static bool Upgrade3Pressed =>
            VirtualPressed(
                MotorCityInputAction.Upgrade3);

        public static bool CycleBodyColorPressed =>
            VirtualPressed(
                MotorCityInputAction.CycleBodyColor);

        public static bool CycleStickerPressed =>
            VirtualPressed(MotorCityInputAction.CycleSticker);

        public static bool CycleVinylPressed =>
            VirtualPressed(MotorCityInputAction.CycleVinyl);

        public static bool CycleWheelsPressed =>
            VirtualPressed(MotorCityInputAction.CycleWheels);

        public static bool CycleNeonPressed =>
            VirtualPressed(MotorCityInputAction.CycleNeon);

        public static bool CyclePlatePressed =>
            VirtualPressed(MotorCityInputAction.CyclePlate);



        public static bool TakePhotoPressed =>
            VirtualPressed(MotorCityInputAction.TakePhoto);

        public static bool ToggleVehiclePassportPressed =>
            VirtualPressed(MotorCityInputAction.ToggleVehiclePassport);

        public static bool ToggleClubPressed =>
            VirtualPressed(MotorCityInputAction.ToggleClub);

        public static bool RewardedBonusPressed =>
            VirtualPressed(MotorCityInputAction.RewardedBonus);

        public static bool ToggleStorePressed =>
            VirtualPressed(MotorCityInputAction.ToggleStore);

        public static bool ToggleNavigatorPressed =>
            VirtualPressed(MotorCityInputAction.ToggleNavigator);

        public static bool CycleDriveModePressed =>
            VirtualPressed(
                MotorCityInputAction.CycleDriveMode);

        public static bool RescuePressed =>
            VirtualPressed(
                MotorCityInputAction.Rescue);

        public static bool AdminTogglePressed =>
            KeyPressed(
                Key.F10) ||
            KeyPressed(
                Key.Backquote) ||
            VirtualPressed(
                MotorCityInputAction.AdminToggle);

        public static bool ThrottleHeld =>
            KeyHeld(
                Key.W) ||
            KeyHeld(
                Key.UpArrow) ||
            GamepadRightTrigger() ||
            VirtualIsHeld(
                MotorCityInputAction.Throttle);

        public static bool ReverseHeld =>
            KeyHeld(
                Key.S) ||
            KeyHeld(
                Key.DownArrow) ||
            GamepadLeftTrigger() ||
            VirtualIsHeld(
                MotorCityInputAction.Reverse);

        public static float SteeringAxis
        {
            get
            {
                float keyboard =
                    (KeyHeld(Key.D) ||
                     KeyHeld(Key.RightArrow)
                        ? 1f
                        : 0f) -
                    (KeyHeld(Key.A) ||
                     KeyHeld(Key.LeftArrow)
                        ? 1f
                        : 0f);

                float gamepad =
                    GamepadSteer();

                float digitalTouch =
                    (VirtualIsHeld(
                         MotorCityInputAction.SteerRight)
                        ? 1f
                        : 0f) -
                    (VirtualIsHeld(
                         MotorCityInputAction.SteerLeft)
                        ? 1f
                        : 0f);

                float best =
                    Mathf.Abs(gamepad) >
                    Mathf.Abs(keyboard)
                        ? gamepad
                        : keyboard;

                if (Mathf.Abs(virtualSteering) >
                    Mathf.Abs(best))
                {
                    best =
                        virtualSteering;
                }

                if (Mathf.Abs(digitalTouch) >
                    Mathf.Abs(best))
                {
                    best =
                        digitalTouch;
                }

                return Mathf.Clamp(
                    best,
                    -1f,
                    1f);
            }
        }

        public static bool SteerLeftHeld =>
            SteeringAxis < -0.08f;

        public static bool SteerRightHeld =>
            SteeringAxis > 0.08f;

        public static bool HandbrakeHeld =>
            KeyHeld(
                Key.Space) ||
            GamepadHandbrake() ||
            VirtualIsHeld(
                MotorCityInputAction.Handbrake);

        public static bool DrivingControlHeld =>
            ThrottleHeld ||
            ReverseHeld ||
            SteerLeftHeld ||
            SteerRightHeld ||
            HandbrakeHeld;

        public static bool PreferTouchPrompts =>
            preferTouchPrompts;

        public static MotorCityControlScheme CurrentControlScheme =>
            controlScheme;

        public static void SetControlScheme(
            MotorCityControlScheme scheme)
        {
            controlScheme =
                scheme;

            preferTouchPrompts =
                scheme !=
                MotorCityControlScheme.Keyboard;

            virtualSteering =
                0f;

            ClearVirtualState();

            PlayerPrefs.SetInt(
                ControlSchemeKey,
                (int)scheme);

            PlayerPrefs.Save();
        }

        public static void SetVirtualSteering(
            float value)
        {
            virtualSteering =
                Mathf.Clamp(
                    value,
                    -1f,
                    1f);
        }

        public static void RefreshTouchPromptPreference()
        {
            if (PlayerPrefs.HasKey(
                    ControlSchemeKey))
            {
                controlScheme =
                    (MotorCityControlScheme)Mathf.Clamp(
                        PlayerPrefs.GetInt(
                            ControlSchemeKey,
                            0),
                        0,
                        2);

                preferTouchPrompts =
                    controlScheme !=
                    MotorCityControlScheme.Keyboard;

                return;
            }

            bool touchCapable =
                Application.isMobilePlatform ||
                SystemInfo.deviceType ==
                    DeviceType.Handheld ||
                UnityEngine.Input.touchSupported ||
                Touchscreen.current != null;

#if UNITY_EDITOR
            // Device Simulator exposes the simulated device through
            // UnityEngine.Device.*. This method is called from runtime
            // initialization, never from MonoBehaviour field initializers.
            touchCapable =
                touchCapable ||
                UnityEngine.Device.Application.isMobilePlatform ||
                UnityEngine.Device.SystemInfo.deviceType ==
                    DeviceType.Handheld;
#endif

            preferTouchPrompts =
                touchCapable;
        }

        public static void SetTouchPromptPreference(
            bool enabled)
        {
            preferTouchPrompts =
                enabled;
        }

        public static void ClearVirtualState()
        {
            virtualSteering =
                0f;

            for (int i = 0;
                 i < VirtualHeld.Length;
                 i++)
            {
                VirtualHeld[i] =
                    false;

                VirtualPressedFrame[i] =
                    int.MinValue;
            }
        }

        public static void PressVirtual(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            if (!Valid(index))
                return;

            VirtualHeld[index] = true;
            VirtualPressedFrame[index] =
                Time.frameCount + 1;
        }

        public static void ReleaseVirtual(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            if (!Valid(index))
                return;

            VirtualHeld[index] = false;
        }

        public static void SetVirtualHeld(
            MotorCityInputAction action,
            bool held)
        {
            int index =
                (int)action;

            if (!Valid(index))
                return;

            if (held &&
                !VirtualHeld[index])
            {
                VirtualPressedFrame[index] =
                Time.frameCount + 1;
            }

            VirtualHeld[index] =
                held;
        }

        public static void PulseVirtual(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            if (!Valid(index))
                return;

            VirtualPressedFrame[index] =
                Time.frameCount + 1;
        }

        public static bool WasVirtualPressed(
            MotorCityInputAction action)
        {
            return
                VirtualPressed(
                    action);
        }

        public static bool VirtualIsHeld(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            return
                Valid(index) &&
                VirtualHeld[index];
        }

        private static bool VirtualPressed(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            return
                Valid(index) &&
                VirtualPressedFrame[index] ==
                Time.frameCount;
        }

        private static bool KeyPressed(
            Key key)
        {
            Keyboard keyboard =
                Keyboard.current;

            return
                keyboard != null &&
                keyboard[key].wasPressedThisFrame;
        }

        private static bool KeyHeld(
            Key key)
        {
            Keyboard keyboard =
                Keyboard.current;

            return
                keyboard != null &&
                keyboard[key].isPressed;
        }

        private static bool GamepadRightTrigger()
        {
            Gamepad gamepad =
                Gamepad.current;

            return
                gamepad != null &&
                gamepad.rightTrigger.ReadValue() >
                0.12f;
        }

        private static bool GamepadLeftTrigger()
        {
            Gamepad gamepad =
                Gamepad.current;

            return
                gamepad != null &&
                gamepad.leftTrigger.ReadValue() >
                0.12f;
        }

        private static float GamepadSteer()
        {
            Gamepad gamepad =
                Gamepad.current;

            return
                gamepad == null
                    ? 0f
                    : gamepad.leftStick.x.ReadValue();
        }

        private static bool GamepadHandbrake()
        {
            Gamepad gamepad =
                Gamepad.current;

            return
                gamepad != null &&
                gamepad.buttonSouth.isPressed;
        }

        private static bool Valid(
            int index)
        {
            return
                index >= 0 &&
                index <
                VirtualHeld.Length;
        }
    }
}
