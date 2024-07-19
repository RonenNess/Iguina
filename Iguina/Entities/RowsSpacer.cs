using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// An entity that doesn't do anything except for creating vertical space between entities that are set with auto anchor.
    /// </summary>
    public class RowsSpacer : Entity
    {
        /// <summary>
        /// Create the spacer.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="rowsCount">How many empty 'rows' to to create. The height of each row is defined by the UI system stylesheet.</param>
        public RowsSpacer(UISystem system, int rowsCount = 1) : base(system, null)
        {
            IgnoreInteractions = true;
            Size.Y.SetPixels(system.SystemStyleSheet.RowSpaceHeight * rowsCount);
        }

        /// <inheritdoc/>
        protected override Anchor GetDefaultEntityTypeAnchor()
        {
            return Anchor.AutoCenter;
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPercents(100f);
            ret.Y.SetPixels(8);
            return ret;
        }

        /// <inheritdoc/>
        internal override void DebugDraw(bool debugDrawChildren)
        {
            if (!Visible) { return; }

            UISystem.Renderer.DrawRectangle(LastBoundingRect, new Color(255, 0, 0, 65));

            if (debugDrawChildren)
            {
                IterateChildren((Entity child) => { 
                    child.DebugDraw(debugDrawChildren); 
                    return true; 
                });
            }
        }
    }
}
