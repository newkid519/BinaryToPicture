using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BinaryToPicture
{
    public partial class MainForm : Form
    {
        Bitmap bitmapBlueplanet;

        public MainForm()
        {
            InitializeComponent();
            bitmapBlueplanet = (Bitmap)Image.FromFile("blueplanet.jpg");
        }

        private void ConvertBinaryToPicture(string fileName, byte[] binaryData)
        {
            var dataWithLength = new byte[binaryData.Length + 4];
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(ms))
                {
                    binaryWriter.Write(binaryData.Length);
                    var temp = ms.ToArray();
                    Array.Copy(temp, dataWithLength, 4);
                    Array.Copy(binaryData, 0, dataWithLength, 4, binaryData.Length);                    
                }
            }

            int rows = dataWithLength.Length / (bitmapBlueplanet.Width * 4) + 1;
            Bitmap bitmap = new Bitmap(bitmapBlueplanet.Width, rows);

            var dataWithPadding = new byte[bitmap.Width * bitmap.Height * 4];
            Array.Copy(dataWithLength, dataWithPadding, dataWithLength.Length);

            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    //Color colorOriginal = bitmapBlueplanet.GetPixel(j, i);

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
            bitmap.Save(pngFileName, ImageFormat.Png);

            //verify
            ResumeToBinary(pngFileName);
        }

        private void ResumeToBinary(string pngFileName)
        {
            Bitmap bitmap = (Bitmap)Image.FromFile(pngFileName);

            var pixel0 = bitmap.GetPixel(0, 0);
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

            File.WriteAllBytes("recover.dat", data);
            ComputeHash(data);
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
    }
}
