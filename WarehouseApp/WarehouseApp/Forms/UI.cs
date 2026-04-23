using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace WarehouseApp.Forms;

public static class UI
{
    public static readonly Color BgLight = Color.FromArgb(230, 230, 230);
    public static readonly Color BgCard = Color.FromArgb(211, 211, 211);
    public static readonly Color Surface = Color.FromArgb(239, 239, 239);

    public static readonly Color TopBar = Color.FromArgb(36, 57, 117);
    public static readonly Color TabActive = Color.FromArgb(170, 201, 238);
    public static readonly Color TabInactive = Color.FromArgb(193, 216, 238);
    public static readonly Color TabHistoryBg = Color.FromArgb(241, 136, 0);
    public static readonly Color BtnLogout = Color.FromArgb(224, 22, 22);

    public static readonly Color InputBg = Color.FromArgb(174, 202, 236);
    public static readonly Color InputBgLogin = Color.FromArgb(174, 202, 236);
    public static readonly Color InputWhite = Color.White;

    public static readonly Color BtnBlue = Color.FromArgb(165, 198, 236);
    public static readonly Color BtnGreen = Color.FromArgb(44, 211, 11);
    public static readonly Color BtnRed = Color.FromArgb(223, 8, 8);
    public static readonly Color BtnOrange = Color.FromArgb(241, 136, 0);
    public static readonly Color FabOrange = Color.FromArgb(241, 136, 0);

    public static readonly Color CategoryRow = Color.FromArgb(108, 108, 108);
    public static readonly Color CategoryText = Color.FromArgb(151, 156, 214);
    public static readonly Color HeaderRow = Color.FromArgb(208, 208, 208);
    public static readonly Color RowBorder = Color.FromArgb(168, 168, 168);
    public static readonly Color ValueBar = Color.FromArgb(216, 216, 216);

    public static readonly Color TextDark = Color.FromArgb(22, 22, 22);
    public static readonly Color TextGray = Color.FromArgb(75, 75, 75);

    // ===== Масштаб: увеличенные размеры для High-DPI =====
    public static Font DefaultFont => Px(20);
    public static Font FontHugeButton => Px(32);
    public static Font FontLarge => Px(22);
    public static Font FontMedBold => Px(18, FontStyle.Bold);
    public static Font FontMed => Px(18);
    public static Font FontTab => Px(17);
    public static Font FontSmall => Px(16);
    public static Font FontTiny => Px(14);

    internal static Font Px(float size, FontStyle style = FontStyle.Regular, string family = "Segoe UI")
        => new(family, size, style, GraphicsUnit.Pixel);

    public const int TopBarHeight = 72;
    public const int HeaderRowHeight = 78;
    public const int CategoryRowHeight = 52;
    public const int ProductRowHeight = 72;
    public const int FooterHeight = 72;
    public const int TabHeight = 48;
    public const int Radius = 18;

    internal static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;

        if (diameter > bounds.Width) diameter = bounds.Width;
        if (diameter > bounds.Height) diameter = bounds.Height;
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    internal static BufferedPanel CreatePanel(Color backColor)
    {
        return new BufferedPanel { BackColor = backColor };
    }

    internal static SmoothScrollPanel CreateScrollPanel(Color backColor)
    {
        return new SmoothScrollPanel { BackColor = backColor };
    }

    internal static RoundedPanel CreateRoundedPanel(Color backColor, int radius = Radius)
    {
        return new RoundedPanel
        {
            BackColor = backColor,
            CornerRadius = radius
        };
    }

    internal static RoundedButton CreateCircleButton(string text, Color backColor, int size, Font? font = null)
    {
        var button = new RoundedButton
        {
            Text = text,
            BackColor = backColor,
            ForeColor = TextDark,
            Cursor = Cursors.Hand,
            Size = new Size(size, size),
            Font = font ?? Px(Math.Max(14f, size / 3f)),
            UseVisualStyleBackColor = false,
            TabStop = false,
            CornerRadius = size / 2,
            KeepPerfectCircle = true
        };
        return button;
    }

    internal static RoundedButton CreatePillButton(string text, Color backColor, Size size, Font? font = null)
    {
        var button = new RoundedButton
        {
            Text = text,
            BackColor = backColor,
            ForeColor = TextDark,
            Cursor = Cursors.Hand,
            Size = size,
            Font = font ?? FontMed,
            UseVisualStyleBackColor = false,
            TabStop = false,
            CornerRadius = Math.Max(8, size.Height / 2)
        };
        return button;
    }

    internal static Panel CreateSearchHost()
    {
        var host = CreateRoundedPanel(InputWhite, 18);
        host.Padding = new Padding(14, 10, 14, 10);
        return host;
    }
}

public class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}

public class RoundedPanel : BufferedPanel
{
    public int CornerRadius { get; set; } = UI.Radius;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = UI.CreateRoundRectPath(rect, CornerRadius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width > 0 && Height > 0)
        {
            Region?.Dispose();
            Region = new Region(UI.CreateRoundRectPath(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), CornerRadius));
        }
    }
}

