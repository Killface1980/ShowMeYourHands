namespace AnimationConverter
{
    partial class MainInterface
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openInputFileDialogButton = new System.Windows.Forms.Button();
            this.openInputFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.startConversionButton = new System.Windows.Forms.Button();
            this.inputFileTextBox = new System.Windows.Forms.TextBox();
            this.scalingXZField = new System.Windows.Forms.NumericUpDown();
            this.scalingXZLabel = new System.Windows.Forms.Label();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.scaleTimeCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.scalingXZField)).BeginInit();
            this.SuspendLayout();
            // 
            // openInputFileDialogButton
            // 
            this.openInputFileDialogButton.Location = new System.Drawing.Point(12, 13);
            this.openInputFileDialogButton.Name = "openInputFileDialogButton";
            this.openInputFileDialogButton.Size = new System.Drawing.Size(126, 23);
            this.openInputFileDialogButton.TabIndex = 0;
            this.openInputFileDialogButton.Text = "SCML file to convert";
            this.openInputFileDialogButton.UseVisualStyleBackColor = true;
            this.openInputFileDialogButton.Click += new System.EventHandler(this.OpenInputFileDialogButton_Click);
            // 
            // openInputFileDialog
            // 
            this.openInputFileDialog.FileName = "";
            this.openInputFileDialog.Filter = "SCML Files (*.scml)|*.scml|All files (*.*)|*.*";
            // 
            // startConversionButton
            // 
            this.startConversionButton.Location = new System.Drawing.Point(12, 52);
            this.startConversionButton.Name = "startConversionButton";
            this.startConversionButton.Size = new System.Drawing.Size(126, 23);
            this.startConversionButton.TabIndex = 2;
            this.startConversionButton.Text = "Start conversion";
            this.startConversionButton.UseVisualStyleBackColor = true;
            this.startConversionButton.Click += new System.EventHandler(this.StartConversionButton_Click);
            // 
            // inputFileTextBox
            // 
            this.inputFileTextBox.Location = new System.Drawing.Point(153, 14);
            this.inputFileTextBox.Name = "inputFileTextBox";
            this.inputFileTextBox.Size = new System.Drawing.Size(615, 20);
            this.inputFileTextBox.TabIndex = 3;
            this.inputFileTextBox.TextChanged += new System.EventHandler(this.InputFileTextBox_TextChanged);
            // 
            // scalingXZField
            // 
            this.scalingXZField.Location = new System.Drawing.Point(707, 55);
            this.scalingXZField.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.scalingXZField.Name = "scalingXZField";
            this.scalingXZField.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.scalingXZField.Size = new System.Drawing.Size(61, 20);
            this.scalingXZField.TabIndex = 5;
            this.scalingXZField.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.scalingXZField.Value = new decimal(new int[] {
            96,
            0,
            0,
            0});
            this.scalingXZField.ValueChanged += new System.EventHandler(this.ScalingXZField_ValueChanged);
            // 
            // scalingXZLabel
            // 
            this.scalingXZLabel.AutoSize = true;
            this.scalingXZLabel.Location = new System.Drawing.Point(573, 58);
            this.scalingXZLabel.Name = "scalingXZLabel";
            this.scalingXZLabel.Size = new System.Drawing.Size(116, 13);
            this.scalingXZLabel.TabIndex = 6;
            this.scalingXZLabel.Text = "X-Z scaling (default 96)";
            // 
            // outputTextBox
            // 
            this.outputTextBox.Location = new System.Drawing.Point(12, 94);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.ReadOnly = true;
            this.outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.outputTextBox.Size = new System.Drawing.Size(756, 635);
            this.outputTextBox.TabIndex = 7;
            // 
            // scaleTimeCheckBox
            // 
            this.scaleTimeCheckBox.AutoSize = true;
            this.scaleTimeCheckBox.Location = new System.Drawing.Point(239, 57);
            this.scaleTimeCheckBox.Name = "scaleTimeCheckBox";
            this.scaleTimeCheckBox.Size = new System.Drawing.Size(257, 17);
            this.scaleTimeCheckBox.TabIndex = 8;
            this.scaleTimeCheckBox.Text = "Convert animation times from milliseconds to ticks";
            this.scaleTimeCheckBox.UseVisualStyleBackColor = true;
            this.scaleTimeCheckBox.CheckedChanged += new System.EventHandler(this.ScaleTimeCheckBox_CheckedChanged);
            // 
            // MainInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(786, 741);
            this.Controls.Add(this.scaleTimeCheckBox);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.scalingXZLabel);
            this.Controls.Add(this.scalingXZField);
            this.Controls.Add(this.inputFileTextBox);
            this.Controls.Add(this.startConversionButton);
            this.Controls.Add(this.openInputFileDialogButton);
            this.Name = "MainInterface";
            this.Text = "AnimationConverter";
            ((System.ComponentModel.ISupportInitialize)(this.scalingXZField)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openInputFileDialogButton;
        private System.Windows.Forms.OpenFileDialog openInputFileDialog;
        private System.Windows.Forms.Button startConversionButton;
        private System.Windows.Forms.TextBox inputFileTextBox;
        private System.Windows.Forms.NumericUpDown scalingXZField;
        private System.Windows.Forms.Label scalingXZLabel;
        private System.Windows.Forms.TextBox outputTextBox;
        private System.Windows.Forms.CheckBox scaleTimeCheckBox;
    }
}

