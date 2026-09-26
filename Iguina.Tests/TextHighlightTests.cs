using Iguina.Defs;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for paragraph text highlights and the 'TextHighlightColor' style property.
    /// </summary>
    public class TextHighlightTests
    {
        const string Text = "Hello wonderful world, what a Wonderful day";

        static readonly Color StyleColor = new Color(10, 20, 30, 40);

        ProportionalRenderer _renderer = null!;
        UISystem _system = null!;

        void CreateSystem()
        {
            _renderer = new ProportionalRenderer();
            _system = new UISystem(_renderer, new ScriptedInputProvider());
        }

        static StyleSheet CreateStylesheet(Color? highlightColor)
        {
            return new StyleSheet() { Default = new StyleSheetState() { TextHighlightColor = highlightColor } };
        }

        void Frame()
        {
            _renderer.ClearRecords();
            _system.Update(0.001f);
            _system.Draw();
        }

        Paragraph CreateParagraph(StyleSheet? stylesheet)
        {
            var paragraph = _system.Root.AddChild(new Paragraph(_system, stylesheet, Text));
            paragraph.Anchor = Anchor.TopLeft;
            Frame();
            Frame();
            return paragraph;
        }

        /// <summary>
        /// Get expected highlight rectangle for a range in the first (and only) line.
        /// </summary>
        (int X, int Right) ExpectedRange(int start, int end)
        {
            var line = _renderer.DrawnTexts.First();
            return (line.Position.X + ProportionalRenderer.Measure(Text.Substring(0, start)), line.Position.X + ProportionalRenderer.Measure(Text.Substring(0, end)));
        }

        [Test]
        public void TestHighlightUsesStyleColorAndPosition()
        {
            CreateSystem();
            var paragraph = CreateParagraph(CreateStylesheet(StyleColor));

            paragraph.SetHighlight(6, 15);
            Frame();
            var rect = _renderer.DrawnRectangles.Single(x => x.Color.Equals(StyleColor)).Rect;
            Assert.That((rect.X, rect.Right), Is.EqualTo(ExpectedRange(6, 15)));

            // reversed range is normalized
            paragraph.SetHighlight(15, 6);
            Assert.That(paragraph.Highlights.Single().Start, Is.EqualTo(6));
            Assert.That(paragraph.Highlights.Single().End, Is.EqualTo(15));
        }

        [Test]
        public void TestHighlightDefaultAndCustomColors()
        {
            CreateSystem();
            var paragraph = CreateParagraph(CreateStylesheet(null));

            // no style property: default color
            paragraph.AddHighlight(0, 5);

            // custom color overrides style
            var custom = new Color(255, 0, 0, 100);
            paragraph.AddHighlight(6, 15, custom);
            Frame();
            Assert.That(_renderer.DrawnRectangles.Count(x => x.Color.Equals(Paragraph.DefaultTextHighlightColor)), Is.EqualTo(1));
            Assert.That(_renderer.DrawnRectangles.Count(x => x.Color.Equals(custom)), Is.EqualTo(1));

            paragraph.ClearHighlights();
            Frame();
            Assert.That(paragraph.Highlights, Is.Empty);
            Assert.That(_renderer.DrawnRectangles.Where(x => x.Color.Equals(custom) || x.Color.Equals(Paragraph.DefaultTextHighlightColor)), Is.Empty);
        }

        [Test]
        public void TestHighlightOccurrences()
        {
            CreateSystem();
            var paragraph = CreateParagraph(CreateStylesheet(StyleColor));

            Assert.That(paragraph.HighlightOccurrences("wonderful"), Is.EqualTo(1));
            paragraph.ClearHighlights();
            Assert.That(paragraph.HighlightOccurrences("wonderful", StringComparison.OrdinalIgnoreCase), Is.EqualTo(2));
            Assert.That(paragraph.Highlights.Select(x => x.Start), Is.EqualTo(new[] { 6, 30 }));
            Assert.That(paragraph.HighlightOccurrences("not found"), Is.EqualTo(0));
            Assert.That(paragraph.HighlightOccurrences(string.Empty), Is.EqualTo(0));

            Frame();
            Assert.That(_renderer.DrawnRectangles.Count(x => x.Color.Equals(StyleColor)), Is.EqualTo(2));
        }

        [Test]
        public void TestHighlightRangesAreCapped()
        {
            CreateSystem();
            var paragraph = CreateParagraph(CreateStylesheet(StyleColor));

            paragraph.AddHighlight(-50, 5000);
            paragraph.AddHighlight(3, 3);
            Assert.That(paragraph.Highlights.Count, Is.EqualTo(1), "Empty ranges should be ignored");
            Frame();
            var rect = _renderer.DrawnRectangles.Single(x => x.Color.Equals(StyleColor)).Rect;
            Assert.That((rect.X, rect.Right), Is.EqualTo(ExpectedRange(0, Text.Length)));

            // text gets shorter than highlight
            paragraph.Text = "Hi";
            Assert.DoesNotThrow(Frame);
            paragraph.Text = string.Empty;
            Assert.DoesNotThrow(Frame);
        }

        [Test]
        public void TestTextInputSelectionUsesStyleColor()
        {
            CreateSystem();
            var textInput = _system.Root.AddChild(new TextInput(_system, CreateStylesheet(StyleColor)));
            textInput.Value = Text;
            Frame();

            // start editing, then select
            var input = (ScriptedInputProvider)_system.Input;
            input.MousePosition = new Point(textInput.LastBoundingRect.X + 10, textInput.LastBoundingRect.Y + 10);
            input.LeftDown = true;
            Frame();
            input.LeftDown = false;
            Frame();
            textInput.SetSelection(6, 15);
            Frame();
            Assert.That(_renderer.DrawnRectangles.Count(x => x.Color.Equals(StyleColor)), Is.EqualTo(1));
        }

        [Test]
        public void TestDefaultThemeDefinesHighlightColor()
        {
            var stylesPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../Iguina.Demo/Assets/DefaultTheme/Styles"));
            foreach (var file in new[] { "paragraph.json", "text_input.json", "numeric_input.json" })
            {
                var stylesheet = StyleSheet.LoadFromJsonFile(Path.Combine(stylesPath, file));
                Assert.That(stylesheet.GetProperty<Color?>("TextHighlightColor", EntityState.Default, null, null), Is.Not.Null, file);
            }
        }
    }
}
