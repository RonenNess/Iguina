using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for scissor regions of entities with hidden overflow.
    /// </summary>
    public class ScissorTests
    {
        /// <summary>
        /// Renderer that records the scissor region every text was drawn with.
        /// </summary>
        class ScissorRecordingRenderer : ProportionalRenderer, IRenderer
        {
            public List<(string Text, Rectangle? Scissor)> TextsScissor = new();

            public new void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
            {
                TextsScissor.Add((text, GetScissorRegion()));
            }
        }

        [Test]
        public void TestNestedHiddenOverflowRestoresScissor()
        {
            var renderer = new ScissorRecordingRenderer();
            var system = new UISystem(renderer, new ScriptedInputProvider());

            // three levels of hidden overflow entities
            var outer = system.Root.AddChild(new Panel(system));
            outer.Anchor = Anchor.TopLeft;
            outer.Size.SetPixels(600, 600);
            outer.OverflowMode = OverflowMode.HideOverflow;

            var before = outer.AddChild(new Paragraph(system, "before"));

            var middle = outer.AddChild(new Panel(system));
            middle.Size.SetPixels(300, 100);
            middle.OverflowMode = OverflowMode.HideOverflow;

            var inner = middle.AddChild(new Panel(system));
            inner.Size.SetPixels(100, 50);
            inner.OverflowMode = OverflowMode.HideOverflow;

            // paragraph after the nested panels should be clipped by outer panel only, like the paragraph before them
            var after = outer.AddChild(new Paragraph(system, "after"));

            system.Update(0.01f);
            system.Draw();
            renderer.TextsScissor.Clear();
            system.Draw();

            var expected = renderer.TextsScissor.First(x => x.Text == "before").Scissor;
            Assert.That(expected, Is.Not.Null);
            Assert.That(renderer.TextsScissor.First(x => x.Text == "after").Scissor, Is.EqualTo(expected));
        }

        [Test]
        public void TestEntitiesAfterComboBoxAreNotClipped()
        {
            var renderer = new ScissorRecordingRenderer();
            var system = new UISystem(renderer, new ScriptedInputProvider());

            var panel = system.Root.AddChild(new Panel(system));
            panel.Anchor = Anchor.TopLeft;
            panel.Size.SetPixels(600, 600);
            panel.OverflowMode = OverflowMode.HideOverflow;

            var before = panel.AddChild(new Paragraph(system, "before"));
            var combo = panel.AddChild(new ComboBox(system));
            combo.AddItem("item");
            var after = panel.AddChild(new Paragraph(system, "after"));

            system.Update(0.01f);
            system.Draw();
            renderer.TextsScissor.Clear();
            system.Draw();

            var expected = renderer.TextsScissor.First(x => x.Text == "before").Scissor;
            Assert.That(renderer.TextsScissor.First(x => x.Text == "after").Scissor, Is.EqualTo(expected));
        }
    }
}
