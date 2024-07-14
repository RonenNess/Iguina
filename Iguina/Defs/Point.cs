
namespace Iguina.Defs
{
    /// <summary>
    /// Serializable point value.
    /// </summary>
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point() { }

        public Point(int x, int y) 
        {  
            X = x; 
            Y = y; 
        }

        public static readonly Point Zero = new Point(0, 0);

        public static readonly Point One = new Point(1, 1);
    }
}
