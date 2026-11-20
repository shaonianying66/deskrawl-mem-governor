using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace DeskrawlMem
{
    public static class Ui
    {
        public static readonly Color Bg = Color.FromArgb(10, 13, 18);
        public static readonly Color Panel = Color.FromArgb(18, 22, 31);
        public static readonly Color Panel2 = Color.FromArgb(23, 29, 40);
        public static readonly Color Border = Color.FromArgb(34, 42, 56);
        public static readonly Color Text = Color.FromArgb(233, 238, 248);
        public static readonly Color Sub = Color.FromArgb(135, 148, 171);
        public static readonly Color Dim = Color.FromArgb(96, 108, 130);
        public static readonly Color Accent = Color.FromArgb(91, 140, 255);
        public static readonly Color Accent2 = Color.FromArgb(140, 110, 255);
        public static readonly Color Green = Color.FromArgb(62, 213, 152);
        public static readonly Color Amber = Color.FromArgb(255, 180, 84);
        public static readonly Color Red = Color.FromArgb(255, 100, 112);

        private static readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private static string _family = null;

        public static string Family
        {
            get
            {
                if (_family == null)
                {
                    _family = "Microsoft YaHei UI";
                    try
                    {
                        using (InstalledFontCollection col = new InstalledFontCollection())
                        {
                            bool hit = false;
                            foreach (FontFamily f in col.Families)
                                if (f.Name == "Microsoft YaHei UI") { hit = true; break; }
                            if (!hit) _family = "Microsoft YaHei";
                        }
                    }
                    catch { }
                }
                return _family;
            }
        }

        public static Font F(float pt)
        {
            return F(pt, FontStyle.Regular);
        }

        public static Font F(float pt, FontStyle st)
        {
            string key = pt.ToString("0.0") + "|" + (int)st;
            Font f;
            if (_fonts.TryGetValue(key, out f)) return f;
            f = new Font(Family, pt, st, GraphicsUnit.Point);
            _fonts[key] = f;
            return f;
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color color)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(color))
                g.FillPath(b, p);
        }

        public static void DrawRound(Graphics g, Rectangle r, int radius, Color color, float width)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(color, width))
                g.DrawPath(pen, p);
        }

        public const TextFormatFlags LeftMid = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        public const TextFormatFlags RightMid = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        public const TextFormatFlags Center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
        public const TextFormatFlags LeftTop = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix;
        public const TextFormatFlags Wrap = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;
    }

    // ------------------------------------------------------------------
    // 卡片容器
    // ------------------------------------------------------------------
    public class Card : Panel
    {
        public int Radius = 14;
        public bool Glow = false;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Ui.Round(r, Radius))
            using (SolidBrush b = new SolidBrush(BackColor))
                g.FillPath(b, path);
            Ui.DrawRound(g, r, Radius, Ui.Border, 1f);
            // 顶部高光，制造一点层次
            using (Pen p = new Pen(Color.FromArgb(26, Color.White), 1f))
            {
                int inset = Radius / 2 + 2;
                g.DrawLine(p, inset, 1, Width - inset, 1);
            }
            if (Glow)
                Ui.DrawRound(g, r, Radius, Color.FromArgb(60, Ui.Accent), 1f);
            base.OnPaint(e);
        }
    }

    // ------------------------------------------------------------------
    // 按钮
    // ------------------------------------------------------------------
    public class Btn : Control
    {
        public Color Back = Ui.Accent;
        public Color Fore = Color.White;
        public Color Hover = Color.Empty;
        public Color BorderColor = Color.Empty;
        public int Radius = 10;
        public bool Outline = false;
        private bool _over, _down;

        public Btn()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.StandardClick, false); // 自己处理点击，避免重复触发
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Ui.F(9.5f);
        }

        protected override void OnMouseEnter(EventArgs e) { _over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _over = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _down = false; Invalidate();
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color back = Back;
            if (Outline)
            {
                back = _over ? Color.FromArgb(30, 40, 58) : Color.FromArgb(16, 20, 28);
            }
            else
            {
                if (!Hover.IsEmpty && _over) back = Hover;
                if (_down) back = ControlPaint.Dark(back, 0.12f);
            }
            using (GraphicsPath path = Ui.Round(r, Radius))
            using (SolidBrush b = new SolidBrush(back))
                g.FillPath(b, path);
            if (!BorderColor.IsEmpty)
                Ui.DrawRound(g, r, Radius, _over ? Color.FromArgb(120, BorderColor) : BorderColor, 1f);
            TextRenderer.DrawText(g, Text, Font, r, Fore, Ui.Center);
        }
    }

    // ------------------------------------------------------------------
    // 上限滑杆（对数刻度，256MB - 32GB）
    // ------------------------------------------------------------------
    public class CapSlider : Control
    {
        public long MinBytes = 256L << 20;
        public long MaxBytes = 32L << 30;
        public long ValueBytes = 4L << 30;
        public long StepBytes = 128L << 20;
        public event EventHandler ValueChanged;

        private bool _drag;

        public CapSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Height = 30;
        }

        private double PosOf(long bytes)
        {
            double mn = Math.Log(MinBytes / 1048576.0);
            double mx = Math.Log(MaxBytes / 1048576.0);
            double v = Math.Log(Math.Max(1, bytes) / 1048576.0);
            return Math.Max(0, Math.Min(1, (v - mn) / (mx - mn)));
        }

        private long BytesOf(double pos)
        {
            double mn = Math.Log(MinBytes / 1048576.0);
            double mx = Math.Log(MaxBytes / 1048576.0);
            double mb = Math.Exp(mn + (mx - mn) * pos);
            long b = (long)(mb * 1048576.0);
            b = (b / StepBytes) * StepBytes;
            if (b < MinBytes) b = MinBytes;
            if (b > MaxBytes) b = MaxBytes;
            return b;
        }

        public void SetValue(long bytes, bool raise)
        {
            if (bytes < MinBytes) bytes = MinBytes;
            if (bytes > MaxBytes) bytes = MaxBytes;
            bytes = (bytes / StepBytes) * StepBytes;
            if (bytes == ValueBytes) return;
            ValueBytes = bytes;
            Invalidate();
            if (raise && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _drag = true;
            DragTo(e.X);
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag) DragTo(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _drag = false;
            base.OnMouseUp(e);
        }

        private void DragTo(int x)
        {
            int pad = ThumbW / 2;
            double pos = (x - pad) / (double)Math.Max(1, Width - ThumbW);
            SetValue(BytesOf(pos), true);
        }

        private const int ThumbW = 20;

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            SetValue(ValueBytes + (e.Delta > 0 ? StepBytes : -StepBytes), true);
            base.OnMouseWheel(e);
        }

        protected override bool IsInputKey(Keys k)
        {
            if (k == Keys.Left || k == Keys.Right) return true;
            return base.IsInputKey(k);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) SetValue(ValueBytes - StepBytes, true);
            if (e.KeyCode == Keys.Right) SetValue(ValueBytes + StepBytes, true);
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cy = Height / 2;
            int x0 = ThumbW / 2;
            int x1 = Width - ThumbW / 2;
            Rectangle track = new Rectangle(x0, cy - 4, Math.Max(1, x1 - x0), 8);
            Ui.FillRound(g, track, 4, Color.FromArgb(28, 35, 48));

            double pos = PosOf(ValueBytes);
            int tx = x0 + (int)Math.Round(pos * (x1 - x0));
            Rectangle fill = new Rectangle(x0, cy - 4, Math.Max(2, tx - x0), 8);
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(x0, 0), new Point(Math.Max(x0 + 2, tx), 0), Ui.Accent, Ui.Accent2))
                g.FillPath(b, Ui.Round(fill, 4));

            for (int i = 0; i < 5; i++)
            {
                int gx = x0 + (x1 - x0) * i / 4;
                using (SolidBrush b = new SolidBrush(i == 0 ? Color.FromArgb(60, 70, 90) : Color.FromArgb(34, 42, 58)))
                    g.FillEllipse(b, gx - 1, cy + 12, 2, 2);
            }

            Rectangle thumb = new Rectangle(tx - ThumbW / 2, cy - ThumbW / 2, ThumbW, ThumbW);
            using (SolidBrush sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(sh, thumb.X, thumb.Y + 2, thumb.Width, thumb.Height);
            using (SolidBrush b = new SolidBrush(Color.White))
                g.FillEllipse(b, thumb);
            using (Pen p = new Pen(Ui.Accent, 3f))
                g.DrawEllipse(p, thumb.X + 3, thumb.Y + 3, thumb.Width - 7, thumb.Height - 7);

            if (Focused)
                using (Pen p = new Pen(Color.FromArgb(90, Ui.Accent), 1f))
                    g.DrawEllipse(p, thumb.X - 3, thumb.Y - 3, thumb.Width + 6, thumb.Height + 6);
        }
    }

    // ------------------------------------------------------------------
    // 走势图
    // ------------------------------------------------------------------
    public class MemChart : Control
    {
        public List<ProcSample> Data = new List<ProcSample>();
        public long CapBytes;
        public string Empty = "等待进程…";

        public MemChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        private Rectangle Plot
        {
            get { return new Rectangle(56, 14, Math.Max(10, Width - 68), Math.Max(10, Height - 38)); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle p = Plot;

            if (Data.Count < 2)
            {
                TextRenderer.DrawText(g, Empty, Ui.F(9.5f), p, Ui.Dim, Ui.Center);
                return;
            }

            long max = 1;
            long maxWs = 0, maxCommit = 0;
            foreach (ProcSample s in Data)
            {
                if (s.WorkingSet > maxWs) maxWs = s.WorkingSet;
                if (s.Commit > maxCommit) maxCommit = s.Commit;
            }
            max = Math.Max(maxWs, maxCommit);
            if (CapBytes > max) max = CapBytes;
            max = (long)(max * 1.12);
            if (max < 512L << 20) max = 512L << 20;

            int lines = 4;
            for (int i = 0; i <= lines; i++)
            {
                int y = p.Bottom - (p.Height * i / lines);
                using (Pen pen = new Pen(i == 0 ? Color.FromArgb(48, 58, 76) : Color.FromArgb(28, 35, 48), 1f))
                    g.DrawLine(pen, p.Left, y, p.Right, y);
                string lbl = Native.Fmt(max * i / lines);
                TextRenderer.DrawText(g, lbl, Ui.F(7.5f), new Rectangle(0, y - 8, p.Left - 8, 16), Ui.Dim, Ui.RightMid);
            }

            double stepX = p.Width / (double)Math.Max(1, Data.Count - 1);

            // 提交（虚线）
            using (Pen pen = new Pen(Color.FromArgb(150, Ui.Amber), 1.2f))
            {
                pen.DashStyle = DashStyle.Dash;
                PointF[] pts = new PointF[Data.Count];
                for (int i = 0; i < Data.Count; i++)
                    pts[i] = new PointF((float)(p.Left + i * stepX), (float)(p.Bottom - p.Height * (Data[i].Commit / (double)max)));
                g.DrawLines(pen, pts);
            }

            // 工作集（面 + 线）
            PointF[] ws = new PointF[Data.Count];
            for (int i = 0; i < Data.Count; i++)
                ws[i] = new PointF((float)(p.Left + i * stepX), (float)(p.Bottom - p.Height * (Data[i].WorkingSet / (double)max)));

            List<PointF> poly = new List<PointF>(ws);
            poly.Add(new PointF(ws[ws.Length - 1].X, p.Bottom));
            poly.Add(new PointF(ws[0].X, p.Bottom));
            using (GraphicsPath area = new GraphicsPath())
            {
                area.AddPolygon(poly.ToArray());
                using (LinearGradientBrush b = new LinearGradientBrush(new Point(0, p.Top), new Point(0, p.Bottom),
                           Color.FromArgb(62, Ui.Accent), Color.FromArgb(2, Ui.Accent)))
                    g.FillPath(b, area);
            }
            using (Pen pen = new Pen(Ui.Accent, 2f))
                g.DrawLines(pen, ws);

            PointF last = ws[ws.Length - 1];
            using (SolidBrush b = new SolidBrush(Ui.Accent))
                g.FillEllipse(b, last.X - 3.5f, last.Y - 3.5f, 7, 7);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, Ui.Accent)))
                g.FillEllipse(b, last.X - 8, last.Y - 8, 16, 16);

            // 上限线
            if (CapBytes > 0)
            {
                int y = (int)(p.Bottom - p.Height * (CapBytes / (double)max));
                if (y > p.Top && y < p.Bottom)
                {
                    using (Pen pen = new Pen(Color.FromArgb(170, Ui.Red), 1f))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        g.DrawLine(pen, p.Left, y, p.Right, y);
                    }
                    string tagText = "上限 " + Native.Fmt(CapBytes);
                    Size ts = TextRenderer.MeasureText(tagText, Ui.F(7.5f));
                    Rectangle tag = new Rectangle(p.Left + 6, y - 9, ts.Width + 16, 18);
                    Ui.FillRound(g, tag, 6, Color.FromArgb(45, Ui.Red));
                    TextRenderer.DrawText(g, "上限 " + Native.Fmt(CapBytes), Ui.F(7.5f), tag,
                        Ui.Red, Ui.Center);
                }
            }

            // 图例
            int lx = p.Left;
            using (Pen pen = new Pen(Ui.Accent, 2f)) g.DrawLine(pen, lx, p.Top - 2, lx + 16, p.Top - 2);
            TextRenderer.DrawText(g, "常驻", Ui.F(8f), new Rectangle(lx + 20, p.Top - 10, 44, 16), Ui.Sub, Ui.LeftMid);
            lx += 62;
            using (Pen pen = new Pen(Color.FromArgb(150, Ui.Amber), 1.4f)) { pen.DashStyle = DashStyle.Dash; g.DrawLine(pen, lx, p.Top - 2, lx + 16, p.Top - 2); }
            TextRenderer.DrawText(g, "提交", Ui.F(8f), new Rectangle(lx + 20, p.Top - 10, 44, 16), Ui.Sub, Ui.LeftMid);

            TextRenderer.DrawText(g, "最近 " + Data.Count + " 秒", Ui.F(8f), new Rectangle(p.Right - 90, p.Top - 10, 90, 16), Ui.Dim, Ui.RightMid);
        }
    }

    // ------------------------------------------------------------------
    // 数据卡片
    // ------------------------------------------------------------------
    public class StatCard : Control
    {
        public string Title = "";
        public string Value = "—";
        public string Sub = "";
        public double Ratio = 0;
        public Color AccentColor = Ui.Accent;
        public bool Danger = false;

        public StatCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Ui.Round(r, 14))
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(0, 0), new Point(0, Height), Ui.Panel2, Ui.Panel))
                g.FillPath(b, path);
            Ui.DrawRound(g, r, 14, Ui.Border, 1f);

            TextRenderer.DrawText(g, Title, Ui.F(8.5f), new Rectangle(16, 12, Width - 32, 18), Ui.Sub, Ui.LeftMid);
            TextRenderer.DrawText(g, Value, Ui.F(20f, FontStyle.Bold), new Rectangle(16, 32, Width - 32, 34),
                Danger ? Ui.Red : Ui.Text, Ui.LeftMid);
            TextRenderer.DrawText(g, Sub, Ui.F(8f), new Rectangle(16, 66, Width - 32, 18), Ui.Dim, Ui.LeftMid);

            Rectangle bar = new Rectangle(16, Height - 16, Width - 32, 4);
            Ui.FillRound(g, bar, 2, Color.FromArgb(30, 37, 50));
            int w = (int)(bar.Width * Math.Max(0, Math.Min(1, Ratio)));
            if (w > 2)
            {
                Rectangle fill = new Rectangle(bar.X, bar.Y, w, bar.Height);
                Color c2 = Danger ? Color.FromArgb(255, 150, 100) : ControlPaint.Light(AccentColor, 0.5f);
                using (LinearGradientBrush lb = new LinearGradientBrush(new Point(bar.X, 0), new Point(bar.Right, 0),
                           AccentColor, c2))
                    g.FillPath(lb, Ui.Round(fill, 2));
            }
        }
    }

    // ------------------------------------------------------------------
    // 单选行
    // ------------------------------------------------------------------
    public class ToggleRow : Control
    {
        public bool Selected = false;
        public string Desc = "";
        public int Index = 0;
        public event EventHandler Picked;
        private bool _over;

        public ToggleRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _over = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Picked != null) Picked(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (Selected)
            {
                using (GraphicsPath path = Ui.Round(r, 9))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(30, 40, 66)))
                    g.FillPath(b, path);
                Ui.DrawRound(g, r, 9, Color.FromArgb(90, Ui.Accent), 1f);
            }
            else if (_over)
            {
                Ui.FillRound(g, r, 9, Color.FromArgb(22, 28, 40));
            }

            int cx = 16, cy = Height / 2;
            using (Pen pen = new Pen(Selected ? Ui.Accent : Color.FromArgb(70, 82, 102), 1.6f))
                g.DrawEllipse(pen, cx - 7, cy - 7, 14, 14);
            if (Selected)
                using (SolidBrush b = new SolidBrush(Ui.Accent))
                    g.FillEllipse(b, cx - 3.5f, cy - 3.5f, 7, 7);

            Rectangle tr = new Rectangle(36, 6, Width - 50, Height - 12);
            TextRenderer.DrawText(g, Text, Ui.F(9.5f, Selected ? FontStyle.Bold : FontStyle.Regular),
                new Rectangle(tr.X, tr.Y, tr.Width, 20), Selected ? Ui.Text : Color.FromArgb(205, 214, 230), Ui.LeftMid);
            if (Desc.Length > 0)
                TextRenderer.DrawText(g, Desc, Ui.F(8f), new Rectangle(tr.X, tr.Y + 20, tr.Width, 18), Ui.Dim, Ui.LeftMid);
        }
    }
}