public class RoundedButton : Button
{
    public int CornerRadius { get; set; } = UI.Radius;
    public bool KeepPerfectCircle { get; set; }

    protected override bool ShowFocusCues => false;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        BackColor = UI.BtnBlue;
        ForeColor = UI.TextDark;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UseCompatibleTextRendering = false;
        UpdateStyles();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        int radius = KeepPerfectCircle ? Math.Min(Width, Height) / 2 : Math.Min(CornerRadius, Math.Min(Width, Height) / 2);
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = UI.CreateRoundRectPath(rect, radius);
        using var brush = new SolidBrush(Enabled ? BackColor : Color.Silver);
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            rect,
            Enabled ? ForeColor : UI.TextGray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        int radius = KeepPerfectCircle ? Math.Min(Width, Height) / 2 : Math.Min(CornerRadius, Math.Min(Width, Height) / 2);
        Region?.Dispose();
        Region = new Region(UI.CreateRoundRectPath(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), radius));
    }
}

public class CoverPictureBox : Control
{
    private Image? _image;

    public Image? Image
    {
        get => _image;
        set
        {
            _image = value;
            Invalidate();
        }
    }

    public CoverPictureBox()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using var back = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(back, ClientRectangle);

        if (_image == null)
            return;

        var dest = ClientRectangle;
        float imageRatio = (float)_image.Width / _image.Height;
        float boxRatio = dest.Width > 0 && dest.Height > 0 ? (float)dest.Width / dest.Height : 1f;

        Rectangle src;
        if (imageRatio > boxRatio)
        {
            int srcWidth = (int)Math.Round(_image.Height * boxRatio);
            int x = (_image.Width - srcWidth) / 2;
            src = new Rectangle(x, 0, Math.Max(1, srcWidth), _image.Height);
        }
        else
        {
            int srcHeight = (int)Math.Round(_image.Width / boxRatio);
            int y = (_image.Height - srcHeight) / 2;
            src = new Rectangle(0, y, _image.Width, Math.Max(1, srcHeight));
        }

        e.Graphics.DrawImage(_image, dest, src, GraphicsUnit.Pixel);
    }
}

/// <summary>Панель с авто-скроллом и тонким красивым кастомным скроллбаром.
/// Родной системный скроллбар скрывается через ShowScrollBar, а поверх рисуется
/// узкий закруглённый ползунок с поддержкой drag-to-scroll и колеса мыши.</summary>
public class SmoothScrollPanel : BufferedPanel
{
    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
    private const int SB_VERT = 1;
    private const int SB_HORZ = 0;

    private const int TrackWidth = 10;
    private const int ThumbMinHeight = 28;
    private const int TrackMargin = 3;

    private readonly BufferedPanel _track;
    private readonly BufferedPanel _thumb;
    private bool _dragging;
    private int _dragStartMouseY;
    private int _dragStartScroll;

    // Кэш высоты контента. Пересчитывается лениво: при добавлении/удалении/ресайзе
    // дочерних контролов, а на обычном скролле — переиспользуется.
    private int _cachedContentHeight = -1;

    public SmoothScrollPanel()
    {
        AutoScroll = true;

        _track = new BufferedPanel
        {
            BackColor = Color.FromArgb(232, 232, 232),
            Width = TrackWidth,
            Visible = false,
            Cursor = Cursors.Default
        };
        _track.Paint += TrackPaint;
        _track.MouseDown += TrackMouseDown;

        _thumb = new BufferedPanel
        {
            BackColor = Color.FromArgb(150, 150, 150),
            Cursor = Cursors.Hand
        };
        _thumb.Paint += ThumbPaint;
        _thumb.MouseEnter += (_, _) => { _thumb.BackColor = Color.FromArgb(120, 120, 120); _thumb.Invalidate(); };
        _thumb.MouseLeave += (_, _) => { if (!_dragging) { _thumb.BackColor = Color.FromArgb(150, 150, 150); _thumb.Invalidate(); } };
        _thumb.MouseDown += ThumbMouseDown;
        _thumb.MouseMove += ThumbMouseMove;
        _thumb.MouseUp += ThumbMouseUp;

        _track.Controls.Add(_thumb);
        Controls.Add(_track);
    }

