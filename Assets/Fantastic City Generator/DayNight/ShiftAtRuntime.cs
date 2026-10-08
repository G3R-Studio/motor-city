using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
Just an example of how to switch day and night at runtime
Requires DayNight prefab to be in scene (Hierarchy)
*/

public class ShiftAtRuntime : MonoBehaviour
{
    DayNight dayNight;

    private void Start()
    {

        dayNight = FindObjectOfType<DayNight>();

    }

    private void Update()
    {

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.nKey.wasPressedThisFrame)
        {
            if (dayNight)
            {
                dayNight.isNight = !dayNight.isNight;
                dayNight.ChangeMaterial();

            }
        }

    }

}
