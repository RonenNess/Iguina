using Iguina.Defs;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for scrollbars, and scrollbars of multiline text inputs.
    /// </summary>
    public class ScrollbarTests
    {
        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        TextInput _textInput = null!;

        /// <summary>
        /// Create system with a multiline text input with scrollbar.
        /// Scrollbar style has big padding (similar to the default theme) so that short text inputs are too small for it.
        /// </summary>
        void CreateSystem(int textInputHeight)
        {
            _input = new ScriptedInputProvider();
            _system = new UISystem(new TestRenderer(), _input);

            var handleLength = new Measurement();
            handleLength.SetPixels(80);
            _system.DefaultStylesheets.VerticalScrollbars = new StyleSheet()
            {
                Default = new StyleSheetState() { Padding = new Sides(0, 0, 70, 70) }
            };
            _system.DefaultStylesheets.VerticalScrollbarsHandle = new StyleSheet() { DefaultHeight = handleLength };

            _textInput = _system.Root.AddChild(new TextInput(_system));
            _textInput.Anchor = Anchor.TopLeft;
            _textInput.Size.SetPixels(400, textInputHeight);
            _textInput.Multiline = true;
            _textInput.CreateVerticalScrollbar();
            _textInput.Value = string.Join("\n", Enumerable.Range(0, 40).Select(i => "line " + i));
            _input.MousePosition = new Point(2000, 2000);
            Frames(3);
        }

        void Frames(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                _system.Update(0.01f);
                _system.Draw();
            }
        }

        [TestCase(300)]
        [TestCase(150)]
        [TestCase(100)]
        [TestCase(60)]
        public void TestScrollbarHandleStaysInsideAndMovesWithValue(int height)
        {
            CreateSystem(height);
            var scrollbar = _textInput.VerticalScrollbar!;
            Assert.That(scrollbar.MaxValue, Is.GreaterThan(0));

            int prevHandleY = int.MinValue;
            foreach (var percent in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                scrollbar.Value = (int)(scrollbar.MaxValue * percent);
                Frames(2);
                var handle = scrollbar.Handle.LastBoundingRect;
                Assert.That(handle.Height, Is.GreaterThan(0));
                Assert.That(handle.Top, Is.GreaterThanOrEqualTo(scrollbar.LastBoundingRect.Top), $"Handle out of scrollbar at {percent}");
                Assert.That(handle.Bottom, Is.LessThanOrEqualTo(scrollbar.LastBoundingRect.Bottom), $"Handle out of scrollbar at {percent}");
                Assert.That(handle.Top, Is.GreaterThan(prevHandleY), $"Handle should move down as value increases (at {percent})");
                prevHandleY = handle.Top;
            }
        }

        [TestCase(300)]
        [TestCase(100)]
        [TestCase(60)]
        public void TestCanDragScrollbarWhileEditing(int height)
        {
            CreateSystem(height);
            var scrollbar = _textInput.VerticalScrollbar!;

            // click on text to start editing
            _input.MousePosition = new Point(50, 20);
            _input.LeftDown = true;
            Frames(1);
            _input.LeftDown = false;
            Frames(1);
            Assert.That(_system.TargetedEntity, Is.SameAs(_textInput));
            var caret = _textInput.CaretOffset;

            // press on the bottom of the scrollbar, then drag to top
            var sbRect = scrollbar.LastBoundingRect;
            _input.MousePosition = new Point(sbRect.X + sbRect.Width / 2, sbRect.Bottom - 1);
            _input.LeftDown = true;
            Frames(2);
            Assert.That(scrollbar.Value, Is.EqualTo(scrollbar.MaxValue), "Pressing on scrollbar bottom should scroll to end");

            // dragging outside the scrollbar keeps dragging it
            _input.MousePosition = new Point(50, sbRect.Top - 50);
            Frames(2);
            Assert.That(scrollbar.Value, Is.EqualTo(0), "Dragging above scrollbar should scroll to start");
            _input.LeftDown = false;
            Frames(1);

            // caret was not moved and text input is still being edited
            Assert.That(_textInput.CaretOffset, Is.EqualTo(caret));
            Assert.That(_system.TargetedEntity, Is.SameAs(_textInput));
        }
    }
}
