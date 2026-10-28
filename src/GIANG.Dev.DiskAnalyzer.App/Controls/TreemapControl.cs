using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GIANG.Dev.DiskAnalyzer.Core.Models;

namespace GIANG.Dev.DiskAnalyzer.App.Controls;

public class TreemapControl : Control
{
    public static readonly DependencyProperty NodesProperty =
        DependencyProperty.Register(
            nameof(Nodes),
            typeof(IEnumerable<TreemapNode>),
            typeof(TreemapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NodeClickCommandProperty =
        DependencyProperty.Register(
            nameof(NodeClickCommand),
            typeof(ICommand),
            typeof(TreemapControl),
            new PropertyMetadata(null));

    public IEnumerable<TreemapNode>? Nodes
    {
        get => (IEnumerable<TreemapNode>?)GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    public ICommand? NodeClickCommand
    {
        get => (ICommand?)GetValue(NodeClickCommandProperty);
        set => SetValue(NodeClickCommandProperty, value);
    }

    private TreemapNode? _hoveredNode;

    static TreemapControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TreemapControl), new FrameworkPropertyMetadata(typeof(TreemapControl)));
    }

    public TreemapControl()
    {
        ClipToBounds = true;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (Nodes == null) return;
        var pos = e.GetPosition(this);
        var hit = Nodes.FirstOrDefault(n => pos.X >= n.X && pos.X <= n.X + n.Width &&
                                            pos.Y >= n.Y && pos.Y <= n.Y + n.Height);

        if (!ReferenceEquals(hit, _hoveredNode))
        {
            _hoveredNode = hit;
            if (hit != null)
            {
                ToolTip = $"{hit.Name}\nDung lượng: {hit.SizeFormatted} ({hit.Percentage:F1}%)\nĐường dẫn: {hit.FullPath}\n(Nhấp đúp hoặc nhấp để phân tích sâu)";
            }
            else
            {
                ToolTip = null;
            }
            InvalidateVisual();
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_hoveredNode != null)
        {
            _hoveredNode = null;
            ToolTip = null;
            InvalidateVisual();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_hoveredNode != null && NodeClickCommand != null && NodeClickCommand.CanExecute(_hoveredNode))
        {
            NodeClickCommand.Execute(_hoveredNode);
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // Draw dark background
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(17, 24, 39)),
            new Pen(new SolidColorBrush(Color.FromRgb(30, 41, 59)), 1),
            new Rect(0, 0, ActualWidth, ActualHeight));

        if (Nodes == null) return;

        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var dpi = VisualTreeHelper.GetDpi(this);

        foreach (var node in Nodes)
        {
            if (node.Width <= 2 || node.Height <= 2) continue;

            bool isHovered = ReferenceEquals(node, _hoveredNode);

            Color accentColor;
            try
            {
                accentColor = (Color)ColorConverter.ConvertFromString(node.AccentColor);
            }
            catch
            {
                accentColor = Color.FromRgb(0, 240, 255);
            }

            // Fill color with subtle alpha
            byte alpha = isHovered ? (byte)90 : (byte)45;
            var fillBrush = new SolidColorBrush(Color.FromArgb(alpha, accentColor.R, accentColor.G, accentColor.B));
            fillBrush.Freeze();
            var borderPen = new Pen(new SolidColorBrush(accentColor), isHovered ? 2 : 1);
            borderPen.Freeze();

            var rect = new Rect(node.X, node.Y, node.Width, node.Height);
            dc.DrawRoundedRectangle(fillBrush, borderPen, rect, 4, 4);

            // Draw label if node has sufficient space
            if (node.Width >= 40 && node.Height >= 24)
            {
                string text = $"{node.Name}\n{node.SizeFormatted}";
                var formattedText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    11,
                    isHovered ? Brushes.White : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    dpi.PixelsPerDip)
                {
                    MaxTextWidth = Math.Max(10, node.Width - 8),
                    MaxTextHeight = Math.Max(10, node.Height - 6),
                    Trimming = TextTrimming.CharacterEllipsis
                };

                dc.DrawText(formattedText, new Point(node.X + 4, node.Y + 4));
            }
        }
    }
}
