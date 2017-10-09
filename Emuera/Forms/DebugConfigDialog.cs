using System;
using System.Drawing;
using System.Windows.Forms;

namespace MinorShift.Emuera.Forms
{
    internal partial class DebugConfigDialog : Form
    {
        private DebugDialog dd;
        public ConfigDialogResult Result = ConfigDialogResult.Cancel;

        public DebugConfigDialog()
        {
            InitializeComponent();

            numericUpDownDWW.Maximum = 10000;
            numericUpDownDWH.Maximum = 10000;
            numericUpDownDWX.Maximum = 10000;
            numericUpDownDWY.Maximum = 10000;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveConfig();
            Result = ConfigDialogResult.Save;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Result = ConfigDialogResult.Cancel;
            Close();
        }

        private void setCheckBox(CheckBox checkbox, ConfigCode code)
        {
            var item = (ConfigItem<bool>) ConfigData.Instance.GetDebugItem(code);
            checkbox.Checked = item.Value;
            checkbox.Enabled = !item.Fixed;
        }

        private void setNumericUpDown(NumericUpDown updown, ConfigCode code)
        {
            var item = (ConfigItem<int>) ConfigData.Instance.GetDebugItem(code);
            decimal value = item.Value;
            if (updown.Maximum < value)
                updown.Maximum = value;
            if (updown.Minimum > value)
                updown.Minimum = value;
            updown.Value = value;
            updown.Enabled = !item.Fixed;
        }

        private void setColorBox(ColorBox colorBox, ConfigCode code)
        {
            var item = (ConfigItem<Color>) ConfigData.Instance.GetDebugItem(code);
            colorBox.SelectingColor = item.Value;
            colorBox.Enabled = !item.Fixed;
        }

        private void setTextBox(TextBox textBox, ConfigCode code)
        {
            var item = (ConfigItem<string>) ConfigData.Instance.GetDebugItem(code);
            textBox.Text = item.Value;
            textBox.Enabled = !item.Fixed;
        }

        public void SetConfig(DebugDialog debugDialog)
        {
            dd = debugDialog;
            var config = ConfigData.Instance;

            setCheckBox(checkBoxShowDW, ConfigCode.DebugShowWindow);
            setCheckBox(checkBoxDWTM, ConfigCode.DebugWindowTopMost);
            setCheckBox(checkBoxSetDWPos, ConfigCode.DebugSetWindowPos);
            setNumericUpDown(numericUpDownDWW, ConfigCode.DebugWindowWidth);
            setNumericUpDown(numericUpDownDWH, ConfigCode.DebugWindowHeight);
            setNumericUpDown(numericUpDownDWX, ConfigCode.DebugWindowPosX);
            setNumericUpDown(numericUpDownDWY, ConfigCode.DebugWindowPosY);
        }

        private void SaveConfig()
        {
            //ConfigData config = ConfigData.Instance.Copy();
            var config = ConfigData.Instance;
            config.GetDebugItem(ConfigCode.DebugShowWindow).SetValue(checkBoxShowDW.Checked);
            config.GetDebugItem(ConfigCode.DebugWindowTopMost).SetValue(checkBoxDWTM.Checked);
            config.GetDebugItem(ConfigCode.DebugSetWindowPos).SetValue(checkBoxSetDWPos.Checked);
            config.GetDebugItem(ConfigCode.DebugWindowWidth).SetValue((int) numericUpDownDWW.Value);
            config.GetDebugItem(ConfigCode.DebugWindowHeight).SetValue((int) numericUpDownDWH.Value);
            config.GetDebugItem(ConfigCode.DebugWindowPosX).SetValue((int) numericUpDownDWX.Value);
            config.GetDebugItem(ConfigCode.DebugWindowPosY).SetValue((int) numericUpDownDWY.Value);
            config.SaveDebugConfig();
        }


        private void comboBoxReduceArgumentOnLoad_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (dd == null || !dd.Created)
                return;
            if (numericUpDownDWW.Enabled)
                numericUpDownDWW.Value = dd.Width;
            if (numericUpDownDWH.Enabled)
                numericUpDownDWH.Value = dd.Height;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dd == null || !dd.Created)
                return;
            if (numericUpDownDWX.Enabled)
            {
                if (numericUpDownDWX.Maximum < dd.Location.X)
                    numericUpDownDWX.Maximum = dd.Location.X;
                if (numericUpDownDWX.Minimum > dd.Location.X)
                    numericUpDownDWX.Minimum = dd.Location.X;
                numericUpDownDWX.Value = dd.Location.X;
            }
            if (numericUpDownDWY.Enabled)
            {
                if (numericUpDownDWY.Maximum < dd.Location.Y)
                    numericUpDownDWY.Maximum = dd.Location.Y;
                if (numericUpDownDWY.Minimum > dd.Location.Y)
                    numericUpDownDWY.Minimum = dd.Location.Y;
                numericUpDownDWY.Value = dd.Location.Y;
            }
        }
    }
}