using Eto.Drawing;

namespace NAPS2.EtoForms.Layout;

// Ignore unreachable code for DEBUG_LAYOUT
#pragma warning disable CS0162
/// <summary>
/// Abstract base class for LayoutColumn and LayoutRow. We use this class to generalize column and row layout logic.
/// </summary>
/// <typeparam name="TOrthogonal">The orthogonal type (e.g. LayoutRow if this is LayoutColumn).</typeparam>
public abstract class LayoutLine<TOrthogonal> : LayoutContainer
    where TOrthogonal : LayoutContainer
{
    protected LayoutLine(LayoutElement[] children) : base(children)
    {
    }

    protected Padding? Padding { get; init; }

    protected int? Spacing { get; init; }

    protected abstract PointF UpdatePosition(PointF position, float delta);

    protected abstract PointF UpdateOrthogonalPosition(PointF position, float delta);

    protected abstract SizeF UpdateTotalSize(SizeF size, SizeF childSize, int spacing);

    public override void DoLayout(LayoutContext context, RectangleF bounds)
    {
        if (DEBUG_LAYOUT)
        {
            Debug.WriteLine($"{new string(' ', context.Depth)}{GetType().Name} layout with bounds {bounds}");
        }
        if (Padding is { } padding)
        {
            bounds = new RectangleF(
                bounds.X + padding.Left, bounds.Y + padding.Top,
                bounds.Width - padding.Horizontal, bounds.Height - padding.Vertical);
        }
        var childContext = GetChildContext(context, bounds);
        GetInitialCellLengthsAndScaling(context, childContext, bounds, out var cellLengths, out var cellScaling);

        UpdateCellLengthsForAvailableSpace(cellLengths, cellScaling, bounds, context);

        // The "cell" size and origin define the space the control can fit in, while the "child" size and origin define
        // the actual space the control fills. The child always fills the cell length-wise, but breadth-wise it depends
        // on the control alignment.
        var cellOrigin = bounds.Location;
        for (int i = 0; i < Children.Length; i++)
        {
            var child = Children[i];
            var cellSize = GetSize(cellLengths[i], GetBreadth(bounds.Size));
            GetChildSizeAndOrigin(child, childContext, cellSize, cellOrigin,
                out var childSize, out var childOrigin);
            child.DoLayout(childContext, new RectangleF(childOrigin, childSize));
            cellOrigin = UpdatePosition(cellOrigin, GetLength(childSize) + GetSpacing(i, context));
        }
    }

    private int GetSpacing(int i, LayoutContext context)
    {
        if (Children.Skip(i + 1).All(child => !child.IsVisible))
        {
            return 0;
        }
        if (!Children[i].IsVisible) return 0;
        return GetSpacingCore(i, context);
    }

    protected virtual int GetSpacingCore(int i, LayoutContext context)
    {
        return Children[i].SpacingAfter ?? Spacing ?? context.DefaultSpacing;
    }

    private void GetChildSizeAndOrigin(LayoutElement child, LayoutContext childContext,
        SizeF cellSize, PointF cellOrigin, out SizeF childSize, out PointF childOrigin)
    {
        var breadth = GetBreadth(
            child.Alignment == LayoutAlignment.Fill
                ? cellSize
                : child.GetPreferredSize(childContext, new RectangleF(cellOrigin, cellSize)));
        var remainingBreadth = GetBreadth(cellSize) - breadth;
        var alignmentOffset = child.Alignment switch
        {
            LayoutAlignment.Leading => 0,
            LayoutAlignment.Center => remainingBreadth / 2,
            LayoutAlignment.Trailing => remainingBreadth,
            _ => 0
        };
        childSize = GetSize(GetLength(cellSize), breadth);
        childOrigin = UpdateOrthogonalPosition(cellOrigin, alignmentOffset);
    }

    public override SizeF GetPreferredSize(LayoutContext context, RectangleF parentBounds)
    {
        var childContext = GetChildContext(context, parentBounds);
        if (!childContext.IsParentVisible)
        {
            return SizeF.Empty;
        }
        var size = SizeF.Empty;
        GetInitialCellLengthsAndScaling(context, childContext, parentBounds, out var cellLengths, out var cellScaling);
        UpdateCellLengthsWithPreferredLength(cellLengths, cellScaling);
        for (int i = 0; i < Children.Length; i++)
        {
            var childSize = Children[i].GetPreferredSize(childContext, parentBounds);
            var childLayoutSize = GetSize(cellLengths[i], GetBreadth(childSize));
            size = UpdateTotalSize(size, childLayoutSize, GetSpacing(i, context));
        }
        size += new SizeF(Padding?.Horizontal ?? 0, Padding?.Vertical ?? 0);
        return size;
    }

    private LayoutContext GetChildContext(LayoutContext context, RectangleF bounds)
    {
        return context with
        {
            CellLengths = GetChildCellLengths(context, bounds),
            CellScaling = GetChildCellScaling(),
            Depth = context.Depth + 1,
            IsParentVisible = context.IsParentVisible && IsVisible
        };
    }

    private void GetInitialCellLengthsAndScaling(LayoutContext context, LayoutContext childContext, RectangleF bounds,
        out List<float> cellLengths, out List<bool> cellScaling)
    {
        // If this line is supposed to be aligned with adjacent lines (e.g. 2 rows in a parent column or vice versa),
        // then our parent will have pre-calculated our cell sizes and scaling for us.
        if (Aligned && context.CellLengths != null && context.CellScaling != null)
        {
            cellLengths = context.CellLengths;
            cellScaling = context.CellScaling;
            return;
        }
        // If we aren't aligned or we don't have a parent to do that pre-calculation, then we just determine our cell
        // sizes and scaling directly without any special alignment constraints.
        cellLengths = new List<float>();
        cellScaling = new List<bool>();
        var lengthChildContext = childContext with { IsCellLengthQuery = true };
        foreach (var child in Children)
        {
            cellLengths.Add(GetLength(child.GetPreferredSize(lengthChildContext, bounds)));
            cellScaling.Add(child.IsVisible && child.Scale);
        }
    }

    private void UpdateCellLengthsWithPreferredLength(List<float> cellLengths, List<bool> cellScaling)
    {
        if (!cellScaling.Any(scales => scales))
        {
            return;
        }
        // If multiple cells scale, then they will end up with the same length. Therefore, the biggest initial length
        // defines the preferred length for all scaled cells.
        float maxScaledLength = 0;
        for (int i = 0; i < cellLengths.Count; i++)
        {
            if (cellScaling[i])
            {
                maxScaledLength = Math.Max(maxScaledLength, cellLengths[i]);
            }
        }
        for (int i = 0; i < cellLengths.Count; i++)
        {
            if (cellScaling[i])
            {
                cellLengths[i] = maxScaledLength;
            }
        }
    }

    private void UpdateCellLengthsForAvailableSpace(List<float> cellLengths, List<bool> cellScaling, RectangleF bounds,
        LayoutContext context)
    {
        var scaleCount = cellScaling.Count(scales => scales);
        if (scaleCount == 0)
        {
            return;
        }
        // If no controls scale, then they will all take up their preferred length.
        // If some controls scale, then we take [excess = remaining space + length of all scaling controls],
        // and divide that evenly among all scaling controls so they all have equal length.
        var excess = GetLength(bounds.Size);
        for (int i = 0; i < Children.Length; i++)
        {
            if (!cellScaling[i])
            {
                excess -= cellLengths[i];
            }
            excess -= GetSpacing(i, context);
        }
        // TODO: This protects against both forms being shrunk below their minimum size, but also
        // on Gtk apparently the bounds are wrong for non-resizable forms. This would become visible
        // if we have a non-resizable form with extra layout size + scaling in the same direction.
        if (excess <= 0) return;
        // Update the lengths of scaling controls
        var scaleAmount = Math.DivRem((int) excess, scaleCount, out int scaleExtra);
        for (int i = 0; i < Children.Length; i++)
        {
            if (cellScaling[i])
            {
                cellLengths[i] = scaleAmount + (scaleExtra-- > 0 ? 1 : 0);
            }
        }
    }

    private List<float> GetChildCellLengths(LayoutContext context, RectangleF bounds)
    {
        var cellLengths = new List<float>();
        foreach (var child in Children)
        {
            if (child is TOrthogonal { Aligned: true } opposite)
            {
                for (int i = 0; i < opposite.Children.Length; i++)
                {
                    if (cellLengths.Count <= i) cellLengths.Add(0);
                    // TODO: We should probably shrink the bounds if needed
                    var preferredLength = GetBreadth(opposite.Children[i].GetPreferredSize(context, bounds));
                    cellLengths[i] = Math.Max(cellLengths[i], preferredLength);
                }
            }
        }
        return cellLengths;
    }

    private List<bool> GetChildCellScaling()
    {
        var cellScaling = new List<bool>();
        foreach (var child in Children)
        {
            if (child is TOrthogonal { Aligned: true } opposite)
            {
                for (int i = 0; i < opposite.Children.Length; i++)
                {
                    if (cellScaling.Count <= i) cellScaling.Add(false);
                    cellScaling[i] = cellScaling[i] || opposite.Children[i].Scale;
                }
            }
        }
        return cellScaling;
    }
}