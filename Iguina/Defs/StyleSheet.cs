using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Iguina.Defs
{
    /// <summary>
    /// Define the graphics style of a GUI entity and a state.
    /// </summary>
    public class StyleSheetState
    {
        /// <summary>
        /// Fill texture, tiled and with frame over the target region.
        /// </summary>
        public FramedTexture? FillTextureFramed { get; set; }

        /// <summary>
        /// Fill texture, stretched over the target region.
        /// </summary>
        public StretchedTexture? FillTextureStretched { get; set; }

        /// <summary>
        /// Fill texture, with size based on its source rectangle.
        /// </summary>
        public IconTexture? Icon { get; set; }

        /// <summary>
        /// Tint color for textures.
        /// </summary>
        public Color? TintColor { get; set; }

        /// <summary>
        /// Background color to draw.
        /// </summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>
        /// Text alignment for entities with text.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TextAlignment? TextAlignment { get; set; }

        /// <summary>
        /// Font identifier, or null to use default font.
        /// </summary>
        public string? FontIdentifier { get; set; }

        /// <summary>
        /// Text fill color.
        /// </summary>
        public Color? TextFillColor { get; set; }

        /// <summary>
        /// Text fill color to use when entity has no value.
        /// </summary>
        public Color? NoValueTextFillColor { get; set; }

        /// <summary>
        /// Text outline color.
        /// </summary>
        public Color? TextOutlineColor { get; set; }

        /// <summary>
        /// Text outline width.
        /// </summary>
        public int? TextOutlineWidth { get; set; }

        /// <summary>
        /// Text spacing factor.
        /// 1f = Default spacing.
        /// </summary>
        public float? TextSpacing { get; set; }

        /// <summary>
        /// Font size for text.
        /// </summary>
        public int? FontSize { get; set; }

        /// <summary>
        /// If defined, will scale texts by this factor.
        /// This is useful combined with inheritance - you can define font size in one place, and use scale to adjust size for specific entity types based on it.
        /// </summary>
        public float? TextScale { get; set; }

        /// <summary>
        /// Effect to use while rendering this entity.
        /// This can be used by the host application as actual shader name, a flag identifier to change rendering, or any other purpose.
        /// </summary>
        public string? EffectIdentifier { get; set; }

        /// <summary>
        /// Internal padding, in pixels.
        /// Padding decrease the size of the internal bounding rect of entities.
        /// </summary>
        public Sides? Padding { get; set; }

        /// <summary>
        /// Expand the bounding rectangle of this entity by these values.
        /// </summary>
        public Sides? ExtraSize { get; set; }

        /// <summary>
        /// Extra offset to add to entities with auto anchor from their siblings.
        /// </summary>
        public Point? MarginBefore { get; set; }

        /// <summary>
        /// Extra offset to add to next entities in parent that has auto anchor from this entity.
        /// </summary>
        public Point? MarginAfter { get; set; }

        /// <summary>
        /// Outline width, in pixels, around the bounding rectangle borders of the entity.
        /// </summary>
        public Sides? BoxOutlineWidth { get; set; }

        /// <summary>
        /// Box outline offset, in pixels.
        /// </summary>
        public Point? BoxOutlineOffset { get; set; }

        /// <summary>
        /// Outline color for the bounding rectangle outlines, if set.
        /// </summary>
        public Color? BoxOutlineColor { get; set; }
    }

    /// <summary>
    /// Define the graphics style of a GUI entity.
    /// </summary>
    public class StyleSheet
    {
        // cached property getters
        static Dictionary<string, PropertyInfo> _cachedProperties = new();

        /// <summary>
        /// Inherit properties from another stylesheet file.
        /// Only works when loading from files.
        /// </summary>
        public string? InheritFrom { get; set; }

        /// <summary>
        /// Default entity width.
        /// </summary>
        public Measurement? DefaultWidth { get; set; }

        /// <summary>
        /// Default entity height.
        /// </summary>
        public Measurement? DefaultHeight { get; set; }

        /// <summary>
        /// Entity min width.
        /// </summary>
        public int? MinWidth { get; set; }

        /// <summary>
        /// Entity min height.
        /// </summary>
        public int? MinHeight { get; set; }

        /// <summary>
        /// Default anchor for the entity.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Anchor? DefaultAnchor { get; set; }

        /// <summary>
        /// Default text anchor for entities with text.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Anchor? DefaultTextAnchor { get; set; }

        /// <summary>
        /// Default stylesheet.
        /// </summary>
        public StyleSheetState? Default { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is targeted.
        /// </summary>
        public StyleSheetState? Targeted { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is the focused entity.
        /// </summary>
        public StyleSheetState? Focused { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is interacted with.
        /// </summary>
        public StyleSheetState? Interacted { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is checked.
        /// </summary>
        public StyleSheetState? Checked { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is targeted checked.
        /// </summary>
        public StyleSheetState? TargetedChecked { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is disabled.
        /// </summary>
        public StyleSheetState? Disabled { get; set; }

        /// <summary>
        /// Stylesheet for when the entity is disabled, but also checked.
        /// </summary>
        public StyleSheetState? DisabledChecked { get; set; }

        // stylesheet to use for null state.
        static StyleSheetState _nullStyle = new StyleSheetState();

        /// <summary>
        /// If bigger than 0, will interpolate between states of the entity using this value as speed factor.
        /// If this value is 0 (default), will not perform interpolation.
        /// </summary>
        /// <remarks>This value affect textures and colors, but not other state properties.</remarks>
        public float? InterpolateStatesSpeed { get; set; }

        /// <summary>
        /// If bigger than 0, will interpolate internal moving parts of this entity using this value as speed factor.
        /// If this value is 0, will not perform interpolation on offset.
        /// Note: this does not affect the entire entity offset, its only for things like slider entity handle offset and internal mechanisms.
        /// Default value is 0.
        /// </summary>
        public float? InterpolateOffsetsSpeed { get; set; }

        /// <summary>
        /// Get stylesheet for a given entity state.
        /// </summary>
        public StyleSheetState GetStyle(EntityState state)
        {
            StyleSheetState? ret = Default;
            switch (state)
            {
                case EntityState.Disabled: ret = Disabled; break;
                case EntityState.DisabledChecked: ret = DisabledChecked; break;
                case EntityState.Interacted: ret = Interacted; break;
                case EntityState.Targeted: ret = Targeted; break;
                case EntityState.Focused: ret = Focused; break;
                case EntityState.TargetedChecked: ret = TargetedChecked; break;
                case EntityState.Checked: ret = Checked; break;
            }
            return ret ?? _nullStyle;
        }

        /// <summary>
        /// Get property value by entity state, or return Default if not set for given state.
        /// </summary>
        /// <typeparam name="T">Property type.</typeparam>
        /// <param name="propertyName">Property name.</param>
        /// <param name="state">Entity state to get property for.</param>
        /// <param name="defaultValue">Default value to return if not found.</param>
        /// <param name="overrideProperties">If provided, will first attempt to get property from this state.</param>
        /// <returns>Value from state, from default, or the given default</returns>
        public T? GetProperty<T>(string propertyName, EntityState state, T? defaultValue, StyleSheetState? overrideProperties)
        {
            // get property info
            if (!_cachedProperties.TryGetValue(propertyName, out var propertyInfo))
            {
                propertyInfo = typeof(StyleSheetState).GetProperty(propertyName);
                if (propertyInfo == null) { throw new Exception($"Stylesheet property '{propertyName}' is not defined!"); }
                _cachedProperties[propertyName] = propertyInfo;
            }

            // get state and value
            var stylesheet = GetStyle(state);
            bool valueFound = false; // <-- due to c# stupidity, you can't set T? to null properly..
            T? ret = default; 
            try
            {
                object? value = propertyInfo.GetValue(stylesheet);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException($"Stylesheet property name '{propertyName}' type is '{propertyInfo.GetType().Name}', but it was requested as type '{typeof(T).Name}'!", e);
            }

            // check if we got override style
            if (overrideProperties != null)
            {
                object? value = propertyInfo.GetValue(overrideProperties);
                if (value != null) 
                { 
                    ret = (T?)value;
                    return ret;
                }
            }

            // special: if state is disabled checked and not defined, revert to disabled state before trying default
            if (!valueFound && (state == EntityState.DisabledChecked) && (Disabled != null)) 
            {
                object? value = propertyInfo.GetValue(Disabled);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }

            // special: if state is targeted checked and not defined, revert to checked state before trying default
            if (!valueFound && (state == EntityState.TargetedChecked) && (Checked != null)) 
            {
                object? value = propertyInfo.GetValue(Checked);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }

            // special: if state is targeted checked and not defined not even in checked, revert to interacted state before trying default
            if (!valueFound && (state == EntityState.TargetedChecked) && (Interacted != null))
            {
                object? value = propertyInfo.GetValue(Interacted);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }

            // special: if state is checked and not defined, revert to interacted state before trying default
            if (!valueFound && (state == EntityState.Checked) && (Interacted != null))
            {
                object? value = propertyInfo.GetValue(Interacted);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }

            // if not set and not default, try to get from default
            if (!valueFound && (stylesheet != Default) && (Default != null)) 
            { 
                object? value = propertyInfo.GetValue(Default);
                if (value != null) { ret = (T?)value; valueFound = true; }
            }

            // return value or default
            return valueFound ? ret : defaultValue;
        }

        /// <summary>
        /// Load and return stylesheet from Json file content.
        /// </summary>
        /// <param name="content">Json file content.</param>
        /// <returns>Loaded stylesheet.</returns>
        public static StyleSheet LoadFromJsonMemory(string content)
        {
            return JsonSerializer.Deserialize<StyleSheet>(content)!;
        }

        /// <summary>
        /// Load and return stylesheet from JSON file path.
        /// </summary>
        /// <param name="filename">Json file path.</param>
        /// <returns>Loaded stylesheet.</returns>
        public static StyleSheet LoadFromJsonFile(string filename)
        {
            var ret = LoadFromJsonMemory(File.ReadAllText(filename));
            if (ret.InheritFrom != null)
            {
                var folder = Path.GetDirectoryName(filename)!;
                var parent = LoadFromJsonFile(Path.Combine(folder, ret.InheritFrom));
                ret.InterpolateOffsetsSpeed = ret.InterpolateOffsetsSpeed ?? parent.InterpolateOffsetsSpeed;
                ret.InterpolateStatesSpeed = ret.InterpolateStatesSpeed ?? parent.InterpolateStatesSpeed;
                ret.DefaultTextAnchor = ret.DefaultTextAnchor ?? parent.DefaultTextAnchor;
                ret.DefaultAnchor = ret.DefaultAnchor ?? parent.DefaultAnchor;
                ret.MinHeight = ret.MinHeight ?? parent.MinHeight;
                ret.MinWidth = ret.MinWidth ?? parent.MinWidth;
                ret.DefaultWidth = ret.DefaultWidth ?? parent.DefaultWidth;
                ret.DefaultHeight = ret.DefaultHeight ?? parent.DefaultHeight;
                ret.Default = InheritStylesheetState(parent.Default, ret.Default);
                ret.Targeted = InheritStylesheetState(parent.Targeted, ret.Targeted);
                ret.TargetedChecked = InheritStylesheetState(parent.TargetedChecked, ret.TargetedChecked);
                ret.DisabledChecked = InheritStylesheetState(parent.DisabledChecked, ret.DisabledChecked);
                ret.Checked = InheritStylesheetState(parent.Checked, ret.Checked);
                ret.Interacted = InheritStylesheetState(parent.Interacted, ret.Interacted);
                ret.Disabled = InheritStylesheetState(parent.Disabled, ret.Disabled);
            }
            return ret;
        }

        /// <summary>
        /// Combine parent and derived entity stylesheet state.
        /// </summary>
        public static StyleSheetState? InheritStylesheetState(StyleSheetState? parent, StyleSheetState? derived)
        {
            // if both are null, return null
            if ((parent == null) && (derived == null))
            {
                return null;
            }

            // if one of them is null, return the other
            if ((parent == null) || (derived == null))
            {
                return derived ?? parent;
            }

            // perform merge of all stylesheet properties
            var ret = new StyleSheetState();
            PropertyInfo[] properties = typeof(StyleSheetState).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    object? value = property.GetValue(derived) ?? property.GetValue(parent);
                    if (value != null)
                    {
                        property.SetValue(ret, value);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Serialize this stylesheet into Json content.
        /// </summary>
        /// <returns>Json content.</returns>
        public string SaveToJsonMemory()
        {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Serialize this stylesheet into Json file.
        /// </summary>
        public void SaveToJsonFile(string filename)
        {
            File.WriteAllText(filename, SaveToJsonMemory());
        }
    }
}
