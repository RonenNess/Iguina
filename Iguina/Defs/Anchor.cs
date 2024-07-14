

namespace Iguina.Defs
{
    /// <summary>
    /// Positions in parent entity to position the entity from.
    /// The anchor also affect the UI entity's offset.
    /// </summary>
    public enum Anchor
    {
        /// <summary>
        /// Auto placement with one entity per row.
        /// Going left-to-right.
        /// </summary>
        AutoLTR,

        /// <summary>
        /// Auto placement in the same row, until exceeding parent width.
        /// Going left-to-right.
        /// </summary>
        AutoInlineLTR,

        /// <summary>
        /// Auto placement with one entity per row.
        /// Going right-to-left.
        /// </summary>
        AutoRTL,

        /// <summary>
        /// Auto placement in the same row, until exceeding parent width.
        /// Going right-to-left.
        /// </summary>
        AutoInlineRTL,

        /// <summary>
        /// Auto placement with one entity per row.
        /// Aligned to center.
        /// </summary>
        AutoCenter,

        /// <summary>
        /// Entity is aligned to parent top-left internal corner.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Entity is aligned to parent top-center internal point.
        /// </summary>
        TopCenter,

        /// <summary>
        /// Entity is aligned to parent top-right internal corner.
        /// </summary>
        TopRight,

        /// <summary>
        /// Entity is aligned to parent bottom-left internal corner.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Entity is aligned to parent bottom-center internal point.
        /// </summary>
        BottomCenter,

        /// <summary>
        /// Entity is aligned to parent bottom-right internal corner.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Entity is aligned to parent center-left internal point.
        /// </summary>
        CenterLeft,

        /// <summary>
        /// Entity is aligned to parent center internal point.
        /// </summary>
        Center,

        /// <summary>
        /// Entity is aligned to parent center-right internal point.
        /// </summary>
        CenterRight,
    }
}
