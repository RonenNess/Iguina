using Iguina.Defs;
using Iguina.Utils;
using System.Text.RegularExpressions;


namespace Iguina.Entities
{
    /// <summary>
    /// Entity to render text.
    /// </summary>
    public class Paragraph : Entity
    {
        /// <summary>
        /// Text to render.
        /// </summary>
        public string Text
        {
            get => _textValue;
            set
            {
                if (_textValue != value)
                {
                    _textValue = value;
                    _cachedProcessedText = null;
                }
            }
        }
        string _textValue;
        LineAndStyleCommands[]? _cachedProcessedText;
        int _cachedTextWidth = 0;
        int _cachedTextFontSize = 0;
        string? _cachedTextFontId;
        int _lineHeight;

        // called before rendering a line
        // params:
        // line string.
        // line index.
        // line first character index.
        internal Func<string, int, int, string> _beforeDrawingLineNoStyleCommands = null!;

        /// <summary>
        /// Max text width.
        /// </summary>
        public int TextMaxWidth { get; private set; }

        // text calculated height and width in pixels
        int _textHeight = 0;
        int _textWidth = 0;

        /// <summary>
        /// If true, this paragraph will support style commands.
        /// A style command is a special code wrapped inside ${}, that can change text color, outline and other style properties mid-sentence.
        /// It's useful to highlight specific words or parts of the paragraph.
        /// A single style command can hold multiple values, separated by commas.
        /// </summary>
        /// <remarks>
        /// Supported commands:
        ///     FC:RRGGBBAA                         Change fill color. RRGGBBAA is the color components in hex. AA is optional.
        ///     OC:RRGGBBAA                         Change outline color. RRGGBBAA is the color components in hex. AA is optional.
        ///     OW:Width                            Change outline width. Width is the new outline width.
        ///     ICO:Texture|sx|sy|sw|sh|scale|utc   Embed an icon inside the text. Texture = texture id, sx,sy,sw,sh = source rectangle, scale = icon scale based on source rect, utc = use text color - if true, will use text color for the icon. 
        ///     RESET                               Reset all previously-set style command properties.
        /// </remarks>
        /// <example>
        /// paragraph.Text = "Hello, ${FC:00FF00}Hero${RESET}! Welcome to my ${OC:FF00FF,OW:2}cave${RESET}."
        /// // in the example above, the word 'Hero' will be rendered with green fill color, and the word 'cave' with purple outline with width value of 2. 
        /// </example>
        public bool EnableStyleCommands = true;

        /// <summary>
        /// If true and we're trying to parse an invalid style command, will throw exception.
        /// If false, we'll just ignore that style command and move on.
        /// </summary>
        static public bool ExceptionOnInvalidStyleCommands = true;

        /// <summary>
        /// If true, will shrink entity width to text actual size.
        /// </summary>
        public bool ShrinkWidthToMinimalSize = true;

        /// <summary>
        /// If true, will shrink entity height to text actual size.
        /// </summary>
        public bool ShrinkHeightToMinimalSize = true;

        /// <summary>
        /// Set if to shrink width and height to text actual size.
        /// </summary>
        public bool ShrinkToMinimalSize
        {
            set
            {
                ShrinkWidthToMinimalSize = ShrinkHeightToMinimalSize = value;
            }
        }

        /// <summary>
        /// If true, will use text fill color from 'NoValueTextFillColor' style property.
        /// </summary>
        internal bool UseEmptyValueTextColor = false;

        /// <summary>
        /// How to handle the text if it overflows the width of the region it renders on.
        /// </summary>
        public TextOverflowMode TextOverflowMode = TextOverflowMode.WrapWords;

        /// <summary>
        /// Commands that change text style mid-paragraph.
        /// </summary>
        struct TextStyleCommand
        {
            /// <summary>
            /// Index in text this command applies from.
            /// </summary>
            public int Index;

            /// <summary>
            /// Fill color to set.
            /// </summary>
            public Color? FillColor;

            /// <summary>
            /// Outline color to set.
            /// </summary>
            public Color? OutlineColor;

            /// <summary>
            /// Outline width to set.
            /// </summary>
            public int? OutlineWidth;

