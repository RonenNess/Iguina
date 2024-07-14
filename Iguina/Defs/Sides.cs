

namespace Iguina.Defs
{
    /// <summary>
    /// Define 4 sides values.
    /// </summary>
    public struct Sides
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }

        public Sides() { }

        public Sides(int left, int right, int top, int bottom) 
        {
            Left = left;
            Right = right;
            Top = top; 
            Bottom = bottom; 
        }

        public static readonly Sides Zero = new Sides(0, 0, 0, 0);
    }
}
