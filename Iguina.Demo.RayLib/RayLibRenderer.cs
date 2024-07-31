using Iguina.Defs;
using Iguina.Drivers;


namespace Iguina.Demo.RayLib
{
    /// <summary>
    /// Provide rendering for the GUI system.
    /// </summary>
    public class RayLibRenderer : IRenderer
    {
        // assets path
        string _assetsPath;

        /// <summary>
        /// Create the ray renderer.
        /// </summary>
        /// <param name="assetsPath">Root directory to load assets from. Check out the demo project for details.</param>
        public RayLibRenderer(string assetsPath)
        {
            // store textures path
            _assetsPath = assetsPath;

            // create black and white effect for disabled entities
            {
                string grayscaleShader = @"
                    #version 330
                    in vec2 fragTexCoord;
                    in vec4 fragColor;

                    uniform sampler2D texture0;
                    uniform vec4 colDiffuse;

                    out vec4 finalColor;

                    void main()
                    {
                        vec4 texelColor = texture(texture0, fragTexCoord) * colDiffuse * fragColor;
                        float gray = (texelColor.r + texelColor.g + texelColor.b) / 3;
                        finalColor = vec4(vec3(gray), texelColor.a);
                    }
                ";
                _disabledShader = Raylib_cs.Raylib.LoadShaderFromMemory(null, grayscaleShader);
            }

            // get default font
            _defaultFont = Raylib_cs.Raylib.GetFontDefault();
        }

        // shader to use for disabled effect
        Raylib_cs.Shader _disabledShader;

        // last effect id
        string? _lastEffect = null!;

        /// <summary>
        /// Called at the beginning of every frame.
        /// </summary>
        public void StartFrame()
        {
            _lastEffect = null;
        }

        /// <summary>
        /// Called at the end of every frame.
        /// </summary>
        public void EndFrame()
        {
            if (_lastEffect != null)
            {
                Raylib_cs.Raylib.EndShaderMode();
            }
        }

        /// <summary>
        /// Set which effect to use.
        /// </summary>
        void SetEffect(string? effect)
        {
            // effect changed?
            if (effect != _lastEffect)
            {
                // cancel previous shader
                if (_lastEffect != null)
                {
                    Raylib_cs.Raylib.EndShaderMode();
                }

                // no effect
                if (effect == null) 
                {
                }
                // set disabled effect
                else if (effect == "disabled")
                {
                    Raylib_cs.Raylib.BeginShaderMode(_disabledShader);
                }
                // undefined effect!
                else
                {
                    throw new Exception("Unknown effect identifier: " + effect);
                }

                // set last effect
                _lastEffect = effect;
            }
        }

        /// <summary>
        /// Get texture instance from texture id.
        /// </summary>
        Raylib_cs.Texture2D GetTexture(string textureId)
        {
            if (_textures.TryGetValue(textureId, out Raylib_cs.Texture2D tex))
            {
                return tex;
            }
            _textures[textureId] = Raylib_cs.Raylib.LoadTexture(Path.Combine(_assetsPath, textureId));
            return _textures[textureId];
        }

        // cached textures
        Dictionary<string, Raylib_cs.Texture2D> _textures = new();

        /// <summary>
        /// Get font from identifier.
        /// </summary>
        Raylib_cs.Font GetFont(string? fontId)
        {
            if (fontId == null)
            {
                return _defaultFont;
            }

            if (_fonts.TryGetValue(fontId, out Raylib_cs.Font font))
            {
                return font;
            }
            _fonts[fontId] = Raylib_cs.Raylib.LoadFont(Path.Combine(_assetsPath, fontId));
            return _fonts[fontId];
        }

        // cached fonts
        Dictionary<string, Raylib_cs.Font> _fonts = new();
        Raylib_cs.Font _defaultFont;

        /// <inheritdoc/>
        public Rectangle GetScreenBounds()
        {
            return new Rectangle(0, 0, Raylib_cs.Raylib.GetRenderWidth(), Raylib_cs.Raylib.GetRenderHeight());
        }

