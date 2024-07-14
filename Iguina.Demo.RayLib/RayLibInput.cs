using Iguina.Defs;
using Iguina.Drivers;


namespace Iguina.Demo.RayLib
{

    /// <summary>
    /// Provide input for the GUI system.
    /// </summary>
    public class RayLibInput : IInputProvider
    {
        public Point GetMousePosition()
        {
            var cp = Raylib_cs.Raylib.GetMousePosition();
            return new Point((int)cp.X, (int)cp.Y);
        }

        public bool IsMouseButtonDown(MouseButton btn)
        {
            switch (btn)
            {
                case MouseButton.Left:
                    return Raylib_cs.Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.Left);

                case MouseButton.Right:
                    return Raylib_cs.Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.Right);

                case MouseButton.Wheel:
                    return Raylib_cs.Raylib.IsMouseButtonDown(Raylib_cs.MouseButton.Middle);
            }
            return false;
        }

        public int GetMouseWheelChange()
        {
            float val = Raylib_cs.Raylib.GetMouseWheelMove();
            return MathF.Sign(val);
        }

        public int[] GetTextInput()
        {
            List<int> ret = new();
            while (true)
            {
                var curr = Raylib_cs.Raylib.GetCharPressed();
                if (curr != 0) { ret.Add(curr); }
                else { break; }
            }
            return ret.ToArray();
        }

        public TextInputCommands[] GetTextInputCommands()
        {
            var ctrlDown = Raylib_cs.Raylib.IsKeyDown(Raylib_cs.KeyboardKey.LeftControl) || Raylib_cs.Raylib.IsKeyDown(Raylib_cs.KeyboardKey.Right);

            List<TextInputCommands> ret = new();
            long millisecondsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            {
                foreach (var value in Enum.GetValues(typeof(TextInputCommands)))
                {
                    var key = _inputTextCommandToKeyboardKey[(int)value];
                    long msPassed = millisecondsSinceEpoch - _timeToAllowNextInputCommand[(int)value];
                    if (Raylib_cs.Raylib.IsKeyDown(key))
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
        long[] _timeToAllowNextInputCommand = new long[] { 0,0,0,0,0,0,0,0,0,0,0 };

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
        static Raylib_cs.KeyboardKey[] _inputTextCommandToKeyboardKey = new Raylib_cs.KeyboardKey[]
        {
            Raylib_cs.KeyboardKey.Left,
            Raylib_cs.KeyboardKey.Right,
            Raylib_cs.KeyboardKey.Up,
            Raylib_cs.KeyboardKey.Down,
            Raylib_cs.KeyboardKey.Backspace,
            Raylib_cs.KeyboardKey.Delete,
            Raylib_cs.KeyboardKey.Enter,
            Raylib_cs.KeyboardKey.End,
            Raylib_cs.KeyboardKey.Home,
            Raylib_cs.KeyboardKey.End,
            Raylib_cs.KeyboardKey.Home
        };
    }
}
