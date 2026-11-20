using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DeskrawlMem
{
    // ==================================================================
    // 深色提示框
    // ==================================================================
    public static class Dlg
    {
        public static void Info(IWin32Window owner, string title, string text)
        {
            using (PromptForm f = new PromptForm(title, text, "知道了", null, false)) f.ShowDialog(owner);
        }

        public static bool Confirm(IWin32Window owner, string title, string text, string okText, bool danger)
        {
            using (PromptForm f = new PromptForm(title, text, okText, null, danger)) return f.ShowDialog(owner) == DialogResult.OK;
        }

        public static string Input(IWin32Window owner, string title, string text, string def)
        {
            using (PromptForm f = new PromptForm(title, text, "确定", def, false))
                return f.ShowDialog(owner) == DialogResult.OK ? f.Value : null;
        }

        private class PromptForm : Form
        {
            public string Value = "";
            private TextBox _box;

            public PromptForm(string title, string text, string okText, string def, bool danger)
            {
                Text = title;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                BackColor = Ui.Bg;
                ForeColor = Ui.Text;
                Font = Ui.F(9.5f);
                ClientSize = new Size(430, def == null ? 210 : 250);

                Label t = new Label();
                t.Text = title;
                t.Font = Ui.F(12f, FontStyle.Bold);
                t.ForeColor = Ui.Text;
                t.BackColor = Color.Transparent;
                t.SetBounds(22, 18, 386, 26);
                Controls.Add(t);

                Label m = new Label();
                m.Text = text;
                m.Font = Ui.F(9.5f);
                m.ForeColor = Ui.Sub;
                m.BackColor = Color.Transparent;
                m.SetBounds(22, 50, 386, def == null ? 84 : 72);
                Controls.Add(m);

                int btnY = ClientSize.Height - 58;

                if (def != null)
                {
                    _box = new TextBox();
                    _box.Text = def;
                    _box.BackColor = Ui.Panel2;
                    _box.ForeColor = Ui.Text;
                    _box.BorderStyle = BorderStyle.FixedSingle;
                    _box.Font = Ui.F(11f);
                    _box.SetBounds(22, 120, 386, 30);
                    Controls.Add(_box);
                    btnY = ClientSize.Height - 58;
                }

                Btn ok = new Btn();
                ok.Text = okText;
                ok.Font = Ui.F(9.5f);
                ok.Back = danger ? Ui.Red : Ui.Accent;
                ok.Fore = Color.White;
                ok.SetBounds(ClientSize.Width - 130, btnY, 106, 38);
                ok.Click += delegate { Value = _box != null ? _box.Text : ""; DialogResult = DialogResult.OK; };
                Controls.Add(ok);

                if (def != null || danger)
                {
                    Btn cancel = new Btn();
                    cancel.Text = "取消";
                    cancel.Font = Ui.F(9.5f);
                    cancel.Outline = true;
                    cancel.Fore = Ui.Sub;
                    cancel.BorderColor = Ui.Border;
                    cancel.SetBounds(ClientSize.Width - 236, btnY, 100, 38);
                    cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
                    Controls.Add(cancel);
                }

                AcceptButton = null;
                KeyPreview = true;
                KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel; };
            }
        }
    }

    // ==================================================================
    // 顶栏
    // ==================================================================
    public class HeaderBar : Panel
    {
        public HeaderBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Ui.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush b = new LinearGradientBrush(new Point(0, 0), new Point(0, Height),
                       Color.FromArgb(16, 22, 34), Ui.Bg))
                g.FillRectangle(b, ClientRectangle);

            // 左侧光晕
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(-120, -150, 420, 300);
                using (PathGradientBrush pb = new PathGradientBrush(p))
                {
                    pb.CenterColor = Color.FromArgb(38, Ui.Accent);
                    pb.SurroundColors = new Color[] { Color.FromArgb(0, Ui.Accent) };
                    g.FillPath(pb, p);
                }
            }
            using (Pen pen = new Pen(Color.FromArgb(40, Ui.Accent), 1f))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }

    // ==================================================================
    // 主窗体
    // ==================================================================
    public class MainForm : Form
    {
        private readonly string[] _args;
        private Config _cfg;
        private Governor _g = new Governor();
        private List<ProcSample> _hist = new List<ProcSample>();
        private System.Windows.Forms.Timer _tick;
        private int _mode;              // 0 上限 1 上限+回收 2 提交硬上限
        private bool _applied;
        private int _pickedPid;
        private DateTime _lastTrim = DateTime.MinValue;
        private string _report = "";
        private bool _admin;

        private HeaderBar _header;
        private Label _lblTitle, _lblSub, _lblChip;
        private Panel _chipDot;

        private Card _cProc, _cLimit, _cMode, _cChart, _cAnalysis;
        private Label _lProcTitle, _lProcName, _lProcMeta;
        private Btn _btnPick;

        private Label _lLimitTitle, _lLimitVal, _lLimitCur, _lLimitScaleA, _lLimitScaleB, _lLimitHint, _lLimitWarn;
        private CapSlider _slider;
        private Btn[] _chips;

        private Label _lModeTitle, _lModeFoot;
        private ToggleRow[] _rows;

        private Btn _btnApply, _btnRelease, _btnTrim;

        private StatCard _statWs, _statCommit, _statHead;
        private Label _lChartTitle;
        private MemChart _chart;
        private Label _lAnalysisTitle;
        private TextBox _txtReport;
        private Btn _btnScan, _btnSave;

        private Panel _status;
        private Label _lMsg, _lVer;
        private CheckBox _chkAuto, _chkBoot;
        private Btn _btnAdmin;

        private static readonly long[] ChipMb = new long[] { 1024, 2048, 4096, 6144, 8192, 12288, 16384 };

        // --------------------------------------------------------------
        public MainForm(string[] args)
        {
            _args = args ?? new string[0];
            _cfg = Config.Load();

            Text = "Deskrawl 内存管家";
            BackColor = Ui.Bg;
            ForeColor = Ui.Text;
            Font = Ui.F(9f);
            ClientSize = new Size(1200, 800);
            MinimumSize = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            try { Icon = MakeIcon(); } catch { }

            _admin = IsAdmin();

            BuildHeader();
            BuildLeft();
            BuildRight();
            BuildStatus();
            LayoutAll();

            _mode = _cfg.Mode;
            if (_mode < 0 || _mode > 2) _mode = 0;
            for (int i = 0; i < _rows.Length; i++) _rows[i].Selected = (i == _mode);
            UpdateModeText();

            long defMb = _cfg.LimitMb > 0 ? _cfg.LimitMb : DefaultLimitMb();
            _slider.SetValue(defMb << 20, false);
            UpdateLimitText(false);

            _tick = new System.Windows.Forms.Timer();
            _tick.Interval = 1000;
            _tick.Tick += Tick;
            _tick.Start();

            Load += delegate
            {
                SetMsg(_admin ? "已就绪（管理员权限）"
                              : "已就绪（普通权限即可管理同一用户下的游戏进程）", Ui.Sub);
                EnsureTarget();
                if (_args.Length > 0 && (Has("--watch") || Has("--autostart")))
                {
                    _chkBoot.Checked = Config.AutoStartEnabled();
                }
            };
        }

        private bool Has(string flag)
        {
            foreach (string a in _args) if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // --------------------------------------------------------------
        // 构建界面
        // --------------------------------------------------------------
        private void BuildHeader()
        {
            _header = new HeaderBar();
            Controls.Add(_header);

            _lblTitle = new Label();
            _lblTitle.Text = "Deskrawl 内存管家";
            _lblTitle.Font = Ui.F(17f, FontStyle.Bold);
            _lblTitle.ForeColor = Ui.Text;
            _lblTitle.BackColor = Color.Transparent;
            _header.Controls.Add(_lblTitle);

            _lblSub = new Label();
            _lblSub.Text = "自己决定游戏能用多少内存 · 实时监测 · 一键回收";
            _lblSub.Font = Ui.F(9f);
            _lblSub.ForeColor = Ui.Sub;
            _lblSub.BackColor = Color.Transparent;
            _header.Controls.Add(_lblSub);

            _chipDot = new Panel();
            _chipDot.BackColor = Ui.Dim;
            _chipDot.SetBounds(0, 0, 8, 8);
            _chipDot.Region = new Region(Ui.Round(new Rectangle(0, 0, 8, 8), 4));
            _header.Controls.Add(_chipDot);

            _lblChip = new Label();
            _lblChip.Text = "未连接";
            _lblChip.Font = Ui.F(9f);
            _lblChip.ForeColor = Ui.Sub;
            _lblChip.BackColor = Color.Transparent;
            _header.Controls.Add(_lblChip);
        }

        private void BuildLeft()
        {
            _cProc = new Card();
            Controls.Add(_cProc);

            _lProcTitle = MkLabel("目标进程", 8.5f, Ui.Sub, _cProc);
            _lProcTitle.SetBounds(16, 12, 200, 18);

            _btnPick = new Btn();
            _btnPick.Text = "切换";
            _btnPick.Font = Ui.F(8.5f);
            _btnPick.Outline = true;
            _btnPick.Fore = Ui.Sub;
            _btnPick.BorderColor = Ui.Border;
            _btnPick.SetBounds(264, 8, 76, 26);
            _btnPick.Click += delegate { PickProcess(); };
            _cProc.Controls.Add(_btnPick);

            _lProcName = MkLabel("未检测到游戏", 14f, Ui.Text, _cProc);
            _lProcName.Font = Ui.F(14f, FontStyle.Bold);
            _lProcName.SetBounds(16, 32, 324, 28);

            _lProcMeta = MkLabel("请先启动 Deskrawl，或点右上角「切换」手动选择进程", 8f, Ui.Dim, _cProc);
            _lProcMeta.SetBounds(16, 64, 324, 44);

            // ---- 上限卡片
            _cLimit = new Card();
            Controls.Add(_cLimit);

            _lLimitTitle = MkLabel("内存上限", 8.5f, Ui.Sub, _cLimit);
            _lLimitTitle.SetBounds(16, 12, 200, 18);

            _lLimitVal = MkLabel("4.00 GB", 24f, Ui.Text, _cLimit);
            _lLimitVal.Font = Ui.F(24f, FontStyle.Bold);
            _lLimitVal.SetBounds(14, 30, 220, 44);
            _lLimitVal.Cursor = Cursors.Hand;
            _lLimitVal.Click += delegate { InputLimit(); };

            _lLimitCur = MkLabel("", 8.5f, Ui.Sub, _cLimit);
            _lLimitCur.SetBounds(190, 40, 150, 18);
            _lLimitCur.TextAlign = ContentAlignment.MiddleRight;

            _slider = new CapSlider();
            _slider.SetBounds(16, 84, 324, 30);
            _slider.ValueChanged += delegate { UpdateLimitText(true); };
            _cLimit.Controls.Add(_slider);

            _lLimitScaleA = MkLabel("256 MB", 7.5f, Ui.Dim, _cLimit);
            _lLimitScaleA.SetBounds(18, 114, 80, 16);
            _lLimitScaleB = MkLabel("32 GB", 7.5f, Ui.Dim, _cLimit);
            _lLimitScaleB.SetBounds(280, 114, 60, 16);
            _lLimitScaleB.TextAlign = ContentAlignment.MiddleRight;

            _chips = new Btn[ChipMb.Length];
            for (int i = 0; i < ChipMb.Length; i++)
            {
                long v = ChipMb[i] << 20;
                Btn b = new Btn();
                b.Text = (ChipMb[i] >= 1024 ? (ChipMb[i] / 1024) + "G" : ChipMb[i] + "M");
                b.Font = Ui.F(8f);
                b.Outline = true;
                b.Fore = Ui.Sub;
                b.BorderColor = Ui.Border;
                b.Radius = 7;
                b.SetBounds(16 + i * 45, 136, 40, 26);
                b.Click += delegate { _slider.SetValue(v, true); };
                _cLimit.Controls.Add(b);
                _chips[i] = b;
            }

            _lLimitHint = MkLabel("拖动滑杆，或用 ← → 每次调整 128 MB；点数字可直接输入", 8f, Ui.Dim, _cLimit);
            _lLimitHint.SetBounds(16, 170, 324, 18);

            _lLimitWarn = MkLabel("", 8f, Ui.Amber, _cLimit);
            _lLimitWarn.SetBounds(16, 190, 324, 48);

            // ---- 方式卡片
            _cMode = new Card();
            Controls.Add(_cMode);

            _lModeTitle = MkLabel("管控方式", 8.5f, Ui.Sub, _cMode);
            _lModeTitle.SetBounds(16, 12, 200, 18);

            string[] titles = new string[] { "工作集上限（推荐）", "上限 + 定时回收", "提交硬上限（有风险）" };
            string[] descs = new string[] { "超过上限的页面自动换出到磁盘，游戏不崩", "每分钟强制回收一次，常驻更低、偶尔卡顿", "把总提交写死，超出会被系统直接结束" };
            _rows = new ToggleRow[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                int idx = i;
                ToggleRow r = new ToggleRow();
                r.Text = titles[i];
                r.Desc = descs[i];
                r.Index = i;
                r.SetBounds(10, 34 + i * 46, 336, 44);
                r.Picked += delegate { SetMode(idx); };
                _cMode.Controls.Add(r);
                _rows[i] = r;
            }

            _lModeFoot = MkLabel("", 8f, Ui.Dim, _cMode);
            _lModeFoot.SetBounds(16, 172, 324, 30);

            // ---- 操作按钮
            _btnApply = new Btn();
            _btnApply.Text = "应用上限";
            _btnApply.Font = Ui.F(10f, FontStyle.Bold);
            _btnApply.Back = Ui.Accent;
            _btnApply.Hover = Color.FromArgb(110, 155, 255);
            _btnApply.Fore = Color.White;
            _btnApply.Click += delegate { ApplyNow(); };
            Controls.Add(_btnApply);

            _btnRelease = new Btn();
            _btnRelease.Text = "解除限制";
            _btnRelease.Outline = true;
            _btnRelease.Fore = Ui.Text;
            _btnRelease.BorderColor = Ui.Border;
            _btnRelease.Click += delegate { ReleaseNow(); };
            Controls.Add(_btnRelease);

            _btnTrim = new Btn();
            _btnTrim.Text = "立即回收";
            _btnTrim.Outline = true;
            _btnTrim.Fore = Ui.Text;
            _btnTrim.BorderColor = Ui.Border;
            _btnTrim.Click += delegate { TrimNow(true); };
            Controls.Add(_btnTrim);
        }

        private void BuildRight()
        {
            _statWs = new StatCard();
            _statWs.Title = "常驻内存（工作集）";
            _statWs.AccentColor = Ui.Accent;
            Controls.Add(_statWs);

            _statCommit = new StatCard();
            _statCommit.Title = "提交内存";
            _statCommit.AccentColor = Ui.Accent2;
            Controls.Add(_statCommit);

            _statHead = new StatCard();
            _statHead.Title = "距上限余量";
            _statHead.AccentColor = Ui.Green;
            Controls.Add(_statHead);

            _cChart = new Card();
            Controls.Add(_cChart);
            _lChartTitle = MkLabel("内存走势", 8.5f, Ui.Sub, _cChart);
            _lChartTitle.SetBounds(16, 8, 200, 18);
            _chart = new MemChart();
            _chart.SetBounds(10, 26, 10, 10);
            _cChart.Controls.Add(_chart);

            _cAnalysis = new Card();
            Controls.Add(_cAnalysis);
            _lAnalysisTitle = MkLabel("内存体检", 8.5f, Ui.Sub, _cAnalysis);
            _lAnalysisTitle.SetBounds(16, 8, 200, 18);

            _btnScan = new Btn();
            _btnScan.Text = "分析原因";
            _btnScan.Font = Ui.F(8.5f);
            _btnScan.Back = Color.FromArgb(38, 48, 70);
            _btnScan.Fore = Ui.Text;
            _btnScan.Radius = 8;
            _cAnalysis.Controls.Add(_btnScan);
            _btnScan.Click += delegate { RunScan(); };

            _btnSave = new Btn();
            _btnSave.Text = "保存报告";
            _btnSave.Font = Ui.F(8.5f);
            _btnSave.Outline = true;
            _btnSave.Fore = Ui.Sub;
            _btnSave.BorderColor = Ui.Border;
            _btnSave.Radius = 8;
            _cAnalysis.Controls.Add(_btnSave);
            _btnSave.Click += delegate { SaveReport(); };

            _txtReport = new TextBox();
            _txtReport.Multiline = true;
            _txtReport.ReadOnly = true;
            _txtReport.BorderStyle = BorderStyle.None;
            _txtReport.BackColor = Color.FromArgb(15, 19, 27);
            _txtReport.ForeColor = Color.FromArgb(198, 209, 226);
            _txtReport.Font = Ui.F(9f);
            _txtReport.ScrollBars = ScrollBars.Vertical;
            _txtReport.Text = "点击右上角「分析原因」，我会拆开这个进程的内存：\r\n\r\n" +
                              "· 哪些是游戏资源文件（不会真占内存）\r\n" +
                              "· 哪些是 Unity 引擎自己吃掉又还不回来的\r\n" +
                              "· 哪些是 mod / 助手注入带进来的\r\n" +
                              "并给出适合你这台机器的上限建议。";
            _cAnalysis.Controls.Add(_txtReport);
        }

        private void BuildStatus()
        {
            _status = new Panel();
            _status.BackColor = Color.FromArgb(13, 17, 24);
            Controls.Add(_status);

            _lMsg = new Label();
            _lMsg.Text = "";
            _lMsg.Font = Ui.F(8.5f);
            _lMsg.ForeColor = Ui.Sub;
            _lMsg.BackColor = Color.Transparent;
            _status.Controls.Add(_lMsg);

            _chkAuto = new CheckBox();
            _chkAuto.Text = "自动接管";
            _chkAuto.Font = Ui.F(8.5f);
            _chkAuto.ForeColor = Ui.Sub;
            _chkAuto.BackColor = Color.Transparent;
            _chkAuto.FlatStyle = FlatStyle.Flat;
            _chkAuto.Checked = _cfg.AutoApply;
            _chkAuto.CheckedChanged += delegate { _cfg.AutoApply = _chkAuto.Checked; _cfg.Save(); };
            _status.Controls.Add(_chkAuto);

            _chkBoot = new CheckBox();
            _chkBoot.Text = "开机守护";
            _chkBoot.Font = Ui.F(8.5f);
            _chkBoot.ForeColor = Ui.Sub;
            _chkBoot.BackColor = Color.Transparent;
            _chkBoot.FlatStyle = FlatStyle.Flat;
            _chkBoot.Checked = Config.AutoStartEnabled();
            _chkBoot.CheckedChanged += delegate
            {
                try
                {
                    Config.SetAutoStart(_chkBoot.Checked);
                    SetMsg(_chkBoot.Checked ? "已加入开机启动，登录后自动接管游戏" : "已取消开机启动", Ui.Sub);
                }
                catch (Exception ex)
                {
                    _chkBoot.Checked = Config.AutoStartEnabled();
                    Dlg.Info(this, "设置失败", ex.Message);
                }
            };
            _status.Controls.Add(_chkBoot);

            _btnAdmin = new Btn();
            _btnAdmin.Text = "以管理员身份重启";
            _btnAdmin.Font = Ui.F(8.5f);
            _btnAdmin.Back = Color.FromArgb(36, 30, 22);
            _btnAdmin.Fore = Ui.Amber;
            _btnAdmin.Radius = 7;
            _btnAdmin.Click += delegate { RestartAsAdmin(); };
            _status.Controls.Add(_btnAdmin);

            _lVer = new Label();
            _lVer.Text = "v1.1";
            _lVer.Font = Ui.F(8f);
            _lVer.ForeColor = Ui.Dim;
            _lVer.BackColor = Color.Transparent;
            _status.Controls.Add(_lVer);
        }

        private Label MkLabel(string text, float pt, Color color, Control parent)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Ui.F(pt);
            l.ForeColor = color;
            l.BackColor = parent.BackColor;
            l.AutoSize = false;
            parent.Controls.Add(l);
            return l;
        }

        // --------------------------------------------------------------
        // 布局
        // --------------------------------------------------------------
        private void LayoutAll()
        {
            int W = ClientSize.Width;
            int H = ClientSize.Height;
            int pad = 22;
            int headerH = 78;
            int statusH = 46;
            int top = headerH + 12;
            int bottom = H - statusH;
            int leftW = 360;
            int gap = 12;
            int avail = bottom - top;

            _header.SetBounds(0, 0, W, headerH);
            _lblTitle.SetBounds(pad + 2, 16, 420, 30);
            _lblSub.SetBounds(pad + 4, 46, 460, 20);
            _chipDot.SetBounds(W - 214, 34, 8, 8);
            _lblChip.SetBounds(W - 200, 26, 180, 24);

            int hA = 118;
            int hB = 248;
            int hBtn = 52;
            int hC = avail - hA - hB - hBtn - gap * 3;
            if (hC < 150) hC = 150;
            if (hC > 230) hC = 230;

            int y = top;
            _cProc.SetBounds(pad, y, leftW, hA); y += hA + gap;
            _cLimit.SetBounds(pad, y, leftW, hB); y += hB + gap;
            _cMode.SetBounds(pad, y, leftW, hC); y += hC + gap;
            _btnApply.SetBounds(pad, y, 160, hBtn);
            _btnRelease.SetBounds(pad + 168, y, 108, hBtn);
            _btnTrim.SetBounds(pad + 284, y, 98, hBtn);

            // 方式卡片内部随高度调整
            int rowH = hC >= 190 ? 44 : 40;
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].SetBounds(10, 32 + i * rowH, leftW - 20, rowH);
            _lModeFoot.SetBounds(16, 32 + 3 * rowH + 6, leftW - 32, 34);

            int rightX = pad + leftW + 16;
            int rightW = W - rightX - pad;
            int statH = 104;
            int statW = (rightW - gap * 2) / 3;
            _statWs.SetBounds(rightX, top, statW, statH);
            _statCommit.SetBounds(rightX + statW + gap, top, statW, statH);
            _statHead.SetBounds(rightX + (statW + gap) * 2, top, statW, statH);

            int chartY = top + statH + gap;
            int analysisH = Math.Max(190, avail - statH - gap - 270 - gap);
            int chartH = avail - statH - gap - analysisH - gap;
            if (chartH < 170) { chartH = 170; analysisH = avail - statH - gap - chartH - gap; }
            _cChart.SetBounds(rightX, chartY, rightW, chartH);
            _chart.SetBounds(8, 28, rightW - 16, chartH - 34);

            int analysisY = chartY + chartH + gap;
            _cAnalysis.SetBounds(rightX, analysisY, rightW, Math.Max(140, bottom - analysisY));

            _btnScan.SetBounds(rightW - 190, 6, 92, 26);
            _btnSave.SetBounds(rightW - 92, 6, 84, 26);
            _txtReport.SetBounds(14, 40, rightW - 28, _cAnalysis.Height - 52);

            _status.SetBounds(0, bottom, W, statusH);
            _lMsg.TextAlign = ContentAlignment.MiddleLeft;

            int rx = W - 18;
            int lvW = 34;
            _lVer.TextAlign = ContentAlignment.MiddleRight;
            if (_admin)
            {
                _btnAdmin.Visible = false;
                _lVer.SetBounds(rx - lvW, 12, lvW, 24);
                _chkBoot.SetBounds(rx - lvW - 14 - 96, 12, 96, 24);
                _chkAuto.SetBounds(rx - lvW - 14 - 96 - 10 - 96, 12, 96, 24);
            }
            else
            {
                _btnAdmin.Visible = true;
                _btnAdmin.SetBounds(rx - 140, 9, 140, 28);
                _lVer.SetBounds(rx - 140 - 12 - lvW, 12, lvW, 24);
                _chkBoot.SetBounds(rx - 140 - 12 - lvW - 14 - 96, 12, 96, 24);
                _chkAuto.SetBounds(rx - 140 - 12 - lvW - 14 - 96 - 10 - 96, 12, 96, 24);
            }
            _lMsg.SetBounds(pad, 0, Math.Max(200, _chkAuto.Left - pad - 20), statusH);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_header != null) LayoutAll();
            Invalidate(true);
        }

        // --------------------------------------------------------------
        // 上限显示
        // --------------------------------------------------------------
        private void UpdateLimitText(bool userChanged)
        {
            long b = _slider.ValueBytes;
            _lLimitVal.Text = Native.Fmt(b);
            if (userChanged) _cfg.LimitMb = b >> 20;
            for (int i = 0; i < _chips.Length; i++)
            {
                bool on = Math.Abs((ChipMb[i] << 20) - b) < (64L << 20);
                _chips[i].Back = on ? Color.FromArgb(40, 58, 96) : Color.FromArgb(16, 20, 28);
                _chips[i].Fore = on ? Ui.Text : Ui.Sub;
                _chips[i].BorderColor = on ? Color.FromArgb(120, Ui.Accent) : Ui.Border;
                _chips[i].Invalidate();
            }
            if (userChanged)
            {
                _cfg.Save();
                if (_applied) ApplyNow(true);
            }
            UpdateWarn();
        }

        private void UpdateWarn()
        {
            long lim = _slider.ValueBytes;
            long ws = _hist.Count > 0 ? _hist[_hist.Count - 1].WorkingSet : 0;
            if (_applied && ws > lim)
                _lLimitWarn.Text = "⚠ 当前常驻 " + Native.Fmt(ws) + " 已超过上限，系统正在回收页面，画面可能卡一下。";
            else if (_mode == 2)
                _lLimitWarn.Text = "⚠ 硬上限模式下，游戏一旦超出就会直接闪退，建议用在«限制摸鱼»而不是日常。";
            else if (lim < (768L << 20))
                _lLimitWarn.Text = "⚠ 上限低于 768 MB 时游戏很可能卡死或崩溃。";
            else
                _lLimitWarn.Text = "";
        }

        private void InputLimit()
        {
            string s = Dlg.Input(this, "直接输入上限", "单位 MB，范围 256 - 32768。例如 4096 = 4 GB。", (_slider.ValueBytes >> 20).ToString());
            if (s == null) return;
            s = s.Trim().ToUpperInvariant().Replace("GB", "").Replace("G", "").Trim();
            double mb;
            if (!double.TryParse(s, out mb) || mb <= 0) { Dlg.Info(this, "输入无效", "请输入一个数字，例如 4096。"); return; }
            if (s.Length <= 3 && mb < 64) mb *= 1024;
            _slider.SetValue((long)(mb) << 20, true);
        }

        private long DefaultLimitMb()
        {
            Native.MEMORYSTATUSEX ms = new Native.MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(typeof(Native.MEMORYSTATUSEX));
            long totalMb = 8192;
            if (Native.GlobalMemoryStatusEx(ref ms) && ms.ullTotalPhys > 0)
                totalMb = (long)(ms.ullTotalPhys >> 20);
            long def = totalMb / 4;
            def = Math.Max(2048, Math.Min(8192, def));
            return (def / 128) * 128;
        }

        // --------------------------------------------------------------
        // 方式
        // --------------------------------------------------------------
        private void SetMode(int m)
        {
            _mode = m;
            for (int i = 0; i < _rows.Length; i++) { _rows[i].Selected = (i == m); _rows[i].Invalidate(); }
            UpdateModeText();
            _cfg.Mode = m;
            _cfg.Save();
            if (_applied) ApplyNow(true);
        }

        private void UpdateModeText()
        {
            if (_lModeFoot == null) return;
            if (_mode == 0) _lModeFoot.Text = "最稳：超过上限的页面被换到硬盘，游戏照常运行。";
            else if (_mode == 1) _lModeFoot.Text = "更省：每分钟把闲置页面清出去，内存更低但偶尔掉帧。";
            else _lModeFoot.Text = "最狠：超出即终止进程，只适合强制自律。";
            UpdateWarn();
        }

        // --------------------------------------------------------------
        // 主循环
        // --------------------------------------------------------------
        private void Tick(object sender, EventArgs e)
        {
            try
            {
                EnsureTarget();
                if (_g.Pid != 0 && _g.Alive)
                {
                    ProcSample s = _g.Sample();
                    if (!string.IsNullOrEmpty(s.Error)) { SetMsg(s.Error, Ui.Red); return; }
                    _hist.Add(s);
                    if (_hist.Count > 150) _hist.RemoveAt(0);
                    _chart.Data = _hist;
                    _chart.CapBytes = _applied ? _slider.ValueBytes : 0;
                    _chart.Invalidate();
                    UpdateStats(s);

                    if (_mode == 1 && _applied && (DateTime.Now - _lastTrim).TotalSeconds >= 20)
                    {
                        string err;
                        _g.Trim(out err);
                        _lastTrim = DateTime.Now;
                    }
                }
                else if (_g.Pid != 0)
                {
                    _g.Detach();
                    _g.Pid = 0;
                    _applied = false;
                    _hist.Clear();
                    _chart.Data = _hist;
                    _chart.CapBytes = 0;
                    _chart.Invalidate();
                    UpdateNoTarget();
                    SetMsg("游戏已退出，等待重新启动…", Ui.Amber);
                }
            }
            catch (Exception ex)
            {
                SetMsg("采样失败：" + ex.Message, Ui.Red);
            }
        }

        private void EnsureTarget()
        {
            if (_g.Pid != 0 && _g.Alive) return;

            if (_g.Pid != 0)
            {
                _g.Detach();
                _g.Pid = 0;
                _applied = false;
            }

            int pid = _pickedPid;
            if (pid != 0)
            {
                bool ok = false;
                try { Process p = Process.GetProcessById(pid); ok = !p.HasExited; } catch { ok = false; }
                if (!ok) { pid = 0; _pickedPid = 0; }
            }
            if (pid == 0) pid = Procs.FindGamePid();
            if (pid == 0) { UpdateNoTarget(); return; }
            if (!_g.Attach(pid)) { UpdateNoTarget(); SetMsg(_g.Error, Ui.Red); return; }

            OnAttached();
        }

        private void OnAttached()
        {
            _hist.Clear();
            ProcEntry e = Procs.ReadEntry(_g.Pid, _g.Name);
            string nm = e.Name;
            if (string.IsNullOrEmpty(nm)) nm = _g.Name;
            if (string.IsNullOrEmpty(nm)) nm = "PID " + _g.Pid;
            _lProcName.Text = nm;

            string kind = Procs.KindOf(e);
            string line1 = "PID " + _g.Pid + " · " +
                           (kind == "游戏" ? "Unity 游戏进程" : (kind == "助手" ? "助手进程" : "普通进程")) +
                           (e.InJob ? " · 已在作业中" : "");
            _lProcMeta.Text = line1 + "\r\n" + ShortPath(e.Path);
            _lProcMeta.ForeColor = Ui.Dim;

            if (_g.ReadOnly)
            {
                SetMsg(_g.Error, Ui.Amber);
                _btnApply.Text = "以管理员重试";
                _btnApply.Back = Ui.Amber;
            }
            else
            {
                _btnApply.Text = "应用上限";
                _btnApply.Back = Ui.Accent;
            }

            SetChip(true, "已接管 " + e.Name);
            SetMsg("已接管 " + e.Name + "（PID " + _g.Pid + "）", Ui.Green);

            if (_cfg.AutoApply && _cfg.LimitMb > 0)
            {
                long lim = _cfg.LimitMb << 20;
                _slider.SetValue(lim, false);
                ApplyNow(true);
            }
        }

        private void UpdateNoTarget()
        {
            _lProcName.Text = "未检测到游戏";
            _lProcMeta.Text = "请先启动 Deskrawl，或点右上角「切换」手动选择进程";
            _lProcMeta.ForeColor = Ui.Dim;
            SetChip(false, "未连接");
            _statWs.Value = "—"; _statWs.Sub = "等待进程"; _statWs.Ratio = 0; _statWs.Invalidate();
            _statCommit.Value = "—"; _statCommit.Sub = ""; _statCommit.Ratio = 0; _statCommit.Invalidate();
            _statHead.Value = "—"; _statHead.Sub = ""; _statHead.Ratio = 0; _statHead.Danger = false; _statHead.Invalidate();
            _lLimitCur.Text = "";
            UpdateWarn();
        }

        private void UpdateStats(ProcSample s)
        {
            _statWs.Value = Native.Fmt(s.WorkingSet);
            _statWs.Sub = "峰值 " + Native.Fmt(s.PeakWorkingSet) + " · 线程 " + s.Threads;
            Native.MEMORYSTATUSEX ms = new Native.MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(typeof(Native.MEMORYSTATUSEX));
            long totalMb = Native.GlobalMemoryStatusEx(ref ms) ? (long)(ms.ullTotalPhys >> 20) : 0;
            _statWs.Ratio = totalMb > 0 ? s.WorkingSet / (double)(totalMb << 20) : 0;
            _statWs.Invalidate();

            _statCommit.Value = Native.Fmt(s.Commit);
            _statCommit.Sub = "私有 " + Native.Fmt(s.Private) + " · 句柄 " + s.Handles;
            _statCommit.Ratio = totalMb > 0 ? s.Commit / (double)(totalMb << 20) : 0;
            _statCommit.Invalidate();

            long lim = _slider.ValueBytes;
            if (_applied)
            {
                long left = lim - s.WorkingSet;
                if (left < 0)
                {
                    _statHead.Value = Native.Fmt(-left);
                    _statHead.Sub = "已超出上限";
                    _statHead.Title = "超出上限";
                    _statHead.Danger = true;
                }
                else if (left < (32L << 20))
                {
                    _statHead.Value = "已顶满";
                    _statHead.Sub = "上限 " + Native.Fmt(lim);
                    _statHead.Title = "距上限余量";
                    _statHead.Danger = false;
                }
                else
                {
                    _statHead.Value = Native.Fmt(left);
                    _statHead.Sub = "上限 " + Native.Fmt(lim);
                    _statHead.Title = "距上限余量";
                    _statHead.Danger = false;
                }
                _statHead.Ratio = Math.Min(1, s.WorkingSet / (double)Math.Max(1, lim));
            }
            else
            {
                _statHead.Value = "未启用";
                _statHead.Sub = "点击「应用上限」开始管控";
                _statHead.Danger = false;
                _statHead.Ratio = 0;
                _statHead.Title = "距上限余量";
            }
            _statHead.Invalidate();

            _lLimitCur.Text = "当前 " + Native.Fmt(s.WorkingSet);
            UpdateWarn();
        }

        private void SetChip(bool on, string text)
        {
            _chipDot.BackColor = on ? Ui.Green : Ui.Dim;
            _chipDot.Invalidate();
            _lblChip.Text = text;
            _lblChip.ForeColor = on ? Color.FromArgb(150, 220, 190) : Ui.Sub;
        }

        private void SetMsg(string msg, Color c)
        {
            _lMsg.Text = DateTime.Now.ToString("HH:mm:ss") + "   " + msg;
            _lMsg.ForeColor = c;
        }

        private string ShortPath(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            string root = p.Length > 2 && p[1] == ':' ? p.Substring(0, 2) : "";
            string[] parts = p.Split('\\');
            string tail = parts[parts.Length - 1];
            if (parts.Length >= 2) tail = parts[parts.Length - 2] + "\\" + tail;
            string s = root + "…\\" + tail;
            if (s.Length > 44) s = "…" + s.Substring(s.Length - 43);
            return s;
        }

        // --------------------------------------------------------------
        // 操作
        // --------------------------------------------------------------
        private void ApplyNow() { ApplyNow(false); }

        private void ApplyNow(bool silent)
        {
            if (_g.Pid == 0 || !_g.Alive) EnsureTarget();
            if (_g.Pid == 0) { if (!silent) Dlg.Info(this, "没有可管控的进程", "还没有连接进程。请先启动 Deskrawl，或点「切换」手动选择。"); return; }

            if (_mode == 2 && !silent)
            {
                if (!Dlg.Confirm(this, "确定使用硬上限？",
                        "这种方式会在提交内存超过上限时由系统直接结束游戏进程，\r\n进度可能丢失。\r\n\r\n先确认你知道后果再继续。",
                        "我确定，执行", true)) return;
            }

            long lim = _slider.ValueBytes;
            string err;
            bool ok;
            if (_mode == 2) ok = _g.ApplyCommitCap(lim, out err);
            else ok = _g.ApplyWorkingSet(lim, out err);

            if (!ok)
            {
                SetMsg("设置失败：" + err, Ui.Red);
                if (!silent) Dlg.Info(this, "设置失败", err + (_admin ? "" : "\r\n\r\n提示：普通权限通常改不了别的进程的内存配额，请点右下角「以管理员身份重启」。"));
                return;
            }

            _applied = true;
            _cfg.LimitMb = lim >> 20;
            _cfg.Mode = _mode;
            _cfg.Save();
            _chart.CapBytes = lim;
            if (_mode == 1) { _g.Trim(out err); _lastTrim = DateTime.Now; }
            SetChip(true, "管控中 · " + Native.Fmt(lim));
            SetMsg("已把上限设为 " + Native.Fmt(lim) + "（" + ModeName() + "）", Ui.Green);
        }

        private string ModeName()
        {
            if (_mode == 0) return "工作集上限";
            if (_mode == 1) return "上限+定时回收";
            return "提交硬上限";
        }

        private void ReleaseNow()
        {
            if (_g.Pid == 0 && !_g.Alive) { EnsureTarget(); }
            if (_g.Pid == 0) return;
            string err;
            _g.ReleaseCommitCap();
            bool ok = _g.ReleaseWorkingSet(0, out err);
            _applied = false;
            _chart.CapBytes = 0;
            SetChip(true, "已解除限制");
            SetMsg(ok ? "已解除限制，游戏可以自由使用内存了" : "解除失败：" + err, ok ? Ui.Sub : Ui.Red);
        }

        private void TrimNow(bool talk)
        {
            if (_g.Pid == 0) { EnsureTarget(); if (_g.Pid == 0) return; }
            ProcSample before = null;
            if (_hist.Count > 0) before = _hist[_hist.Count - 1];
            string err;
            if (_g.Trim(out err))
            {
                string extra = "";
                if (before != null)
                {
                    _g.Sample();
                    ProcSample after = _g.Sample();
                    long freed = before.WorkingSet - after.WorkingSet;
                    if (freed > 0) extra = "，回收 " + Native.Fmt(freed);
                }
                SetMsg("已回收闲置内存" + extra, Ui.Green);
            }
            else SetMsg("回收失败：" + err, Ui.Red);
        }

        // --------------------------------------------------------------
        // 体检
        // --------------------------------------------------------------
        private void RunScan()
        {
            if (_g.Pid == 0) { EnsureTarget(); if (_g.Pid == 0) return; }
            int pid = _g.Pid;
            _btnScan.Text = "分析中…";
            _btnScan.Enabled = false;
            _txtReport.Text = "正在读取进程内存布局…\r\n\r\n稍等，通常 1 秒内完成。";

            ThreadPool.QueueUserWorkItem(delegate(object st)
            {
                Snapshot snap = null;
                string err = null;
                try { snap = Inspector.Run(pid); }
                catch (Exception ex) { err = ex.Message; }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _btnScan.Text = "分析原因";
                        _btnScan.Enabled = true;
                        if (err != null || snap == null || snap.Error != null)
                        {
                            _txtReport.Text = "分析失败：" + (err ?? (snap != null ? snap.Error : "未知错误")) +
                                              "\r\n\r\n如果是权限问题，请以管理员身份重启再试。";
                            return;
                        }
                        _report = snap.ToReport();
                        _txtReport.Text = _report;
                        _txtReport.SelectionStart = 0;
                        _txtReport.ScrollToCaret();

                        long suggest = snap.SuggestLimit();
                        long ws = snap.WorkingSet;
                        _lLimitVal.Text = Native.Fmt(_slider.ValueBytes);
                        if (!_applied)
                        {
                            _slider.SetValue(suggest, true);
                            SetMsg("体检完成：建议上限 " + Native.Fmt(suggest) + "（当前常驻 " + Native.Fmt(ws) + "）", Ui.Green);
                        }
                        else SetMsg("体检完成，报告已更新", Ui.Green);
                    });
                }
                catch { }
            });
        }

        private void SaveReport()
        {
            if (string.IsNullOrEmpty(_report))
            {
                Dlg.Info(this, "还没有报告", "先点「分析原因」生成一份体检报告，再保存。");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            sfd.FileName = "Deskrawl内存报告-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".txt";
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                File.WriteAllText(sfd.FileName, _report, new UTF8Encoding(true));
                SetMsg("报告已保存：" + sfd.FileName, Ui.Green);
            }
            catch (Exception ex) { Dlg.Info(this, "保存失败", ex.Message); }
        }

        // --------------------------------------------------------------
        // 选择进程
        // --------------------------------------------------------------
        private void PickProcess()
        {
            using (ProcessPicker f = new ProcessPicker())
            {
                if (f.ShowDialog(this) == DialogResult.OK && f.Selected != null)
                {
                    _pickedPid = f.Selected.Pid;
                    _g.Detach();
                    _g.Pid = 0;
                    _applied = false;
                    EnsureTarget();
                }
            }
        }

        // --------------------------------------------------------------
        // 管理员
        // --------------------------------------------------------------
        private static bool IsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Close();
            }
            catch (Exception ex) { Dlg.Info(this, "无法提权", ex.Message); }
        }

        // --------------------------------------------------------------
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _tick.Stop();
                if (_cfg.AutoApply) _cfg.Save();
            }
            catch { }
            base.OnFormClosing(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Ui.Bg)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        private static Icon MakeIcon()
        {
            Bitmap bmp = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(2, 2, 60, 60);
                using (GraphicsPath p = Ui.Round(r, 16))
                using (LinearGradientBrush b = new LinearGradientBrush(new Point(0, 0), new Point(64, 64),
                           Color.FromArgb(110, 160, 255), Color.FromArgb(120, 90, 235)))
                    g.FillPath(b, p);
                using (SolidBrush w = new SolidBrush(Color.White))
                {
                    g.FillRectangle(w, 16, 22, 8, 22);
                    g.FillRectangle(w, 28, 16, 8, 28);
                    g.FillRectangle(w, 40, 26, 8, 18);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    // ==================================================================
    // 进程选择器
    // ==================================================================
    public class ProcessPicker : Form
    {
        public ProcEntry Selected;
        private ListBox _list;
        private List<ProcEntry> _items = new List<ProcEntry>();
        private CheckBox _chkAll;

        public ProcessPicker()
        {
            Text = "选择要管控的进程";
            BackColor = Ui.Bg;
            ForeColor = Ui.Text;
            Font = Ui.F(9f);
            ClientSize = new Size(940, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label t = new Label();
            t.Text = "选择进程";
            t.Font = Ui.F(12f, FontStyle.Bold);
            t.ForeColor = Ui.Text;
            t.BackColor = Color.Transparent;
            t.SetBounds(18, 14, 300, 26);
            Controls.Add(t);

            Label s = new Label();
            s.Text = "默认只显示 Deskrawl 相关进程；勾选下面的选项可以看到系统里的全部进程。";
            s.Font = Ui.F(8.5f);
            s.ForeColor = Ui.Sub;
            s.BackColor = Color.Transparent;
            s.SetBounds(18, 40, 800, 20);
            Controls.Add(s);

            _list = new ListBox();
            _list.DrawMode = DrawMode.OwnerDrawFixed;
            _list.ItemHeight = 26;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = Color.FromArgb(15, 19, 27);
            _list.ForeColor = Ui.Text;
            _list.Font = Ui.F(9f);
            _list.SetBounds(18, 68, 904, 372);
            _list.DrawItem += DrawItem;
            _list.DoubleClick += delegate { Accept(); };
            Controls.Add(_list);

            _chkAll = new CheckBox();
            _chkAll.Text = "显示全部进程（含不相干的）";
            _chkAll.Font = Ui.F(8.5f);
            _chkAll.ForeColor = Ui.Sub;
            _chkAll.BackColor = Color.Transparent;
            _chkAll.FlatStyle = FlatStyle.Flat;
            _chkAll.SetBounds(18, 452, 260, 24);
            _chkAll.CheckedChanged += delegate { Reload(); };
            Controls.Add(_chkAll);

            Btn ok = new Btn();
            ok.Text = "选择";
            ok.Back = Ui.Accent;
            ok.Fore = Color.White;
            ok.SetBounds(742, 448, 180, 42);
            ok.Click += delegate { Accept(); };
            Controls.Add(ok);

            Btn cancel = new Btn();
            cancel.Text = "取消";
            cancel.Outline = true;
            cancel.Fore = Ui.Sub;
            cancel.BorderColor = Ui.Border;
            cancel.SetBounds(652, 448, 80, 42);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);

            Reload();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Ui.Bg)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        private void Reload()
        {
            _items = Procs.List(_chkAll.Checked);
            _list.Items.Clear();
            foreach (ProcEntry e in _items)
            {
                if (!_chkAll.Checked && e.WorkingSet < (20L << 20)) continue;
                _list.Items.Add(e.Kind + "|" + e.Name);
            }
        }

        private void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _list.Items.Count) return;
            Graphics g = e.Graphics;
            string key = (string)_list.Items[e.Index];
            ProcEntry en = null;
            int slash = key.IndexOf('|');
            string kind = key.Substring(0, slash);
            string name = key.Substring(slash + 1);
            foreach (ProcEntry p in _items)
                if (p.Kind == kind && p.Name == name) { en = p; break; }
            if (en == null) return;

            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(34, 48, 78) : (e.Index % 2 == 0 ? Color.FromArgb(15, 19, 27) : Color.FromArgb(17, 22, 31))))
                g.FillRectangle(b, e.Bounds);

            Color kc = en.Kind == "游戏" ? Ui.Accent : (en.Kind == "助手" ? Ui.Green : Ui.Dim);
            TextRenderer.DrawText(g, en.Pid.ToString(), Ui.F(8.5f), new Rectangle(e.Bounds.X + 8, e.Bounds.Y, 64, e.Bounds.Height), Ui.Sub, Ui.LeftMid);
            TextRenderer.DrawText(g, en.Kind ?? "", Ui.F(8.5f), new Rectangle(e.Bounds.X + 72, e.Bounds.Y, 48, e.Bounds.Height), kc, Ui.LeftMid);
            TextRenderer.DrawText(g, en.Name ?? "", Ui.F(9f), new Rectangle(e.Bounds.X + 124, e.Bounds.Y, 210, e.Bounds.Height), Ui.Text, Ui.LeftMid);
            TextRenderer.DrawText(g, Native.Fmt(en.WorkingSet), Ui.F(8.5f), new Rectangle(e.Bounds.X + 340, e.Bounds.Y, 90, e.Bounds.Height), Ui.Sub, Ui.RightMid);
            TextRenderer.DrawText(g, en.Path ?? "", Ui.F(8f), new Rectangle(e.Bounds.X + 442, e.Bounds.Y, e.Bounds.Width - 452, e.Bounds.Height), Ui.Dim, Ui.LeftMid);
        }

        private void Accept()
        {
            int i = _list.SelectedIndex;
            if (i < 0) return;
            string key = (string)_list.Items[i];
            int slash = key.IndexOf('|');
            string kind = key.Substring(0, slash);
            string name = key.Substring(slash + 1);
            foreach (ProcEntry p in _items)
                if (p.Kind == kind && p.Name == name) { Selected = p; break; }
            if (Selected != null) DialogResult = DialogResult.OK;
        }
    }

    // ==================================================================
    // 配置
    // ==================================================================
    public class Config
    {
        public int Mode = 0;
        public long LimitMb = 0;
        public bool AutoApply = true;

        private static string FilePath()
        {
            string p = Path.Combine(Application.StartupPath, "deskrawl-mem.conf");
            try
            {
                string probe = p + ".probe";
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return p;
            }
            catch { }
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskrawlMem");
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, "config.conf");
        }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                string p = FilePath();
                if (!File.Exists(p)) return c;
                foreach (string line in File.ReadAllLines(p))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    if (k == "mode") { int m; if (int.TryParse(v, out m)) c.Mode = m; }
                    else if (k == "limit") { long l; if (long.TryParse(v, out l)) c.LimitMb = l; }
                    else if (k == "autoapply") c.AutoApply = (v == "1");
                }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Deskrawl 内存管家配置");
                sb.AppendLine("mode=" + Mode);
                sb.AppendLine("limit=" + LimitMb);
                sb.AppendLine("autoapply=" + (AutoApply ? "1" : "0"));
                File.WriteAllText(FilePath(), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static bool AutoStartEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                    return k != null && k.GetValue("DeskrawlMemGovernor") != null;
            }
            catch { return false; }
        }

        public static void SetAutoStart(bool on)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (on) k.SetValue("DeskrawlMemGovernor", "\"" + Application.ExecutablePath + "\" --watch");
                else k.DeleteValue("DeskrawlMemGovernor", false);
            }
        }
    }
}
