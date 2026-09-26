using Iguina.Defs;
using Iguina.Drivers;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Iguina.Demo.MonoGame
{
    /// <summary>
    /// Provide input for GUI.
    /// </summary>
    internal class MonoGameInput : Drivers.IInputProvider
    {
        int _lastWheelValue;

        private List<int> _textInput = new();
        private const double InitialDelay = 0.45; // Initial delay before repeating input
        private const double RepeatRate = 0.045;  // Rate of repeated input
        private double[] _charsDelay = new double[255];
        Keys[] _lastPressedKeys = new Keys[0];

        public void StartFrame(Microsoft.Xna.Framework.GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var keysPressed = keyboardState.GetPressedKeys();

            // get keyboard text input
            {
                HashSet<Keys> _lastKeys = new(_lastPressedKeys);
                _textInput.Clear();
                var currSeconds = gameTime.TotalGameTime.TotalSeconds;

                foreach (var key in keysPressed)
                {
                    char keyChar = ConvertKeyToChar(key, keyboardState);
                    int keyCharForDelay = (int)keyChar.ToString().ToLower()[0];
                    if (keyChar != '\0')
                    {
                        if ((currSeconds > _charsDelay[keyCharForDelay]) || (!_lastPressedKeys.Contains(key)))
                        {
                            _textInput.Add(keyChar);
                            _charsDelay[keyCharForDelay] = currSeconds + (_lastKeys.Contains(key) ? RepeatRate : InitialDelay);
                        }
                    }
                }
                _lastPressedKeys = keysPressed;
            }
        }

        public void EndFrame()
        {
            var mouse = Mouse.GetState();
            _lastWheelValue = mouse.ScrollWheelValue;
        }

        public Point GetMousePosition()
        {
            var mouse =Mouse.GetState();
            return new Point(mouse.X, mouse.Y);
        }

        public bool IsMouseButtonDown(MouseButton btn)
        {
            var mouse =Mouse.GetState();
            switch (btn)
            {
                case MouseButton.Left: return mouse.LeftButton == ButtonState.Pressed;
                case MouseButton.Right: return mouse.RightButton == ButtonState.Pressed;
                case MouseButton.Wheel: return mouse.MiddleButton == ButtonState.Pressed;
            }
            return false;
        }

        public int GetMouseWheelChange()
        {
            var mouse =Mouse.GetState();
            return Math.Sign(mouse.ScrollWheelValue - _lastWheelValue);
        }


        public int[] GetTextInput()
        {
            return _textInput.ToArray();
        }


        private char ConvertKeyToChar(Keys key, KeyboardState state)
        {
            bool shift = state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
            bool capsLock = state.CapsLock;

            switch (key)
            {
                case Keys.A: return shift ^ capsLock ? 'A' : 'a';
                case Keys.B: return shift ^ capsLock ? 'B' : 'b';
                case Keys.C: return shift ^ capsLock ? 'C' : 'c';
                case Keys.D: return shift ^ capsLock ? 'D' : 'd';
                case Keys.E: return shift ^ capsLock ? 'E' : 'e';
                case Keys.F: return shift ^ capsLock ? 'F' : 'f';
                case Keys.G: return shift ^ capsLock ? 'G' : 'g';
                case Keys.H: return shift ^ capsLock ? 'H' : 'h';
                case Keys.I: return shift ^ capsLock ? 'I' : 'i';
                case Keys.J: return shift ^ capsLock ? 'J' : 'j';
                case Keys.K: return shift ^ capsLock ? 'K' : 'k';
                case Keys.L: return shift ^ capsLock ? 'L' : 'l';
                case Keys.M: return shift ^ capsLock ? 'M' : 'm';
                case Keys.N: return shift ^ capsLock ? 'N' : 'n';
                case Keys.O: return shift ^ capsLock ? 'O' : 'o';
                case Keys.P: return shift ^ capsLock ? 'P' : 'p';
                case Keys.Q: return shift ^ capsLock ? 'Q' : 'q';
                case Keys.R: return shift ^ capsLock ? 'R' : 'r';
                case Keys.S: return shift ^ capsLock ? 'S' : 's';
                case Keys.T: return shift ^ capsLock ? 'T' : 't';
                case Keys.U: return shift ^ capsLock ? 'U' : 'u';
                case Keys.V: return shift ^ capsLock ? 'V' : 'v';
                case Keys.W: return shift ^ capsLock ? 'W' : 'w';
                case Keys.X: return shift ^ capsLock ? 'X' : 'x';
                case Keys.Y: return shift ^ capsLock ? 'Y' : 'y';
                case Keys.Z: return shift ^ capsLock ? 'Z' : 'z';
                case Keys.D0: return shift ? ')' : '0';
                case Keys.D1: return shift ? '!' : '1';
                case Keys.D2: return shift ? '@' : '2';
                case Keys.D3: return shift ? '#' : '3';
                case Keys.D4: return shift ? '$' : '4';
                case Keys.D5: return shift ? '%' : '5';
                case Keys.D6: return shift ? '^' : '6';
                case Keys.D7: return shift ? '&' : '7';
                case Keys.D8: return shift ? '*' : '8';
                case Keys.D9: return shift ? '(' : '9';
                case Keys.Space: return ' ';
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.OemComma: return shift ? '<' : ',';
                case Keys.OemQuestion: return shift ? '?' : '/';
                case Keys.OemSemicolon: return shift ? ':' : ';';
                case Keys.OemQuotes: return shift ? '"' : '\'';
                case Keys.OemBackslash: return shift ? '|' : '\\';
                case Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.OemPlus: return shift ? '+' : '=';
                default: return '\0';
            }
        }

        public TextInputCommands[] GetTextInputCommands()
        {
            List<TextInputCommands> ret = new();
            var keyboard = Keyboard.GetState();
            var ctrlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            long millisecondsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            {
                foreach (var value in Enum.GetValues(typeof(TextInputCommands)))
                {
                    var key = _inputTextCommandToKeyboardKey[(int)value];
                    long msPassed = millisecondsSinceEpoch - _timeToAllowNextInputCommand[(int)value];
                    if (keyboard.IsKeyDown(key))
                    {
                        if (msPassed > 0)
                        {
                            _timeToAllowNextInputCommand[(int)value] = (millisecondsSinceEpoch + (msPassed >= 250 ? 450 : 45));
                            var command = (TextInputCommands)value;
                            if ((command == TextInputCommands.MoveCaretEnd) && !ctrlDown) { continue; }
                            if ((command == TextInputCommands.MoveCaretEndOfLine) && ctrlDown) { continue; }
                            if ((command == TextInputCommands.MoveCaretStart) && !ctrlDown) { continue; }
                            if ((command == TextInputCommands.MoveCaretStartOfLine) && ctrlDown) { continue; }
                            ret.Add(command);
                        }
                    }
                    else
                    {
                        _timeToAllowNextInputCommand[(int)value] = 0;
                    }
                }
            }
            return ret.ToArray();
        }

        // to add rate delay and ray limit to input commands
        long[] _timeToAllowNextInputCommand = new long[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // convert text input command to keyboard key
        /*
         * enum values:
            MoveCaretLeft,
            MoveCaretRight,
            MoveCaretUp,
            MoveCaretDown,
            Backspace,
            Delete,
            End,
            Start,
            EndOfLine,
            StartOfLine
         */
        static Keys[] _inputTextCommandToKeyboardKey = new Keys[]
        {
           Keys.Left,
           Keys.Right,
           Keys.Up,
           Keys.Down,
           Keys.Back,
           Keys.Delete,
           Keys.Enter,
           Keys.End,
           Keys.Home,
           Keys.End,
           Keys.Home
        };

        public bool IsTextSelectionKeyDown()
        {
            var keyboardState = Keyboard.GetState();
            return keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        }

        public bool IsCopyCommand()
        {
            var keyboardState = Keyboard.GetState();
            return IsCtrlDown(keyboardState) && keyboardState.IsKeyDown(Keys.C);
        }

        public bool IsPasteCommand()
        {
            var keyboardState = Keyboard.GetState();
            return IsCtrlDown(keyboardState) && keyboardState.IsKeyDown(Keys.V);
        }

        public bool IsCutCommand()
        {
            var keyboardState = Keyboard.GetState();
            return IsCtrlDown(keyboardState) && keyboardState.IsKeyDown(Keys.X);
        }

        public bool IsSelectAllCommand()
        {
            var keyboardState = Keyboard.GetState();
            return IsCtrlDown(keyboardState) && keyboardState.IsKeyDown(Keys.A);
        }

        static bool IsCtrlDown(KeyboardState keyboardState)
        {
            return keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        }

        public string? GetClipboardText()
        {
            return SdlClipboard.GetText() ?? _fallbackClipboard;
        }

        public void SetClipboardText(string text)
        {
            if (!SdlClipboard.SetText(text))
            {
                _fallbackClipboard = text;
            }
        }

        // used if SDL clipboard is not available
        string? _fallbackClipboard;

        /// <summary>
        /// MonoGame doesn't provide clipboard access, but DesktopGL runs on SDL2 which does.
        /// </summary>
        static class SdlClipboard
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr GetClipboardTextFn();
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int SetClipboardTextFn(byte[] utf8Text);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void FreeFn(IntPtr ptr);

            static readonly GetClipboardTextFn? _getText;
            static readonly SetClipboardTextFn? _setText;
            static readonly FreeFn? _free;

            static SdlClipboard()
            {
                foreach (var name in new[] { "SDL2", "libSDL2-2.0.so.0", "libSDL2-2.0.0.dylib" })
                {
                    if (NativeLibrary.TryLoad(name, typeof(Microsoft.Xna.Framework.Game).Assembly, null, out var handle) &&
                        NativeLibrary.TryGetExport(handle, "SDL_GetClipboardText", out var getText) &&
                        NativeLibrary.TryGetExport(handle, "SDL_SetClipboardText", out var setText) &&
                        NativeLibrary.TryGetExport(handle, "SDL_free", out var free))
                    {
                        _getText = Marshal.GetDelegateForFunctionPointer<GetClipboardTextFn>(getText);
                        _setText = Marshal.GetDelegateForFunctionPointer<SetClipboardTextFn>(setText);
                        _free = Marshal.GetDelegateForFunctionPointer<FreeFn>(free);
                        return;
                    }
                }
            }

            public static string? GetText()
            {
                if (_getText == null) { return null; }
                var ptr = _getText();
                if (ptr == IntPtr.Zero) { return null; }
                var ret = Marshal.PtrToStringUTF8(ptr);
                _free!(ptr);
                return ret;
            }

            public static bool SetText(string text)
            {
                if (_setText == null) { return false; }
                var bytes = Encoding.UTF8.GetBytes(text + '\0');
                return _setText(bytes) == 0;
            }
        }

        public KeyboardInteractions? GetKeyboardInteraction()
        {
            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                return KeyboardInteractions.MoveLeft;
            }
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                return KeyboardInteractions.MoveRight;
            }
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                return KeyboardInteractions.MoveUp;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                return KeyboardInteractions.MoveDown;
            }
            if (keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Enter))
            {
                return KeyboardInteractions.Select;
            }
            return null;
        }
    }
}
