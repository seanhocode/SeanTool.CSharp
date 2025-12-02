using System.Text;

namespace SeanTool.Tools
{
    public partial class SingleFile : Form
    {
        public SingleFile()
        {
            InitializeComponent();
        }

        private void ShowExampleBtn_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "csprojExample.txt");
            string content = File.ReadAllText(path, Encoding.UTF8);
            new TextViewerForm(content, Path.GetFileName(path)).Show();
        }



        private void ShowPublishSettingBtn_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PublishSetting.png");
            new ImageViewerForm(path, "顯示圖片").Show();
        }

        private void ShowGitSettingBtn_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "release.yml");
            string content = File.ReadAllText(path, Encoding.UTF8);
            new TextViewerForm(content, Path.GetFileName(path)).Show();
        }
    }

    public class TextViewerForm : Form
    {
        public TextViewerForm(string content, string title = null)
        {
            Text = title ?? "Text Viewer";
            Width = 900;
            Height = 600;

            var tb = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,   // 可水平/垂直捲動
                WordWrap = false,               // 不自動換行（方便看長行）
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10) // 等寬字體，閱讀程式/Log 友善
            };

            tb.Text = content;
            Controls.Add(tb);
        }
    }

    public class ImageViewerForm : Form
    {
        public ImageViewerForm(string imagePath, string title = null)
        {
            Text = title ?? "Image Viewer";
            Width = 800;
            Height = 600;

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom // 縮放圖片，保持比例
            };

            if (File.Exists(imagePath))
                pictureBox.Image = Image.FromFile(imagePath);
            else
                MessageBox.Show($"找不到圖片：{imagePath}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Controls.Add(pictureBox);
        }
    }
}
