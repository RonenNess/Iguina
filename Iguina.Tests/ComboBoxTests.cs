using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for <see cref="ComboBox"/>.
    /// </summary>
    public class ComboBoxTests
    {
        /// <summary>
        /// Combo box that exposes displayed items count for tests.
        /// </summary>
        class TestComboBox : ComboBox
        {
            public TestComboBox(UISystem system) : base(system) { }
            public int VisibleItems => DisplayedItemsCount;
        }

        static readonly string[] Items = { "Human", "Elf", "Orc", "Dwarf", "Half Elf" };

        ProportionalRenderer _renderer = null!;
        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        TestComboBox _combo = null!;
        int _valueChangedEvents;

        void CreateSystem(bool allowFreeText)
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(_renderer, _input);
            _combo = _system.Root.AddChild(new TestComboBox(_system));
            _combo.Anchor = Anchor.TopLeft;
            _combo.Size.X.SetPixels(400);
            _combo.AllowFreeText = allowFreeText;
            foreach (var item in Items)
            {
                _combo.AddItem(item.ToLower().Replace(" ", "_"), item);
            }
            _valueChangedEvents = 0;
            _combo.Events.OnValueChanged = (Entity entity) => { _valueChangedEvents++; };
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

        void ClickAt(Point position)
        {
            _input.MousePosition = position;
            _input.LeftDown = true;
            Frame();
            _input.LeftDown = false;
            Frame();
        }

        void ClickTextInput()
        {
            ClickAt(_combo.TextInputField.LastBoundingRect.Center);
        }

        void Type(string text)
        {
            _input.TextInput.AddRange(text.Select(c => (int)c));
            Frame();
        }

        void Press(TextInputCommands command)
        {
            _input.Commands.Add(command);
            Frame();
        }

        [Test]
        public void TestTypingItemTextSelectsItem()
        {
            CreateSystem(allowFreeText: true);
            ClickTextInput();
            Type("dwarf");

            Assert.That(_combo.SelectedValue, Is.EqualTo("dwarf"));
            Assert.That(_combo.Value, Is.EqualTo("dwarf"));
            Assert.That(_combo.IsFreeText, Is.False);

            // confirming normalizes text to the item text
            Press(TextInputCommands.BreakLine);
            Assert.That(_combo.Text, Is.EqualTo("Dwarf"));
            Assert.That(_combo.IsOpened, Is.False);
        }

        [Test]
        public void TestFreeText()
        {
            CreateSystem(allowFreeText: true);
            ClickTextInput();
            Type("Goblin");

            Assert.That(_combo.SelectedValue, Is.Null);
            Assert.That(_combo.Value, Is.EqualTo("Goblin"));
            Assert.That(_combo.IsFreeText, Is.True);

            // free text remains after confirming
            Press(TextInputCommands.BreakLine);
            Assert.That(_combo.Value, Is.EqualTo("Goblin"));

            // set free text by code
            _combo.Value = "Troll";
            Assert.That(_combo.Text, Is.EqualTo("Troll"));
            Assert.That(_combo.IsFreeText, Is.True);
        }

        [Test]
        public void TestStrictModeRevertsUnmatchedText()
        {
            CreateSystem(allowFreeText: false);
            _combo.Value = "orc";
            Assert.That(_combo.Text, Is.EqualTo("Orc"));

            ClickTextInput();
            _combo.TextInputField.SelectAll();
            Type("Goblin");

            // value doesn't change while typing text that doesn't match
            Assert.That(_combo.Value, Is.EqualTo("orc"));
            Assert.That(_combo.IsFreeText, Is.False);

            // losing focus reverts text
            ClickAt(new Point(2000, 2000));
            Frame();
            Assert.That(_combo.Text, Is.EqualTo("Orc"));
            Assert.That(_combo.Value, Is.EqualTo("orc"));
        }

        [Test]
        public void TestStrictModeEnterSelectsFirstMatch()
        {
            CreateSystem(allowFreeText: false);
            ClickTextInput();
            Type("elf");

            // exact match
            Assert.That(_combo.Value, Is.EqualTo("elf"));

            Type(" ");
            Press(TextInputCommands.MoveCaretStart);
            Type("half");

            // "halfelf " matches nothing, keep previous value
            Assert.That(_combo.Value, Is.EqualTo("elf"));

            // clear and type partial text, then enter picks first filtered item
            _combo.TextInputField.SelectAll();
            Type("alf");
            Press(TextInputCommands.BreakLine);
            Assert.That(_combo.Value, Is.EqualTo("half_elf"));
            Assert.That(_combo.Text, Is.EqualTo("Half Elf"));
        }

        [Test]
        public void TestStrictModeEmptyTextClearsOnCommit()
        {
            CreateSystem(allowFreeText: false);
            _combo.Value = "orc";

            ClickTextInput();
            _combo.TextInputField.SelectAll();
            Press(TextInputCommands.Backspace);

            // value is kept while editing, and cleared when confirmed
            Assert.That(_combo.Value, Is.EqualTo("orc"));
            Press(TextInputCommands.BreakLine);
            Assert.That(_combo.Value, Is.Null);
            Assert.That(_combo.Text, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TestStrictModeRejectsUnknownValue()
        {
            CreateSystem(allowFreeText: false);
            Assert.Throws<KeyNotFoundException>(() => _combo.Value = "goblin");
            Assert.That(_combo.Value, Is.Null);
        }

        [Test]
        public void TestTypingFiltersAndOpensList()
        {
            CreateSystem(allowFreeText: true);
            Assert.That(_combo.IsOpened, Is.False);
            Assert.That(_combo.VisibleItems, Is.EqualTo(Items.Length));

            ClickTextInput();
            Type("elf");
            Frame();
            Assert.That(_combo.IsOpened, Is.True);
            Assert.That(_combo.VisibleItems, Is.EqualTo(2)); // "Elf" and "Half Elf"

            // closing the list clears the filter
            _combo.CloseList();
            Frame();
            Assert.That(_combo.VisibleItems, Is.EqualTo(Items.Length));
        }

        [Test]
        public void TestFilterIsCaseSensitiveWhenSet()
        {
            CreateSystem(allowFreeText: true);
            _combo.CaseSensitive = true;
            ClickTextInput();
            Type("elf");
            Frame();
            Assert.That(_combo.VisibleItems, Is.EqualTo(0));
            Assert.That(_combo.SelectedValue, Is.Null);
            Assert.That(_combo.Value, Is.EqualTo("elf"));
        }

        [Test]
        public void TestArrowKeysNavigateFilteredItems()
        {
            CreateSystem(allowFreeText: true);
            ClickTextInput();
            Type("elf");

            // typing "elf" already selected "Elf", so next item is "Half Elf"
            Assert.That(_combo.Value, Is.EqualTo("elf"));
            Press(TextInputCommands.MoveCaretDown);
            Assert.That(_combo.Value, Is.EqualTo("half_elf"));
            Assert.That(_combo.Text, Is.EqualTo("Half Elf"));
            Assert.That(_combo.TextInputField.CaretOffset, Is.EqualTo("Half Elf".Length));

            // wraps around, and filter is kept while navigating
            Press(TextInputCommands.MoveCaretDown);
            Assert.That(_combo.Value, Is.EqualTo("elf"));
            Press(TextInputCommands.MoveCaretUp);
            Assert.That(_combo.Value, Is.EqualTo("half_elf"));
            Assert.That(_combo.VisibleItems, Is.EqualTo(2));
        }

        [Test]
        public void TestClickingItemSelectsIt()
        {
            CreateSystem(allowFreeText: true);
            ClickTextInput();
            Assert.That(_combo.IsOpened, Is.True);

            var item = _renderer.DrawnTexts.Last(x => x.Text == "Orc");
            ClickAt(new Point(item.Position.X + 5, item.Position.Y + 5));

            Assert.That(_combo.Value, Is.EqualTo("orc"));
            Assert.That(_combo.Text, Is.EqualTo("Orc"));
            Assert.That(_combo.IsOpened, Is.False);
        }

        [Test]
        public void TestSettingSelectionByCodeUpdatesText()
        {
            CreateSystem(allowFreeText: true);
            _combo.SelectedValue = "human";
            Assert.That(_combo.Text, Is.EqualTo("Human"));
            Assert.That(_valueChangedEvents, Is.EqualTo(1));

            _combo.SelectedIndex = 1;
            Assert.That(_combo.Text, Is.EqualTo("Elf"));
            Assert.That(_valueChangedEvents, Is.EqualTo(2));

            _combo.Value = null;
            Assert.That(_combo.Text, Is.EqualTo(string.Empty));
            Assert.That(_combo.SelectedValue, Is.Null);
            Assert.That(_valueChangedEvents, Is.EqualTo(3));
        }

        [Test]
        public void TestValueChangedEventsWhileTyping()
        {
            CreateSystem(allowFreeText: false);
            ClickTextInput();

            // no events while text doesn't match anything
            Type("Hu");
            Assert.That(_valueChangedEvents, Is.EqualTo(0));

            // one event when matching an item
            Type("man");
            Assert.That(_valueChangedEvents, Is.EqualTo(1));
            Assert.That(_combo.Value, Is.EqualTo("human"));
        }
    }
}
