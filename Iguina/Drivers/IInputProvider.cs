﻿using Iguina.Defs;


namespace Iguina.Drivers
{
    /// <summary>
    /// An interface to provide input functionality for the GUI system.
    /// This is one of the drivers your application needs to provide.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Get current mouse position.
        /// </summary>
        /// <remarks>
        /// This doesn't have to actually be a mouse. For example, you can get input from gamepad and emulate mouse for gamepad controls.
        /// </remarks>
        /// <returns>Mouse position.</returns>
        Point GetMousePosition();

        /// <summary>
        /// Get the state of a mouse button.
        /// </summary>
        /// <param name="btn">Mouse button to check.</param>
        /// <remarks>
        /// This doesn't have to actually be a mouse. For example, you can get input from gamepad and emulate mouse for gamepad controls.
        /// </remarks>
        /// <returns>True if mouse button is currently down.</returns>
        bool IsMouseButtonDown(MouseButton btn);

        /// <summary>
        /// Get current mouse wheel value.
        /// -1 = mouse wheel scrolled up.
        /// 1 = mouse wheel scrolled down.
        /// 0 = no change to mouse wheel.
        /// </summary>
        /// <returns>Mouse wheel change value.</returns>
        int GetMouseWheelChange();

        /// <summary>
        /// Get characters input from typing.
        /// </summary>
        /// <remarks>Its the input provider responsibility to add 'Repeat Delay' and 'Repeating Rate'. Iguina will not limit or impose any delays on typing speed.</remarks>
        /// <returns>Characters that were pressed this frame, as characters unicode values.</returns>
        int[] GetTextInput();

        /// <summary>
        /// Get special text input commands from typing.
        /// </summary>
        /// <remarks>Its the input provider responsibility to add 'Repeat Delay' and 'Repeat Rate'. Iguina will not limit or impose any delays on typing speed.</remarks>
        /// <returns>Text commands that were pressed this frame.</returns>
        TextInputCommands[] GetTextInputCommands();
    }

    /// <summary>
    /// Special text input commands.
    /// </summary>
    public enum TextInputCommands
    {
        MoveCaretLeft,
        MoveCaretRight,
        MoveCaretUp,
        MoveCaretDown,
        Backspace,
        Delete,
        BreakLine,
        MoveCaretEnd,
        MoveCaretStart,
        MoveCaretEndOfLine,
        MoveCaretStartOfLine,
    }

    /// <summary>
    /// Mouse buttons.
    /// </summary>
    public enum MouseButton
    {
        Left,
        Wheel,
        Right
    }
}