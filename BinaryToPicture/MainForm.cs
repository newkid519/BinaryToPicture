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
            
            Bitmap bitmap = new Bitmap(bitmapBlueplanet.Width, bitmapBlueplanet.Height);
            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Color colorOriginal = bitmapBlueplanet.GetPixel(j, i);

                    int counter = j + bitmap.Width * i;

                    if (counter >= dataWithLength.Length)
                    {
                        bitmap.SetPixel(j, i, colorOriginal);
                    }
                    else
                    {
                        Color c = Color.FromArgb(dataWithLength[counter], colorOriginal);
                        bitmap.SetPixel(j, i, c);
                    }
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

            byte[] lengthData = new byte[4] 
            { 
                bitmap.GetPixel(0, 0).A, 
                bitmap.GetPixel(1, 0).A, 
                bitmap.GetPixel(2, 0).A, 
                bitmap.GetPixel(3, 0).A 
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
                    Color color = bitmap.GetPixel(j, i);

                    int counter = j + bitmap.Width * i - 4;

                    if (counter >= 0 && counter <= length - 1)
                    {
                        data[counter] = color.A;
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
