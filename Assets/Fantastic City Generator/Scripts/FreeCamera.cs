using UnityEngine;
using UnityEngine.InputSystem;

namespace FCG
{

    public class FreeCamera : MonoBehaviour
    {
        public float speedNormal = 10.0f;
        public float speedFast = 50.0f;

        public float mouseSensitivityX = 5.0f;
        public float mouseSensitivityY = 5.0f;

        float rotY = 0.0f;
        float forwardAxis;
        float strafeAxis;

        void Start()
        {
            if (GetComponent<Rigidbody>())
                GetComponent<Rigidbody>().freezeRotation = true;
        }

        void Update()
        {
            // rotation        
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                float rotX = transform.localEulerAngles.y + (mouse.delta.ReadValue().x * 0.1f) * mouseSensitivityX;
                rotY += (mouse.delta.ReadValue().y * 0.1f) * mouseSensitivityY;
                rotY = Mathf.Clamp(rotY, -89.5f, 89.5f);
                transform.localEulerAngles = new Vector3(-rotY, rotX, 0.0f);
            }

            Keyboard keyboard = Keyboard.current;
            float forwardTarget =
                (Held(keyboard, Key.W) || Held(keyboard, Key.UpArrow) ? 1f : 0f) -
                (Held(keyboard, Key.S) || Held(keyboard, Key.DownArrow) ? 1f : 0f);
            float strafeTarget =
                (Held(keyboard, Key.D) || Held(keyboard, Key.RightArrow) ? 1f : 0f) -
                (Held(keyboard, Key.A) || Held(keyboard, Key.LeftArrow) ? 1f : 0f);

            // Preserve the Input Manager's default Horizontal/Vertical
            // acceleration and deceleration rate of 3 per second.
            forwardAxis = Mathf.MoveTowards(forwardAxis, forwardTarget, 3f * Time.deltaTime);
            strafeAxis = Mathf.MoveTowards(strafeAxis, strafeTarget, 3f * Time.deltaTime);
            float forward = forwardAxis;
            float strafe = strafeAxis;

            // move forwards/backwards
            if (forward != 0.0f)
            {
                float speed = Held(keyboard, Key.LeftShift) ? speedFast : speedNormal;
                Vector3 trans = new Vector3(0.0f, 0.0f, forward * speed * Time.deltaTime);
                gameObject.transform.localPosition += gameObject.transform.localRotation * trans;
            }

            // strafe left/right
            if (strafe != 0.0f)
            {
                float speed = Held(keyboard, Key.LeftShift) ? speedFast : speedNormal;
                Vector3 trans = new Vector3(strafe * speed * Time.deltaTime, 0.0f, 0.0f);
                gameObject.transform.localPosition += gameObject.transform.localRotation * trans;
            }
        }


        private static bool Held(Keyboard keyboard, Key key)
        {
            return keyboard != null && keyboard[key].isPressed;
        }
    }
}
