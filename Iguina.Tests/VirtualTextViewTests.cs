using Iguina.Defs;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for the virtual text view entity.
    /// </summary>
    public class VirtualTextViewTests
    {
        /// <summary>
        /// Provider that records which entries were requested.
        /// </summary>
        class RecordingProvider : IVirtualTextProvider
        {
            public int Count;
            public HashSet<int> Requested = new();
            public RecordingProvider(int count) { Count = count; }
            public int GetEntriesCount() => Count;
            public string GetEntry(int index)
            {
                Assert.That(index, Is.InRange(0, Count - 1));
                Requested.Add(index);
                return "line " + index;
            }
        }

        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        VirtualTextView _view = null!;
        RecordingProvider _provider = null!;

        void CreateSystem(int entriesCount, int visibleEntries = 5)
        {
            _input = new ScriptedInputProvider();
            _system = new UISystem(new TestRenderer(), _input);
            _provider = new RecordingProvider(entriesCount);
            _view = _system.Root.AddChild(new VirtualTextView(_system, _provider));
            _view.Anchor = Anchor.TopLeft;
            _view.Size.X.SetPixels(400);
            _view.SetVisibleEntriesCount(visibleEntries);
            _input.MousePosition = new Point(2000, 2000);
            Frames(2);
        }

        void Frames(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                _system.Update(0.01f);
                _system.Draw();
            }
        }

        [Test]
        public void TestOnlyVisibleEntriesAreRequested()
        {
            CreateSystem(1_000_000);
            Assert.That(_view.EntriesCount, Is.EqualTo(1_000_000));
            Assert.That(_view.VisibleEntriesCount, Is.EqualTo(5));
            Assert.That(_provider.Requested.Max(), Is.LessThan(10));
            Assert.That(_view.VerticalScrollbar!.MaxValue, Is.EqualTo(1_000_000 - 5));
            Assert.That(_view.VerticalScrollbar.Visible, Is.True);
        }

        [Test]
        public void TestScrolling()
        {
            CreateSystem(1000);
            _view.ScrollOffset = 500;
            _provider.Requested.Clear();
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(500));
            Assert.That(_provider.Requested.Min(), Is.EqualTo(500));
            Assert.That(_provider.Requested.Max(), Is.LessThan(510));

            _view.ScrollToEnd();
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(1000 - 5));
            Assert.That(_provider.Requested, Does.Contain(999));

            _view.ScrollToStart();
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(0));

            _view.ScrollToEntry(20);
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(16));
            _view.ScrollToEntry(17);
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(16));
            _view.ScrollToEntry(3);
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(3));
        }

        [Test]
        public void TestNoScrollbarWhenEntriesFit()
        {
            CreateSystem(3);
            Assert.That(_view.VerticalScrollbar!.Visible, Is.False);
            Assert.That(_view.ScrollOffset, Is.EqualTo(0));

            CreateSystem(0);
            Assert.That(_view.VerticalScrollbar!.Visible, Is.False);
            Assert.That(_provider.Requested, Is.Empty);
        }

        [Test]
        public void TestEntriesCountShrinking()
        {
            CreateSystem(100);
            _view.ScrollToEnd();
            Frames(1);
            _provider.Count = 10;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(5));
            _provider.Count = 2;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(0));
        }

        [Test]
        public void TestAutoScrollToEnd()
        {
            CreateSystem(10);
            _view.AutoScrollToEnd = true;
            _view.ScrollToEnd();
            Frames(1);
            _provider.Count = 50;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(45));

            // scroll up and stop following
            _view.ScrollOffset = 10;
            Frames(1);
            _provider.Count = 80;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(10));
        }

        [Test]
        public void TestWithoutAutoScrollToEnd()
        {
            CreateSystem(10);
            _view.ScrollToEnd();
            Frames(1);
            _provider.Count = 50;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(5));
        }

        [Test]
        public void TestMouseWheelScrollsOneEntry()
        {
            CreateSystem(100);
            _view.OverrideStyles.Padding = new Sides(10, 10, 10, 30);
            _view.SetVisibleEntriesCount(5);
            Frames(2);
            Assert.That(_view.VisibleEntriesCount, Is.EqualTo(5));

            // point on an entry, and then on the view's padding (below the last entry)
            var rect = _view.LastBoundingRect;
            foreach (var mousePos in new[] { new Point(rect.X + 50, _view.LastInternalBoundingRect.Y + _view.EntryHeight / 2), new Point(rect.X + 50, rect.Bottom - 1) })
            {
                _view.ScrollToStart();
                _input.MousePosition = mousePos;
                Frames(2);

                _input.MouseWheelChange = -1;
                Frames(1);
                Assert.That(_view.ScrollOffset, Is.EqualTo(1), $"Mouse at {mousePos.X}, {mousePos.Y}, targeted: {_system.TargetedEntity?.GetType().Name}");

                _input.MouseWheelChange = 1;
                Frames(1);
                Assert.That(_view.ScrollOffset, Is.EqualTo(0));
            }
        }

        [Test]
        public void TestCountChangingEveryFrame()
        {
            var random = new Random(1234);
            foreach (var autoScroll in new[] { false, true })
            {
                CreateSystem(50);
                _view.AutoScrollToEnd = autoScroll;
                for (int frame = 0; frame < 2000; ++frame)
                {
                    // change count every frame, sometimes to 0, sometimes to a value lower than current offset
                    _provider.Count = random.Next(5) switch
                    {
                        0 => 0,
                        1 => random.Next(3),
                        2 => Math.Max(0, _view.ScrollOffset - random.Next(10)),
                        3 => random.Next(1_000_000),
                        _ => _provider.Count + random.Next(20),
                    };

                    // randomly scroll
                    switch (random.Next(8))
                    {
                        case 0: _view.ScrollOffset = random.Next(-100, 2_000_000); break;
                        case 1: _view.ScrollToEnd(); break;
                        case 2: _view.ScrollToStart(); break;
                        case 3: _view.ScrollToEntry(random.Next(-100, 2_000_000)); break;
                        case 4: _input.MouseWheelChange = random.Next(-3, 4); break;
                    }

                    // provider asserts every requested index is in range
                    Frames(1);
                    Assert.That(_view.ScrollOffset, Is.InRange(0, Math.Max(0, _provider.Count - _view.VisibleEntriesCount)));
                }
            }
        }

        [Test]
        public void TestNegativeCountNullProviderAndSwapping()
        {
            CreateSystem(-5);
            Assert.That(_view.EntriesCount, Is.EqualTo(0));
            Assert.That(_provider.Requested, Is.Empty);

            _view.Provider = null;
            _view.ScrollToEnd();
            _view.ScrollToEntry(10);
            Frames(2);
            Assert.That(_view.EntriesCount, Is.EqualTo(0));

            _view.Provider = new RecordingProvider(500);
            _view.ScrollOffset = 400;
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(400));

            _view.Provider = new RecordingProvider(3);
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(0));
        }

        [Test]
        public void TestBadEntriesText()
        {
            CreateSystem(0);
            var entries = new[] { null!, "", "multi\nline\r\ntext", "bad ${style} command", "${FC:ZZZ}bad color", "${", "}${}", "good ${FC:FF0000}red${RESET}" };
            _view.Provider = new CallbackVirtualTextProvider(() => entries.Length, (int i) => entries[i]);
            _view.SetVisibleEntriesCount(entries.Length);

            // with and without style commands, and toggling back and forth
            foreach (var enable in new[] { false, true, false, true })
            {
                _view.EnableStyleCommands = enable;
                Assert.DoesNotThrow(() => Frames(2));
            }
        }

        [Test]
        public void TestHugeCountAndDraggingScrollbar()
        {
            CreateSystem(int.MaxValue);
            Assert.That(_view.VerticalScrollbar!.MaxValue, Is.EqualTo(int.MaxValue - 5));

            // drag scrollbar handle all the way down and back up
            var scrollbar = _view.VerticalScrollbar;
            var x = scrollbar.LastBoundingRect.Center.X;
            _input.MousePosition = new Point(x, scrollbar.LastBoundingRect.Top + 1);
            Frames(2);
            _input.LeftDown = true;
            Frames(2);
            for (int y = scrollbar.LastBoundingRect.Top; y <= scrollbar.LastBoundingRect.Bottom + 50; y += 3)
            {
                _input.MousePosition = new Point(x, y);
                Frames(1);
            }
            Assert.That(_view.ScrollOffset, Is.EqualTo(scrollbar.MaxValue));
            Assert.That(_provider.Requested.Max(), Is.EqualTo(int.MaxValue - 1));
            _input.MousePosition = new Point(x, scrollbar.LastBoundingRect.Top - 50);
            Frames(1);
            Assert.That(_view.ScrollOffset, Is.EqualTo(0));
            _input.LeftDown = false;
            Frames(1);

            // keyboard / wheel at the end
            _view.ScrollToEnd();
            _input.MouseWheelChange = -5;
            Frames(2);
            Assert.That(_view.ScrollOffset, Is.EqualTo(int.MaxValue - 5));

            // auto height with huge count
            _view.AutoHeight = true;
            _view.AutoHeightMaxSize = 300;
            Assert.DoesNotThrow(() => Frames(2));
        }

        [Test]
        public void TestTinyView()
        {
            CreateSystem(100);
            _view.OverrideStyles.Padding = new Sides(10, 10, 30, 30);
            foreach (var height in new[] { 0, 1, 5, 40, 61 })
            {
                _view.Size.Y.SetPixels(height);
                Assert.DoesNotThrow(() => Frames(2));
                _view.ScrollToEnd();
                Assert.DoesNotThrow(() => Frames(2));
            }
        }

        /// <summary>
        /// Create a view with a recording renderer and entries stylesheet with margins.
        /// </summary>
        (ProportionalRenderer Renderer, VirtualTextView View) CreateRecordingView(IVirtualTextProvider provider, int marginBefore, int marginAfter, int visibleEntries = 5)
        {
            var renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(renderer, _input);
            var entriesStyle = new StyleSheet() { Default = new StyleSheetState() { MarginBefore = new Point(0, marginBefore), MarginAfter = new Point(0, marginAfter) } };
            var view = _system.Root.AddChild(new VirtualTextView(_system, null, entriesStyle, provider));
            view.Anchor = Anchor.TopLeft;
            view.Size.X.SetPixels(400);
            view.SetVisibleEntriesCount(visibleEntries);
            _input.MousePosition = new Point(2000, 2000);
            _view = view;
            Frames(2);
            return (renderer, view);
        }

        static List<(string Text, Point Position)> DrawnEntries(ProportionalRenderer renderer, string prefix = "line ")
        {
            return renderer.DrawnTexts.Where(x => x.Text.StartsWith(prefix)).OrderBy(x => x.Position.Y).ToList();
        }

        void RecordFrame(ProportionalRenderer renderer)
        {
            Frames(1);
            renderer.ClearRecords();
            Frames(1);
        }

        [Test]
        public void TestRowsDrawnEntryHeightApartWithMargins()
        {
            var (renderer, view) = CreateRecordingView(new ListVirtualTextProvider(Enumerable.Range(0, 100).Select(i => "line " + i).ToList()), 2, 6);
            Assert.That(view.EntryHeight, Is.EqualTo(20 + 2 + 6));
            Assert.That(view.VisibleEntriesCount, Is.EqualTo(5));

            RecordFrame(renderer);
            var drawn = DrawnEntries(renderer);
            Assert.That(drawn.Count, Is.GreaterThanOrEqualTo(5));
            for (int i = 1; i < drawn.Count; ++i)
            {
                Assert.That(drawn[i].Position.Y - drawn[i - 1].Position.Y, Is.EqualTo(view.EntryHeight));
            }

            // 5 rows fill the view: last fully visible row ends within one margin of the bottom
            var bottom = view.LastInternalBoundingRect.Bottom;
            Assert.That(drawn[4].Position.Y + 20, Is.LessThanOrEqualTo(bottom));
            Assert.That(bottom - (drawn[4].Position.Y + 20), Is.LessThanOrEqualTo(6));

            // override margins at runtime: entry height and drawing follow
            view.OverrideEntryStyles.MarginBefore = new Point(0, 0);
            view.OverrideEntryStyles.MarginAfter = new Point(0, 0);
            Assert.That(view.EntryHeight, Is.EqualTo(20));
            RecordFrame(renderer);
            drawn = DrawnEntries(renderer);
            for (int i = 1; i < drawn.Count; ++i)
            {
                Assert.That(drawn[i].Position.Y - drawn[i - 1].Position.Y, Is.EqualTo(20));
            }
            Assert.That(view.VisibleEntriesCount, Is.EqualTo(5 * 28 / 20));
        }

        [Test]
        public void TestScrollToEndShowsLastEntryFully()
        {
            var (renderer, view) = CreateRecordingView(new ListVirtualTextProvider(Enumerable.Range(0, 100).Select(i => "line " + i).ToList()), 0, 6, 7);
            view.ScrollToEnd();
            RecordFrame(renderer);
            Assert.That(view.ScrollOffset, Is.EqualTo(view.VerticalScrollbar!.MaxValue));
            var last = DrawnEntries(renderer).Last();
            Assert.That(last.Text, Is.EqualTo("line 99"));
            Assert.That(last.Position.Y + 20, Is.LessThanOrEqualTo(view.LastInternalBoundingRect.Bottom));
        }

        [Test]
        public void TestEntriesDontOverlapScrollbar()
        {
            var longLine = string.Concat(Enumerable.Repeat("word ", 30));
            var (renderer, view) = CreateRecordingView(new ListVirtualTextProvider(Enumerable.Range(0, 100).Select(i => "line " + i + " " + longLine).ToList()), 0, 0);
            view.EntriesOverflowMode = TextOverflowMode.WrapWords;
            RecordFrame(renderer);
            Assert.That(view.VerticalScrollbar!.Visible, Is.True);
            var scrollbarLeft = view.VerticalScrollbar.LastBoundingRect.Left;
            Assert.That(renderer.DrawnTexts, Is.Not.Empty);
            foreach (var text in renderer.DrawnTexts)
            {
                Assert.That(text.Position.X + ProportionalRenderer.Measure(text.Text), Is.LessThanOrEqualTo(scrollbarLeft), text.Text);
                Assert.That(ProportionalRenderer.Measure(text.Text), Is.LessThanOrEqualTo(view.EntryTextWidth));
            }
        }

        [Test]
        public void TestWrappedAndMultilineEntries()
        {
            var longLine = string.Concat(Enumerable.Repeat("word ", 30)).Trim();
            var entries = Enumerable.Range(0, 50).Select(i => i % 2 == 0 ? $"line {i} short" : $"line {i} {longLine}").ToList();
            var (renderer, view) = CreateRecordingView(new ListVirtualTextProvider(entries), 0, 4, 10);
            view.EntriesOverflowMode = TextOverflowMode.WrapWords;
            RecordFrame(renderer);

            // long entries wrap into multiple lines, so fewer entries fit
            Assert.That(view.VisibleEntriesCount, Is.LessThan(10));
            Assert.That(renderer.DrawnTexts.Count(x => x.Text.StartsWith("word") || x.Text.Contains(" word")), Is.GreaterThan(view.VisibleEntriesCount / 2));

            // lines of the same entry are packed (line height apart), entries are separated by margin
            var drawn = renderer.DrawnTexts.OrderBy(x => x.Position.Y).ToList();
            for (int i = 1; i < drawn.Count; ++i)
            {
                var diff = drawn[i].Position.Y - drawn[i - 1].Position.Y;
                Assert.That(diff, Is.EqualTo(drawn[i].Text.StartsWith("line ") ? 24 : 20));
            }

            // scroll to end shows last entry fully, and scrolling to an entry makes it fully visible
            view.ScrollToEnd();
            RecordFrame(renderer);
            var bottomText = renderer.DrawnTexts.OrderBy(x => x.Position.Y).Last();
            Assert.That(bottomText.Position.Y + 20, Is.LessThanOrEqualTo(view.LastInternalBoundingRect.Bottom));
            Assert.That(renderer.DrawnTexts.Any(x => x.Text.StartsWith("line 49")), Is.True);

            view.ScrollToStart();
            Frames(1);
            view.ScrollToEntry(30);
            RecordFrame(renderer);
            Assert.That(view.ScrollOffset + view.VisibleEntriesCount - 1, Is.EqualTo(30));

            // line breaks
            view.EntriesOverflowMode = TextOverflowMode.Overflow;
            view.Provider = new ListVirtualTextProvider(new[] { "line A\nsecond\r\nthird", "line B" });
            RecordFrame(renderer);
            Assert.That(renderer.DrawnTexts.Select(x => x.Text), Does.Contain("line A second third"));
            view.AllowLineBreaks = true;
            RecordFrame(renderer);
            var texts = renderer.DrawnTexts.OrderBy(x => x.Position.Y).Select(x => x.Text).ToList();
            Assert.That(texts, Is.EqualTo(new[] { "line A", "second", "third", "line B" }));
        }

        [Test]
        public void TestDefaultThemeRowsMatchEntryHeight()
        {
            // the default theme paragraph style has MarginAfter.Y > 0, which used to make layout and drawing disagree
            var themePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../Iguina.Demo/Assets/DefaultTheme"));
            var renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(Path.Combine(themePath, "system_style.json"), renderer, _input);
            var view = _system.Root.AddChild(new VirtualTextView(_system, new ListVirtualTextProvider(Enumerable.Range(0, 100).Select(i => "line " + i).ToList())));
            view.Anchor = Anchor.TopLeft;
            view.Size.X.SetPixels(500);
            view.SetVisibleEntriesCount(14);
            _view = view;
            Assert.That(view.EntryHeight, Is.GreaterThan(20));
            RecordFrame(renderer);

            Assert.That(view.VisibleEntriesCount, Is.EqualTo(14));
            var drawn = DrawnEntries(renderer).Select(x => x.Position).Distinct().ToList();
            for (int i = 1; i < drawn.Count; ++i)
            {
                Assert.That(drawn[i].Y - drawn[i - 1].Y, Is.EqualTo(view.EntryHeight));
            }
            Assert.That(view.LastInternalBoundingRect.Bottom - (drawn[13].Y + 20), Is.LessThan(view.EntryHeight - 20 + 1));
        }

        [Test]
        public void TestListAndCallbackProviders()
        {
            var list = new List<string> { "a", "b" };
            var listProvider = new ListVirtualTextProvider(list);
            list.Add("c");
            Assert.That(listProvider.GetEntriesCount(), Is.EqualTo(3));
            Assert.That(listProvider.GetEntry(2), Is.EqualTo("c"));

            var callbackProvider = new CallbackVirtualTextProvider(() => 7, (int i) => "x" + i);
            Assert.That(callbackProvider.GetEntriesCount(), Is.EqualTo(7));
            Assert.That(callbackProvider.GetEntry(3), Is.EqualTo("x3"));
        }
    }
}
