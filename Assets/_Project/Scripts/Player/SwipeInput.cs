using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WannabeSkattebo
{
    public enum SwipeDir { None, Left, Right, Up, Down }

    /// <summary>
    /// Swipe detection for touch (and mouse drag in the editor) plus arrow/WASD/space keys.
    /// Works with either the new Input System or the legacy Input Manager.
    /// </summary>
    public class SwipeInput
    {
        Vector2 _start;
        bool _tracking;
        bool _consumed;

        float Threshold => Screen.dpi > 0 ? Screen.dpi * 0.2f : 50f;

        public SwipeDir Poll()
        {
            var key = PollKeyboard();
            if (key != SwipeDir.None) return key;

            if (!ReadPointer(out bool pressed, out Vector2 pos))
            {
                _tracking = false;
                return SwipeDir.None;
            }

            if (!pressed)
            {
                _tracking = false;
                return SwipeDir.None;
            }

            if (!_tracking)
            {
                _tracking = true;
                _consumed = false;
                _start = pos;
                return SwipeDir.None;
            }

            if (_consumed) return SwipeDir.None;

            Vector2 d = pos - _start;
            if (d.magnitude < Threshold) return SwipeDir.None;
            _consumed = true;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? SwipeDir.Right : SwipeDir.Left;
            return d.y > 0 ? SwipeDir.Up : SwipeDir.Down;
        }

#if ENABLE_INPUT_SYSTEM
        static SwipeDir PollKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return SwipeDir.None;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return SwipeDir.Left;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return SwipeDir.Right;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) return SwipeDir.Up;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return SwipeDir.Down;
            return SwipeDir.None;
        }

        static bool ReadPointer(out bool pressed, out Vector2 pos)
        {
            var ts = Touchscreen.current;
            if (ts != null)
            {
                pressed = ts.primaryTouch.press.isPressed;
                pos = ts.primaryTouch.position.ReadValue();
                if (pressed) return true;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                pressed = mouse.leftButton.isPressed;
                pos = mouse.position.ReadValue();
                return true;
            }
            pressed = false;
            pos = default;
            return ts != null;
        }
#else
        static SwipeDir PollKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return SwipeDir.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return SwipeDir.Right;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) return SwipeDir.Up;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return SwipeDir.Down;
            return SwipeDir.None;
        }

        static bool ReadPointer(out bool pressed, out Vector2 pos)
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pressed = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                pos = t.position;
                return true;
            }
            pressed = Input.GetMouseButton(0);
            pos = Input.mousePosition;
            return true;
        }
#endif
    }
}
