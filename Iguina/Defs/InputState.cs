

using Iguina.Drivers;

namespace Iguina.Defs
{
    /// <summary>
    /// All input properties for entities interactions.
    /// </summary>
    public struct InputState
    {
        /// <summary>
        /// Current frame state.
        /// </summary>
        internal CurrentInputState _Current;

        /// <summary>
        /// Previous frame state.
        /// </summary>
        internal CurrentInputState _Previous;

        /// <summary>
        /// Is mouse left button is currently down.
        /// </summary>
        public bool LeftMouseDown => _Current.LeftMouseButton;

        /// <summary>
        /// Is mouse right button is currently down.
        /// </summary>
        public bool RightMouseDown => _Current.RightMouseButton;

        /// <summary>
        /// Is mouse wheel button is currently down.
        /// </summary>
        public bool WheelMouseDown => _Current.WheelMouseButton;

        /// <summary>
        /// Was left mouse button pressed this frame.
        /// </summary>
        public bool LeftMousePressedNow => _Current.LeftMouseButton && !_Previous.LeftMouseButton;

        /// <summary>
        /// Was right mouse button pressed this frame.
        /// </summary>
        public bool RightMousePressedNow => _Current.RightMouseButton && !_Previous.RightMouseButton;

        /// <summary>
        /// Was mouse wheel button pressed this frame.
        /// </summary>
        public bool WheelMousePressedNow => _Current.WheelMouseButton && !_Previous.WheelMouseButton;

        /// <summary>
        /// Was left mouse button released this frame.
        /// </summary>
        public bool LeftMouseReleasedNow => !_Current.LeftMouseButton && _Previous.LeftMouseButton;

        /// <summary>
        /// Was right mouse button released this frame.
        /// </summary>
        public bool RightMouseReleasedNow => !_Current.RightMouseButton && _Previous.RightMouseButton;

        /// <summary>
        /// Was mouse wheel button released this frame.
        /// </summary>
        public bool WheelMouseReleasedNow => !_Current.WheelMouseButton && _Previous.WheelMouseButton;

        /// <summary>
        /// Mouse wheel change.
        /// </summary>
        public int MouseWheelChange => _Current.MouseWheelChange;

        /// <summary>
        /// Current mouse position.
        /// </summary>
        public Point MousePosition => _Current.MousePosition;

        /// <summary>
        /// Mouse movement this frame.
        /// </summary>
        public Point MouseMove => new Point(_Current.MousePosition.X - _Previous.MousePosition.X, _Current.MousePosition.Y - _Previous.MousePosition.Y);

        /// <summary>
        /// Get current frame text input characters.
        /// </summary>
        public int[] TextInput => _Current.TextInput;

        /// <summary>
        /// Get current frame text input commands.
        /// </summary>
        public TextInputCommands[] TextInputCommands => _Current.TextInputCommands;

        /// <summary>
        /// Get current keyboard interactions.
        /// </summary>
        public KeyboardInteractions? KeyboardInteraction => (_Current.KeyboardInteraction != _Previous.KeyboardInteraction) ? _Current.KeyboardInteraction : null;

        /// <summary>
        /// Get if keyboard select button is currently held down.
        /// </summary>
        public bool IsKeyboardSelectPressedDown => _Current.KeyboardInteraction == KeyboardInteractions.Select;

        /// <summary>
        /// Is shift key currently down.
        /// </summary>
        public bool ShiftDown => _Current.ShiftDown;

        /// <summary>
        /// Is copy command currently down.
        /// </summary>
        public bool CopyCommandDown => _Current.CopyCommand;

        /// <summary>
        /// Was copy command pressed this frame.
        /// </summary>
        public bool CopyCommandPressedNow => _Current.CopyCommand && !_Previous.CopyCommand;

        /// <summary>
        /// Is paste command currently down.
        /// </summary>
        public bool PasteCommandDown => _Current.PasteCommand;

        /// <summary>
        /// Was paste command pressed this frame.
        /// </summary>
        public bool PasteCommandPressedNow => _Current.PasteCommand && !_Previous.PasteCommand;

        /// <summary>
        /// Is cut command currently down.
        /// </summary>
        public bool CutCommandDown => _Current.CutCommand;

        /// <summary>
        /// Was cut command pressed this frame.
        /// </summary>
        public bool CutCommandPressedNow => _Current.CutCommand && !_Previous.CutCommand;

        /// <summary>
        /// Is select all command currently down.
        /// </summary>
        public bool SelectAllCommandDown => _Current.SelectAllCommand;

        /// <summary>
        /// Was select all command pressed this frame.
        /// </summary>
        public bool SelectAllCommandPressedNow => _Current.SelectAllCommand && !_Previous.SelectAllCommand;

        /// <summary>
        /// Current screen bounds.
        /// </summary>
        public Rectangle ScreenBounds;
    }

    /// <summary>
    /// Input state for a specific frame.
    /// </summary>
    public struct CurrentInputState
    {
        public bool LeftMouseButton;
        public bool RightMouseButton;
        public bool WheelMouseButton;
        public Point MousePosition;
        public int MouseWheelChange;
        public int[] TextInput;
        public TextInputCommands[] TextInputCommands;
        public KeyboardInteractions? KeyboardInteraction;
        public bool ShiftDown;
        public bool CopyCommand;
        public bool PasteCommand;
        public bool CutCommand;
        public bool SelectAllCommand;
    }
}
