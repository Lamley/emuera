using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MinorShift.Emuera.Forms;
using MinorShift.Emuera.GameView;

namespace MinorShift.Emuera
{
    internal sealed partial class MainWindow : Form
    {
        private bool changeTextbyMouse;
        private EmueraConsole console;

        private readonly FileVersionInfo emueraVer =
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        private int labelTimerCount;
        private string last_inputed = "";
        private int lastSelected = 100;

        private int macroGroup;
        private readonly ToolStripMenuItem[] macroMenuItems = new ToolStripMenuItem[KeyMacro.MaxFkey];

        private readonly string[] prevInputs = new string[100];
        private int selectedInputs = 100;

        private bool textBox_flag = true;

        public MainWindow()
        {
            InitializeComponent();
            if (Program.DebugMode)
                デバッグToolStripMenuItem.Visible = true;

            mainPicBox.SetStyle();
            initControlSizeAndLocation();

            TextBox.ForeColor = Config.ForeColor;
            TextBox.BackColor = Config.BackColor;
            mainPicBox.BackColor = Config.BackColor; //これは実際には使用されないはず
            BackColor = Config.BackColor;

            TextBox.Font = Config.Font;
            TextBox.LanguageOption = RichTextBoxLanguageOptions.UIFonts;
            folderSelectDialog.SelectedPath = Program.ErbDir;
            folderSelectDialog.ShowNewFolderButton = false;

            openFileDialog.InitialDirectory = Program.ErbDir;
            openFileDialog.Filter = "ERBファイル (*.erb)|*.erb";
            openFileDialog.FileName = "";
            openFileDialog.Multiselect = true;
            openFileDialog.RestoreDirectory = true;

            var Emuera_verInfo = "Emuera Ver. " + emueraVer.FileVersion.Remove(5);
            if (emueraVer.FileBuildPart > 0)
                Emuera_verInfo += "+v" + emueraVer.FileBuildPart +
                                  (emueraVer.FilePrivatePart > 0 ? "." + emueraVer.FilePrivatePart : "");
            EmuVerToolStripTextBox.Text = Emuera_verInfo;

            timer.Enabled = true;
            console = new EmueraConsole(this);
            macroMenuItems[0] = マクロ01ToolStripMenuItem;
            macroMenuItems[1] = マクロ02ToolStripMenuItem;
            macroMenuItems[2] = マクロ03ToolStripMenuItem;
            macroMenuItems[3] = マクロ04ToolStripMenuItem;
            macroMenuItems[4] = マクロ05ToolStripMenuItem;
            macroMenuItems[5] = マクロ06ToolStripMenuItem;
            macroMenuItems[6] = マクロ07ToolStripMenuItem;
            macroMenuItems[7] = マクロ08ToolStripMenuItem;
            macroMenuItems[8] = マクロ09ToolStripMenuItem;
            macroMenuItems[9] = マクロ10ToolStripMenuItem;
            macroMenuItems[10] = マクロ11ToolStripMenuItem;
            macroMenuItems[11] = マクロ12ToolStripMenuItem;
            foreach (var item in macroMenuItems)
                item.Click += マクロToolStripMenuItem_Click;

            TextBox.MouseWheel += richTextBox1_MouseWheel;
            mainPicBox.MouseWheel += richTextBox1_MouseWheel;
            ScrollBar.MouseWheel += richTextBox1_MouseWheel;
        }

        public PictureBox MainPicBox => mainPicBox;
        public VScrollBar ScrollBar { get; private set; }

        public RichTextBox TextBox { get; private set; }

