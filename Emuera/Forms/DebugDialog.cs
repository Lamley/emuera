using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.Forms
{
    public partial class DebugDialog : Form
    {
        private readonly string consoleFilepath = Program.DebugDir + "console.log";
        private readonly string traceFilepath = Program.DebugDir + "trace.log";


        private readonly string watchFilepath = Program.DebugDir + "watchlist.csv";
        private Process emuera;
        private int lastSelected = 100;
        private EmueraConsole mainConsole;

        //1750 MainWindowからほぼコピペ
        private readonly string[] prevInputs = new string[100];

        private int selectedInputs = 100;

        public DebugDialog()
        {
            InitializeComponent();
            listViewWatch.AfterLabelEdit += listViewWatch_AfterLabelEdit;

            TopMost = Config.DebugWindowTopMost;
            var width = Math.Max(MinimumSize.Width, Config.DebugWindowWidth);
            var height = Math.Max(MinimumSize.Height, Config.DebugWindowHeight);
            Size = new Size(width, height);
            if (Config.DebugSetWindowPos)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Config.DebugWindowPosX, Config.DebugWindowPosY);
            }
            updateSize();
            checkBoxTopMost.Checked = TopMost;
            loadWatchList();
        }

        public string ConsoleText
        {
            get => textBoxConsole.Text;
            set => textBoxConsole.Text = value;
        }

        public string TraceText
        {
            get => textBoxTrace.Text;
            set => textBoxTrace.Text = value;
        }

        internal void SetParent(EmueraConsole console, Process process)
        {
            emuera = process;
            mainConsole = console;
        }

        public void AddTraceText(string str)
        {
            SuspendLayout();
            textBoxTrace.Text += str;
            ResumeLayout(false);
        }

        public void UpdateData()
        {
            if (tabControlMain.SelectedTab == tabPageWatch)
                updateVarWatch();
            else if (tabControlMain.SelectedTab == tabPageTrace)
                updateTrace();
            else if (tabControlMain.SelectedTab == tabPageConsole)
                updateConsole();
        }

        private void tabControlMain_Selected(object sender, TabControlEventArgs e)
        {
            UpdateData();
            updateSize();
        }

        private void updateTrace()
        {
            var str = mainConsole.GetDebugTraceLog(false);
            if (str != null)
                textBoxTrace.Text = str;
            //textBoxTrace.SelectionStart = textBoxTrace.Text.Length;
            //textBoxTrace.Focus();
            //textBoxTrace.ScrollToCaret();
        }

        private void updateConsole()
        {
            textBoxConsole.Text = mainConsole.DebugConsoleLog;
            //textBoxConsole.SelectionStart = textBoxConsole.Text.Length;
            //textBoxConsole.Focus();
            //textBoxConsole.ScrollToCaret();
        }

        private void updateVarWatch()
        {
            GlobalStatic.Process.saveCurrentState(false);
            for (var i = 0; i < listViewWatch.Items.Count - 1; i++)
                //無名のアイテムを削除
                if (listViewWatch.Items[i].Text.Length == 0)
                {
                    listViewWatch.Items.RemoveAt(i);
                    i--;
                }
            if (listViewWatch.Items.Count == 0 ||
                !string.IsNullOrEmpty(listViewWatch.Items[listViewWatch.Items.Count - 1].Text))
            {
                var newLVI = new ListViewItem("");
                newLVI.SubItems.Add(new ListViewItem.ListViewSubItem(newLVI, ""));
                listViewWatch.Items.Add(newLVI);
            }
            foreach (ListViewItem lvi in listViewWatch.Items)
                lvi.SubItems[1].Text = getValueString(lvi.Text);
            GlobalStatic.Process.clearMethodStack();
            GlobalStatic.Process.loadPrevState();
            Update();
        }

        private string getValueString(string str)
        {
            if (emuera == null || GlobalStatic.EMediator == null)
                return "";
            if (string.IsNullOrEmpty(str))
                return "";
            mainConsole.RunERBFromMemory = true;
            try
            {
                var st = new StringStream(str);
                var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
                var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
                var value = term.GetValue(GlobalStatic.EMediator);
                return value.ToString();
            }
            catch (CodeEE e)
            {
                return e.Message;
            }
            catch (Exception e)
            {
                return e.GetType() + ":" + e.Message;
            }
            finally
            {
                mainConsole.RunERBFromMemory = false;
            }
        }

        private void listViewWatch_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Label))
            {
                //	if (e.Item != listViewWatch.Items.Count - 1)
                //		listViewWatch.Items.RemoveAt(e.Item);
            }
            else
            {
                listViewWatch.Items[e.Item].SubItems[1].Text = getValueString(e.Label);
                if (e.Item == listViewWatch.Items.Count - 1)
                {
                    var newLVI = new ListViewItem("");
                    newLVI.SubItems.Add(new ListViewItem.ListViewSubItem(newLVI, ""));
                    listViewWatch.Items.Add(newLVI);
                }
            }
        }

        private void checkBoxTopMost_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = checkBoxTopMost.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 閉じるToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ウォッチリストの読込ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadWatchList();
            updateVarWatch();
        }

        private void ウォッチリストの保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveWatchList();
        }

        private void saveData()
        {
            saveWatchList();

            StreamWriter writer = null;
            //トレースの仕様をいじってるうちに保存する意味が無いものになった
            //try
            //{
            //    writer = new StreamWriter(traceFilepath, false, StaticConfig.Encode);
            //    writer.Write(mainConsole.GetDebugTraceLog(true));
            //}
            //catch
            //{
            //    MessageBox.Show("トレースログの保存に失敗しました", "デバッグウインドウ");
            //    return;
            //}
            //finally
            //{
            //    if (writer != null)
            //        writer.Close();
            //}
            writer = null;
            try
            {
                writer = new StreamWriter(consoleFilepath, false, Config.Encode);
                writer.Write(mainConsole.DebugConsoleLog);
            }
            catch
            {
                MessageBox.Show("コンソールログの保存に失敗しました", "デバッグウインドウ");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void saveWatchList()
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(watchFilepath, false, Config.Encode);
                foreach (ListViewItem lvi in listViewWatch.Items)
                    if (!string.IsNullOrEmpty(lvi.Text))
                        writer.WriteLine(lvi.Text);
            }
            catch
            {
                MessageBox.Show("変数ウォッチリストの保存に失敗しました", "デバッグウインドウ");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void loadWatchList()
        {
            if (!File.Exists(watchFilepath))
                return;
            var saveStrList = new List<string>();

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(watchFilepath, Config.Encode);
                string line = null;
                while ((line = reader.ReadLine()) != null)
                    if (line.Length > 0)
                        saveStrList.Add(line);
            }
            catch
            {
                MessageBox.Show("変数ウォッチリストの読込に失敗しました", "デバッグウインドウ");
                return;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            listViewWatch.Items.Clear();
            foreach (var str in saveStrList)
                if (!string.IsNullOrEmpty(str))
                {
                    var newLVI = new ListViewItem(str);
                    newLVI.SubItems.Add(new ListViewItem.ListViewSubItem(newLVI, ""));
                    listViewWatch.Items.Add(newLVI);
                }
        }

        private void DebugDialog_Activated(object sender, EventArgs e)
        {
            UpdateData();
        }


        private void updateSize()
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            if (tabControlMain.SelectedTab == tabPageConsole)
                textBoxConsole.Height = tabControlMain.Height - 26 - textBoxCommand.Height - 9;
        }

        private void DebugDialog_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                return;
            //環境依存かもしれない。誰かに指摘されたら考えよう。
            tabControlMain.Height = Size.Height - 103;
            updateSize();
        }

        private void listViewWatch_KeyUp(object sender, KeyEventArgs e)
        {
            //F2キーで名前の変更。
            if (e.KeyCode == Keys.F2 && listViewWatch.FocusedItem != null)
                listViewWatch.FocusedItem.BeginEdit();
        }

        private void DebugDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveData();
        }


        private void listViewWatch_MouseUp(object sender, MouseEventArgs e)
        {
            var item = listViewWatch.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                item.Selected = true;
                item.BeginEdit();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //これをクリックする時点で情報が最新でないことは普通ないので実はあんまり意味が無い。
            //最新の情報であることを確認するためのボタンってことで
            UpdateData();
        }

        private void textBoxCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                e.SuppressKeyPress = true;
                if (!mainConsole.IsInProcess && textBoxCommand.Text.Length > 0)
                {
                    mainConsole.DebugPrint(textBoxCommand.Text);
                    mainConsole.DebugNewLine();
                    mainConsole.DebugCommand(textBoxCommand.Text, false, true);
                    updateConsole();
                    textBoxConsole.SelectionStart = textBoxConsole.Text.Length;
                    textBoxConsole.Focus();
                    textBoxConsole.ScrollToCaret();
                    updateInputs();
                    textBoxCommand.Focus();
                }
                return;
            }
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (mainConsole.IsInProcess)
                    return;
                movePrev(-1);
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (mainConsole.IsInProcess)
                    return;
                movePrev(1);
            }
        }

        private void updateInputs()
        {
            var cur = textBoxCommand.Text;
            if (string.IsNullOrEmpty(cur))
                return;
            for (var i = 0; i < prevInputs.Length - 1; i++)
                prevInputs[i] = prevInputs[i + 1];
            prevInputs[prevInputs.Length - 1] = cur;
            //entered = console.IsWaintingOnePhrase;
            textBoxCommand.Text = "";
            //1729a eramakerと同じ処理系に変更 1730a 再修正
            if (selectedInputs != prevInputs.Length && cur == prevInputs[selectedInputs - 1])
                lastSelected = --selectedInputs;
            else
                lastSelected = 100;
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
                textBoxCommand.Text = "";
                return;
            }
            if (string.IsNullOrEmpty(prevInputs[next]))
                if (++next == prevInputs.Length)
                    return;

            selectedInputs = next;
            textBoxCommand.Text = prevInputs[next];
            textBoxCommand.SelectionStart = 0;
            textBoxCommand.SelectionLength = textBoxCommand.Text.Length;
        }

        private void 設定ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var tempTopMost = TopMost;
            TopMost = false;
            var dialog = new DebugConfigDialog();
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.SetConfig(this);
            dialog.ShowDialog();
            TopMost = tempTopMost;
        }
    }
}