using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="TextInput"/> caret positioning with mouse and arrows, using a non-monospace font.
    /// </summary>
    public class TextInputCaretTests
    {
        /// <summary>
        /// Renderer with a non-monospace font, that records drawn texts.
        /// </summary>
        class ProportionalRenderer : TestRenderer, IRenderer
        {
            public List<(string Text, Point Position)> DrawnTexts = new();

            public static int CharWidth(char c) => c switch
            {
                'i' or 'l' or '.' or ' ' or '|' => 4,
                'W' or 'M' or 'm' => 16,
                _ => 9
            };

            public new Point MeasureText(string text, string? fontId, int fontSize, float spacing)
            {
                return new Point(text.Sum(CharWidth), 20);
            }

            public new void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
            {
                DrawnTexts.Add((text, position));
            }
        }

        const string LongText = "The quick brown fox jumps over the lazy dog. Will Wimbledon iii WWW mmm lll iWiWiW.\nSecond paragraph, with Mmm and illi words.\n\nAfter empty line: MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM end";

        ProportionalRenderer _renderer = null!;
        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        TextInput _textInput = null!;

        void CreateSystem(string value)
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(_renderer, _input);
            _textInput = _system.Root.AddChild(new TextInput(_system));
            _textInput.Anchor = Anchor.TopLeft;
            _textInput.Size.SetPixels(220, 600);
            _textInput.Multiline = true;
            _textInput.Value = value;
            _input.MousePosition = new Point(2000, 2000);
            Frame();
            Frame();
        }

        void Frame()
        {
            _renderer.DrawnTexts.Clear();
            _system.Update(0.001f);
            _system.Draw();
        }

        void Click(Point position)
        {
            _input.MousePosition = position;
            _input.LeftDown = true;
            Frame();
            _input.LeftDown = false;
            Frame();
        }

        void Press(TextInputCommands command)
        {
            _input.Commands.Add(command);
            Frame();
        }

        /// <summary>
        /// Get drawn lines of text input value (excluding caret), and the index in value each line starts at.
        /// </summary>
        List<(string Text, Point Position, int SourceStart)> GetDrawnLines()
        {
            var ret = new List<(string, Point, int)>();
            int cursor = 0;
            foreach (var drawn in _renderer.DrawnTexts.Where(x => x.Text != _textInput.CaretCharacter))
            {
                // empty lines are not drawn, add them
                while (ret.Count > 0 && drawn.Position.Y > ret[^1].Item2.Y + 20)
                {
                    ret.Add((string.Empty, new Point(drawn.Position.X, ret[^1].Item2.Y + 20), cursor + 1));
                    cursor++;
                }

                int start = _textInput.Value.IndexOf(drawn.Text, cursor, StringComparison.Ordinal);
                Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Drawn line '{drawn.Text}' not found in value");
                ret.Add((drawn.Text, drawn.Position, start));
                cursor = start + drawn.Text.Length;
            }
            return ret;
        }

        static int Measure(string text) => text.Sum(ProportionalRenderer.CharWidth);

        [Test]
        public void TestWrappedTextDoesntLoseCharacters()
        {
            CreateSystem("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz0123456789 and some more words");
            var lines = GetDrawnLines();
            Assert.That(lines.Count, Is.GreaterThan(2));
            Assert.That(string.Concat(lines.Select(x => x.Text)).Replace(" ", ""), Is.EqualTo(_textInput.Value.Replace(" ", "")));
        }

        [Test]
        public void TestClickPlacesCaretOnCharacterBoundary()
        {
            CreateSystem(LongText);
            var lines = GetDrawnLines();
            Assert.That(lines.Count, Is.GreaterThan(6), "Text is expected to wrap");

            for (int i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];

                // if a word was broken in the middle, end of line index belongs to next line so caret can only reach the last character
                bool brokenMidWord = (i < lines.Count - 1) && (lines[i + 1].SourceStart == line.SourceStart + line.Text.Length);
                int lastCol = brokenMidWord ? line.Text.Length - 1 : line.Text.Length;

                for (int col = 0; col <= line.Text.Length; ++col)
                {
                    // click slightly right to the boundary before character 'col'
                    var boundaryX = line.Position.X + Measure(line.Text.Substring(0, col));
                    Click(new Point(boundaryX + 1, line.Position.Y + 10));
                    Assert.That(_textInput.CaretOffset, Is.EqualTo(line.SourceStart + Math.Min(col, lastCol)), $"Line '{line.Text}', column {col}");

                    // click slightly left to the boundary
                    if (col > 0)
                    {
                        Click(new Point(boundaryX - 1, line.Position.Y + 10));
                        Assert.That(_textInput.CaretOffset, Is.EqualTo(line.SourceStart + Math.Min(col, lastCol)), $"Line '{line.Text}', column {col} (from left)");
                    }
                }

                // click far to the right of the line should go to end of line
                Click(new Point(line.Position.X + 205, line.Position.Y + 10));
                Assert.That(_textInput.CaretOffset, Is.EqualTo(line.SourceStart + lastCol), $"Line '{line.Text}', end of line");
            }
        }

        [Test]
        public void TestCaretIsDrawnAtCaretOffset()
        {
            CreateSystem(LongText);
            var lines = GetDrawnLines();
            var line = lines[3];
            Click(new Point(line.Position.X + Measure(line.Text.Substring(0, 5)) + 1, line.Position.Y + 5));
            Assert.That(_textInput.CaretOffset, Is.EqualTo(line.SourceStart + 5));

            var caret = _renderer.DrawnTexts.Single(x => x.Text == _textInput.CaretCharacter);
            Assert.That(caret.Position.Y, Is.EqualTo(line.Position.Y));
            Assert.That(caret.Position.X, Is.EqualTo(line.Position.X + Measure(line.Text.Substring(0, 5)) - Measure(_textInput.CaretCharacter) / 2));
        }

        [Test]
        public void TestArrowsUpAndDownFollowWrappedLinesAndKeepX()
        {
            CreateSystem(LongText);
            var lines = GetDrawnLines();

            // start from a column in the middle of first line
            var first = lines[0];
            int startCol = first.Text.Length / 2;
            int desiredX = first.Position.X + Measure(first.Text.Substring(0, startCol));
            Click(new Point(desiredX + 1, first.Position.Y + 10));
            Assert.That(_textInput.CaretOffset, Is.EqualTo(first.SourceStart + startCol));

            // go down through all lines and make sure caret is always at nearest boundary to the original X
            for (int i = 1; i < lines.Count; ++i)
            {
                Press(TextInputCommands.MoveCaretDown);
                Assert.That(_textInput.CaretOffset, Is.EqualTo(lines[i].SourceStart + NearestColumn(lines, i, desiredX)), $"Moving down to line {i}: '{lines[i].Text}'");
            }

            // going down from last line goes to end
            Press(TextInputCommands.MoveCaretDown);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(_textInput.Value.Length));

            // go back up, desired X is kept as long as we only move up and down
            for (int i = lines.Count - 2; i >= 0; --i)
            {
                Press(TextInputCommands.MoveCaretUp);
                Assert.That(_textInput.CaretOffset, Is.EqualTo(lines[i].SourceStart + NearestColumn(lines, i, desiredX)), $"Moving up to line {i}: '{lines[i].Text}'");
            }

            // going up from first line goes to start
            Press(TextInputCommands.MoveCaretUp);
            Assert.That(_textInput.CaretOffset, Is.EqualTo(0));
        }

        /// <summary>
        /// Get the column in line nearest to a relative X position.
        /// </summary>
        static int NearestColumn(List<(string Text, Point Position, int SourceStart)> lines, int lineIndex, int x)
        {
            var line = lines[lineIndex].Text;
            int relativeX = x - lines[lineIndex].Position.X;
            bool brokenMidWord = (lineIndex < lines.Count - 1) && (lines[lineIndex + 1].SourceStart == lines[lineIndex].SourceStart + line.Length);
            int best = 0;
            for (int col = 0; col <= line.Length; ++col)
            {
                if (Math.Abs(Measure(line.Substring(0, col)) - relativeX) <= Math.Abs(Measure(line.Substring(0, best)) - relativeX))
                {
                    best = col;
                }
            }
            return (brokenMidWord && best == line.Length) ? best - 1 : best;
        }
    }
}
