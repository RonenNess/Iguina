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
        public bool ShiftDown;
        public List<int> TextInput = new();
        public List<TextInputCommands> Commands = new();

        public Point GetMousePosition() => MousePosition;
        public bool IsMouseButtonDown(MouseButton btn) => (btn == MouseButton.Left) && LeftDown;
        public int GetMouseWheelChange() => 0;
        public int[] GetTextInput()
        {
            var ret = TextInput.ToArray();
            TextInput.Clear();
            return ret;
        }
        public bool IsTextSelectionKeyDown() => ShiftDown;
        public TextInputCommands[] GetTextInputCommands()
        {
            var ret = Commands.ToArray();
            Commands.Clear();
            return ret;
        }
        public KeyboardInteractions? GetKeyboardInteraction() => null;

        public bool CopyDown;
        public bool PasteDown;
        public string? Clipboard;
        public bool IsCopyCommand() => CopyDown;
        public bool IsPasteCommand() => PasteDown;
        public bool CutDown;
        public bool SelectAllDown;
        public bool IsCutCommand() => CutDown;
        public bool IsSelectAllCommand() => SelectAllDown;
        public string? GetClipboardText() => Clipboard;
        public void SetClipboardText(string text) => Clipboard = text;
    }
}