        /// <inheritdoc/>
        [Obsolete("Note: currently we render outline in a primitive way. To improve performance and remove some visual artifact during transitions, its best to implement a shader that draw text with outline properly.")]
        public void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
        {
            // set text effect
            SetEffect(effectIdentifier);

            // get font
            var font = GetFont(fontId);

            // draw outline
            if ((outlineColor.A > 0) && (outlineWidth > 0))
            {
                // because we draw outline in a primitive way, we want it to fade a lot faster than fill color
                if (outlineColor.A < 255)
                {
                    float alphaFactor = (float)(outlineColor.A / 255f);
                    outlineColor.A = (byte)((float)fillColor.A * Math.Pow(alphaFactor, 7));
                }

                // draw outline
                var outline = new Raylib_cs.Color(outlineColor.R, outlineColor.G, outlineColor.B, outlineColor.A);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X - outlineWidth, position.Y), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X, position.Y - outlineWidth), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X + outlineWidth, position.Y), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X, position.Y + outlineWidth), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X - outlineWidth, position.Y - outlineWidth), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X - outlineWidth, position.Y + outlineWidth), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X + outlineWidth, position.Y - outlineWidth), fontSize, spacing, outline);
                Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X + outlineWidth, position.Y + outlineWidth), fontSize, spacing, outline);
            }

            // draw text
            var fill = new Raylib_cs.Color(fillColor.R, fillColor.G, fillColor.B, fillColor.A);
            Raylib_cs.Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(position.X, position.Y), fontSize, spacing, fill);
        }

        /// <inheritdoc/>
        public int GetTextLineHeight(string? fontId, int fontSize)
        {
            var font = GetFont(fontId);
            float scale = fontSize / (float)font.BaseSize;
            return (int)(font.BaseSize * scale);
        }

        /// <inheritdoc/>
        public Point MeasureText(string text, string? fontId, int fontSize, float spacing)
        {
            var font = GetFont(fontId);
            var ret = Raylib_cs.Raylib.MeasureTextEx(font, text, fontSize, spacing);
            return new Point((int)ret.X, (int)ret.Y);
        }

        /// <inheritdoc/>
        public void DrawTexture(string? effectIdentifier, string textureId, Rectangle destRect, Rectangle sourceRect, Color color)
        {
            // set active effect
            SetEffect(effectIdentifier);

            // get texture
            var texture = GetTexture(textureId);

            // draw texture
            Raylib_cs.Raylib.DrawTexturePro(texture,
                new Raylib_cs.Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height),
                new Raylib_cs.Rectangle(destRect.X, destRect.Y, destRect.Width, destRect.Height),
                new System.Numerics.Vector2(0f, 0f),
                0f, 
                new Raylib_cs.Color(color.R, color.G, color.B, color.A));
        }

        // currently set scissor region
        Rectangle? _currentScissorRegion;

        /// <inheritdoc/>
        public void SetScissorRegion(Rectangle region)
        {
            _currentScissorRegion = region;
            Raylib_cs.Raylib.BeginScissorMode(region.X, region.Y, region.Width, region.Height);
        }

        /// <inheritdoc/>
        public void DrawRectangle(Rectangle rectangle, Color color)
        {
            SetEffect(null);
            Raylib_cs.Raylib.DrawRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, new Raylib_cs.Color(color.R, color.G, color.B, color.A));
        }

        /// <inheritdoc/>
        public void ClearScissorRegion()
        {
            _currentScissorRegion = null;
            Raylib_cs.Raylib.EndScissorMode();
        }

        /// <inheritdoc/>
        public Rectangle? GetScissorRegion()
        {
            return _currentScissorRegion;
        }

        /// <inheritdoc/>
        public Color GetPixelFromTexture(string textureId, Point sourcePosition)
        {
            var texture = GetTexture(textureId);
            Raylib_cs.Image image;
            if (!_cachedImageData.TryGetValue(textureId, out image))
            { 
                image = Raylib_cs.Raylib.LoadImageFromTexture(texture);
                _cachedImageData[textureId] = image;
            }
            var pixelColor = Raylib_cs.Raylib.GetImageColor(image, sourcePosition.X, sourcePosition.Y);
            return new Color(pixelColor.R, pixelColor.G, pixelColor.B, pixelColor.A);
        }
        Dictionary<string, Raylib_cs.Image> _cachedImageData = new();
    }
}
