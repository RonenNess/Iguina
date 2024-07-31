using Iguina.Defs;
using Iguina.Entities;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;


namespace Iguina.Demo
{
    /// <summary>
    /// Initialize demo project.
    /// </summary>
    public class IguinaDemoStarter
    {
        // ui system instance
        Iguina.UISystem _system = null!;

        /// <summary>
        /// Start the demo.
        /// </summary>
        /// <param name="renderer">Renderer implementation.</param>
        /// <param name="input">Input provider.</param>
        /// <param name="uiThemeFolder">Ui theme folder path.</param>
        public void Start(Drivers.IRenderer renderer, Drivers.IInputProvider input, string uiThemeFolder)
        {
            // create system
            _system = new UISystem(Path.Combine(uiThemeFolder, "system_style.json"), renderer, input);
            _system.DebugRenderEntities = false;

            // load some alt stylesheets that are not loaded by default from the system stylesheet
            var hProgressBarAltStyle = StyleSheet.LoadFromJsonFile(Path.Combine(uiThemeFolder, "Styles", "progress_bar_horizontal_alt.json"));
            var hProgressBarAltFillStyle = StyleSheet.LoadFromJsonFile(Path.Combine(uiThemeFolder, "Styles", "progress_bar_horizontal_alt_fill.json"));
            var panelTitleStyle = StyleSheet.LoadFromJsonFile(Path.Combine(uiThemeFolder, "Styles", "panel_title.json"));

            // create button for enable/ disable debug mode
            {
                Button enableDebugModeBtn = new Button(_system, "Debug Mode")
                {
                    ToggleCheckOnClick = true,
                    Anchor = Anchor.TopRight
                };
                enableDebugModeBtn.Events.OnValueChanged = (Entity entity) =>
                {
                    _system.DebugRenderEntities = enableDebugModeBtn.Checked;
                };
                enableDebugModeBtn.Size.X.SetPixels(200);
                _system.Root.AddChild(enableDebugModeBtn);
            }

            // create button to open git repo
            {
                Button openGitRepoBtn = new Button(_system, "Git Repo")
                {
                    ToggleCheckOnClick = true,
                    Anchor = Anchor.TopLeft
                };
                openGitRepoBtn.Events.OnValueChanged = (Entity entity) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/RonenNess/Iguina",
                        UseShellExecute = true
                    });
                };
                openGitRepoBtn.Size.X.SetPixels(200);
                _system.Root.AddChild(openGitRepoBtn);
            }

            // create panel for demo buttons
            Panel demoSelection;
            {
                // create panel to select demo
                demoSelection = new Panel(_system);
                demoSelection.Anchor = Anchor.CenterLeft;
                demoSelection.AutoWidth = true;
                demoSelection.AutoHeight = true;
                _system.Root.AddChild(demoSelection);

                // add title and horizontal line
                demoSelection.AddChild(new Title(_system, "Demo Selection:")
                {
                    Anchor = Anchor.AutoCenter
                });
                demoSelection.AddChild(new HorizontalLine(_system));
            }

            // create container for a demo example
            List<Panel> _demoPanels = new();
            Panel CreateDemoContainer(string demoTitle, Point size)
            {
                // create panel
                bool isFirst = _demoPanels.Count == 0;
                var panel = new Panel(_system);
                panel.Size.SetPixels(size.X, size.Y);
                panel.Anchor = Anchor.Center;
                panel.AutoHeight = true;
                panel.OverflowMode = OverflowMode.HideOverflow;
                panel.Visible = _demoPanels.Count == 0;
                _system.Root.AddChild(panel);
                _demoPanels.Add(panel);

                // add title and underline
                panel.AddChild(new Title(_system, demoTitle) { Anchor = Anchor.AutoCenter });
                panel.AddChild(new HorizontalLine(_system));

                // create button to select this demo
                var button = new Button(_system, demoTitle);
                button.Identifier = demoTitle;
                button.Anchor = Anchor.AutoLTR;
                button.Events.OnClick = (Entity entity) =>
                {
                    foreach (var sibling in _demoPanels) { sibling.Visible = false; }
                    panel.Visible = true;
                };
                button.ToggleCheckOnClick = true;
                button.ExclusiveSelection = true;
                button.CanClickToUncheck = false;
                button.Checked = isFirst;
                demoSelection.AddChild(button);

                // return panel
                return panel;
            }

            // intro page
            {
                var panel = CreateDemoContainer("Welcome To Iguina!", new Point(690, 1));
                panel.AddChild(new Paragraph(_system, $@"Welcome to ${{FC:00FFFF}}Iguina${{RESET}} UI demo!

Iguina is framework-agnostic UI library that can work with any rendering framework, provided the host application offers 'drivers' for rendering and input. In this demo, we are using '${{FC:00FFFF}}{renderer.GetType().Name}${{RESET}}' drivers.

On the left panel, you'll find a list of all the subjects included in this demo. ${{FC:00FF00}}Click on a subject to select it and explore the components and features it showcases${{RESET}}.

PS. this demo UI theme was made by me and its public domain, so feel free to use it!") { TextOverflowMode = TextOverflowMode.WrapWords });
            }

            // anchors
            {
                var panel = CreateDemoContainer("Anchors", new Point(780, 1));
                panel.AddChild(new Paragraph(_system,
                    @"In Iguina, Entities are positioned using Anchors. An Anchor can be a pre-defined position on the parent entity, like Top-Left, or an automatic anchor, like AutoLTR, which places entities in rows from left to right.

The panel below shows all built-in non-automatic anchors:"));

                var anchorsPanel = new Panel(_system);
                anchorsPanel.Size.X.SetPercents(100f);
                anchorsPanel.Size.Y.SetPixels(400);
                anchorsPanel.Anchor = Anchor.AutoCenter;
                panel.AddChild(anchorsPanel);

                anchorsPanel.AddChild(new Paragraph(_system, "TopLeft") { Anchor = Anchor.TopLeft });
                anchorsPanel.AddChild(new Paragraph(_system, "TopRight") { Anchor = Anchor.TopRight });
                anchorsPanel.AddChild(new Paragraph(_system, "TopCenter") { Anchor = Anchor.TopCenter });
                anchorsPanel.AddChild(new Paragraph(_system, "BottomLeft") { Anchor = Anchor.BottomLeft });
                anchorsPanel.AddChild(new Paragraph(_system, "BottomRight") { Anchor = Anchor.BottomRight });
                anchorsPanel.AddChild(new Paragraph(_system, "BottomCenter") { Anchor = Anchor.BottomCenter });
                anchorsPanel.AddChild(new Paragraph(_system, "CenterLeft") { Anchor = Anchor.CenterLeft });
                anchorsPanel.AddChild(new Paragraph(_system, "CenterRight") { Anchor = Anchor.CenterRight });
                anchorsPanel.AddChild(new Paragraph(_system, "Center") { Anchor = Anchor.Center });
            }

            // auto anchors
            {
                var panel = CreateDemoContainer("Auto Anchors", new Point(750, 1));
                panel.AddChild(new Paragraph(_system,
                    @"Previously we saw regular Anchors. Now its time to explore the Automatic anchors.

Auto Anchors are a set of anchors that place entities automatically, based on their siblings. For example:"));

                panel.AddChild(new RowsSpacer(_system));
                var anchorsPanel = new Panel(_system);
                anchorsPanel.Size.X.SetPercents(100f);
                anchorsPanel.AutoHeight = true;
                anchorsPanel.Anchor = Anchor.AutoCenter;
                panel.AddChild(anchorsPanel);

                {
                    anchorsPanel.AddChild(new Paragraph(_system, "AutoLTR first item.") { Anchor = Anchor.AutoLTR });
                    anchorsPanel.AddChild(new Paragraph(_system, "AutoLTR second item. Will be in a different row.") { Anchor = Anchor.AutoLTR });
                    var btn = anchorsPanel.AddChild(new Button(_system, "Button set to AutoLTR too.") { Anchor = Anchor.AutoLTR });
                    btn.Size.X.SetPixels(400);
                }
                anchorsPanel.AddChild(new HorizontalLine(_system));
                {
                    anchorsPanel.AddChild(new Paragraph(_system, "This item is AutoRTL.") { Anchor = Anchor.AutoRTL });
                    anchorsPanel.AddChild(new Paragraph(_system, "AutoRTL second item. Will be in a different row.") { Anchor = Anchor.AutoRTL });
                    var btn = anchorsPanel.AddChild(new Button(_system, "Button set to AutoRTL too.") { Anchor = Anchor.AutoRTL });
                    btn.Size.X.SetPixels(400);
                }
                anchorsPanel.AddChild(new HorizontalLine(_system));
                {
                    {
                        anchorsPanel.AddChild(new Paragraph(_system, "We also have inline anchors that arrange entities next to each other, and only break line when need to. For example, AutoInlineLTR buttons:") { Anchor = Anchor.AutoLTR });
                        for (int i = 0; i < 5; ++i)
                        {
                            var btn = anchorsPanel.AddChild(new Button(_system, "AutoInlineLTR") { Anchor = Anchor.AutoInlineLTR });
                            btn.Size.X.SetPixels(200);
                        }
                    }
                }
            }

            // panels
            {
                var panel = CreateDemoContainer("Panels", new Point(650, 1));
                panel.AddChild(new Paragraph(_system,
                    @"Panels are simple containers for entities. They can have graphics, like the panel this text is in, or be transparent and used only for grouping.

For example, see these two buttons and two paragraphs? Each set is inside an invisible panel that takes up 50% of the parent panel's width. One is aligned left, the other right: "));
                panel.AddChild(new HorizontalLine(_system));

                {
                    var panelLeft = new Panel(_system, null!);
                    panelLeft.Size.X.SetPercents(50f);
                    panelLeft.Size.Y.SetPixels(160);
                    panelLeft.Anchor = Anchor.AutoInlineLTR;
                    panel.AddChild(panelLeft);

                    panelLeft.AddChild(new Paragraph(_system, "This is left panel!\n"));
                    panelLeft.AddChild(new Button(_system));
                }
                {
                    var panelRight = new Panel(_system, null!);
                    panelRight.Size.X.SetPercents(50f);
                    panelRight.Size.Y.SetPixels(160);
                    panelRight.Anchor = Anchor.AutoInlineLTR;
                    panel.AddChild(panelRight);

                    panelRight.AddChild(new Paragraph(_system, "This is right panel!\n"));
                    panelRight.AddChild(new Button(_system));
                }

                panel.AddChild(new Paragraph(_system, @"You can add a small title to panels when you create them. It's not a built-in feature in Iguina, but its very easy to pull off: "));
                panel.AddChild(new RowsSpacer(_system, 2));
                {
                    var titledPanel = new Panel(_system);
                    titledPanel.Size.X.SetPercents(100f);
                    titledPanel.Size.Y.SetPixels(150);
                    titledPanel.Anchor = Anchor.AutoLTR;
                    panel.AddChild(titledPanel);

                    var title = new Paragraph(_system, panelTitleStyle, "Panel Title");
                    titledPanel.AddChild(title);
                    title.Anchor = Anchor.TopCenter;
                    title.Offset.Y.SetPixels(-26);

                    titledPanel.AddChild(new Paragraph(_system, "Looks nice, isn't it? Check out the source code to see how we did it."));
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system,
                    @"Did you know that entities can be draggable? This panel can be dragged, lets try it out!
The small box in the corner is draggable too:"));
                panel.DraggableMode = DraggableMode.DraggableConfinedToScreen;

                // create draggable small box
                var draggableBox = panel.AddChild(new Panel(_system)
                {
                    DraggableMode = DraggableMode.DraggableConfinedToParent,
                    Anchor = Anchor.BottomRight,
                });
                draggableBox.Size.SetPixels(20, 20);
            }

            // buttons
            {
                var panel = CreateDemoContainer("Buttons", new Point(650, 1));
                panel.AddChild(new Paragraph(_system,@"If you see this panel, it means you already used buttons! Here's a simple button with an OnClick event handler:"));
                {
                    int clicksCount = 0;
                    var btn = panel.AddChild(new Button(_system, "Click Me!"));
                    btn.Events.OnClick += (Entity entity) =>
                    {
                        clicksCount++;
                        btn.Paragraph.Text = "Thanks x " + clicksCount;
                    };
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system,
    @"Buttons can also function as checkboxes, allowing you to click on them to toggle their state (checked/unchecked):"));
                {
                    var btn = panel.AddChild(new Button(_system, "Toggle Me!"));
                    btn.ToggleCheckOnClick = true;
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system,
    @"And they can even function as radio buttons:"));
                {
                    var btn = panel.AddChild(new Button(_system, "Pick Me!"));
                    btn.ToggleCheckOnClick = true;
                    btn.CanClickToUncheck = false;
                    btn.ExclusiveSelection = true;
                }
                {
                    var btn = panel.AddChild(new Button(_system, "No, pick Me!"));
                    btn.ToggleCheckOnClick = true;
                    btn.CanClickToUncheck = false;
                    btn.ExclusiveSelection = true;

                }
                {
                    var btn = panel.AddChild(new Button(_system, "Ignore them pick me!!"));
                    btn.ToggleCheckOnClick = true;
                    btn.CanClickToUncheck = false;
                    btn.ExclusiveSelection = true;
                }
            }

            // paragraphs
            {
                var panel = CreateDemoContainer("Paragraphs", new Point(650, 1));
                panel.AddChild(new Paragraph(_system,
                    @"${FC:00FF00}Paragraphs${RESET} are entities that draw text.
They can be used as labels for buttons, titles, or long texts like the one you read now.

${FC:00FF00}Paragraphs${RESET} support special ${OC:FF0000}style changing commands${RESET}, so you can easily ${OC:00FFFF,FC:000000,OW:2}highlight specific words${RESET} within the paragraph.

You can change ${FC:00FF00}Fill Color${RESET}, ${OC:AA0000}Outline Color${RESET}, and ${OW:0}Outline Width${RESET}. 

And you can even embed icons ${ICO:Textures/UI.png|0|64|16|16|2}      inside text paragraphs!

To learn more, check out the source code of this demo project, or read the ${FC:FF00FF}official docs${RESET}.

PS. you can also change the way words wrap when exceeding the parent width."));
                
            }

            // checkbox and radio
            {
                var panel = CreateDemoContainer("Checkbox / Radio", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Iguina provides a basic Checkbox entity:"));
                panel.AddChild(new Checkbox(_system, "Checkbox Option 1"));
                panel.AddChild(new Checkbox(_system, "Checkbox Option 2"));
                panel.AddChild(new Checkbox(_system, "Checkbox Option 3"));

                panel.AddChild(new HorizontalLine(_system));

                panel.AddChild(new Paragraph(_system, @"Iguina also provides radio button entities:"));
                panel.AddChild(new RadioButton(_system, "Radio Option 1")).Checked = true;
                panel.AddChild(new RadioButton(_system, "Radio Option 2"));
                panel.AddChild(new RadioButton(_system, "Radio Option 3"));
            }

            // sliders
            {
                var panel = CreateDemoContainer("Sliders", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Sliders are useful to select numeric values:"));
                {
                    var slider = panel.AddChild(new Slider(_system));
                    var label = panel.AddChild(new Label(_system, @$"Slider Value: {slider.Value}"));
                    panel.AddChild(new RowsSpacer(_system, 2));
                    slider.Events.OnValueChanged = (Entity entity) => { label.Text = $"Slider Value: {slider.Value}"; };
                }

                panel.AddChild(new Paragraph(_system, @"Sliders can also be vertical:"));
                {
                    var slider = panel.AddChild(new Slider(_system, Orientation.Vertical));
                    
                    slider.Size.Y.SetPixels(280);
                    slider.Offset.X.SetPixels(40);
                    var label = panel.AddChild(new Label(_system, @$"Slider Value: {slider.Value}"));
                    slider.Events.OnValueChanged = (Entity entity) => { label.Text = $"Slider Value: {slider.Value}"; };
                }
            }

            // progress bars
            {
                var panel = CreateDemoContainer("Progress Bars", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Progress Bars are similar to sliders, but are designed to show progress or things like health bars:")); 
                {
                    var progressBar = panel.AddChild(new ProgressBar(_system));
                    var label = panel.AddChild(new Label(_system, @$"Progress Bar Value: {progressBar.Value}"));
                    panel.AddChild(new RowsSpacer(_system));
                    float _timeForNextValueChange = 3f;
                    progressBar.Events.AfterUpdate = (Entity entity) =>
                    {
                        _timeForNextValueChange -= _system.LastDeltaTime;
                        if (_timeForNextValueChange <= 0f)
                        {
                            progressBar.Value = Random.Shared.Next(progressBar.MaxValue);
                            _timeForNextValueChange = 3f;
                        }
                    };
                    progressBar.Events.OnValueChanged = (Entity entity) => { label.Text = $"Progress Bar Value: {progressBar.Value}"; };
                }

                panel.AddChild(new Paragraph(_system, @"By default Progress Bars are not interactable, but you can make them behave like sliders by settings 'IgnoreInteractions' to false:"));
                {
                    var progressBar = panel.AddChild(new ProgressBar(_system));
                    var label = panel.AddChild(new Label(_system, @$"Progress Bar Value: {progressBar.Value}"));
                    panel.AddChild(new RowsSpacer(_system));
                    progressBar.Handle.OverrideStyles.TintColor = new Color(255, 0, 0, 255);
                    progressBar.IgnoreInteractions = false;
                    progressBar.Events.OnValueChanged = (Entity entity) => { label.Text = $"Progress Bar Value: {progressBar.Value}"; };
                }

                panel.AddChild(new Paragraph(_system, @"And finally, here's an alternative progress bar design, without animation:"));
                {
                    var progressBar = panel.AddChild(new ProgressBar(_system, hProgressBarAltStyle, hProgressBarAltFillStyle));
                    progressBar.Size.X.SetPixels(420 + 36);
                    progressBar.MaxValue = 11;
                    progressBar.Value = 6;
                    progressBar.IgnoreInteractions = false;
                    progressBar.Anchor = Anchor.AutoCenter;
                }
            }

            // for lists and dropdowns
            List<string> dndClasses = new List<string> { "Barbarian", "Bard", "Cleric", "Druid", "Fighter", "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard", "Artificer", "Blood Hunter", "Mystic", "Psion", "Alchemist", "Cavalier", "Hexblade", "Arcane Archer", "Samurai", "Zzz" };
            dndClasses.Sort(StringComparer.OrdinalIgnoreCase);

            // list box
            {
                var panel = CreateDemoContainer("List Box", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"List Boxes allow you to add items and select them from a list. For example:"));
                panel.AddChild(new RowsSpacer(_system));
                {
                    panel.AddChild(new Label(_system, @"Select Race:"));
                    var listbox = panel.AddChild(new ListBox(_system));
                    listbox.AddItem("Human");
                    listbox.SetItemLabel("Human", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(0, 0, 32, 32) }, true);
                    listbox.AddItem("Elf");
                    listbox.SetItemLabel("Elf", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(32, 0, 32, 32) }, true);
                    listbox.AddItem("Orc");
                    listbox.SetItemLabel("Orc", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(64, 0, 32, 32) }, true);
                    listbox.AddItem("Dwarf");
                    listbox.SetItemLabel("Dwarf", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(96, 0, 32, 32) }, true);
                    listbox.AutoHeight = true;
                    listbox.AllowDeselect = false;
                }
                {
                    panel.AddChild(new Paragraph(_system, @"Did you notice that you can't clear selection once a value is set? That is a configurable property. In the class selection below, you can clear by clicking the selected item again."));
                    panel.AddChild(new RowsSpacer(_system));
                    panel.AddChild(new Label(_system, @"Select Class:"));
                    var listbox = panel.AddChild(new ListBox(_system));
                    listbox.AutoHeight = false;
                    foreach (var val in dndClasses)
                    {
                        listbox.AddItem(val);
                    }
                    var selectedParagraph = panel.AddChild(new Paragraph(_system));
                    selectedParagraph.Text = "Selected Class: None";
                    listbox.Events.OnValueChanged = (Entity entity) =>
                    {
                        selectedParagraph.Text = "Selected Class: " + (listbox.SelectedValue ?? "None");
                    };
                }
            }

            // drop down
            {
                var panel = CreateDemoContainer("Drop Down", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Drop Down entities are basically list boxes, but the list is hidden while not interacted with. For example:"));
                panel.AddChild(new RowsSpacer(_system));
                {
                    panel.AddChild(new Label(_system, @"Select Race:"));
                    var dropdown = panel.AddChild(new DropDown(_system));
                    dropdown.DefaultSelectedText = "< Select Race >";
                    dropdown.AddItem("Human");
                    dropdown.SetItemLabel("Human", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(0, 0, 32, 32) }, true);
                    dropdown.AddItem("Elf");
                    dropdown.SetItemLabel("Elf", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(32, 0, 32, 32) }, true);
                    dropdown.AddItem("Orc");
                    dropdown.SetItemLabel("Orc", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(64, 0, 32, 32) }, true);
                    dropdown.AddItem("Dwarf");
                    dropdown.SetItemLabel("Dwarf", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(96, 0, 32, 32) }, true);
                    dropdown.AddItem("Gnome");
                    dropdown.SetItemLabel("Gnome", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(128, 0, 32, 32) }, true);
                    dropdown.AddItem("Tiefling");
                    dropdown.SetItemLabel("Tiefling", new IconTexture() { TextureId = "Textures/Icons.png", SourceRect = new Rectangle(160, 0, 32, 32) }, true);
                    dropdown.AllowDeselect = false;
                    dropdown.AutoHeight = true;
                }
                {
                    panel.AddChild(new Paragraph(_system, @"In the dropdown below, you can clear selection by clicking the selected item again."));
                    panel.AddChild(new RowsSpacer(_system));
                    panel.AddChild(new Label(_system, @"Select Class:"));
                    var dropdown = panel.AddChild(new DropDown(_system));
                    dropdown.SetVisibleItemsCount(7);
                    dropdown.DefaultSelectedText = "< Select Class >";
                    foreach (var val in dndClasses)
                    {
                        dropdown.AddItem(val);
                    }
                    var selectedParagraph = panel.AddChild(new Paragraph(_system));
                    selectedParagraph.Text = "Selected Class: None";
                    dropdown.Events.OnValueChanged = (Entity entity) =>
                    {
                        selectedParagraph.Text = "Selected Class: " + (dropdown.SelectedValue ?? "None");
                    };
                }
            }

            // color inputs
            {
                var panel = CreateDemoContainer("Color Pickers", new Point(650, 350));
                panel.AutoHeight = true;

                // color slider
                {
                    panel.AddChild(new Paragraph(_system, @"Iguina provides Color Slider entities, which are used to get a color value from a range, using a slider and a source texture:"));
                    var slider = panel.AddChild(new ColorSlider(_system));
                    var value = panel.AddChild(new Label(_system));
                    slider.Events.OnValueChanged = (Entity entity) =>
                    {
                        var color = slider.ColorValue;
                        value.Text = $"Color value: {color.R}, {color.G}, {color.B}, {color.A}";
                        value.OverrideStyles.TextFillColor = color;
                    };
                    slider.Value = 0;
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new HorizontalLine(_system));

                // color picker
                {
                    panel.AddChild(new Paragraph(_system, @"Iguina also provides Color Picker entities, which are used to get a color value from a rectangle region, by picking pixels off a source texture:"));
                    var picker = panel.AddChild(new ColorPicker(_system));
                    var value = panel.AddChild(new Label(_system));
                    picker.Events.OnValueChanged = (Entity entity) =>
                    {
                        var color = picker.ColorValue;
                        value.Text = $"Color value: {color.R}, {color.G}, {color.B}, {color.A}";
                        value.OverrideStyles.TextFillColor = color;
                    };
                }
            }

            // scrollbars
            {
                var panel = CreateDemoContainer("Scrollbars", new Point(780, 350));
                panel.AutoHeight = false;
                panel.CreateVerticalScrollbar(true);
                panel.AddChild(new Paragraph(_system, @"Sometimes panels content is too long, and we need scrollbars to show everything.
This panel has some random entities below that go wayyyy down.

Use the scrollbar on the right to see more of it."));
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Button(_system, "Some Button"));
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Button(_system, "Another Button"));
                panel.AddChild(new Slider(_system));
                panel.AddChild(new Checkbox(_system, "A Checkbox") );
                panel.AddChild(new RadioButton(_system, "A Radio Button") );
                var listbox = panel.AddChild(new ListBox(_system));
                listbox.AddItem("Human");
                listbox.AddItem("Elf");
                listbox.AddItem("Orc");
                listbox.AddItem("Dwarf");
                listbox.Size.Y.SetPixels(170);
            }

            // text input
            {
                var panel = CreateDemoContainer("Text Input", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Text Input entity is useful to get free text input from users. This is a single-line text input:"));
                {
                    var textInput = panel.AddChild(new TextInput(_system));
                    textInput.PlaceholderText = "Click to edit text input.";
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"And here's a multiline text input:"));
                {
                    var textInput = panel.AddChild(new TextInput(_system));
                    textInput.PlaceholderText = "A multiline text input..\nClick to edit.";
                    textInput.Size.Y.SetPixels(300);
                    textInput.Multiline = true;
                    //textInput.MaxLines = 8;
                    textInput.CreateVerticalScrollbar();
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"You can also mask the text, for password input:"));
                {
                    var textInput = panel.AddChild(new TextInput(_system));
                    textInput.PlaceholderText = "Password";
                    textInput.MaskingCharacter = '*';
                    var showPassword = panel.AddChild(new Checkbox(_system, "Show Password"));
                    showPassword.Events.OnValueChanged = (Entity entity) =>
                    {
                        textInput.MaskingCharacter = showPassword.Checked ? null : '*';
                    };
                }
            }

            // numeric text input
            {
                var panel = CreateDemoContainer("Numeric Input", new Point(680, 1));

                panel.AddChild(new Paragraph(_system, @"Numeric text input get float or integer value from the user in a form similar to a text input. For example, with decimal point:"));
                {
                    var textInput = panel.AddChild(new NumericInput(_system));
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"This Numeric Input don't accept a decimal point:"));
                {
                    var textInput = panel.AddChild(new NumericInput(_system));
                    textInput.AcceptsDecimal = false;
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"This Numeric Input has min and max limits (-10, 10):"));
                {
                    var textInput = panel.AddChild(new NumericInput(_system));
                    textInput.MinValue = -10;
                    textInput.MaxValue = 10;
                }

                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"You can also create a Numeric Input entity without the buttons:"));
                {
                    var textInput = panel.AddChild(new NumericInput(_system, false, false));
                }
            }

            // message boxes
            {
                var panel = CreateDemoContainer("Message Boxes", new Point(780, 1));
                panel.AddChild(new Paragraph(_system, @"Message boxes are useful to get quick input from the user. Iguina comes with a utility to quickly generate common message boxes.

Click below to see an example."));

                panel.AddChild(new Button(_system, "Show Message Box")).Events.OnClick = (Entity entity) =>
                {
                    _system.MessageBoxes.ShowConfirmMessageBox("Hi There!", 
                        @"This is a simple message box with just confirm / cancel options.

Note that message boxes can have their own stylesheets, and you can set their defaults per-system.

This specific message box won't do much.
You can just close it.");
                };

                panel.AddChild(new RowsSpacer(_system));
            }

            // locked / disabled
            {
                var panel = CreateDemoContainer("Locked / Disabled", new Point(780, 1));
                panel.AddChild(new Paragraph(_system, @"You can disable entities to make them ignore user interactions and render them with 'disabled' effect (in this demo, grayscale):"));
                panel.AddChild(new Button(_system, "Disabled Button") { Enabled = false });
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"When you disable a panel, all entities under it will be disabled too.

If you want to just lock items without rendering them with 'disabled' style, you can also set the Locked property. For example the following button is locked, but will render normally:"));
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Button(_system, "Locked Button") { Locked = true });
                panel.AddChild(new RowsSpacer(_system));
                panel.AddChild(new Paragraph(_system, @"Any type of entity can be locked and disabled and locked:"));
                panel.AddChild(new Slider(_system) { Enabled = false });
                panel.AddChild(new Checkbox(_system, "Disabled Checkbox") { Enabled = false });
                panel.AddChild(new RadioButton(_system, "Disabled Radio Button") { Enabled = false });
                var listbox = panel.AddChild(new ListBox(_system));
                listbox.AddItem("Human");
                listbox.AddItem("Elf");
                listbox.AddItem("Orc");
                listbox.AddItem("Dwarf");
                listbox.Size.Y.SetPixels(140);
                listbox.Enabled = false;
            }
        }

        /// <summary>
        /// Perform updates.
        /// </summary>
        public void Update(float deltaTime)
        {
            _system.Update(deltaTime);
        }

        /// <summary>
        /// Draw the UI.
        /// </summary>
        public void Draw()
        {
            _system.Draw();
        }
    }
}