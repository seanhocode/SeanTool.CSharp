namespace SeanTool.CSharp.Forms
{
    partial class TextForm
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
            TextFormInputTextBox = new TextBox();
            TextFormSubmitBtn = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            TextFormMsgLabel = new Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // TextFormInputTextBox
            // 
            TextFormInputTextBox.Dock = DockStyle.Fill;
            TextFormInputTextBox.Font = new Font("Microsoft JhengHei UI", 15F);
            TextFormInputTextBox.Location = new Point(3, 41);
            TextFormInputTextBox.Multiline = true;
            TextFormInputTextBox.Name = "TextFormInputTextBox";
            TextFormInputTextBox.Size = new Size(374, 108);
            TextFormInputTextBox.TabIndex = 0;
            // 
            // TextFormSubmitBtn
            // 
            TextFormSubmitBtn.Dock = DockStyle.Fill;
            TextFormSubmitBtn.Font = new Font("Microsoft JhengHei UI", 15F);
            TextFormSubmitBtn.Location = new Point(3, 155);
            TextFormSubmitBtn.Name = "TextFormSubmitBtn";
            TextFormSubmitBtn.Size = new Size(374, 33);
            TextFormSubmitBtn.TabIndex = 1;
            TextFormSubmitBtn.Text = "Enter";
            TextFormSubmitBtn.UseVisualStyleBackColor = true;
            TextFormSubmitBtn.Click += TextFormSubmitBtn_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(TextFormSubmitBtn, 0, 2);
            tableLayoutPanel1.Controls.Add(TextFormInputTextBox, 0, 1);
            tableLayoutPanel1.Controls.Add(TextFormMsgLabel, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Size = new Size(380, 191);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // TextFormMsgLabel
            // 
            TextFormMsgLabel.Anchor = AnchorStyles.None;
            TextFormMsgLabel.AutoSize = true;
            TextFormMsgLabel.Font = new Font("Microsoft JhengHei UI", 15F);
            TextFormMsgLabel.Location = new Point(130, 6);
            TextFormMsgLabel.Name = "TextFormMsgLabel";
            TextFormMsgLabel.Size = new Size(119, 25);
            TextFormMsgLabel.TabIndex = 2;
            TextFormMsgLabel.Text = "PleaseEnter";
            // 
            // TextForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(380, 191);
            Controls.Add(tableLayoutPanel1);
            Name = "TextForm";
            Text = "TextForm";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox TextFormInputTextBox;
        private Button TextFormSubmitBtn;
        private TableLayoutPanel tableLayoutPanel1;
        private Label TextFormMsgLabel;
    }
}