using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Tesseract;

namespace VzlomJopi
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowExA(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;        // x position of upper-left corner
            public int top;         // y position of upper-left corner
            public int right;       // x position of lower-right corner
            public int bottom;      // y position of lower-right corner
            public override string ToString()
            {
                return string.Format(
                            "left, top: {0}, {1}; right, bottom {2},{3}; width x height: {4}x{5}",
                            left, top, right, bottom, right - left, bottom - top
                            );
            }
        }

        public static byte[] ToByteArray(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var child1 = FindWindow("TMainForm", null);
            IntPtr child3 = FindWindowExA(child1, new IntPtr(0), "TRichView", null);

            RECT rct;
            GetWindowRect(child3, out rct);
            int rWidth = rct.right - rct.left;
            int rHeight = rct.bottom - rct.top;

            Bitmap BM = new Bitmap(rWidth, rHeight);
            Graphics GH = Graphics.FromImage(BM);
            GH.CopyFromScreen(rct.left, rct.top, 0, 0, BM.Size);
            //BM.Save("img.png", System.Drawing.Imaging.ImageFormat.Png);

            var ocrengine = new TesseractEngine(Environment.CurrentDirectory, "rus", EngineMode.Default);
            var byteImage = ToByteArray(BM as Image, System.Drawing.Imaging.ImageFormat.Bmp);
            var img = Pix.LoadFromMemory(byteImage);
            var res = ocrengine.Process(img);

            richTextBox1.Text = res.GetText();

            GetModelFromCsv<CsvModel>(Environment.CurrentDirectory + "\\test.csv");
        }

        private List<TModel> GetModelFromCsv<TModel>(string path)
        {
            List<TModel> records = new List<TModel>();
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" }))
            {
                records = csv.GetRecords<TModel>().ToList();
            }
            return records;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
