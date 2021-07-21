using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace BinaryToPicture
{
    public partial class MainForm : Form
    {
        private readonly int PixelsPerRow = 2048;

        public MainForm()
        {
            InitializeComponent();
        }

        private void ConvertBinaryToPicture(string fileName, byte[] binaryData)
        {
            int rows = (binaryData.Length + 4) / (PixelsPerRow * 4) + 1;
            Bitmap bitmap = new Bitmap(PixelsPerRow, rows);
            var dataWithPadding = new byte[bitmap.Width * bitmap.Height * 4];

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(ms))
                {
                    binaryWriter.Write(binaryData.Length);
                    Array.Copy(ms.ToArray(), dataWithPadding, 4);
                    Array.Copy(binaryData, 0, dataWithPadding, 4, binaryData.Length);                    
                }
            }

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    int counter = (j + bitmap.Width * i) * 4;
                   
                    Color c = Color.FromArgb(
                        dataWithPadding[counter],
                        dataWithPadding[counter + 1],
                        dataWithPadding[counter + 2],
                        dataWithPadding[counter + 3]);
                    bitmap.SetPixel(j, i, c);
                }
            }

            var pngFileName = Path.ChangeExtension(fileName, "png");
            if (File.Exists(pngFileName))
                File.Delete(pngFileName);
            bitmap.Save(pngFileName, ImageFormat.Png);
        }

        private void ResumeToBinary(string pngFileName)
        {
            using (Bitmap bitmap = (Bitmap)Image.FromFile(pngFileName))
            {
                int length = GetArrayLength(bitmap.GetPixel(0, 0));

                byte[] data = new byte[length];

                for (int i = 0; i < bitmap.Height; i++)
                {
                    for (int j = 0; j < bitmap.Width; j++)
                    {
                        int counter = j + bitmap.Width * i - 1;

                        if (counter >= 0)
                        {
                            Color color = bitmap.GetPixel(j, i);
                            if (counter * 4 < data.Length)
                                data[counter * 4] = color.A;
                            if (counter * 4 + 1 < data.Length)
                                data[counter * 4 + 1] = color.R;
                            if (counter * 4 + 2 < data.Length)
                                data[counter * 4 + 2] = color.G;
                            if (counter * 4 + 3 < data.Length)
                                data[counter * 4 + 3] = color.B;
                        }
                    }
                }

                var datFileName = Path.ChangeExtension(pngFileName, "dat");
                File.WriteAllBytes(datFileName, data);
                ComputeHash(data);
            }
        }

        private static int GetArrayLength(Color pixel0)
        {
            byte[] lengthData = new byte[4]
                            {
                                pixel0.A,
                                pixel0.R,
                                pixel0.G,
                                pixel0.B
                            };

            int length = 0;
            using (MemoryStream ms = new MemoryStream(lengthData))
            {
                using (var reader = new BinaryReader(ms))
                {
                    length = reader.ReadInt32();
                }
            }

            return length;
        }

        private void ComputeHash(byte[] data)
        {
            string str = Convert.ToBase64String(SHA1.Create().ComputeHash(data));
            Debug.WriteLine(str);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All Files|*.*";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = fileDialog.FileName;
                byte[] binaryData = File.ReadAllBytes(fileName);
                ComputeHash(binaryData);
                ConvertBinaryToPicture(fileName, binaryData);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "PNG Files|*.png";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                var pngFileName = fileDialog.FileName;
                ResumeToBinary(pngFileName);
            }
        }
    }
}
