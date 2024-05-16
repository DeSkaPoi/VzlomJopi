using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;

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

        public static byte[] ToByteArray(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        private static List<TestCsvModel> _tests = new();

        public Form1()
        {
            InitializeComponent();
            _tests = GetModelFromCsv<TestCsvModel>(Environment.CurrentDirectory + "\\test.csv");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IntPtr tTester = FindWindow("TMainForm", null);
            IntPtr tTesterTRichView = FindWindowExA(tTester, new IntPtr(0), "TRichView", null);

            if(tTesterTRichView == IntPtr.Zero)
                System.Windows.Forms.Application.Exit();

            RECT rct;
            GetWindowRect(tTesterTRichView, out rct);
            int rWidth = rct.right - rct.left;
            int rHeight = rct.bottom - rct.top;

            Bitmap BM = new Bitmap(rWidth, rHeight);
            Graphics GH = Graphics.FromImage(BM);
            GH.CopyFromScreen(rct.left, rct.top, 0, 0, BM.Size);
            //BM.Save("img.png", System.Drawing.Imaging.ImageFormat.Png);

            TesseractEngine ocrengine = new TesseractEngine(Environment.CurrentDirectory, "rus", EngineMode.Default);
            byte[] byteImage = ToByteArray(BM, System.Drawing.Imaging.ImageFormat.Bmp);
            Pix img = Pix.LoadFromMemory(byteImage);
            Page res = ocrengine.Process(img);
            
            string questionInTtester = res.GetText();
            int minCountOfEdit = int.MaxValue;
            TestCsvModel foundTest = null;
            foreach (TestCsvModel test in _tests)
            {
                int countOfEdit = CountOfEditsInString(test.Question, questionInTtester);
                if (countOfEdit < minCountOfEdit)
                {
                    minCountOfEdit = countOfEdit;
                    foundTest = test;
                }
            }

            Form2 dlg1 = new Form2(foundTest != null ? foundTest.Answer : "Вопрос не распознан. Удачи)");
            dlg1.ShowDialog();
        }

        private int CountOfEditsInString(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
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
