using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="TextInput"/> text selection.
    /// </summary>
    public class TextInputSelectionTests
    {
        const string Text = "Hello wonderful world";

        ProportionalRenderer _renderer = null!;
        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        TextInput _textInput = null!;

        void CreateSystem(string value, bool multiline = false, int width = 400)
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(_renderer, _input);
            _textInput = _system.Root.AddChild(new TextInput(_system));
            _textInput.Anchor = Anchor.TopLeft;
            _textInput.Size.SetPixels(width, multiline ? 300 : 40);
            _textInput.Multiline = multiline;
            _textInput.Value = value;
            _input.MousePosition = new Point(2000, 2000);
            Frame();
            Frame();
        }

        void Frame()
        {
            _renderer.ClearRecords();
            _system.Update(0.001f);
            _system.Draw();
        }

        /// <summary>
        /// Get screen position of the boundary before a given character index, in the first drawn line.
        /// </summary>
        Point CharPosition(int index)
        {
            var line = _renderer.DrawnTexts.First(x => x.Text != _textInput.CaretCharacter);
            return new Point(line.Position.X + ProportionalRenderer.Measure(_textInput.Value.Substring(0, index)) + 1, line.Position.Y + 10);
        }

        void Click(int index)
        {
            _input.MousePosition = CharPosition(index);
            _input.LeftDown = true;
            Frame();
            _input.LeftDown = false;
            Frame();
        }

        void Drag(int fromIndex, int toIndex)
        {
            _input.MousePosition = CharPosition(fromIndex);
            _input.LeftDown = true;
            Frame();
            _input.MousePosition = CharPosition(toIndex);
            Frame();
            _input.LeftDown = false;
            Frame();
        }

        void Press(TextInputCommands command, bool shift = false)
        {
            _input.ShiftDown = shift;
            _input.Commands.Add(command);
            Frame();
            _input.ShiftDown = false;
        }

        void Type(string text)
        {
            _input.TextInput.AddRange(text.Select(c => (int)c));
            Frame();
        }

        [Test]
        public void TestSetSelectionApi()
        {
            CreateSystem(Text);

            // start before end: caret at end, selection behind caret
            _textInput.SetSelection(6, 15);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(15));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(-9));
            Assert.That(_textInput.SelectionStart, Is.EqualTo(6));
            Assert.That(_textInput.SelectionEnd, Is.EqualTo(15));
            Assert.That(_textInput.SelectionLength, Is.EqualTo(9));
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful"));
            Assert.That(_textInput.GetSelection(out int start, out int end), Is.True);
            Assert.That((start, end), Is.EqualTo((6, 15)));

            // start after end: caret at end, selection after caret
            _textInput.SetSelection(15, 6);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(6));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(9));
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful"));

            // select all / clear
            _textInput.SelectAll();
            Assert.That(_textInput.SelectedText, Is.EqualTo(Text));
            Assert.That(_textInput.CaretOffset, Is.EqualTo(Text.Length));
            _textInput.ClearSelection();
            Assert.That(_textInput.HasSelection, Is.False);
            Assert.That(_textInput.SelectedText, Is.EqualTo(string.Empty));
            Assert.That(_textInput.GetSelection(out _, out _), Is.False);
        }

        [Test]
        public void TestSelectionRangesAreCapped()
        {
            CreateSystem(Text);

            _textInput.SetSelection(-100, 1000);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(Text.Length));
            Assert.That(_textInput.SelectionStart, Is.EqualTo(0));
            Assert.That(_textInput.SelectedText, Is.EqualTo(Text));

            _textInput.SetSelection(1000, -100);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(0));
            Assert.That(_textInput.SelectionEnd, Is.EqualTo(Text.Length));

            _textInput.CaretOffset = 5;
            _textInput.SelectionOffset = 1000;
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(Text.Length - 5));
            _textInput.SelectionOffset = -1000;
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(-5));

            // value changes under the selection
            _textInput.SetSelection(6, 21);
            _textInput.Value = "Hi";
            Assert.That(_textInput.SelectionStart, Is.InRange(0, 2));
            Assert.That(_textInput.SelectionEnd, Is.InRange(0, 2));
            Assert.DoesNotThrow(() => { var _ = _textInput.SelectedText; });
            Assert.DoesNotThrow(() => _textInput.DeleteSelection());
            Assert.DoesNotThrow(Frame);

            _textInput.Value = string.Empty;
            Assert.That(_textInput.HasSelection, Is.False);
            Assert.DoesNotThrow(() => _textInput.SetSelection(3, 7));
            Assert.That(_textInput.HasSelection, Is.False);
            Assert.DoesNotThrow(Frame);
        }

        [Test]
        public void TestMouseDragSelects()
        {
            CreateSystem(Text);

            // drag left: caret stays where we pressed, selection is negative
            Drag(15, 6);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(15));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(-9));
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful"));

            // click resets selection and moves caret
            Click(3);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(3));
            Assert.That(_textInput.HasSelection, Is.False);

            // drag right: selection is positive
            Drag(6, 15);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(6));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(9));
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful"));

            // selection is drawn behind text
            Frame();
            var rect = _renderer.DrawnRectangles.Single(x => x.Color.Equals(Paragraph.DefaultTextHighlightColor)).Rect;
            Assert.That(rect.X, Is.EqualTo(CharPosition(6).X - 1));
            Assert.That(rect.Right, Is.EqualTo(CharPosition(15).X - 1));
        }

        [Test]
        public void TestShiftArrowsSelect()
        {
            CreateSystem(Text);
            Click(6);

            // shift + right x3 selects after caret
            for (int i = 0; i < 3; ++i) { Press(TextInputCommands.MoveCaretRight, true); }
            Assert.That(_textInput.CaretOffset, Is.EqualTo(6));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(3));
            Assert.That(_textInput.SelectedText, Is.EqualTo("won"));

            // shift + left x5 goes to the other side of the caret
            for (int i = 0; i < 5; ++i) { Press(TextInputCommands.MoveCaretLeft, true); }
            Assert.That(_textInput.CaretOffset, Is.EqualTo(6));
            Assert.That(_textInput.SelectionOffset, Is.EqualTo(-2));
            Assert.That(_textInput.SelectedText, Is.EqualTo("o "));

            // shift + end / home
            Press(TextInputCommands.MoveCaretEndOfLine, true);
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful world"));
            Press(TextInputCommands.MoveCaretStartOfLine, true);
            Assert.That(_textInput.SelectedText, Is.EqualTo("Hello "));

            // left without shift goes to selection start and clears selection
            Press(TextInputCommands.MoveCaretLeft);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(0));
            Assert.That(_textInput.HasSelection, Is.False);

            // right without shift goes to selection end
            _textInput.SetSelection(2, 8);
            Press(TextInputCommands.MoveCaretRight);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(8));
            Assert.That(_textInput.HasSelection, Is.False);
        }

        [Test]
        public void TestShiftUpDownSelectsVisualLines()
        {
            CreateSystem("aaaa bbbb cccc dddd eeee ffff gggg hhhh iiii jjjj", multiline: true, width: 120);
            var lines = _renderer.DrawnTexts.Where(x => x.Text != _textInput.CaretCharacter).ToList();
            Assert.That(lines.Count, Is.GreaterThan(2), "Text should wrap");

            Click(2);
            Press(TextInputCommands.MoveCaretDown, true);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(2));
            Assert.That(_textInput.SelectionEnd, Is.EqualTo(_textInput.Value.IndexOf(lines[1].Text) + 2));
            Press(TextInputCommands.MoveCaretUp, true);
            Assert.That(_textInput.HasSelection, Is.False);
        }

        [Test]
        public void TestTypingReplacesSelection()
        {
            CreateSystem(Text);
            Click(0);

            _textInput.SetSelection(6, 15);
            Type("great");
            Assert.That(_textInput.Value, Is.EqualTo("Hello great world"));
            Assert.That(_textInput.CaretOffset, Is.EqualTo(11));
            Assert.That(_textInput.HasSelection, Is.False);

            // api
            _textInput.SetSelection(0, 5);
            Assert.That(_textInput.ReplaceSelection("Bye"), Is.EqualTo(3));
            Assert.That(_textInput.Value, Is.EqualTo("Bye great world"));
            _textInput.SetSelection(3, 9);
            Assert.That(_textInput.ReplaceSelection(string.Empty), Is.EqualTo(0));
            Assert.That(_textInput.Value, Is.EqualTo("Bye world"));
        }

        [Test]
        public void TestLineBreakInSingleLineDoesntDeleteSelection()
        {
            CreateSystem(Text);
            Click(0);
            _textInput.SetSelection(6, 15);
            Press(TextInputCommands.BreakLine);
            Assert.That(_textInput.Value, Is.EqualTo(Text));
            Assert.That(_textInput.SelectedText, Is.EqualTo("wonderful"));
        }

        [TestCase(TextInputCommands.Backspace)]
        [TestCase(TextInputCommands.Delete)]
        public void TestDeletingRemovesSelection(TextInputCommands command)
        {
            CreateSystem(Text);
            Click(0);
            _textInput.SetSelection(15, 5);
            Press(command);
            Assert.That(_textInput.Value, Is.EqualTo("Hello world"));
            Assert.That(_textInput.CaretOffset, Is.EqualTo(5));
            Assert.That(_textInput.HasSelection, Is.False);

            // without selection, deletes a single character as before
            Press(command);
            Assert.That(_textInput.Value, Is.EqualTo(command == TextInputCommands.Backspace ? "Hell world" : "Helloworld"));
        }

        [Test]
        public void TestSelectionDisabled()
        {
            CreateSystem(Text);
            _textInput.AllowTextSelection = false;

            // dragging just moves caret
            Drag(15, 6);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(6));
            Assert.That(_textInput.HasSelection, Is.False);

            // shift + arrows just move caret
            Press(TextInputCommands.MoveCaretRight, true);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(7));
            Assert.That(_textInput.HasSelection, Is.False);

            // api doesn't select
            _textInput.SetSelection(0, 5);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(5));
            Assert.That(_textInput.HasSelection, Is.False);
            Assert.That(_textInput.SelectedText, Is.EqualTo(string.Empty));

            // typing just inserts
            Type("X");
            Assert.That(_textInput.Value, Is.EqualTo("HelloX wonderful world"));
        }

        [Test]
        public void TestMaxLength()
        {
            CreateSystem(string.Empty);
            _textInput.MaxLength = 5;
            Assert.That(_textInput.InsertCharacters("abcdefg"), Is.EqualTo(5));
            Assert.That(_textInput.Value, Is.EqualTo("abcde"));

            // replacing selection when at max length
            _textInput.SetSelection(1, 3);
            Assert.That(_textInput.InsertCharacters("XYZ"), Is.EqualTo(2));
            Assert.That(_textInput.Value, Is.EqualTo("aXYde"));
        }

        [Test]
        public void TestNumericInputSelection()
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(_renderer, _input);
            var numeric = _system.Root.AddChild(new NumericInput(_system, addPlusButton: false, addMinusButton: false));
            numeric.Value = "12345";
            Frame();
            Frame();
            numeric.SetSelection(1, 4);
            Assert.That(numeric.SelectedText, Is.EqualTo("234"));
            Assert.That(numeric.ReplaceSelection("9"), Is.EqualTo(1));
            Assert.That(numeric.Value, Is.EqualTo("195"));

            // invalid replacement is rejected, and selection is kept
            numeric.SelectAll();
            Assert.That(numeric.InsertCharacters("x"), Is.EqualTo(0));
            Assert.That(numeric.Value, Is.EqualTo("195"));
            Assert.That(numeric.SelectedText, Is.EqualTo("195"));
        }
    }
}
