
namespace Iguina.Defs
{
    /// <summary>
    /// Serializable rectangle.
    /// </summary>
    public struct Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Left => X;
        public int Top => Y;
        public int Right => X + Width;
        public int Bottom => Y + Height;
        public Point Center => new Point(X + Width / 2, Y + Height / 2);

        public static readonly Rectangle Empty = new Rectangle();

        public Rectangle() { }

        public Rectangle(int x, int y, int width, int height) 
        {  
            X = x; 
            Y = y; 
            Width = width; 
            Height = height; 
        }

        /// <summary>
        /// Merge two rectangles.
        /// </summary>
        public static Rectangle MergeRectangles(Rectangle rect1, Rectangle rect2)
        {
            int x1 = Math.Max(rect1.X, rect2.X);
            int y1 = Math.Max(rect1.Y, rect2.Y);
            int x2 = Math.Min(rect1.X + rect1.Width, rect2.X + rect2.Width);
            int y2 = Math.Min(rect1.Y + rect1.Height, rect2.Y + rect2.Height);

            int width = x2 - x1;
            int height = y2 - y1;

            if (width > 0 && height > 0)
            {
                return new Rectangle(x1, y1, width, height);
            }
            else
            {
                return new Rectangle(0, 0, 0, 0);
            }
        }
    }
}
