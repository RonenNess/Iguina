using Iguina.Utils;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="MessageBoxUtils"/>.
    /// </summary>
    public class MessageBoxTests
    {
        [Test]
        public void CloseWithoutBackdrop()
        {
            var system = new UISystem(new TestRenderer(), new TestInputProvider());
            var options = system.MessageBoxes.DefaultOptions;
            options.AddBackdrop = false;

            var handle = system.MessageBoxes.ShowInfoMessageBox("Title", "Text", options: options);
            Assert.That(handle.Backdrop, Is.Null);

            Assert.DoesNotThrow(() => handle.Close());
            Assert.That(handle.MessageBoxPanel.Parent, Is.Null);
        }

        [Test]
        public void CloseWithBackdrop()
        {
            var system = new UISystem(new TestRenderer(), new TestInputProvider());

            var handle = system.MessageBoxes.ShowInfoMessageBox("Title", "Text");
            Assert.That(handle.Backdrop, Is.Not.Null);

            handle.Close();
            Assert.That(handle.Backdrop!.Parent, Is.Null);
            Assert.That(handle.MessageBoxPanel.Parent, Is.Null);
        }

        [Test]
        public void ShowMessageBoxWithCustomButtons()
        {
            var system = new UISystem(new TestRenderer(), new TestInputProvider());

            var handle = system.MessageBoxes.ShowMessageBox("Title", "Text", new MessageBoxUtils.MessageBoxButtons[]
            {
                new MessageBoxUtils.MessageBoxButtons("Save", () => { }),
                new MessageBoxUtils.MessageBoxButtons("Don't Save", () => { }),
                new MessageBoxUtils.MessageBoxButtons("Cancel", () => true),
            });

            Assert.That(handle.Buttons.Length, Is.EqualTo(3));
            Assert.That(handle.Buttons[1].Paragraph.Text, Is.EqualTo("Don't Save"));
        }
    }
}
