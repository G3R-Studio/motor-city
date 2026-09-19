using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class AdminDebugPanel : MonoBehaviour
    {
        private const int WindowId = 73921;

        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private bool visible;
        private Rect windowRect =
            new(20f, 130f, 360f, 390f);

        public void Initialize(
            PlayerWallet playerWallet,
            PlayerReputation playerReputation)
        {
            wallet = playerWallet;
            reputation = playerReputation;
        }

        private void Update()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.f10Key.wasPressedThisFrame ||
                keyboard.backquoteKey.wasPressedThisFrame)
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            windowRect =
                GUI.Window(
                    WindowId,
                    windowRect,
                    DrawWindow,
                    "MOTOR CITY — ADMIN");
        }

        private void DrawWindow(
            int id)
        {
            GUILayout.Space(4f);

            GUILayout.Label(
                $"КР: {(wallet == null ? 0 : wallet.Credits):N0}");

            GUILayout.Label(
                $"REP: {(reputation == null ? 0 : reputation.Reputation):N0}   " +
                $"УР.: {(reputation == null ? 1 : reputation.Level)}");

            GUILayout.Space(8f);

            GUILayout.Label("ДЕНЬГИ");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "+1 000 КР",
                    GUILayout.Height(32f)))
            {
                wallet?.AddCredits(
                    1000);
            }

            if (GUILayout.Button(
                    "+10 000 КР",
                    GUILayout.Height(32f)))
            {
                wallet?.AddCredits(
                    10000);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "+100 000 КР",
                    GUILayout.Height(32f)))
            {
                wallet?.AddCredits(
                    100000);
            }

            if (GUILayout.Button(
                    "КР = 0",
                    GUILayout.Height(32f)))
            {
                wallet?.SetCredits(
                    0);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("РЕПУТАЦИЯ");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "+500 REP",
                    GUILayout.Height(32f)))
            {
                reputation?.AddReputation(
                    500);
            }

            if (GUILayout.Button(
                    "+2 500 REP",
                    GUILayout.Height(32f)))
            {
                reputation?.AddReputation(
                    2500);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "REP = 5 000",
                    GUILayout.Height(32f)))
            {
                reputation?.SetReputation(
                    5000);
            }

            if (GUILayout.Button(
                    "REP = 0",
                    GUILayout.Height(32f)))
            {
                reputation?.SetReputation(
                    0);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            if (GUILayout.Button(
                    "ОТКРЫТЬ ВСЕ МАШИНЫ ПО REP",
                    GUILayout.Height(36f)))
            {
                reputation?.SetReputation(
                    5000);
            }

            GUILayout.Space(8f);

            GUILayout.Label(
                "F10 / BACKQUOTE — закрыть панель");

            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    10000f,
                    24f));
        }
    }
}