            /// <summary>
            /// Icon to embed in text.
            /// </summary>
            public IconTexture? Icon;

            /// <summary>
            /// If true, will use text current color for icon (if icon is set).
            /// </summary>
            public bool IconUseTextColor;

            /// <summary>
            /// Reset all style commands.
            /// </summary>
            public bool ResetAll;

            /// <summary>
            /// Merge properties with other style command.
            /// </summary>
            public void MergeSelfWith(TextStyleCommand other)
            {
                ResetAll = ResetAll || other.ResetAll;
                OutlineWidth = other.OutlineWidth ?? OutlineWidth;
                OutlineColor = other.OutlineColor ?? OutlineColor;
                FillColor = other.FillColor ?? FillColor;
            }

            /// <summary>
            /// Null text style command.
            /// </summary>
            public static readonly TextStyleCommand Null = new TextStyleCommand();
        }

        /// <summary>
        /// Create the paragraph.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Paragraph stylesheet.</param>
        /// <param name="text">Paragraph text.</param>
        /// <param name="ignoreInteractions">If true, this paragraph will ignore user interactions.</param>
        public Paragraph(UISystem system, StyleSheet? stylesheet, string text = "New paragraph", bool ignoreInteractions = true) : base(system, stylesheet)
        {
            _textValue = text;
            IgnoreInteractions = ignoreInteractions;
        }

        /// <summary>
        /// Create the paragraph with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Paragraph text.</param>
        /// <param name="ignoreInteractions">If true, this paragraph will ignore user interactions.</param>
        public Paragraph(UISystem system, string text = "New paragraph", bool ignoreInteractions = true) : this(system, system.DefaultStylesheets.Paragraphs, text, ignoreInteractions)
        {
        }

        /// <summary>
        /// Extract style commands from input text.
        /// </summary>
        /// <param name="input">Text to parse</param>
        /// <param name="extractedCommands">List with extracted style commands.</param>
        /// <returns>Input text without the style commands part.</returns>
        /// <exception cref="FormatException">Throw exception on invalid style commands format.</exception>
        static string ExtractStyleCommands(string input, out List<TextStyleCommand> extractedCommands)
        {
            var extracted = new List<TextStyleCommand>();
            string pattern = @"\$\{(.*?)\}";

            // replace style commands with empty strings, while processing them
            int indexOffset = 0;
            string output = Regex.Replace(input, pattern, match =>
            {
                // create current command struct and break the commands text by commas
                var currCommand = new TextStyleCommand()
                {
                    Index = match.Groups[1].Index + indexOffset - 2
                };
                var commandParts = match.Groups[1].Value.Split(',');
                indexOffset -= match.Value.Length;

                // iterate all instructions in current command
                foreach (var command in commandParts)
                {
                    // split key-value and make sure not too many parts
                    var split = command.Trim().Split(':');
                    if (split.Length > 2)
                    {
                        if (ExceptionOnInvalidStyleCommands)
                        {
                            throw new FormatException("Illegal style command: " + command);
                        }
                        continue;
                    }
                    var key = split[0].Trim();
                    string? value = (split.Length == 2) ? split[1].Trim() : null;

                    // parse instruction and value
                    int expectedValuesCount = 0;
                    try
                    {
                        switch (key)
                        {
                            case "RESET":
                                currCommand.ResetAll = true;
                                break;

                            case "OW":
                                currCommand.OutlineWidth = int.Parse(value!);
                                expectedValuesCount = 1;
                                break;

                            case "FC":
                                currCommand.FillColor = Color.Parse(value!);
                                expectedValuesCount = 1;
                                break;

                            case "OC":
                                currCommand.OutlineColor = Color.Parse(value!);
                                expectedValuesCount = 1;
                                break;

                            case "ICO":
                                expectedValuesCount = 1;
                                var iconParams = value!.Split('|').Select(x => x.Trim()).ToArray();
                                currCommand.Icon = new IconTexture()
                                {
                                    TextureId = iconParams[0],
                                    SourceRect = new Rectangle(int.Parse(iconParams[1]), int.Parse(iconParams[2]), int.Parse(iconParams[3]), int.Parse(iconParams[4])),
                                    TextureScale = (iconParams.Length == 5) ? 1f : float.Parse(iconParams[5])
                                };
                                if (iconParams.Length == 7)
                                {
                                    if (iconParams[6] == "y")
                                    {
                                        currCommand.IconUseTextColor = true;
                                    }
                                    else if (iconParams[6] == "n")
                                    {
                                        currCommand.IconUseTextColor = false;
                                    }
                                    else
                                    {
                                        throw new Exception("Icon 'use text color' flag must be either 'y' or 'n'");
                                    }
                                }
                                break;

                            default:
                                throw new Exception("Unknown style command!");
                        }
                    }
                    // make sure its a valid command
                    catch (Exception e)
                    {
                        if (ExceptionOnInvalidStyleCommands)
                        {
                            throw new FormatException("Illegal style command value: " + command, e);
                        }
                    }

                    // sanity - check number of values
                    if (ExceptionOnInvalidStyleCommands && ((split.Length - 1) != expectedValuesCount))
                    {
                        throw new FormatException("Illegal number of values for command: " + command);
                    }
                }

                // add new command
                extracted.Add(currCommand);
                return string.Empty;
            });

            extractedCommands = extracted;
            return output;
        }

