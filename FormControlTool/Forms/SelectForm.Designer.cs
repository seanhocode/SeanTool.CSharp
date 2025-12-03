namespace SeanTool.CSharp.Net8.Forms
{
    partial class SelectForm
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
            SelectComboBox = new ComboBox();
            SelectBtn = new Button();
            SuspendLayout();
            // 
            // SelectComboBox
            // 
            SelectComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            SelectComboBox.Dock = DockStyle.Fill;
            SelectComboBox.FormattingEnabled = true;
            SelectComboBox.Location = new Point(0, 0);
            SelectComboBox.Name = "SelectComboBox";
            SelectComboBox.Size = new Size(292, 23);
            SelectComboBox.TabIndex = 0;
            // 
            // SelectBtn
            // 
            SelectBtn.Dock = DockStyle.Bottom;
            SelectBtn.Location = new Point(0, 23);
            SelectBtn.Name = "SelectBtn";
            SelectBtn.Size = new Size(292, 39);
            SelectBtn.TabIndex = 1;
            SelectBtn.Text = "Select";
            SelectBtn.UseVisualStyleBackColor = true;
            SelectBtn.Click += SelectBtn_Click;
            // 
            // SelectForm
            // 
            AutoSize = true;
            ClientSize = new Size(292, 62);
            Controls.Add(SelectBtn);
            Controls.Add(SelectComboBox);
            Name = "SelectForm";
            Text = "Select";
            Load += SelectProjectInfoForm_Load;
            ResumeLayout(false);

        }

        #endregion
    }
}