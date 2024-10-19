using Iguina.Defs;
using Iguina.Drivers;

namespace Iguina.Tests
{
    public class TestInputProvider : IInputProvider
    {
        public Point GetMousePosition()
        {
            return new Point(200, 100);
        }

        public bool IsMouseButtonDown(MouseButton btn)
        {
            return false;
        }

        public int GetMouseWheelChange()
        {
            return 0;
        }

        public int[] GetTextInput()
        {
            return [];
        }

        public TextInputCommands[] GetTextInputCommands()
        {
            return [];
        }

        public KeyboardInteractions? GetKeyboardInteraction()
        {
            return null;
        }
    }
}