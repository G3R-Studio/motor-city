using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Input
{
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
        BuyVehicle = 17,
        CyclePetSkin = 18,
        CycleBodyColor = 19,
        CycleSticker = 20,
        CycleVinyl = 21,
        CycleWheels = 22,
        CycleNeon = 23,
        CyclePlate = 24,
        SaveCustomizationPreset = 25,
        LoadCustomizationPreset = 26,
        TakePhoto = 27,
        ToggleVehiclePassport = 28,
        ToggleClub = 29,
        RewardedBonus = 30,
        ToggleStore = 31,
        ToggleNavigator = 32
    }

    public static class MotorCityInput
    {
        private static readonly bool[] VirtualHeld =
            new bool[33];

        private static readonly int[] VirtualPressedFrame =
            new int[33];

        static MotorCityInput()
        {
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
                MotorCityInputAction.EliteModifier);

        public static bool PreviousVehiclePressed =>
            KeyPressed(
                Key.Z) ||
            VirtualPressed(
                MotorCityInputAction.PreviousVehicle);

        public static bool NextVehiclePressed =>
            KeyPressed(
                Key.X) ||
            VirtualPressed(
                MotorCityInputAction.NextVehicle);

        public static bool Upgrade1Pressed =>
            KeyPressed(
                Key.Digit1) ||
            KeyPressed(
                Key.Numpad1) ||
            VirtualPressed(
                MotorCityInputAction.Upgrade1);

        public static bool Upgrade2Pressed =>
            KeyPressed(
                Key.Digit2) ||
            KeyPressed(
                Key.Numpad2) ||
            VirtualPressed(
                MotorCityInputAction.Upgrade2);

        public static bool Upgrade3Pressed =>
            KeyPressed(
                Key.Digit3) ||
            KeyPressed(
                Key.Numpad3) ||
            VirtualPressed(
                MotorCityInputAction.Upgrade3);

        public static bool BuyVehiclePressed =>
            KeyPressed(
                Key.B) ||
            VirtualPressed(
                MotorCityInputAction.BuyVehicle);

        public static bool CyclePetSkinPressed =>
            KeyPressed(
                Key.C) ||
            VirtualPressed(
                MotorCityInputAction.CyclePetSkin);

        public static bool CycleBodyColorPressed =>
            KeyPressed(
                Key.V) ||
            VirtualPressed(
                MotorCityInputAction.CycleBodyColor);

        public static bool CycleStickerPressed =>
            KeyPressed(Key.G) ||
            VirtualPressed(MotorCityInputAction.CycleSticker);

        public static bool CycleVinylPressed =>
            KeyPressed(Key.H) ||
            VirtualPressed(MotorCityInputAction.CycleVinyl);

        public static bool CycleWheelsPressed =>
            KeyPressed(Key.J) ||
            VirtualPressed(MotorCityInputAction.CycleWheels);

        public static bool CycleNeonPressed =>
            KeyPressed(Key.N) ||
            VirtualPressed(MotorCityInputAction.CycleNeon);

        public static bool CyclePlatePressed =>
            KeyPressed(Key.L) ||
            VirtualPressed(MotorCityInputAction.CyclePlate);

        public static bool SaveCustomizationPresetPressed =>
            KeyPressed(Key.F5) ||
            VirtualPressed(MotorCityInputAction.SaveCustomizationPreset);

        public static bool LoadCustomizationPresetPressed =>
            KeyPressed(Key.F6) ||
            VirtualPressed(MotorCityInputAction.LoadCustomizationPreset);

        public static bool TakePhotoPressed =>
            KeyPressed(Key.P) ||
            VirtualPressed(MotorCityInputAction.TakePhoto);

        public static bool ToggleVehiclePassportPressed =>
            KeyPressed(Key.K) ||
            VirtualPressed(MotorCityInputAction.ToggleVehiclePassport);

        public static bool ToggleClubPressed =>
            KeyPressed(Key.U) ||
            VirtualPressed(MotorCityInputAction.ToggleClub);

        public static bool RewardedBonusPressed =>
            KeyPressed(Key.Y) ||
            VirtualPressed(MotorCityInputAction.RewardedBonus);

        public static bool ToggleStorePressed =>
            KeyPressed(Key.T) ||
            VirtualPressed(MotorCityInputAction.ToggleStore);

        public static bool ToggleNavigatorPressed =>
            KeyPressed(Key.M) ||
            VirtualPressed(MotorCityInputAction.ToggleNavigator);

        public static bool CycleDriveModePressed =>
            KeyPressed(
                Key.Q) ||
            VirtualPressed(
                MotorCityInputAction.CycleDriveMode);

        public static bool RescuePressed =>
            KeyPressed(
                Key.R) ||
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

        public static bool SteerLeftHeld =>
            KeyHeld(
                Key.A) ||
            KeyHeld(
                Key.LeftArrow) ||
            GamepadSteer() < -0.16f ||
            VirtualIsHeld(
                MotorCityInputAction.SteerLeft);

        public static bool SteerRightHeld =>
            KeyHeld(
                Key.D) ||
            KeyHeld(
                Key.RightArrow) ||
            GamepadSteer() > 0.16f ||
            VirtualIsHeld(
                MotorCityInputAction.SteerRight);

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

        public static void PressVirtual(
            MotorCityInputAction action)
        {
            int index =
                (int)action;

            if (!Valid(index))
                return;

            VirtualHeld[index] = true;
            VirtualPressedFrame[index] =
                Time.frameCount;
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
                    Time.frameCount;
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
                Time.frameCount;
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
