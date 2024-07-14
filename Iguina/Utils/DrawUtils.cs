using Iguina.Defs;
using Iguina.Drivers;

namespace Iguina.Utils
{
    /// <summary>
    /// Render utilities.
    /// </summary>
    internal static class DrawUtils
    {
        /// <summary>
        /// Render framed texture.
        /// </summary>
        public static void Draw(IRenderer renderer, string? effectId, FramedTexture texture, Rectangle dest, Color color)
        {
            // to avoid glitches
            if (dest.Width <= 0 || dest.Height <= 0) { return; }

            // get all source rects
            var topLeftSrc = texture.TopLeftSourceRect;
            var topRightSrc = texture.TopRightSourceRect;
            var bottomLeftSrc = texture.BottomLeftSourceRect;
            var bottomRightSrc = texture.BottomRightSourceRect;

            // add offset
            dest.X += texture.Offset.X;
            dest.Y += texture.Offset.Y;

            // is this a horizontal strip only?
            bool isHorizontalStrip = texture.InternalSourceRect.Height == texture.ExternalSourceRect.Height;

            // calculate all dest rects
            var topLeftDest = new Rectangle(dest.X, dest.Y, (int)(topLeftSrc.Width * texture.TextureScale), (int)(topLeftSrc.Height * texture.TextureScale));
            var topRightDest = new Rectangle(dest.Right - (int)(topRightSrc.Width * texture.TextureScale), dest.Y, (int)(topRightSrc.Width * texture.TextureScale), (int)(topRightSrc.Height * texture.TextureScale));
            var bottomLeftDest = new Rectangle(dest.X, dest.Bottom - (int)(bottomLeftSrc.Height * texture.TextureScale), (int)(bottomLeftSrc.Width * texture.TextureScale), (int)(bottomLeftSrc.Height * texture.TextureScale));
            var bottomRightDest = new Rectangle(dest.Right - (int)(bottomRightSrc.Width * texture.TextureScale), dest.Bottom - (int)(bottomRightSrc.Height * texture.TextureScale), (int)(bottomRightSrc.Width * texture.TextureScale), (int)(bottomRightSrc.Height * texture.TextureScale));

            // render center parts
            {
                var srcRect = texture.InternalSourceRect;
                var destRect = new Rectangle(topLeftDest.Right, topLeftDest.Bottom, (int)(srcRect.Width * texture.TextureScale), (int)(srcRect.Height * texture.TextureScale));

                bool lastRow = isHorizontalStrip;
                while (true)
                {
                    // end of row?
                    var cutOff = destRect.Right - topRightDest.Left;
                    if (cutOff >= 0)
                    {
                        destRect.Width -= cutOff;
                        srcRect.Width -= (int)(cutOff / texture.TextureScale);
                    }

                    // check if we exceed bottom
                    var cutOffBottom = destRect.Bottom - bottomLeftDest.Top;
                    if (cutOffBottom >= 0)
                    {
                        destRect.Height -= cutOffBottom;
                        srcRect.Height -= (int)(cutOffBottom / texture.TextureScale);
                        lastRow = true;
                    }

                    // edge case - last row align perfectly
                    if (destRect.Height == 0) { break; }

                    // draw part
                    if (destRect.Width > 0 && destRect.Height > 0)
                    {
                        renderer.DrawTexture(effectId, texture.TextureId, destRect, srcRect, color);
                    }
                    destRect.X += destRect.Width;
                
                    // go row down
                    if (cutOff >= 0)
                    {
                        // last row? break
                        if (lastRow) { break; }

                        // reset width and go row down
                        destRect.X = topLeftDest.Right;
                        srcRect.Width = texture.InternalSourceRect.Width;
                        destRect.Y += destRect.Height;
                    }
                }
            }

            // render frame
            // top frame
            {
                bool keepDrawing = true;
                var srcRect = isHorizontalStrip ? texture.InternalSourceRect : texture.TopSourceRect;
                var destRect = new Rectangle(topLeftDest.Right, dest.Top, (int)(srcRect.Width * texture.TextureScale), (int)(srcRect.Height * texture.TextureScale));
                while (keepDrawing)
                {
                    var cutOff = destRect.Right - topRightDest.Left;
                    if (cutOff >= 0)
                    {
                        destRect.Width -= cutOff;
                        srcRect.Width -= (int)(cutOff / texture.TextureScale);
                        if (destRect.Width <= 0 || srcRect.Width <= 0) { break; }
                        keepDrawing = false;
                    }
                    renderer.DrawTexture(effectId, texture.TextureId, destRect, srcRect, color);
                    destRect.X += destRect.Width;
                }
            }
            if (!isHorizontalStrip)
            {
                // bottom frame
                {
                    bool keepDrawing = true;
                    var srcRect = texture.BottomSourceRect;
                    var destRect = new Rectangle(bottomLeftDest.Right, bottomLeftDest.Top, (int)(srcRect.Width * texture.TextureScale), (int)(srcRect.Height * texture.TextureScale));
                    while (keepDrawing)
                    {
                        var cutOff = destRect.Right - bottomRightDest.Left;
                        if (cutOff >= 0)
                        {
                            destRect.Width -= cutOff;
                            srcRect.Width -= (int)(cutOff / texture.TextureScale);
                            if (destRect.Width <= 0 || srcRect.Width <= 0) { break; }
                            keepDrawing = false;
                        }
                        renderer.DrawTexture(effectId, texture.TextureId, destRect, srcRect, color);
                        destRect.X += destRect.Width;
                    }
                }
                // left frame
                {
                    bool keepDrawing = true;
                    var srcRect = texture.LeftSourceRect;
                    var destRect = new Rectangle(dest.Left, topLeftDest.Bottom, (int)(srcRect.Width * texture.TextureScale), (int)(srcRect.Height * texture.TextureScale));
                    while (keepDrawing)
                    {
                        var cutOff = destRect.Bottom - bottomLeftDest.Top;
                        if (cutOff >= 0)
                        {
                            destRect.Height -= cutOff;
                            srcRect.Height -= (int)(cutOff / texture.TextureScale);
                            if (destRect.Height <= 0 || srcRect.Height <= 0) { break; }
                            keepDrawing = false;
                        }
                        renderer.DrawTexture(effectId, texture.TextureId, destRect, srcRect, color);
                        destRect.Y += destRect.Height;
                    }
                }
                // right frame
                {
                    bool keepDrawing = true;
                    var srcRect = texture.RightSourceRect;
                    var destRect = new Rectangle(topRightDest.Left, topRightDest.Bottom, (int)(srcRect.Width * texture.TextureScale), (int)(srcRect.Height * texture.TextureScale));
                    while (keepDrawing)
                    {
                        var cutOff = destRect.Bottom - bottomRightDest.Top;
                        if (cutOff >= 0)
                        {
                            destRect.Height -= cutOff;
                            srcRect.Height -= (int)(cutOff / texture.TextureScale);
                            if (destRect.Height <= 0 || srcRect.Height <= 0) { break; }
                            keepDrawing = false;
                        }
                        renderer.DrawTexture(effectId, texture.TextureId, destRect, srcRect, color);
                        destRect.Y += destRect.Height;
                    }
                }
            }

            // render corners
            if (!isHorizontalStrip)
            {
                renderer.DrawTexture(effectId, texture.TextureId, topLeftDest, topLeftSrc, color);
                renderer.DrawTexture(effectId, texture.TextureId, topRightDest, topRightSrc, color);
                renderer.DrawTexture(effectId, texture.TextureId, bottomLeftDest, bottomLeftSrc, color);
                renderer.DrawTexture(effectId, texture.TextureId, bottomRightDest, bottomRightSrc, color);
            }
            // render sides for horizontal strip
            else
            {
                topLeftSrc.Height = texture.InternalSourceRect.Height;
                topLeftDest.Height = (int)(topLeftSrc.Height * texture.TextureScale);
                renderer.DrawTexture(effectId, texture.TextureId, topLeftDest, topLeftSrc, color);

                topRightSrc.Height = texture.InternalSourceRect.Height;
                topRightDest.Height = (int)(topRightSrc.Height * texture.TextureScale);
                renderer.DrawTexture(effectId, texture.TextureId, topRightDest, topRightSrc, color);
            }
        }

        /// <summary>
        /// Render stretched texture.
        /// </summary>
        public static void Draw(IRenderer renderer, string? effectId, StretchedTexture texture, Rectangle dest, Color color)
        {
            if (texture.ExtraSize.HasValue)
            {
                dest.X -= texture.ExtraSize.Value.Left;
                dest.Width += texture.ExtraSize.Value.Left + texture.ExtraSize.Value.Right;
                dest.Y -= texture.ExtraSize.Value.Top;
                dest.Height += texture.ExtraSize.Value.Top + texture.ExtraSize.Value.Bottom;
            }

            // to avoid glitches
            if (dest.Width <= 0 || dest.Height <= 0) { return; }

            renderer.DrawTexture(effectId, texture.TextureId, dest, texture.SourceRect, color);
        }

        /// <summary>
        /// Render icon texture.
        /// </summary>
        public static void Draw(IRenderer renderer, string? effectId, IconTexture texture, Rectangle dest, Color color)
        {
            // to avoid glitches
            if (dest.Width <= 0 || dest.Height <= 0) { return; }

            renderer.DrawTexture(effectId, texture.TextureId, dest, texture.SourceRect, color);
        }
    }
}