    /// <summary>Включаем WS_EX_COMPOSITED — даёт плавный скролл без мерцания
    /// на панелях с большим числом дочерних контролов.</summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    private static void TrackPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UI.CreateRoundRectPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), p.Width / 2);
        using var brush = new SolidBrush(p.BackColor);
        e.Graphics.Clear(p.Parent?.BackColor ?? Color.Transparent);
        e.Graphics.FillPath(brush, path);
    }

    private static void ThumbPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = UI.CreateRoundRectPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), Math.Min(p.Width, p.Height) / 2);
        using var brush = new SolidBrush(p.BackColor);
        e.Graphics.Clear(p.Parent?.BackColor ?? Color.Transparent);
        e.Graphics.FillPath(brush, path);
    }

    private void TrackMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int page = Math.Max(1, ClientSize.Height - 20);
        int current = -AutoScrollPosition.Y;
        int maxScroll = Math.Max(0, GetContentHeight() - ClientSize.Height);
        int target = e.Y < _thumb.Top
            ? Math.Max(0, current - page)
            : Math.Min(maxScroll, current + page);
        AutoScrollPosition = new Point(0, target);
    }

    private void ThumbMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragStartMouseY = Cursor.Position.Y;
        _dragStartScroll = -AutoScrollPosition.Y;
        _thumb.Capture = true;
    }

    private void ThumbMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        int delta = Cursor.Position.Y - _dragStartMouseY;
        int trackH = _track.Height;
        int thumbH = _thumb.Height;
        int travel = trackH - thumbH;
        int maxScroll = Math.Max(0, GetContentHeight() - ClientSize.Height);
        if (travel <= 0 || maxScroll <= 0) return;
        int scrollDelta = (int)((long)delta * maxScroll / travel);
        int newScroll = Math.Max(0, Math.Min(maxScroll, _dragStartScroll + scrollDelta));
        AutoScrollPosition = new Point(0, newScroll);
    }

    private void ThumbMouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = false;
        _thumb.Capture = false;
    }

    /// <summary>Лениво считает высоту контента; кешируется до изменения набора/размеров контролов.</summary>
    private int GetContentHeight()
    {
        if (_cachedContentHeight >= 0) return _cachedContentHeight;

        int bottom = 0;
        foreach (Control c in Controls)
        {
            if (c == _track) continue;
            int absBottom = c.Bottom - AutoScrollPosition.Y;
            if (absBottom > bottom) bottom = absBottom;
        }
        _cachedContentHeight = bottom;
        return bottom;
    }

    /// <summary>Сбросить кэш — вызывается при изменении набора/размеров контролов.</summary>
    private void InvalidateContentHeight() => _cachedContentHeight = -1;

    /// <summary>Публичный метод: попросить панель пересчитать скроллбар (например, после изменения высоты вложенной панели).</summary>
    internal void UpdateScrollbar()
    {
        InvalidateContentHeight();
        ApplyScrollbarLayout();
    }

    /// <summary>Основная работа: скрывает нативный скроллбар и позиционирует trail+thumb.
    /// Вызывается только когда реально нужно — при скролле это дешёвый путь.</summary>
    private void ApplyScrollbarLayout()
    {
        if (!IsHandleCreated) return;
        ShowScrollBar(Handle, SB_VERT, false);
        ShowScrollBar(Handle, SB_HORZ, false);

        int contentH = GetContentHeight();
        int viewH = ClientSize.Height;

        if (contentH <= viewH + 2)
        {
            if (_track.Visible) _track.Visible = false;
            return;
        }

        // Позиция трека должна компенсировать скролл, иначе он уезжает вместе с контентом.
        int trackX = ClientSize.Width - TrackWidth - TrackMargin - AutoScrollPosition.X;
        int trackY = TrackMargin - AutoScrollPosition.Y;
        int trackH = viewH - 2 * TrackMargin;

        var newTrackBounds = new Rectangle(trackX, trackY, TrackWidth, trackH);
        if (_track.Bounds != newTrackBounds) _track.Bounds = newTrackBounds;
        if (!_track.Visible) { _track.Visible = true; _track.BringToFront(); }

        int thumbH = Math.Max(ThumbMinHeight, (int)((long)viewH * trackH / contentH));
        thumbH = Math.Min(thumbH, trackH);

        int maxScroll = Math.Max(1, contentH - viewH);
        int scrollY = -AutoScrollPosition.Y;
        int travel = trackH - thumbH;
        int thumbY = travel > 0 ? (int)((long)scrollY * travel / maxScroll) : 0;

        var newThumbBounds = new Rectangle(1, thumbY, _track.Width - 2, thumbH);
        if (_thumb.Bounds != newThumbBounds) _thumb.Bounds = newThumbBounds;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ShowScrollBar(Handle, SB_VERT, false);
        ShowScrollBar(Handle, SB_HORZ, false);
        UpdateScrollbar();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateScrollbar();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        // На скролле высота контента не меняется — используем кэш.
        ApplyScrollbarLayout();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        // OnScroll сработает следом и применит лейаут; здесь делать ничего не нужно.
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control != _track)
        {
            InvalidateContentHeight();
            if (IsHandleCreated) BeginInvoke((Action)ApplyScrollbarLayout);
        }
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        if (e.Control != _track)
        {
            InvalidateContentHeight();
            if (IsHandleCreated) BeginInvoke((Action)ApplyScrollbarLayout);
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        const int WM_NCCALCSIZE = 0x0083;
        const int WM_SIZE = 0x0005;
        if (m.Msg == WM_NCCALCSIZE || m.Msg == WM_SIZE)
        {
            if (IsHandleCreated)
            {
                ShowScrollBar(Handle, SB_VERT, false);
                ShowScrollBar(Handle, SB_HORZ, false);
            }
        }
    }
}

