
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Gemcom.Klabr
{
    /// <summary>
    /// This class is used to render column headers that indicate sorting order for a grid view.
    /// </summary>
    public class SortAdorner : Adorner
    {
        public ListSortDirection SortDirection { get; private set; }

        // Geometry of up and down arrows
        private readonly static Geometry _ascendingGeometry = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");
        private readonly static Geometry _descendingGeometry = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");

        
        public SortAdorner(UIElement element, ListSortDirection sortDirection) : base(element)
        { 
            SortDirection = sortDirection;
            // We want to let the things we're adorning handle events
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // If the column is too narrow, don't render an arrow at all
            if (AdornedElement.RenderSize.Width < 20)
                return;

            // Draw an up or down arrow (depending on the sort order) to the right of the column title
            drawingContext.PushTransform(
                new TranslateTransform(
                    AdornedElement.RenderSize.Width - 15, 
                    (AdornedElement.RenderSize.Height - 5) / 2));
                drawingContext.DrawGeometry(Brushes.Black, null, 
                    SortDirection == ListSortDirection.Ascending ? _ascendingGeometry : _descendingGeometry);

            drawingContext.Pop();
        }
    }
}