using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AnimationConverter
{
    public partial class MainInterface : Form
    {
        private string inputFilePath;
        private int scaleXZ = 96;
        private bool scaleTiming = false;

        public MainInterface()
        {
            this.InitializeComponent();
        }

        private void OpenInputFileDialogButton_Click(object sender, EventArgs e)
        {
            if (this.openInputFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.inputFilePath = this.openInputFileDialog.FileName;
                this.inputFileTextBox.Text = this.inputFilePath;
            }
        }

        private void StartConversionButton_Click(object sender, EventArgs e)
        {
            bool errorFlag = false;

            if (!File.Exists(this.inputFilePath))
            { this.outputTextBox.Text = "Input .scml file does not exist"; errorFlag = true; }

            if (errorFlag)
            { return; }

            Debug.WriteLine("passed flag");

            XMLParsing newObj = new XMLParsing();
            newObj.BeginParsing(this.inputFilePath, this.outputTextBox, this.scaleXZ, this.scaleTiming);
        }

        private void InputFileTextBox_TextChanged(object sender, EventArgs e)
        {
            this.inputFilePath = this.inputFileTextBox.Text;
        }

        private void ScalingXZField_ValueChanged(object sender, EventArgs e)
        {
            this.scaleXZ = (int)this.scalingXZField.Value;
        }

        private void ScaleTimeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.scaleTiming = this.scaleTimeCheckBox.Checked;
        }
    }
}
