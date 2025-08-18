using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Color buttons is a type of color picker that instead of a slider or position based selection, it uses buttons with colors assigned to them.
    /// This is useful for color pickers that have a predefined set of colors to pick from.
    /// </summary>
    public class ColorButtons : Entity, IColorPicker
    {
        /// <summary>
        /// Get / set the picker color value.
        /// </summary>
        public Color ColorValue
        {
            get => (_colorButtons.Count == 0) ? Color.Black : _colorButtons[ColorIndex].Color;
            set
            {
                var index = _colorButtons.FindIndex(c => c.Color == value);
                if (index < 0) {
                    throw new ArgumentException("Color not found in the color buttons list.", nameof(value));
                }
                ColorIndex = (ushort)index;
            }
        }

        /// <summary>
        /// Get / set the color index of the selected color.
        /// </summary>
        public ushort ColorIndex
        {
            get => (_colorButtons.Count == 0) ? (ushort)0 : _selectedColorIndex;
            set
            {
                if (_selectedColorIndex == value) { return; }
                if (value < 0 || value >= _colorButtons.Count)
                    throw new ArgumentOutOfRangeException(nameof(value), "Color index is out of range.");
                _selectedColorIndex = value;
                _colorButtons[_selectedColorIndex].ButtonEntity.Checked = true;
                Events.OnValueChanged?.Invoke(this);
            }
        }
        ushort _selectedColorIndex = ushort.MaxValue;

        /// <summary>
        /// Get currently selected color label.
        /// </summary>
        public string ColorLabel => _colorButtons.Count == 0 ? string.Empty : _colorButtons[ColorIndex].ButtonEntity.Paragraph.Text;

        /// <summary>
        /// Color choices.
        /// </summary>
        struct ColorButton
        {
            public Color Color;
            public Button ButtonEntity;
        }
        List<ColorButton> _colorButtons = new List<ColorButton>();

        // button stylesheet
        StyleSheet? _buttonsStylesheet;

        /// <inheritdoc/>
        public void SetColorValueApproximate(Color value)
        {   
            // find the closest color in the buttons list
            if (_colorButtons.Count == 0) {
                throw new InvalidOperationException("No colors available in the color buttons.");
            }

            // find the closest color
            var closestColor = _colorButtons.OrderBy(c => Color.Distance(c.Color, value)).First();
            ColorValue = closestColor.Color;
        }

        /// <summary>
        /// Create the color buttons picker.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Color buttons container stylesheet, used for the entire entity background.</param>
        /// <param name="buttonsStylesheet">Color buttons stylesheet, used for the color buttons themselves.</param>
        public ColorButtons(UISystem system, StyleSheet? stylesheet, StyleSheet? buttonsStylesheet) 
            : base(system, stylesheet)
        {
            _buttonsStylesheet = buttonsStylesheet;
        }

        /// <summary>
        /// Create the color buttons picker with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public ColorButtons(UISystem system) : this(system, system.DefaultStylesheets.ColorButtonsPanel, system.DefaultStylesheets.ColorButtonsButton)
        {
        }

        /// <summary>
        /// Add a color selection button.
        /// The button background color will be set to the given color.
        /// </summary>
        /// <param name="color">Color choice to add.</param>
        /// <param name="label">Optional color label.</param>
        public void AddColor(Color color, string label = "")
        {
            // create the button entity
            var btn = new Button(UISystem, _buttonsStylesheet, label);
            btn.ExclusiveSelection = true;
            btn.ToggleCheckOnClick = true;
            btn.CanClickToUncheck = false;
            btn.Anchor = Anchor.AutoInlineLTR;
            AddChild(btn);

            // add to buttons selection list
            _colorButtons.Add(new ColorButton
            {
                Color = color,
                ButtonEntity = btn
            });

            // add checked event
            ushort index = (ushort)(_colorButtons.Count - 1);
            btn.Events.OnChecked += (Entity entity) =>
            {
                ColorIndex = index;
            };

            // set button color
            btn.OverrideStyles.BackgroundColor = color;

            // if first color added, select it
            if (_colorButtons.Count == 1) {
                ColorIndex = 0;
            }
        }
    }
}
