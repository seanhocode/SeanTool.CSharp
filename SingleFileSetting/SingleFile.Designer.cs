namespace SeanTool.Tools
{
    partial class SingleFile
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
            ShowExampleBtn = new Button();
            ShowPublishSettingBtn = new Button();
            ShowGitSettingBtn = new Button();
            SuspendLayout();
            // 
            // ShowExampleBtn
            // 
            ShowExampleBtn.Location = new Point(12, 12);
            ShowExampleBtn.Name = "ShowExampleBtn";
            ShowExampleBtn.Size = new Size(260, 60);
            ShowExampleBtn.TabIndex = 0;
            ShowExampleBtn.Text = ".csproj Example";
            ShowExampleBtn.UseVisualStyleBackColor = true;
            ShowExampleBtn.Click += ShowExampleBtn_Click;
            // 
            // ShowPublishSettingBtn
            // 
            ShowPublishSettingBtn.Location = new Point(12, 78);
            ShowPublishSettingBtn.Name = "ShowPublishSettingBtn";
            ShowPublishSettingBtn.Size = new Size(260, 60);
            ShowPublishSettingBtn.TabIndex = 1;
            ShowPublishSettingBtn.Text = "Show Publish Setting";
            ShowPublishSettingBtn.UseVisualStyleBackColor = true;
            ShowPublishSettingBtn.Click += ShowPublishSettingBtn_Click;
            // 
            // ShowGitSettingBtn
            // 
            ShowGitSettingBtn.Location = new Point(12, 144);
            ShowGitSettingBtn.Name = "ShowGitSettingBtn";
            ShowGitSettingBtn.Size = new Size(260, 60);
            ShowGitSettingBtn.TabIndex = 2;
            ShowGitSettingBtn.Text = "Show Git yml Setting";
            ShowGitSettingBtn.UseVisualStyleBackColor = true;
            ShowGitSettingBtn.Click += ShowGitSettingBtn_Click;
            // 
            // SingleFile
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(284, 211);
            Controls.Add(ShowGitSettingBtn);
            Controls.Add(ShowPublishSettingBtn);
            Controls.Add(ShowExampleBtn);
            Name = "SingleFile";
            Text = "SingleFile";
            ResumeLayout(false);
        }

        #endregion

        private Button ShowExampleBtn;
        private Button ShowPublishSettingBtn;
        private Button ShowGitSettingBtn;
    }
}