        public string InternalEmueraVer => emueraVer.FileVersion;
        public string EmueraVerText => EmuVerToolStripTextBox.Text;
        public ToolTip ToolTip => toolTipButton;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.B && (keyData & Keys.Modifiers & Keys.Control) == Keys.Control)
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Minimized;
                    return true;
                }
            }
            else if ((keyData & Keys.KeyCode) == Keys.C && (keyData & Keys.Modifiers & Keys.Control) == Keys.Control ||
                     (keyData & Keys.KeyCode) == Keys.Insert &&
                     (keyData & Keys.Modifiers & Keys.Control) == Keys.Control)
            {
                if (TextBox.SelectedText == "")
                {
                    var dialog = new ClipBoardDialog();
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.Setup(this, console);
                    dialog.ShowDialog();
                    return true;
                }
            }
            else if ((keyData & Keys.KeyCode) == Keys.V && (keyData & Keys.Modifiers & Keys.Control) == Keys.Control ||
                     (keyData & Keys.KeyCode) == Keys.Insert && (keyData & Keys.Modifiers & Keys.Shift) == Keys.Shift)
            {
                if (Clipboard.GetDataObject() == null || !Clipboard.ContainsText())
                    return true;
                if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
                    TextBox.Paste(DataFormats.GetFormat(DataFormats.UnicodeText));
                return true;
            }
            //else if (((int)keyData == (int)Keys.Control + (int)Keys.D) && Program.DebugMode)
            //{
            //    console.OpenDebugDialog();
            //    return true;
            //}
            //else if (((int)keyData == (int)Keys.Control + (int)Keys.R) && Program.DebugMode)
            //{
            //    if ((console.DebugDialog != null) && (console.DebugDialog.Created))
            //        console.DebugDialog.UpdateData();
            //}
            else if (Config.UseKeyMacro)
            {
                var keyCode = (int) (keyData & Keys.KeyCode);
                var shiftPressed = (keyData & Keys.Modifiers) == Keys.Shift;
                var ctrlPressed = (keyData & Keys.Modifiers) == Keys.Control;
                var unPressed = (int) (keyData & Keys.Modifiers) == 0;
                if (keyCode >= (int) Keys.F1 && keyCode <= (int) Keys.F12)
                {
                    var macroNum = keyCode - (int) Keys.F1;
                    if (shiftPressed)
                    {
                        if (TextBox.Text != "")
                            KeyMacro.SetMacro(macroNum, macroGroup, TextBox.Text);
                        return true;
                    }
                    if (unPressed)
                    {
                        TextBox.Text = KeyMacro.GetMacro(macroNum, macroGroup);
                        TextBox.SelectionStart = TextBox.Text.Length;
                        return true;
                    }
                }
                else if (ctrlPressed)
                {
                    var newGroupNum = -1;
                    if (keyCode >= (int) Keys.D0 && keyCode <= (int) Keys.D9)
                        newGroupNum = keyCode - (int) Keys.D0;
                    else if (keyCode >= (int) Keys.NumPad0 && keyCode <= (int) Keys.NumPad9)
                        newGroupNum = keyCode - (int) Keys.NumPad0;
                    if (newGroupNum >= 0)
                        setNewMacroGroup(newGroupNum);
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }


        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const int WM_MOUSEWHEEL = 0x020A;
            const int SC_MOVE = 0xf010;
            const int SC_MAXIMIZE = 0xf030;

            // WM_SYSCOMMAND (SC_MOVE) を無視することでフォームを移動できないようにする
            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                {
                    var wparam = m.WParam.ToInt32() & 0xfff0;
                    switch (wparam)
                    {
                        case SC_MOVE:
                            if (WindowState == FormWindowState.Maximized)
                                return;
                            break;
                        case SC_MAXIMIZE:
                            if (Screen.AllScreens.Length == 1)
                                MaximizedBounds = new Rectangle(Left, 0, Config.WindowX,
                                    Screen.PrimaryScreen.WorkingArea.Height);
                            else
                                for (var i = 0; i < Screen.AllScreens.Length; i++)
                                    if (Left >= Screen.AllScreens[i].Bounds.Left &&
                                        Left < Screen.AllScreens[i].Bounds.Right)
                                    {
                                        MaximizedBounds = new Rectangle(Left - Screen.AllScreens[i].Bounds.Left,
                                            Screen.AllScreens[i].Bounds.Top, Config.WindowX,
                                            Screen.AllScreens[i].WorkingArea.Height);
                                        break;
                                    }
                            break;
                    }
                    break;
                }

                //MouseWheelイベントをここで処理しようと思ったけどなんかここまで来ない (Windows 7)
                //case WM_MOUSEWHEEL:
                //	{
                //		if (!vScrollBar.Enabled)
                //			break;
                //		if (console == null)
                //			break;
                //		//int wparam_hiword = m.WParam.ToInt32() >> 16;
                //		int move = (m.WParam.ToInt32() >> 16) / 120 * -1;
                //		if ((vScrollBar.Value == vScrollBar.Maximum && move > 0) || (vScrollBar.Value == vScrollBar.Minimum && move < 0))
                //			break;
                //		int value = vScrollBar.Value + move;
                //		if (value >= vScrollBar.Maximum)
                //			vScrollBar.Value = vScrollBar.Maximum;
                //		else if (value <= vScrollBar.Minimum)
                //			vScrollBar.Value = vScrollBar.Minimum;
                //		else
                //			vScrollBar.Value = value;
                //		bool force_refresh = (vScrollBar.Value == vScrollBar.Maximum) || (vScrollBar.Value == vScrollBar.Minimum);

                //		//ボタンとの関係をチェック
                //		if (Config.UseMouse)
                //			force_refresh = console.MoveMouse(mainPicBox.PointToClient(Control.MousePosition)) || force_refresh;
                //		//上端でも下端でもなくボタン選択状態のアップデートも必要ないなら描画を控えめに。
                //		console.RefreshStrings(force_refresh);

                //		break;
                //	}
            }
            base.WndProc(ref m);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!Created)
                return;
            timer.Enabled = false;
            console.Initialize();
        }

        /// <summary>
        ///     1819 リサイズ時の処理を全廃しAnchor&Dock処理にマルナゲ
        ///     初期設定のみここで行う。ついでに再起動時の位置・サイズ処理も追加
        /// </summary>
        private void initControlSizeAndLocation()
        {
            //Windowのサイズ設定
            var winWidth = Config.WindowX + ScrollBar.Width;
            var winHeight = Config.WindowY;
            var winMaximize = false;
            if (Config.SizableWindow)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                MaximizeBox = true;
                winMaximize = Config.WindowMaximixed || Program.RebootWinState == FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Fixed3D;
                MaximizeBox = false;
            }

            var menuHeight = 0;
            if (Config.UseMenu)
            {
                menuStrip.Enabled = true;
                menuStrip.Visible = true;
                winHeight += menuStrip.Height;
                menuHeight = menuStrip.Height;
            }
            else
            {
                menuStrip.Enabled = false;
                menuStrip.Visible = false;
                menuHeight = 0;
            }
            //Windowの位置設定
            if (Config.SetWindowPos)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Config.WindowPosX, Config.WindowPosY);
            }
            else if (!winMaximize && Program.RebootLocation != new Point())
            {
                StartPosition = FormStartPosition.Manual;
                Location = Program.RebootLocation;
            }
            //Windowのサイズ設定・再起動時
            if (!winMaximize && Program.RebootClientY > 0)
                winHeight = Program.RebootClientY;
            ClientSize = new Size(winWidth, winHeight);

            //EmuVerToolStripTextBox.Location = new Point(Config.WindowX - vScrollBar.Width - EmuVerToolStripTextBox.Width, 3);

            mainPicBox.Location = new Point(0, menuHeight);
            mainPicBox.Size = new Size(Config.WindowX, winHeight - menuHeight - Config.LineHeight);

            TextBox.Location = new Point(0, winHeight - Config.LineHeight);
            TextBox.Size = new Size(Config.WindowX, Config.LineHeight);
            ScrollBar.Location = new Point(winWidth - ScrollBar.Size.Width, menuHeight);
            ScrollBar.Size = new Size(ScrollBar.Size.Width, winHeight - menuHeight);

            var minimamY = 100;
            if (minimamY < menuHeight + Config.LineHeight * 2)
                minimamY = menuHeight + Config.LineHeight * 2;
            if (minimamY > Height)
                minimamY = Height;
            var maximamY = 2560;
            if (maximamY < Height)
                maximamY = Height;
            MinimumSize = new Size(Width, minimamY);
            MaximumSize = new Size(Width, maximamY);
            if (winMaximize)
                WindowState = FormWindowState.Maximized;
        }

        private void mainPicBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Config.UseMouse)
                return;
            if (console == null)
                return;
            if (console.MoveMouse(e.Location))
                console.RefreshStrings(true);
        }

        private void mainPicBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Config.UseMouse)
                return;
            if (console == null || console.IsInProcess)
                return;
            var isBacklog = ScrollBar.Value != ScrollBar.Maximum;
            var str = console.SelectedString;

            if (isBacklog)
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    ScrollBar.Value = ScrollBar.Maximum;
                    console.RefreshStrings(true);
                }
            if (console.IsWaitingEnterKey && !console.IsError && str == null)
            {
                if (isBacklog)
                    return;
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    if (e.Button == MouseButtons.Right)
                        PressEnterKey(true);
                    else
                        PressEnterKey(false);
                    return;
                }
            }
            //左が押されたなら選択。
            if (str != null && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                changeTextbyMouse = console.IsWaintingOnePhrase;
                TextBox.Text = str;
                //念のため
                if (console.IsWaintingOnePhrase)
                    last_inputed = "";
                //右が押しっぱなしならスキップ追加。
                if ((MouseButtons & MouseButtons.Right) == MouseButtons.Right)
                    PressEnterKey(true);
                else
                    PressEnterKey(false);
            }
        }

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            //上端でも下端でもないなら描画を控えめに。
            if (console == null)
                return;
            console.RefreshStrings(ScrollBar.Value == ScrollBar.Maximum || ScrollBar.Value == ScrollBar.Minimum);
        }

        public void PressEnterKey(bool mesSkip)
        {
            if (console == null)
                return;
            //if (console.inProcess)
            //{
            //	richTextBox1.Text = "";
            //	return;
            //}
            var str = TextBox.Text;
            if (console.IsWaintingOnePhrase && last_inputed.Length > 0)
            {
                str = str.Remove(0, last_inputed.Length);
                last_inputed = "";
            }
            var mouseFlag = changeTextbyMouse;
            changeTextbyMouse = false;
            updateInputs(str);
            console.PressEnterKey(mesSkip, str, mouseFlag);
        }

        private void updateInputs(string cur)
        {
            if (string.IsNullOrEmpty(cur))
            {
                TextBox.Text = "";
                return;
            }
            if (selectedInputs == prevInputs.Length || cur != prevInputs[prevInputs.Length - 1])
            {
                for (var i = 0; i < prevInputs.Length - 1; i++)
                    prevInputs[i] = prevInputs[i + 1];
                prevInputs[prevInputs.Length - 1] = cur;
                //1729a eramakerと同じ処理系に変更 1730a 再修正
                if (selectedInputs > 0 && selectedInputs != prevInputs.Length && cur == prevInputs[selectedInputs - 1])
                    lastSelected = --selectedInputs;
                else
                    lastSelected = 100;
            }
            else
            {
                lastSelected = selectedInputs;
            }
            TextBox.Text = "";
            selectedInputs = prevInputs.Length;
        }

        private void movePrev(int move)
        {
            if (move == 0)
                return;
            //if((selectedInputs != prevInputs.Length) &&(prevInputs[selectedInputs] != richTextBox1.Text))
            //	selectedInputs =  prevInputs.Length;
            int next;
            if (lastSelected != prevInputs.Length && selectedInputs == prevInputs.Length)
            {
                if (move == -1)
                    move = 0;
                next = lastSelected + move;
                lastSelected = prevInputs.Length;
            }
            else
            {
                next = selectedInputs + move;
            }
            if (next < 0 || next > prevInputs.Length)
                return;
            if (next == prevInputs.Length)
            {
                selectedInputs = next;
                TextBox.Text = "";
                return;
            }
            if (string.IsNullOrEmpty(prevInputs[next]))
                if (++next == prevInputs.Length)
                    return;

            selectedInputs = next;
            TextBox.Text = prevInputs[next];
            TextBox.SelectionStart = 0;
            TextBox.SelectionLength = TextBox.Text.Length;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("ゲームを終了します", "終了", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK)
                return;
            Close();
        }

        private void rebootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("ゲームを再起動します", "再起動", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK)
                return;
            Reboot();
        }

        //private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    openFileDialog.InitialDirectory = StaticConfig.SavDir;
        //    DialogResult result = openFileDialog.ShowDialog();
        //    string filepath = openFileDialog.FileName;
        //    if (!File.Exists(filepath))
        //    {
        //        MessageBox.Show("ファイルがありません", "File Not Found");
        //        return;
        //    }
        //}

        public void Reboot()
        {
            console.forceStopTimer();
            Program.Reboot = true;
            Close();
        }

        public void GotoTitle()
        {
            if (console == null)
                return;
            console.GotoTitle();
        }

        public void ReloadErb()
        {
            if (console == null)
                return;
            console.ReloadErb();
        }

        private void mainPicBox_MouseLeave(object sender, EventArgs e)
        {
            if (console == null)
                return;
            if (Config.UseMouse)
                console.LeaveMouse();
        }


        private void コンフィグCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowConfigDialog();
        }

        public void ShowConfigDialog()
        {
            var dialog = new ConfigDialog();
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.SetConfig(this);
            dialog.ShowDialog();
            if (dialog.Result == ConfigDialogResult.SaveReboot)
            {
                console.forceStopTimer();
                Program.Reboot = true;
                Close();
            }
        }

        private void タイトルへ戻るTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null)
                return;
            if (console.IsInProcess)
            {
                MessageBox.Show("スクリプト動作中には使用できません");
                return;
            }
            if (console.notToTitle)
            {
                if (console.byError)
                    MessageBox.Show("コード解析でエラーが発見されたため、タイトルへは飛べません");
                else
                    MessageBox.Show("解析モードのためタイトルへは飛べません");
                return;
            }
            var result = MessageBox.Show("タイトル画面へ戻ります", "タイトル画面に戻る", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK)
                return;
            GotoTitle();
        }

        private void コードを読み直すcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null)
                return;
            if (console.IsInProcess)
            {
                MessageBox.Show("スクリプト動作中には使用できません");
                return;
            }
            var result = MessageBox.Show("ERBファイルを読み直します", "ERBファイル読み直し", MessageBoxButtons.OKCancel);
            if (result != DialogResult.OK)
                return;
            ReloadErb();
        }

        private void mainPicBox_Paint(object sender, PaintEventArgs e)
        {
            if (console == null)
                return;
            console.OnPaint(e.Graphics);
        }

        private void ログを保存するSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null)
                return;
            saveFileDialog.InitialDirectory = Program.ExeDir;
            var time = DateTime.Now;
            var fname = time.ToString("yyyyMMdd-HHmmss");
            fname += ".log";
            saveFileDialog.FileName = fname;
            var result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
                console.OutputLog(Path.GetFullPath(saveFileDialog.FileName));
        }

        private void ログをクリップボードにコピーToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var dialog = new ClipBoardDialog();
                dialog.Setup(this, console);
                dialog.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("予期せぬエラーが発生したためクリップボードを開けません");
            }
        }

        private void ファイルを読み直すFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null)
                return;
            if (console.IsInProcess)
            {
                MessageBox.Show("スクリプト動作中には使用できません");
                return;
            }
            var result = openFileDialog.ShowDialog();
            var filepath = new List<string>();
            if (result == DialogResult.OK)
            {
                foreach (var fname in openFileDialog.FileNames)
                {
                    if (!File.Exists(fname))
                    {
                        MessageBox.Show("ファイルがありません", "File Not Found");
                        return;
                    }
                    if (Path.GetExtension(fname).ToUpper() != ".ERB")
                    {
                        MessageBox.Show("ERBファイル以外は読み込めません", "ファイル形式エラー");
                        return;
                    }
                    if (fname.StartsWith(Program.ErbDir, StringComparison.OrdinalIgnoreCase))
                        filepath.Add(Program.ErbDir + fname.Substring(Program.ErbDir.Length));
                    else
                        filepath.Add(fname);
                }
                console.ReloadPartialErb(filepath);
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Config.UseKeyMacro)
                KeyMacro.SaveMacro();
            if (console != null)
            {
                //ほっとしても勝手に閉じるが、その場合はDebugDialogのClosingイベントが発生しない
                if (Program.DebugMode && console.DebugDialog != null && console.DebugDialog.Created)
                    console.DebugDialog.Close();
                console.Dispose();
                console = null;
            }
        }

        private void フォルダを読み直すFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null)
                return;
            if (console.IsInProcess)
            {
                MessageBox.Show("スクリプト動作中には使用できません");
                return;
            }
            var filepath = new List<KeyValuePair<string, string>>();
            if (folderSelectDialog.ShowDialog() == DialogResult.OK)
                console.ReloadFolder(folderSelectDialog.SelectedPath);
        }

        private void richTextBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            //if (!Config.UseMouse)
            //	return;
            if (!ScrollBar.Enabled)
                return;
            if (console == null)
                return;
            //e.Deltaには大きな値が入っているので符号のみ採用する
            var move = -Math.Sign(e.Delta) * ScrollBar.SmallChange * Config.ScrollHeight;
            //スクロールが必要ないならリターンする
            if (ScrollBar.Value == ScrollBar.Maximum && move > 0 || ScrollBar.Value == ScrollBar.Minimum && move < 0)
                return;
            var value = ScrollBar.Value + move;
            if (value >= ScrollBar.Maximum)
                ScrollBar.Value = ScrollBar.Maximum;
            else if (value <= ScrollBar.Minimum)
                ScrollBar.Value = ScrollBar.Minimum;
            else
                ScrollBar.Value = value;
            var force_refresh = ScrollBar.Value == ScrollBar.Maximum || ScrollBar.Value == ScrollBar.Minimum;

            //ボタンとの関係をチェック
            if (Config.UseMouse)
                force_refresh = console.MoveMouse(mainPicBox.PointToClient(MousePosition)) || force_refresh;
            //上端でも下端でもなくボタン選択状態のアップデートも必要ないなら描画を控えめに。
            console.RefreshStrings(force_refresh);
        }

        public void update_lastinput()
        {
            TextBox.TextChanged -= richTextBox1_TextChanged;
            TextBox.KeyDown -= richTextBox1_KeyDown;
            Application.DoEvents();
            TextBox.TextChanged += richTextBox1_TextChanged;
            TextBox.KeyDown += richTextBox1_KeyDown;
            last_inputed = TextBox.Text;
        }

        public void clear_richText()
        {
            TextBox.Clear();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess)
                return;
            if (!textBox_flag)
                return;
            if (!console.IsWaintingOnePhrase && !console.IsWaitAnyKey)
                return;
            if (string.IsNullOrEmpty(TextBox.Text))
                return;
            if (changeTextbyMouse)
                return;
            //テキストの削除orテキストに変化がない場合は入力されたとみなさない
            if (TextBox.Text.Length <= last_inputed.Length)
            {
                last_inputed = TextBox.Text;
                return;
            }
            textBox_flag = false;
            if (console.IsWaitAnyKey)
            {
                TextBox.Clear();
                last_inputed = "";
            }
            //if (richTextBox1.Text.Length > 1)
            //    richTextBox1.Text = richTextBox1.Text.Remove(1);
            PressEnterKey(false);
            textBox_flag = true;
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (console == null)
                return;
            if ((int) e.KeyData == (int) Keys.PageUp || (int) e.KeyData == (int) Keys.PageDown)
            {
                e.SuppressKeyPress = true;
                var move = 10;
                if ((int) e.KeyData == (int) Keys.PageUp)
                    move *= -1;
                //スクロールが必要ないならリターンする
                if (ScrollBar.Value == ScrollBar.Maximum && move > 0 ||
                    ScrollBar.Value == ScrollBar.Minimum && move < 0)
                    return;
                var value = ScrollBar.Value + move;
                if (value >= ScrollBar.Maximum)
                    ScrollBar.Value = ScrollBar.Maximum;
                else if (value <= ScrollBar.Minimum)
                    ScrollBar.Value = ScrollBar.Minimum;
                else
                    ScrollBar.Value = value;
                //上端でも下端でもないなら描画を控えめに。
                console.RefreshStrings(ScrollBar.Value == ScrollBar.Maximum || ScrollBar.Value == ScrollBar.Minimum);
                return;
            }
            if (ScrollBar.Value != ScrollBar.Maximum)
            {
                ScrollBar.Value = ScrollBar.Maximum;
                console.RefreshStrings(true);
            }
            if (e.KeyCode == Keys.Return)
            {
                e.SuppressKeyPress = true;
                if (!console.IsInProcess)
                    PressEnterKey(false);
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                console.KillMacro = true;
                if (!console.IsInProcess)
                    PressEnterKey(true);
                return;
            }
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Home || e.KeyCode == Keys.Back)
                if (TextBox.SelectionStart == 0 && TextBox.SelectedText.Length == 0 || TextBox.Text.Length == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.End)
                if (TextBox.SelectionStart == TextBox.Text.Length || TextBox.Text.Length == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (console.IsInProcess)
                    return;
                movePrev(-1);
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (console.IsInProcess)
                    return;
                movePrev(1);
                return;
            }
            if (e.KeyCode == Keys.Insert)
                e.SuppressKeyPress = true;
        }

        private void デバッグウインドウを開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Program.DebugMode)
                return;
            console.OpenDebugDialog();
        }

        private void デバッグ情報の更新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Program.DebugMode)
                return;
            if (console.DebugDialog != null && console.DebugDialog.Created)
                console.DebugDialog.UpdateData();
        }

        private void AutoVerbMenu_Opened(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess)
            {
                切り取り.Enabled = false;
                コピー.Enabled = false;
                貼り付け.Enabled = false;
                実行.Enabled = false;
                削除.Enabled = false;
                マクロToolStripMenuItem.Enabled = false;
                for (var i = 0; i < macroMenuItems.Length; i++)
                    macroMenuItems[i].Enabled = false;
                return;
            }
            実行.Enabled = true;
            if (Config.UseKeyMacro)
            {
                マクロToolStripMenuItem.Enabled = true;

                for (var i = 0; i < macroMenuItems.Length; i++)
                    macroMenuItems[i].Enabled = KeyMacro.GetMacro(i, macroGroup).Length > 0;
            }
            else
            {
                マクロToolStripMenuItem.Enabled = false;
                for (var i = 0; i < macroMenuItems.Length; i++)
                    macroMenuItems[i].Enabled = false;
            }
            if (TextBox.SelectedText.Length > 0)
            {
                切り取り.Enabled = true;
                コピー.Enabled = true;
                削除.Enabled = true;
            }
            else
            {
                切り取り.Enabled = false;
                コピー.Enabled = false;
                削除.Enabled = false;
            }
            if (Clipboard.ContainsText())
                貼り付け.Enabled = true;
            else
                貼り付け.Enabled = false;
        }

        private void 切り取り_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess || !切り取り.Enabled)
                return;
            if (TextBox.SelectedText.Length > 0)
                TextBox.Cut();
        }

        private void コピー_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess || !コピー.Enabled)
                return;
            if (TextBox.SelectedText.Length > 0)
                TextBox.Copy();
        }

        private void 貼り付け_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess || !貼り付け.Enabled)
                return;
            if (Clipboard.GetDataObject() != null && Clipboard.ContainsText())
                if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
                    //Clipboard.SetText(Clipboard.GetText(TextDataFormat.UnicodeText));
                    TextBox.Paste(DataFormats.GetFormat(DataFormats.UnicodeText));
        }

        private void 削除_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess || !削除.Enabled)
                return;
            if (TextBox.SelectedText.Length > 0)
                TextBox.SelectedText = "";
        }

        private void 実行_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess || !実行.Enabled)
                return;
            PressEnterKey(false);
        }

        private void マクロToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess)
                return;
            if (!Config.UseKeyMacro)
                return;
            var item = (ToolStripMenuItem) sender;
            var fkeynum = (int) item.ShortcutKeys - (int) Keys.F1;
            var macro = KeyMacro.GetMacro(fkeynum, macroGroup);
            if (macro.Length > 0)
            {
                TextBox.Text = macro;
                TextBox.SelectionStart = TextBox.Text.Length;
            }
        }

        private void グループToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (console == null || console.IsInProcess)
                return;
            if (!Config.UseKeyMacro)
                return;
            var item = (ToolStripMenuItem) sender;
            setNewMacroGroup(int.Parse((string) item.Tag)); //とても無駄なキャスト&Parse
        }

        private void timerKeyMacroChanged_Tick(object sender, EventArgs e)
        {
            labelTimerCount++;
            if (labelTimerCount > 10)
            {
                timerKeyMacroChanged.Stop();
                timerKeyMacroChanged.Enabled = false;
                labelMacroGroupChanged.Visible = false;
            }
        }

        private void setNewMacroGroup(int group)
        {
            labelTimerCount = 0;
            macroGroup = group;
            labelMacroGroupChanged.Text = KeyMacro.GetGroupName(group);
            timerKeyMacroChanged.Interval = 200;
            timerKeyMacroChanged.Enabled = true;
            timerKeyMacroChanged.Start();
            labelMacroGroupChanged.Location = new Point(4, TextBox.Location.Y - labelMacroGroupChanged.Height - 4);
            labelMacroGroupChanged.Visible = true;
        }
    }
}