        // pack line and its style commands
        class LineAndStyleCommands
        {
            public string Line = null!;
            public List<TextStyleCommand>? StyleCommands;
        }

        /// <summary>
        /// Build processed text with words wrap and color commands.
        /// </summary>
        LineAndStyleCommands[] BuildProcessedText(string? font, int fontSize, float maxWidth, float spacing)
        {
            // normalize line breaks and tabs
            string normalizedString = _textValue.Replace("\r", "").Replace("\t", "   ");

            // break into lines and trim end of lines
            var _lines = new List<string>(normalizedString.Split('\n'));
            var ret = new List<LineAndStyleCommands>();
            for (int i = 0; i < _lines.Count; ++i)
            {
                ret.Add( new LineAndStyleCommands()
                {
                    Line = _lines[i]
                });
            }

            // parse style commands
            if (EnableStyleCommands)
            {
                int lineIndex = 0;
                while (lineIndex < ret.Count)
                {
                    if (ret[lineIndex].Line.Contains("${"))
                    {
                        ret[lineIndex].Line = ExtractStyleCommands(ret[lineIndex].Line, out List<TextStyleCommand> lineStyleCommands);
                        ret[lineIndex].StyleCommands = lineStyleCommands;
                    }
                    lineIndex++;
                }
            }

            // wrap words
            if (TextOverflowMode != TextOverflowMode.Overflow)
            {
                int lineIndex = 0;
                bool immediateMode = (TextOverflowMode == TextOverflowMode.WrapImmediate);
                while (lineIndex < ret.Count)
                {
                    // get current line
                    var line = ret[lineIndex].Line;

                    // measure line and check if exceed region width
                    if (line.Length > 1)
                    {
                        // break current line if exceed max width
                        void BreakLineIfNeeded(int lineWidth, int maxWidth, TextOverflowMode mode)
                        {
                            if (lineWidth > maxWidth)
                            {
                                // should we retry but with immediate mode?
                                bool retryWithImmediateMode = (mode == TextOverflowMode.WrapWords);

                                // find last space or begin from end of line
                                int lastSpaceIndex = immediateMode ? (line.Length - 1) : line.LastIndexOf(' ');
                                bool keepSearching = true;

                                // handle no spaces condition
                                bool noSpaces = false;
                                if (lastSpaceIndex == -1)
                                {
                                    lastSpaceIndex = line.Length - 1;
                                    noSpaces = true;
                                }

                                // find best place to break line
                                while (keepSearching && (lastSpaceIndex > 0))
                                {
                                    // check the width of the line if we cut it here, and if its short enough to fit in max width
                                    var cutLine = line.Substring(0, lastSpaceIndex);
                                    var cutLineWidth = UISystem.Renderer.MeasureText(cutLine, font, fontSize, spacing).X;
                                    if (cutLineWidth <= maxWidth)
                                    {
                                        // set new shortened line and next line we broke to
                                        ret[lineIndex].Line = cutLine;
                                        var nextLine = immediateMode ? line.Substring(lastSpaceIndex) : line.Substring(lastSpaceIndex + 1);
                                        ret.Insert(lineIndex + 1, new LineAndStyleCommands()
                                        {
                                            Line = nextLine
                                        });

                                        // migrate style commands to next line
                                        if (ret[lineIndex].StyleCommands?.Count > 0)
                                        {
                                            for (int i = ret[lineIndex].StyleCommands!.Count - 1; i >= 0; --i)
                                            {
                                                var currCmd = ret[lineIndex].StyleCommands![i];
                                                if (currCmd.Index >= cutLine.Length)
                                                {
                                                    currCmd.Index -= cutLine.Length + 1;
                                                    if (currCmd.Index < 0) { currCmd.Index = 0; }
                                                    ret[lineIndex + 1].StyleCommands ??= new List<TextStyleCommand>();
                                                    ret[lineIndex + 1].StyleCommands!.Insert(0, currCmd);
                                                    ret[lineIndex].StyleCommands!.RemoveAt(i);
                                                }
                                            }
                                        }

                                        // done!
                                        keepSearching = false;
                                        retryWithImmediateMode = false;
                                    }
                                    // if we're in immediate wrap mode, go one character back
                                    else if (mode == TextOverflowMode.WrapImmediate || noSpaces)
                                    {
                                        lastSpaceIndex--;
                                    }
                                    // if we're in wrap words mode, go to previous space
                                    else if (mode == TextOverflowMode.WrapWords)
                                    {
                                        lastSpaceIndex = line.LastIndexOf(' ', lastSpaceIndex - 1);
                                    }
                                }

                                // check if should retry with immediate mode
                                if (retryWithImmediateMode)
                                {
                                    BreakLineIfNeeded(lineWidth, maxWidth, TextOverflowMode.WrapImmediate);
                                }
                            }
                        }

                        var lineWidth = UISystem.Renderer.MeasureText(line, font, fontSize, spacing).X;
                        BreakLineIfNeeded(lineWidth, (int)maxWidth, TextOverflowMode);
                    }

                    // next line
                    lineIndex++;
                }
            }

            return ret.ToArray();
        }

