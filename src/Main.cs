using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("洛克王国页游悟空神辅脚本弹窗修复工具")]
[assembly: System.Reflection.AssemblyProduct("Wukong Popup Fix")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0")]

namespace WukongPopupFix
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    /// <summary>圆圈问号：鼠标移上去立即显示提示，移开立即隐藏。</summary>
    internal sealed class HelpTip : Control
    {
        private readonly ToolTip _tip;
        private bool _hot;

        public HelpTip(string text)
        {
            Cursor = Cursors.Hand;
            TabStop = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            _tip = new ToolTip();
            _tip.InitialDelay = 0;
            _tip.ReshowDelay = 0;
            _tip.AutoPopDelay = 60000;
            _tip.ShowAlways = true;

            MouseEnter += delegate { _hot = true; Invalidate(); _tip.Show(text, this, Width + 4, -4, 60000); };
            MouseLeave += delegate { _hot = false; Invalidate(); _tip.Hide(this); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (_hot)
            {
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(226, 241, 252)))
                    e.Graphics.FillEllipse(fill, 0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            }
            Color ring = _hot ? Color.FromArgb(52, 152, 219) : Color.FromArgb(130, 130, 130);
            float penWidth = _hot ? 1.6f : 1f;
            using (Pen pen = new Pen(ring, penWidth))
            {
                e.Graphics.DrawEllipse(pen, 0.5f, 0.5f, Width - 2f, Height - 2f);
            }
            using (SolidBrush brush = new SolidBrush(_hot ? Color.FromArgb(23, 96, 150) : Color.FromArgb(90, 90, 90)))
            using (Font font = new Font("Segoe UI", Height * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString("?", font, brush, new RectangleF(0, 0, Width, Height), sf);
            }
        }
    }

    /// <summary>凸起按钮：上白下灰渐变、深色描边、右下阴影，按下时反转变凹。</summary>
    internal sealed class RaisedButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public RaisedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        private static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            const int radius = 2;
            Rectangle shadow = new Rectangle(1, 1, Width - 1, Height - 1);
            Rectangle body = _pressed
                ? new Rectangle(1, 1, Width - 2, Height - 2)
                : new Rectangle(0, 0, Width - 2, Height - 2);

            using (GraphicsPath sp = Round(Rectangle.Inflate(shadow, -1, -1), radius))
            using (SolidBrush sb = new SolidBrush(Enabled ? Color.FromArgb(150, 150, 150) : Color.FromArgb(190, 190, 190)))
                g.FillPath(sb, sp);

            Color top, bottom;
            if (!Enabled) { top = Color.FromArgb(245, 245, 245); bottom = Color.FromArgb(232, 232, 232); }
            else if (_pressed) { top = Color.FromArgb(216, 232, 245); bottom = Color.FromArgb(237, 246, 253); }
            else if (_hover) { top = Color.FromArgb(240, 249, 255); bottom = Color.FromArgb(198, 226, 246); }
            else { top = Color.FromArgb(255, 255, 255); bottom = Color.FromArgb(228, 241, 251); }

            Rectangle inner = Rectangle.Inflate(body, -1, -1);
            using (GraphicsPath bp = Round(inner, radius))
            using (LinearGradientBrush lb = new LinearGradientBrush(
                new Rectangle(inner.X, inner.Y, Math.Max(1, inner.Width), Math.Max(1, inner.Height + 1)),
                top, bottom, LinearGradientMode.Vertical))
                g.FillPath(lb, bp);

            Color border = _pressed ? Color.FromArgb(120, 150, 180)
                : (_hover ? Color.FromArgb(52, 152, 219) : Color.FromArgb(156, 176, 204));
            using (GraphicsPath dp = Round(body, radius))
            using (Pen pen = new Pen(border))
                g.DrawPath(pen, dp);

            if (!_pressed && Enabled)
            {
                using (Pen hl = new Pen(Color.FromArgb(255, 255, 255)))
                    g.DrawLine(hl, inner.X + 1, inner.Y + 2, inner.Right - 2, inner.Y + 2);
            }

            Color fore = !Enabled ? Color.FromArgb(150, 150, 150)
                : (_hover ? Color.FromArgb(23, 96, 150) : Color.FromArgb(36, 44, 53));
            Rectangle tr = _pressed ? new Rectangle(1, 1, Width - 2, Height - 2) : new Rectangle(0, 0, Width - 2, Height - 2);
            TextRenderer.DrawText(g, Text, Font, tr, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>自绘标题栏：右侧最小化/关闭凸起按钮，其余区域可拖动窗口。</summary>
    internal sealed class TitleBar : Control
    {
        private readonly string _title;
        private readonly float _k;
        private int _hot = -1;
        private bool _down;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        public TitleBar(string title, float k)
        {
            _title = title;
            _k = k;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }

        private int S(int v)
        {
            return (int)Math.Round(v * _k);
        }

        private Rectangle MinRect
        {
            get { return new Rectangle(Width - S(86), S(6), S(34), S(22)); }
        }

        private Rectangle CloseRect
        {
            get { return new Rectangle(Width - S(46), S(6), S(34), S(22)); }
        }

        private int HitIndex(Point p)
        {
            if (CloseRect.Contains(p)) return 1;
            if (MinRect.Contains(p)) return 0;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = HitIndex(e.Location);
            if (idx != _hot) { _hot = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = -1; _down = false; Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int idx = HitIndex(e.Location);
                if (idx >= 0)
                {
                    _down = true;
                    Invalidate();
                }
                else
                {
                    Form f = FindForm();
                    if (f != null)
                    {
                        ReleaseCapture();
                        SendMessage(f.Handle, 0x00A1, (IntPtr)2, IntPtr.Zero); // WM_NCLBUTTONDOWN / HTCAPTION
                    }
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_down && e.Button == MouseButtons.Left)
            {
                int idx = HitIndex(e.Location);
                _down = false;
                Invalidate();
                Form f = FindForm();
                if (f != null)
                {
                    if (idx == 1) f.Close();
                    else if (idx == 0) f.WindowState = FormWindowState.Minimized;
                }
            }
            base.OnMouseUp(e);
        }

        private static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void DrawCap(Graphics g, Rectangle r, bool isClose, bool hot, bool down)
        {
            Color top, bottom, border;
            bool redHot = isClose && (hot || down);
            if (redHot)
            {
                top = down ? Color.FromArgb(196, 55, 42) : Color.FromArgb(240, 104, 88);
                bottom = down ? Color.FromArgb(224, 92, 78) : Color.FromArgb(214, 74, 58);
                border = Color.FromArgb(180, 46, 32);
            }
            else if (down) { top = Color.FromArgb(191, 222, 245); bottom = Color.FromArgb(224, 241, 253); border = Color.FromArgb(120, 158, 190); }
            else if (hot) { top = Color.FromArgb(235, 246, 254); bottom = Color.FromArgb(193, 224, 246); border = Color.FromArgb(52, 152, 219); }
            else { top = Color.FromArgb(253, 254, 255); bottom = Color.FromArgb(221, 237, 249); border = Color.FromArgb(169, 197, 223); }

            using (GraphicsPath p = Round(r, S(3)))
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height + 1)), top, bottom, LinearGradientMode.Vertical))
                g.FillPath(b, p);

            using (GraphicsPath p = Round(r, S(3)))
            using (Pen pen = new Pen(border))
                g.DrawPath(pen, p);

            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;
            int half = S(3);
            Color glyph = redHot ? Color.White : (hot ? Color.FromArgb(43, 122, 178) : Color.FromArgb(132, 181, 214));
            using (Pen shadow = new Pen(redHot ? Color.FromArgb(110, 120, 30, 20) : (hot ? Color.FromArgb(190, 255, 255, 255) : Color.FromArgb(238, 248, 255)), 1.3f))
            using (Pen main = new Pen(glyph, 1.3f))
            {
                main.StartCap = LineCap.Round; main.EndCap = LineCap.Round;
                shadow.StartCap = LineCap.Round; shadow.EndCap = LineCap.Round;
                if (isClose)
                {
                    g.DrawLine(shadow, cx - half + 1, cy - half + 1, cx + half + 1, cy + half + 1);
                    g.DrawLine(shadow, cx + half + 1, cy - half + 1, cx - half + 1, cy + half + 1);
                    g.DrawLine(main, cx - half, cy - half, cx + half, cy + half);
                    g.DrawLine(main, cx + half, cy - half, cx - half, cy + half);
                }
                else
                {
                    g.DrawLine(shadow, cx - half + 1, cy + 1, cx + half + 1, cy + 1);
                    g.DrawLine(main, cx - half, cy, cx + half, cy);
                }
            }

            // 顶部一条柔和高光，增强凸起质感（红色状态减弱，避免出现白线）。
            using (Pen hl = new Pen(Color.FromArgb(redHot ? 55 : 170, 255, 255, 255)))
                g.DrawLine(hl, r.X + S(4), r.Y + 2, r.Right - S(4), r.Y + 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Font f = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold))
                TextRenderer.DrawText(g, _title, f,
                    new Rectangle(S(14), 0, Width - S(120), Height),
                    Color.FromArgb(29, 92, 138),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            DrawCap(g, MinRect, false, _hot == 0, _down && _hot == 0);
            DrawCap(g, CloseRect, true, _hot == 1, _down && _hot == 1);

            using (Pen pen = new Pen(Color.FromArgb(196, 219, 236)))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }

    internal sealed class MainForm : Form
    {
        private const string RegPath = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
        private const int Ie11Mode = 11001;
        private const string AppVersion = "1.0.0";
        private const int TitleHeight = 36;
        private const int WS_THICKFRAME = 0x00040000;

        // Win11 窗口圆角控制；Win10 上调用直接失败，忽略即可。
        private const int DwmAttrCornerPreference = 33;
        private const int DwmCornerRoundSmall = 3;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private static readonly Color AccentColor = Color.FromArgb(30, 111, 175);
        private static readonly Color WarningColor = Color.FromArgb(192, 57, 43);

        private static string _dataDir;

        private readonly float _k;
        private readonly TitleBar _titleBar;
        private readonly TextBox _txtDir = new TextBox();
        private readonly Label _lblStatus = new Label();
        private readonly CheckBox _chkFix = new CheckBox();
        private readonly CheckBox _chkClean = new CheckBox();
        private readonly CheckBox _chkKeep = new CheckBox();
        private readonly CheckBox _chkSub = new CheckBox();
        private readonly TextBox _txtKeep = new TextBox();
        private readonly RaisedButton _btnBrowseKeep = new RaisedButton();
        private readonly RaisedButton _btnRun = new RaisedButton();
        private readonly RaisedButton _btnLog = new RaisedButton();
        private readonly RaisedButton _btnBrowseDir = new RaisedButton();
        private readonly TextBox _txtOut = new TextBox();

        public MainForm()
        {
            Text = "洛克王国页游悟空神辅脚本弹窗修复工具 v" + AppVersion;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
            AutoScaleMode = AutoScaleMode.None;

            _k = 1f;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    _k = g.DpiX / 96f;
                }
            }
            catch (Exception) { _k = 1f; }
            if (_k < 1f) _k = 1f;

            Font = MakeFont(9f, FontStyle.Regular);
            BackColor = Color.FromArgb(239, 246, 252);
            ClientSize = new Size(S(644), S(612 + TitleHeight));
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            int titleH = S(TitleHeight);
            foreach (Control c in Controls) c.Top += titleH;
            _titleBar = new TitleBar(Text, _k);
            _titleBar.SetBounds(0, 0, ClientSize.Width, titleH);
            _titleBar.Dock = DockStyle.Top;
            Controls.Add(_titleBar);
            LoadSettings();
            // 首次运行没有配置时，默认就用工具所在目录（工具通常与悟空放在一起）。
            if (_txtDir.Text.Trim().Length == 0)
                _txtDir.Text = Path.GetDirectoryName(Application.ExecutablePath);
            ApplyKeepState();
            RefreshStatus();
            UpdateRunState();
        }

        private int S(int v)
        {
            return (int)Math.Round(v * _k);
        }

        /// <summary>无边框窗口只有带 THICKFRAME 才能拿到 DWM 阴影和 Win11 圆角；实际缩放会在 WndProc 里挡掉。</summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_THICKFRAME;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                int hit = m.Result.ToInt32();
                // 10~17 是八个方向的拉伸热区，改成无动作的边框，避免拉坏固定布局。
                if (hit >= 10 && hit <= 17) m.Result = (IntPtr)18;
                return;
            }
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE)
                return;
            base.WndProc(ref m);
        }

        /// <summary>使用系统的小圆角（4px），既不像默认 8px 那样与方角图标冲突，又不至于太生硬。</summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (_titleBar != null) _titleBar.Height = S(TitleHeight);
            }
            catch (Exception) { }
            try
            {
                int pref = DwmCornerRoundSmall;
                DwmSetWindowAttribute(Handle, DwmAttrCornerPreference, ref pref, sizeof(int));
            }
            catch (Exception) { }
        }

        /// <summary>整窗自上而下的浅蓝渐变，无分隔线，顶部略深。</summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width <= 0 || Height <= 0) return;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Math.Max(1, Height)),
                Color.FromArgb(221, 235, 247), Color.FromArgb(250, 253, 255), LinearGradientMode.Vertical))
                e.Graphics.FillRectangle(b, 0, 0, Width, Height);
        }

        private Font MakeFont(float pt, FontStyle style)
        {
            // 字号用磅值即可，DPI 感知后系统会自动按物理 DPI 渲染；此处不能再乘缩放系数。
            return new Font("Microsoft YaHei UI", pt, style);
        }

        private static string DataDir
        {
            get
            {
                if (_dataDir != null) return _dataDir;
                // 配置与工具放在同一目录，方便随工具整体搬迁；目录不可写时保存失败会被忽略。
                _dataDir = Path.GetDirectoryName(Application.ExecutablePath);
                return _dataDir;
            }
        }

        private static string SettingsPath
        {
            get { return Path.Combine(DataDir, "settings.json"); }
        }

        private void BuildUi()
        {
            Label lblDir = new Label();
            lblDir.Text = "程序所在目录：";
            lblDir.BackColor = Color.Transparent;
            lblDir.SetBounds(S(16), S(18), S(120), S(22));
            Controls.Add(lblDir);

            HelpTip tipDir = new HelpTip("选择包含「悟空神辅 *.exe」的文件夹，例如 桌面\\洛克\\悟空。\r\n注意：这里选的是悟空所在目录，不是本工具所在目录。");
            tipDir.SetBounds(S(140), S(20), S(18), S(18));
            Controls.Add(tipDir);

            _txtDir.SetBounds(S(164), S(14), S(330), S(26));
            _txtDir.TextChanged += delegate { OnSettingChanged(); };
            Controls.Add(_txtDir);

            _btnBrowseDir.Text = "浏览文件夹";
            _btnBrowseDir.SetBounds(S(500), S(13), S(112), S(28));
            _btnBrowseDir.Click += BrowseDirectory;
            Controls.Add(_btnBrowseDir);

            Label lblHint = new Label();
            lblHint.Text = "请选择包含「悟空神辅 *.exe」的目录（是悟空目录，不是本工具目录）";
            lblHint.BackColor = Color.Transparent;
            lblHint.ForeColor = Color.FromArgb(95, 124, 147);
            lblHint.Font = MakeFont(8.5f, FontStyle.Regular);
            lblHint.SetBounds(S(16), S(48), S(612), S(20));
            Controls.Add(lblHint);

            _lblStatus.SetBounds(S(16), S(72), S(612), S(24));
            _lblStatus.BackColor = Color.Transparent;
            Controls.Add(_lblStatus);

            _chkFix.Text = "1. 一键修复（登记最新版）";
            _chkFix.BackColor = Color.Transparent;
            _chkFix.SetBounds(S(16), S(106), S(340), S(26));
            _chkFix.Checked = true;
            _chkFix.CheckedChanged += delegate { OnSettingChanged(); };
            Controls.Add(_chkFix);

            HelpTip tipFix = new HelpTip("自动扫描该目录，取版本号最高的 exe，在注册表登记 IE11 文档模式（11001），\r\n用于消除快捷登录时反复出现的「脚本错误」弹窗。");
            tipFix.SetBounds(S(360), S(110), S(18), S(18));
            Controls.Add(tipFix);

            _chkClean.Text = "2. 清理残留（删除失效项）";
            _chkClean.BackColor = Color.Transparent;
            _chkClean.SetBounds(S(16), S(140), S(340), S(26));
            _chkClean.Checked = true;
            _chkClean.CheckedChanged += delegate { OnSettingChanged(); };
            Controls.Add(_chkClean);

            HelpTip tipClean = new HelpTip("删除注册表中以「悟空」开头、且对应 exe 已不在所选目录、\r\n且未列入「保留旧版」的条目。不会影响其他程序。");
            tipClean.SetBounds(S(360), S(144), S(18), S(18));
            Controls.Add(tipClean);

            _chkKeep.Text = "3. 保留旧版";
            _chkKeep.BackColor = Color.Transparent;
            _chkKeep.SetBounds(S(16), S(174), S(110), S(26));
            _chkKeep.CheckedChanged += delegate { ApplyKeepState(); OnSettingChanged(); };
            Controls.Add(_chkKeep);

            HelpTip tipKeep = new HelpTip("勾选后填入或选择一个旧版 exe 文件名，该版本的注册表项不会被清理。\r\n适用于旧版本更好用的情况。");
            tipKeep.SetBounds(S(126), S(178), S(18), S(18));
            Controls.Add(tipKeep);

            _txtKeep.SetBounds(S(150), S(172), S(288), S(26));
            _txtKeep.TextChanged += delegate { OnSettingChanged(); };
            Controls.Add(_txtKeep);

            _btnBrowseKeep.Text = "浏览";
            _btnBrowseKeep.SetBounds(S(446), S(171), S(96), S(28));
            _btnBrowseKeep.Click += BrowseKeepFile;
            Controls.Add(_btnBrowseKeep);

            _chkSub.Text = "包含子目录";
            _chkSub.BackColor = Color.Transparent;
            _chkSub.SetBounds(S(16), S(208), S(110), S(26));
            _chkSub.Checked = true;
            _chkSub.CheckedChanged += delegate { OnSettingChanged(); };
            Controls.Add(_chkSub);

            HelpTip tipSub = new HelpTip("勾选后会同时扫描所选目录下的子文件夹。");
            tipSub.SetBounds(S(126), S(212), S(18), S(18));
            Controls.Add(tipSub);

            _btnRun.Text = "执 行";
            _btnRun.SetBounds(S(206), S(248), S(110), S(34));
            _btnRun.Click += delegate { Execute(); };
            Controls.Add(_btnRun);

            _btnLog.Text = "复制日志";
            _btnLog.SetBounds(S(336), S(248), S(110), S(34));
            _btnLog.Click += CopyLog;
            Controls.Add(_btnLog);

            _txtOut.SetBounds(S(16), S(296), S(612), S(240));
            _txtOut.Multiline = true;
            _txtOut.ReadOnly = true;
            _txtOut.ScrollBars = ScrollBars.Vertical;
            _txtOut.BorderStyle = BorderStyle.FixedSingle;
            _txtOut.BackColor = Color.White;
            _txtOut.Font = MakeFont(9f, FontStyle.Regular);
            Controls.Add(_txtOut);

            Label lblFoot = new Label();
            lblFoot.Text = "说明：仅修改当前用户注册表（HKCU），无需管理员权限；不会修改游戏或悟空程序文件。\r\n配置文件 settings.json 保存在本工具所在目录；日志仅在窗口内显示，关闭后不保留。\r\nv" + AppVersion + "　若首次运行出现「未知发布者」提示，点「仍要运行」即可。";
            lblFoot.BackColor = Color.Transparent;
            lblFoot.ForeColor = Color.FromArgb(95, 124, 147);
            lblFoot.Font = MakeFont(8.5f, FontStyle.Regular);
            lblFoot.SetBounds(S(16), S(544), S(612), S(60));
            Controls.Add(lblFoot);

            foreach (Control c in Controls)
            {
                if (c is CheckBox) c.Cursor = Cursors.Hand;
            }
        }

        private void BrowseDirectory(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "请选择包含「悟空神辅 *.exe」的文件夹";
                dlg.ShowNewFolderButton = false;
                if (Directory.Exists(_txtDir.Text.Trim())) dlg.SelectedPath = _txtDir.Text.Trim();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtDir.Text = dlg.SelectedPath;
                }
            }
        }

        private void BrowseKeepFile(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择要保留的旧版程序";
                dlg.Filter = "悟空程序 (*.exe)|*.exe|所有文件 (*.*)|*.*";
                if (Directory.Exists(_txtDir.Text.Trim())) dlg.InitialDirectory = _txtDir.Text.Trim();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtKeep.Text = Path.GetFileName(dlg.FileName);
                    _chkKeep.Checked = true;
                }
            }
        }

        private void CopyLog(object sender, EventArgs e)
        {
            if (_txtOut.TextLength == 0)
            {
                MessageBox.Show(this, "暂无日志，先执行一次即可生成。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Clipboard.SetText(_txtOut.Text);
                MessageBox.Show(this, "本次运行记录已复制到剪贴板。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "复制失败：" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyKeepState()
        {
            bool on = _chkKeep.Checked;
            _txtKeep.Enabled = on;
            _btnBrowseKeep.Enabled = on;
            if (!on) _txtKeep.Text = "";
        }

        private void OnSettingChanged()
        {
            ApplyKeepState();
            RefreshStatus();
            UpdateRunState();
            SaveSettings();
        }

        private void UpdateRunState()
        {
            bool hasAction = _chkFix.Checked || _chkClean.Checked || _chkKeep.Checked;
            _btnRun.Enabled = Directory.Exists(_txtDir.Text.Trim()) && hasAction;
        }

        private void RefreshStatus()
        {
            string dir = _txtDir.Text.Trim();
            if (dir.Length == 0)
            {
                _lblStatus.Text = "尚未选择目录";
                _lblStatus.ForeColor = WarningColor;
                return;
            }
            if (!Directory.Exists(dir))
            {
                _lblStatus.Text = "× 目录不存在，请重新选择";
                _lblStatus.ForeColor = WarningColor;
                return;
            }
            List<FileInfo> exes = ScanExes(dir, _chkSub.Checked);
            if (exes.Count == 0)
            {
                _lblStatus.Text = "× 该目录下未找到文件名含「悟空」的 exe";
                _lblStatus.ForeColor = WarningColor;
                return;
            }
            FileInfo newest = PickNewest(exes);
            object cur = GetRegisteredValue(newest.Name);
            string reg;
            if (cur is int && (int)cur == Ie11Mode) reg = "已登记 11001";
            else if (cur == null) reg = "未登记";
            else reg = "已登记 " + cur;
            _lblStatus.Text = "已检测到 " + exes.Count + " 个版本；最新 " + newest.Name + "；注册表：" + reg;
            _lblStatus.ForeColor = (cur is int && (int)cur == Ie11Mode) ? AccentColor : WarningColor;
        }

        private void Execute()
        {
            List<string> lines = new List<string>();
            string dir = _txtDir.Text.Trim();

            lines.Add("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] 执行");
            lines.Add("目录：" + dir);
            lines.Add("勾选：" + string.Join(" / ", CheckedItems()) + (_chkSub.Checked ? "；包含子目录" : "；不含子目录"));
            lines.Add("");

            if (!Directory.Exists(dir))
            {
                lines.Add("× 所选目录不存在，请重新选择。");
                ShowResult(lines);
                return;
            }

            List<FileInfo> exes = ScanExes(dir, _chkSub.Checked);
            if (exes.Count == 0)
            {
                lines.Add("× 该目录下未找到文件名含「悟空」的 exe。");
                ShowResult(lines);
                return;
            }

            FileInfo newest = PickNewest(exes);
            Version nv = ParseVer(newest.Name);

            lines.Add("扫描到 " + exes.Count + " 个版本：");
            foreach (FileInfo f in exes.OrderByDescending(x => ParseVer(x.Name) ?? new Version(0, 0, 0, 0)).ThenByDescending(x => x.LastWriteTimeUtc))
            {
                lines.Add("  · " + f.Name);
            }
            lines.Add("最新版：" + newest.Name + (nv != null ? "（" + nv + "）" : "（未识别版本号，按修改时间判定）"));
            lines.Add("");

            string keep = _chkKeep.Checked ? _txtKeep.Text.Trim() : "";
            int registered = 0;
            int deleted = 0;

            if (_chkFix.Checked)
            {
                lines.Add("[一键修复]");
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RegPath))
                    {
                        if (k == null) throw new InvalidOperationException("无法打开注册表键");
                        object cur = k.GetValue(newest.Name);
                        if (cur is int && (int)cur == Ie11Mode)
                        {
                            lines.Add("· 已是 IE11 模式（11001），无需修改：" + newest.Name);
                        }
                        else
                        {
                            k.SetValue(newest.Name, Ie11Mode, RegistryValueKind.DWord);
                            lines.Add("· 已登记：" + newest.Name + " = 11001");
                            registered++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lines.Add("× 登记失败：" + ex.Message);
                }
                lines.Add("");
            }

            if (_chkClean.Checked)
            {
                lines.Add("[清理残留]");
                try
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RegPath, true))
                    {
                        if (k == null)
                        {
                            lines.Add("· 注册表键不存在，无需清理。");
                        }
                        else
                        {
                            bool anything = false;
                            foreach (string name in k.GetValueNames().Where(n => n.IndexOf("悟空", StringComparison.Ordinal) >= 0).ToList())
                            {
                                if (string.Equals(name, newest.Name, StringComparison.OrdinalIgnoreCase)) continue;
                                if (keep.Length > 0 && string.Equals(name, keep, StringComparison.OrdinalIgnoreCase)) continue;
                                bool exists = exes.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                                if (exists) continue;
                                k.DeleteValue(name, false);
                                lines.Add("· 已删除残留：" + name);
                                deleted++;
                                anything = true;
                            }
                            if (keep.Length > 0) lines.Add("· 保留旧版（不清理）：" + keep);
                            if (!anything) lines.Add("· 未发现需要清理的条目。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lines.Add("× 清理失败：" + ex.Message);
                }
                lines.Add("");
            }

            if (!_chkFix.Checked && !_chkClean.Checked)
            {
                lines.Add("· 仅勾选「保留旧版」，没有可执行的动作。");
                lines.Add("");
            }

            lines.Add("完成：登记 " + registered + " 条，清理 " + deleted + " 条。");
            ShowResult(lines);
        }

        private List<string> CheckedItems()
        {
            List<string> list = new List<string>();
            if (_chkFix.Checked) list.Add("一键修复");
            if (_chkClean.Checked) list.Add("清理残留");
            if (_chkKeep.Checked) list.Add("保留旧版");
            if (list.Count == 0) list.Add("（无）");
            return list;
        }

        private void ShowResult(List<string> lines)
        {
            // 日志只留在窗口内，不写文件，关闭程序即消失。
            _txtOut.Lines = lines.ToArray();
            RefreshStatus();
        }

        private static List<FileInfo> ScanExes(string dir, bool includeSub)
        {
            List<FileInfo> list = new List<FileInfo>();
            SearchOption opt = includeSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string self = Application.ExecutablePath;
            try
            {
                foreach (string p in Directory.EnumerateFiles(dir, "*.exe", opt))
                {
                    FileInfo fi = new FileInfo(p);
                    if (string.Equals(fi.FullName, self, StringComparison.OrdinalIgnoreCase)) continue;
                    if (fi.Name.IndexOf("悟空", StringComparison.Ordinal) >= 0) list.Add(fi);
                }
            }
            catch (Exception) { }
            return list;
        }

        private static Version ParseVer(string fileName)
        {
            Match m = Regex.Match(fileName, @"(\d+(?:\.\d+){1,3})");
            Version v;
            if (m.Success && Version.TryParse(m.Groups[1].Value, out v)) return v;
            return null;
        }

        private static FileInfo PickNewest(List<FileInfo> files)
        {
            var withVer = files.Select(f => new { File = f, Ver = ParseVer(f.Name) })
                               .Where(x => x.Ver != null)
                               .OrderByDescending(x => x.Ver)
                               .FirstOrDefault();
            if (withVer != null) return withVer.File;
            return files.OrderByDescending(f => f.LastWriteTimeUtc).First();
        }

        private static object GetRegisteredValue(string name)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (k == null) return null;
                    return k.GetValue(name);
                }
            }
            catch (Exception) { return null; }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                string s = File.ReadAllText(SettingsPath, Encoding.UTF8);
                _txtDir.Text = ReadString(s, "dir");
                _chkFix.Checked = ReadBool(s, "fix", true);
                _chkClean.Checked = ReadBool(s, "clean", true);
                _chkKeep.Checked = ReadBool(s, "keep", false);
                _txtKeep.Text = ReadString(s, "keepName");
                _chkSub.Checked = ReadBool(s, "includeSub", true);
            }
            catch (Exception) { }
        }

        private void SaveSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"dir\": \"" + Escape(_txtDir.Text) + "\",");
                sb.AppendLine("  \"fix\": " + BoolText(_chkFix.Checked) + ",");
                sb.AppendLine("  \"clean\": " + BoolText(_chkClean.Checked) + ",");
                sb.AppendLine("  \"keep\": " + BoolText(_chkKeep.Checked) + ",");
                sb.AppendLine("  \"keepName\": \"" + Escape(_txtKeep.Text) + "\",");
                sb.AppendLine("  \"includeSub\": " + BoolText(_chkSub.Checked));
                sb.AppendLine("}");
                File.WriteAllText(SettingsPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception) { }
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string BoolText(bool b)
        {
            return b ? "true" : "false";
        }

        private static string ReadString(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (!m.Success) return "";
            return m.Groups[1].Value.Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        private static bool ReadBool(string json, string key, bool def)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false)");
            if (!m.Success) return def;
            return m.Groups[1].Value == "true";
        }
    }
}
