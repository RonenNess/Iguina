using Iguina.Defs;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="TextInput"/> copy and paste.
    /// </summary>
    public class TextInputClipboardTests
    {
        const string Text = "Hello wonderful world";

        ScriptedInputProvider _input = null!;
        UISystem _system = null!;

        TextInput CreateSystem<T>(T textInput, string value, bool multiline = false) where T : TextInput
        {
            _system.Root.AddChild(textInput);
            textInput.Anchor = Anchor.TopLeft;
            textInput.Size.SetPixels(600, multiline ? 300 : 40);
            if (multiline) { textInput.Multiline = true; }
            textInput.Value = value;
            Frame();

            // click to start editing
            _input.MousePosition = new Point(textInput.LastBoundingRect.X + 10, textInput.LastBoundingRect.Y + 10);
            _input.LeftDown = true;
            Frame();
            _input.LeftDown = false;
            Frame();
            return textInput;
        }

        TextInput CreateTextInput(string value, bool multiline = false)
        {
            CreateUISystem();
            return CreateSystem(new TextInput(_system), value, multiline);
        }

        NumericInput CreateNumericInput(string value)
        {
            CreateUISystem();
            return (NumericInput)CreateSystem(new NumericInput(_system, addPlusButton: false, addMinusButton: false), value);
        }

        void CreateUISystem()
        {
            _input = new ScriptedInputProvider();
            _system = new UISystem(new TestRenderer(), _input);
        }

        void Frame()
        {
            _system.Update(0.001f);
            _system.Draw();
        }

        /// <summary>
        /// Press copy command, while typing its key character in the same frame (like some drivers do).
        /// </summary>
        void PressCopy()
        {
            _input.CopyDown = true;
            _input.TextInput.Add('c');
            Frame();
            _input.CopyDown = false;
            Frame();
        }

        /// <summary>
        /// Press paste command, while typing its key character in the same frame (like some drivers do).
        /// </summary>
        void PressPaste()
        {
            _input.PasteDown = true;
            _input.TextInput.Add('v');
            Frame();
            _input.PasteDown = false;
            Frame();
        }

        /// <summary>
        /// Press cut command, while typing its key character in the same frame (like some drivers do).
        /// </summary>
        void PressCut()
        {
            _input.CutDown = true;
            _input.TextInput.Add('x');
            Frame();
            _input.CutDown = false;
            Frame();
        }

        /// <summary>
        /// Press select all command, while typing its key character in the same frame (like some drivers do).
        /// </summary>
        void PressSelectAll()
        {
            _input.SelectAllDown = true;
            _input.TextInput.Add('a');
            Frame();
            _input.SelectAllDown = false;
            Frame();
        }

        [Test]
        public void TestCutSelection()
        {
            var textInput = CreateTextInput(Text);
            textInput.SetSelection(5, 15);
            PressCut();
            Assert.That(_input.Clipboard, Is.EqualTo(" wonderful"));
            Assert.That(textInput.Value, Is.EqualTo("Hello world"), "Cut command key should not be typed");
            Assert.That(textInput.CaretOffset, Is.EqualTo(5));
            Assert.That(textInput.HasSelection, Is.False);

            // no selection: nothing happens
            PressCut();
            Assert.That(_input.Clipboard, Is.EqualTo(" wonderful"));
            Assert.That(textInput.Value, Is.EqualTo("Hello world"));

            // cut then paste elsewhere
            textInput.CaretOffset = textInput.Value.Length;
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("Hello world wonderful"));
        }

        [Test]
        public void TestCutFromMaskedInputIsBlocked()
        {
            var textInput = CreateTextInput("secret");
            textInput.MaskingCharacter = '*';
            textInput.SelectAll();
            _input.Clipboard = "before";
            PressCut();
            Assert.That(_input.Clipboard, Is.EqualTo("before"));
            Assert.That(textInput.Value, Is.EqualTo("secret"), "Masked text should not be deleted by cut");
            Assert.That(textInput.CutSelection(), Is.False);
        }

        [Test]
        public void TestNumericInputCut()
        {
            var numeric = CreateNumericInput("12345");
            numeric.SetSelection(1, 4);
            PressCut();
            Assert.That(_input.Clipboard, Is.EqualTo("234"));
            Assert.That(numeric.Value, Is.EqualTo("15"));
        }

        [Test]
        public void TestSelectAll()
        {
            var textInput = CreateTextInput(Text);
            textInput.CaretOffset = 3;
            PressSelectAll();
            Assert.That(textInput.Value, Is.EqualTo(Text), "Select all command key should not be typed");
            Assert.That(textInput.SelectedText, Is.EqualTo(Text));
            Assert.That(textInput.CaretOffset, Is.EqualTo(Text.Length));

            // typing replaces everything
            _input.TextInput.Add('X');
            Frame();
            Assert.That(textInput.Value, Is.EqualTo("X"));

            // select all + copy
            PressSelectAll();
            PressCopy();
            Assert.That(_input.Clipboard, Is.EqualTo("X"));
        }

        [Test]
        public void TestSelectAllWhenSelectionIsDisabled()
        {
            var textInput = CreateTextInput(Text);
            textInput.AllowTextSelection = false;
            textInput.CaretOffset = 3;
            PressSelectAll();
            Assert.That(textInput.Value, Is.EqualTo(Text), "Select all command key should not be typed");
            Assert.That(textInput.HasSelection, Is.False);
            Assert.That(textInput.CaretOffset, Is.EqualTo(3), "Caret should not move");
        }

        [Test]
        public void TestCopySelection()
        {
            var textInput = CreateTextInput(Text);
            textInput.SetSelection(6, 15);
            PressCopy();
            Assert.That(_input.Clipboard, Is.EqualTo("wonderful"));
            Assert.That(textInput.Value, Is.EqualTo(Text), "Copy command key should not be typed");
            Assert.That(textInput.SelectedText, Is.EqualTo("wonderful"), "Copy should keep selection");

            // no selection: clipboard is not changed
            textInput.ClearSelection();
            PressCopy();
            Assert.That(_input.Clipboard, Is.EqualTo("wonderful"));
            Assert.That(textInput.Value, Is.EqualTo(Text));
        }

        [Test]
        public void TestCopyFromMaskedInputIsBlocked()
        {
            var textInput = CreateTextInput("secret");
            textInput.MaskingCharacter = '*';
            textInput.SelectAll();
            _input.Clipboard = "before";
            PressCopy();
            Assert.That(_input.Clipboard, Is.EqualTo("before"));
            Assert.That(textInput.CopySelection(), Is.False);
        }

        [Test]
        public void TestPasteAtCaretAndOverSelection()
        {
            var textInput = CreateTextInput(Text);
            _input.Clipboard = "big ";

            // paste at caret
            textInput.CaretOffset = 6;
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("Hello big wonderful world"), "Paste command key should not be typed");
            Assert.That(textInput.CaretOffset, Is.EqualTo(10));

            // paste over selection
            _input.Clipboard = "great";
            textInput.SetSelection(10, 19);
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("Hello big great world"));
            Assert.That(textInput.CaretOffset, Is.EqualTo(15));
            Assert.That(textInput.HasSelection, Is.False);

            // empty clipboard does nothing
            _input.Clipboard = null;
            textInput.SetSelection(0, 5);
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("Hello big great world"));
            Assert.That(textInput.SelectedText, Is.EqualTo("Hello"));
        }

        [Test]
        public void TestHoldingPasteOnlyPastesOnceAndDoesntType()
        {
            var textInput = CreateTextInput("ab");
            textInput.CaretOffset = 2;
            _input.Clipboard = "X";

            // hold paste for several frames, while driver keeps repeating the 'v' key
            _input.PasteDown = true;
            for (int i = 0; i < 5; ++i)
            {
                _input.TextInput.Add('v');
                Frame();
            }
            _input.PasteDown = false;
            Frame();
            Assert.That(textInput.Value, Is.EqualTo("abX"));

            // after releasing, typing works again
            _input.TextInput.Add('v');
            Frame();
            Assert.That(textInput.Value, Is.EqualTo("abXv"));
        }

        [Test]
        public void TestControlCharactersAreNotTyped()
        {
            var textInput = CreateTextInput("ab");
            textInput.CaretOffset = 2;
            _input.TextInput.AddRange(new[] { 3, 22, 127, 'c' });
            Frame();
            Assert.That(textInput.Value, Is.EqualTo("abc"));
        }

        [Test]
        public void TestPasteLineBreaks()
        {
            // single line: line breaks become spaces
            var textInput = CreateTextInput(string.Empty);
            _input.Clipboard = "line1\r\nline2\nline3";
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("line1 line2 line3"));

            // multiline: line breaks are kept
            textInput = CreateTextInput(string.Empty, multiline: true);
            _input.Clipboard = "line1\r\nline2\nline3";
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("line1\nline2\nline3"));
        }

        [Test]
        public void TestPasteRespectsMaxLength()
        {
            var textInput = CreateTextInput("abc");
            textInput.MaxLength = 5;
            textInput.CaretOffset = 3;
            _input.Clipboard = "12345";
            PressPaste();
            Assert.That(textInput.Value, Is.EqualTo("abc12"));
        }

        [TestCase("5", 1, "12", "512")]
        [TestCase("5", 0, "12", "125")]
        [TestCase("5", 1, " 42\n", "542")]
        [TestCase("5", 1, ".25", "5.25")]
        [TestCase("5", 0, "-", "-5")]
        [TestCase("5", 1, "abc", "5")]
        [TestCase("5", 1, "1a", "5")]
        [TestCase("5", 1, "-3", "5")]
        [TestCase("5", 1, "1,000", "5")]
        [TestCase("5.5", 1, ".2", "5.5")]
        [TestCase("", 0, "-12.5", "-12.5")]
        public void TestNumericInputPaste(string value, int caret, string clipboard, string expected)
        {
            var numeric = CreateNumericInput(value);
            numeric.CaretOffset = caret;
            _input.Clipboard = clipboard;
            PressPaste();
            Assert.That(numeric.Value, Is.EqualTo(expected));
        }

        [Test]
        public void TestNumericInputPasteOverSelection()
        {
            var numeric = CreateNumericInput("12345");
            numeric.SetSelection(1, 4);
            _input.Clipboard = "x";
            PressPaste();
            Assert.That(numeric.Value, Is.EqualTo("12345"), "Invalid paste should be rejected");
            Assert.That(numeric.SelectedText, Is.EqualTo("234"), "Selection should be kept when paste is rejected");

            _input.Clipboard = "0";
            PressPaste();
            Assert.That(numeric.Value, Is.EqualTo("105"));
        }

        [Test]
        public void TestNumericInputPasteDecimalNotAccepted()
        {
            var numeric = CreateNumericInput("5");
            numeric.AcceptsDecimal = false;
            numeric.CaretOffset = 1;
            _input.Clipboard = "1.5";
            PressPaste();
            Assert.That(numeric.Value, Is.EqualTo("5"));
            _input.Clipboard = "15";
            PressPaste();
            Assert.That(numeric.Value, Is.EqualTo("515"));
        }

        [Test]
        public void TestDefaultInternalClipboard()
        {
            // input provider that doesn't implement clipboard methods uses internal clipboard
            var system = new UISystem(new TestRenderer(), new TestInputProvider());
            var first = system.Root.AddChild(new TextInput(system));
            var second = system.Root.AddChild(new TextInput(system));
            first.Value = Text;
            second.Value = "Say: ";
            system.Update(0.001f);
            system.Draw();

            first.SetSelection(6, 15);
            Assert.That(first.CopySelection(), Is.True);
            second.CaretOffset = second.Value.Length;
            Assert.That(second.PasteFromClipboard(), Is.EqualTo(9));
            Assert.That(second.Value, Is.EqualTo("Say: wonderful"));
        }
    }
}
