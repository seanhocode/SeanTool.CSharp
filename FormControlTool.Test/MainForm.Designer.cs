namespace FormControlTool.Test
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SelectFormTestBtn = new Button();
            ModelEditorTestBtn = new Button();
            TextFormTestBtn = new Button();
            SuspendLayout();
            // 
            // SelectFormTestBtn
            // 
            SelectFormTestBtn.Font = new Font("Microsoft JhengHei UI", 9F);
            SelectFormTestBtn.Location = new Point(12, 12);
            SelectFormTestBtn.Name = "SelectFormTestBtn";
            SelectFormTestBtn.Size = new Size(125, 25);
            SelectFormTestBtn.TabIndex = 0;
            SelectFormTestBtn.Text = "SelectFormTest";
            SelectFormTestBtn.UseVisualStyleBackColor = true;
            SelectFormTestBtn.Click += SelectFormTestBtn_Click;
            // 
            // ModelEditorTestBtn
            // 
            ModelEditorTestBtn.Font = new Font("Microsoft JhengHei UI", 9F);
            ModelEditorTestBtn.Location = new Point(12, 74);
            ModelEditorTestBtn.Name = "ModelEditorTestBtn";
            ModelEditorTestBtn.Size = new Size(125, 25);
            ModelEditorTestBtn.TabIndex = 1;
            ModelEditorTestBtn.Text = "ModelEditorTest";
            ModelEditorTestBtn.UseVisualStyleBackColor = true;
            ModelEditorTestBtn.Click += ModelEditorTestBtn_Click;
            // 
            // TextFormTestBtn
            // 
            TextFormTestBtn.Font = new Font("Microsoft JhengHei UI", 9F);
            TextFormTestBtn.Location = new Point(12, 43);
            TextFormTestBtn.Name = "TextFormTestBtn";
            TextFormTestBtn.Size = new Size(125, 25);
            TextFormTestBtn.TabIndex = 2;
            TextFormTestBtn.Text = "TextFormTest";
            TextFormTestBtn.UseVisualStyleBackColor = true;
            TextFormTestBtn.Click += TextFormTestBtn_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(TextFormTestBtn);
            Controls.Add(ModelEditorTestBtn);
            Controls.Add(SelectFormTestBtn);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button SelectFormTestBtn;
        private Button ModelEditorTestBtn;
        private Button TextFormTestBtn;
    }
}