        /// <inheritdoc/>
        protected override Point CalculateBoundingRectSize(Rectangle parentIntRect)
        {
            var ret = new Point();
            ret.X = ShrinkWidthToMinimalSize ? _textWidth : Size.X.GetValueInPixels(parentIntRect.Width);
            ret.Y = ShrinkHeightToMinimalSize ? _textHeight : Size.Y.GetValueInPixels(parentIntRect.Height);
            return ret;
        }

        /// <inheritdoc/>
        protected override Rectangle ProcessParentInternalRect(Rectangle parentIntRect)
        {
            // gives buffer from sides so text outline won't get cut due to overflow hidden mode
            parentIntRect.X += 5;
            parentIntRect.Width -= 12;
            return parentIntRect;
        }

        /// <summary>
        /// Get current font size.
        /// </summary>
        int GetFontSize()
        {
            var scale = StyleSheet.GetProperty<float>("TextScale", State, 1f, OverrideStyles);
            return (int)((float)StyleSheet.GetProperty<int>("FontSize", State, 24, OverrideStyles) * scale * UISystem.TextsScale);
        }

        /// <summary>
        /// Measure text line height, in pixels.
        /// </summary>
        public int MeasureTextLineHeight()
        {
            var fontSize = GetFontSize();
            var font = StyleSheet.GetProperty<string?>("FontIdentifier", State, null, OverrideStyles);
            return UISystem.Renderer.GetTextLineHeight(font, fontSize);
        }

