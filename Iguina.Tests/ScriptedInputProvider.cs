using Iguina.Defs;
using Iguina.Drivers;

namespace Iguina.Tests
{
    /// <summary>
    /// Input provider we can control.
    /// </summary>
    public class ScriptedInputProvider : IInputProvider
    {
        public Point MousePosition;
        public bool LeftDown;
        public List<TextInputCommands> Commands = new();

        public Point GetMousePosition() => MousePosition;
        public bool IsMouseButtonDown(MouseButton btn) => (btn == MouseButton.Left) && LeftDown;
        public int GetMouseWheelChange() => 0;
        public int[] GetTextInput() => [];
        public TextInputCommands[] GetTextInputCommands()
        {
            var ret = Commands.ToArray();
            Commands.Clear();
            return ret;
        }
        public KeyboardInteractions? GetKeyboardInteraction() => null;
    }
}
