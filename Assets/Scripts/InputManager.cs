using UnityEngine;
using InputSystem = UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private void Update()
    {
        if(InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Left mouse button pressed");
        }
    }
}