        /// <summary>
        /// Measure given text size, in pixels.
        /// </summary>
        public Point MeasureText(string txt)
        {
            var fontSize = GetFontSize();
            var font = StyleSheet.GetProperty<string?>("FontIdentifier", State, null, OverrideStyles);
            return UISystem.Renderer.MeasureText(txt, font, fontSize, 1f);
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // call base rendering
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // draw text part
            if (!string.IsNullOrEmpty(Text))
            {
                // calculate max text width
                TextMaxWidth = Size.X.GetValueInPixels(parentDrawResult.InternalBoundingRect.Width) - 6;

                // calculate text params
                var state = State;
                var fontSize = GetFontSize();
                var font = StyleSheet.GetProperty<string?>("FontIdentifier", state, null, OverrideStyles);
                var alignment = StyleSheet.GetProperty("TextAlignment", state, TextAlignment.Left, OverrideStyles)!;
                var spacing = StyleSheet.GetProperty<float>("TextSpacing", state, 1f, OverrideStyles);

                // check if need to build processed text
                if ((_cachedProcessedText == null) ||
                    (_cachedTextWidth != TextMaxWidth) ||
                    (_cachedTextFontSize != fontSize) ||
                    (_cachedTextFontId != font))
                {
                    _cachedTextWidth = TextMaxWidth;
                    _cachedTextFontSize = fontSize;
                    _cachedTextFontId = font;
                    _cachedProcessedText = BuildProcessedText(font, fontSize, TextMaxWidth, spacing);
                    _lineHeight = UISystem.Renderer.GetTextLineHeight(font, fontSize);
                    _textHeight = _lineHeight * _cachedProcessedText.Length;
                    _textWidth = 0;
                    foreach (var line in _cachedProcessedText)
                    {
                        var lineWidth = UISystem.Renderer.MeasureText(line.Line, font, fontSize, spacing).X;
                        _textWidth = (int)Math.Max(_textWidth, lineWidth);
                    }
                }

                // draw text for a given state
                void DrawStateText(EntityState state, Rectangle internalBoundingRect, float alpha)
                {
                    // get colors
                    var fillColor = StyleSheet.GetProperty("TextFillColor", state, Color.White, OverrideStyles)!;
                    if (UseEmptyValueTextColor)
                    {
                        fillColor = StyleSheet.GetProperty("NoValueTextFillColor", state, fillColor, OverrideStyles)!;
                    }
                    var outlineColor = StyleSheet.GetProperty("TextOutlineColor", state, Color.Black, OverrideStyles)!;
                    var effectId = StyleSheet.GetProperty<string>("EffectIdentifier", state, null, OverrideStyles);
                    var outlineWidth = StyleSheet.GetProperty<int>("TextOutlineWidth", state, 1, OverrideStyles);

                    // animate colors
                    if (ColorAnimator != null)
                    {
                        fillColor = ColorAnimator(this, fillColor);
                    }

                    // apply alpha
                    if (alpha < 1f)
                    {
                        fillColor.A = (byte)((float)fillColor.A * alpha);
                        outlineColor.A = (byte)((float)outlineColor.A * alpha);
                    }

                    // not visible? skip
                    if (fillColor.A <= 0 && outlineColor.A <= 0f)
                    {
                        return;
                    }

                    // calculate position
                    var position = new Point(0,
                        (Anchor == Anchor.Center || Anchor == Anchor.CenterLeft || Anchor == Anchor.CenterRight) ?
                        internalBoundingRect.Top + internalBoundingRect.Height / 2 - _lineHeight / 2 :
                        internalBoundingRect.Top
                    );

                    // measure space width
                    var spaceWidth = (int)(UISystem.Renderer.MeasureText("_", font, fontSize, 1f).X * 0.5f);

                    // iterate lines and render them
                    int lineIndex = 0;
                    int lineStartIndex = 0;
                    TextStyleCommand currTextStyle = TextStyleCommand.Null;
                    foreach (var line in _cachedProcessedText)
                    {
                        // get line style commands
                        var styleCommands = line.StyleCommands;

                        // measure line entire size
                        var lineSize = UISystem.Renderer.MeasureText(line.Line, font, fontSize, spacing);

                        // adjust position X based on alignment
                        switch (alignment)
                        {
                            case TextAlignment.Left:
                                position.X = internalBoundingRect.Left;
                                break;
                            case TextAlignment.Right:
                                position.X = internalBoundingRect.Right - lineSize.X;
                                break;
                            case TextAlignment.Center:
                                position.X = internalBoundingRect.X + internalBoundingRect.Width / 2 - lineSize.X / 2;
                                break;
                        }

                        // draw line without style commands
                        if (styleCommands == null || styleCommands.Count == 0)
                        {
                            if (_beforeDrawingLineNoStyleCommands != null)
                            {
                                var processedLine = _beforeDrawingLineNoStyleCommands(line.Line, lineIndex, lineStartIndex);
                                if (!string.IsNullOrEmpty(processedLine))
                                {
                                    UISystem.Renderer.DrawText(effectId, processedLine, font, fontSize, position, currTextStyle.FillColor ?? fillColor, currTextStyle.OutlineColor ?? outlineColor, currTextStyle.OutlineWidth ?? outlineWidth, spacing);
                                }
                            }
                            else
                            {
                                UISystem.Renderer.DrawText(effectId, line.Line, font, fontSize, position, currTextStyle.FillColor ?? fillColor, currTextStyle.OutlineColor ?? outlineColor, currTextStyle.OutlineWidth ?? outlineWidth, spacing);
                            }
                        }
                        // draw line with style commands
                        else
                        {
                            int offsetX = 0;
                            for (int i = -1; i < styleCommands.Count; ++i)
                            {
                                // get current style command
                                var newStyleCommand = i >= 0 ? styleCommands[i] : TextStyleCommand.Null;

                                // special: reset command
                                if (newStyleCommand.ResetAll) { currTextStyle = TextStyleCommand.Null; }

                                // merge styles
                                currTextStyle.MergeSelfWith(newStyleCommand);

                                // calculate text segment position
                                var segmentPosition = new Point(position.X + offsetX, position.Y);

                                // draw style command icon
                                if (newStyleCommand.Icon != null)
                                {
                                    var dest = new Rectangle(segmentPosition.X, segmentPosition.Y + _lineHeight / 2, 
                                        (int)(newStyleCommand.Icon.SourceRect.Width * newStyleCommand.Icon.TextureScale), 
                                        (int)(newStyleCommand.Icon.SourceRect.Height * newStyleCommand.Icon.TextureScale));
                                    dest.Y -= dest.Height / 2;
                                    var color = (newStyleCommand.IconUseTextColor) ? (currTextStyle.FillColor ?? fillColor) : (currTextStyle.FillColor ?? Color.White);
                                    DrawUtils.Draw(UISystem.Renderer, effectId, newStyleCommand.Icon, dest, color);
                                }

                                // get range and segment to render
                                var toIndex = (i + 1) < styleCommands.Count ? (styleCommands[i + 1].Index - newStyleCommand.Index) : -1;
                                var segment = toIndex >= 0 ? line.Line.Substring(newStyleCommand.Index, toIndex) : line.Line.Substring(newStyleCommand.Index);
                                
                                // skip empty segments
                                if (string.IsNullOrEmpty(segment)) 
                                { 
                                    continue; 
                                }

                                // render text!
                                UISystem.Renderer.DrawText(effectId, segment, font, fontSize, segmentPosition, currTextStyle.FillColor ?? fillColor, currTextStyle.OutlineColor ?? outlineColor, currTextStyle.OutlineWidth ?? outlineWidth, spacing);
                                offsetX += UISystem.Renderer.MeasureText(segment, font, fontSize, spacing).X;

                                // add space before and after style commands
                                offsetX += spaceWidth;
                            }
                        }

                        // move position to next line
                        position.Y += _lineHeight;
                        lineStartIndex += line.Line.Length + 1;
                        lineIndex++;
                    }
                }

                // draw state with interpolation
                if (InterpolateStates && (_interpolateToNextState < 1f))
                {
                    DrawStateText(_prevState, internalBoundingRect, 1f);
                    DrawStateText(state, internalBoundingRect, _interpolateToNextState);
                }
                // draw current state without interpolation
                else
                {
                    DrawStateText(state, internalBoundingRect, 1f);
                }
            }
        }
    }

    /// <summary>
    /// How to handle text that overflow the paragraph region.
    /// </summary>
    public enum TextOverflowMode
    {
        /// <summary>
        /// Text will just overflow and exceed its limit.
        /// </summary>
        Overflow,

        /// <summary>
        /// Text will wrap and break line, but will maintain intact words. This mode can potentially overflow if there is a word that is longer than the width limit.
        /// </summary>
        WrapWords,

        /// <summary>
        /// Text will wrap and break line on the character that exceeded the width limit. It will not take whole-words into consideration.
        /// </summary>
        WrapImmediate,
    }
}
