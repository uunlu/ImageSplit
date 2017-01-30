using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageSplitter
{
    public partial class Form1 : Form
    {
        public Image Img { get; set; }
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Img = GetImage();
            pictureBox1.Image = Img;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        Image GetImage()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(folder, "captcha.jpg");
            return Image.FromFile(path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int x = 50;
            int y = 210;
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(folder, "1.jpg");
            var greyPath = Path.Combine(folder, "gscale.jpg");

            Bitmap flag = new Bitmap(x, y);

            var currentImage = new Bitmap(Img);
            System.Drawing.Imaging.PixelFormat format =
             Img.PixelFormat;
            var bitmaps = new List<Bitmap>();
            for (int i = 0; i < 3; i++)
            {
                Rectangle cloneRect = new Rectangle(40 + (x * i), 0, x, y);
                flag = currentImage.Clone(cloneRect, format);
                bitmaps.Add(flag);
            }


            pictureBox2.Image = bitmaps.ElementAt(0);
            pictureBox3.Image = bitmaps.ElementAt(1);
            pictureBox4.Image = bitmaps.ElementAt(2);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;

            var greyscale = MakeGrayscale3((Bitmap)pictureBox1.Image);

            //greyscale.Save(greyPath);
            HistoGram(greyscale);
            FindImageSegmentationLocations();
        }

        void FindImageSegmentationLocations()
        {
            int index = 1;
            var segmantationLocations = new List<Locations>();
            int start = 0;
            int end = 0;
            bool isCapturing = false;

            foreach (var histo in HistoList)
            {
                if (!histo.ContainsKey("black") && !isCapturing)
                {
                    index++;
                    continue;
                }
                if (histo.ContainsKey("black") && !isCapturing)
                {
                    isCapturing = true;
                    start = index;
                }
                if (!histo.ContainsKey("black") && isCapturing)
                {
                    isCapturing = false;
                    end = index;
                    segmantationLocations.Add(new Locations { Start = start, End = end });
                }
                index++;
            }

            var normalizedLocations = FineTuneResult(segmantationLocations);
            DrawCharacters(normalizedLocations);
        }

        private void DrawCharacters(List<Locations> normalizedLocations)
        {
            var currentImage = new Bitmap(Img);
            System.Drawing.Imaging.PixelFormat format =
          Img.PixelFormat;
            var bitmaps = new List<Bitmap>();

            foreach (var item in normalizedLocations)
            {
                Bitmap flag = new Bitmap(item.End - item.Start, 210);
                Rectangle cloneRect = new Rectangle(item.Start, 0, item.End-item.Start, 210);
                flag = currentImage.Clone(cloneRect, format);
                bitmaps.Add(flag);
            }

            pictureBox2.Image = bitmaps.ElementAt(0);
            pictureBox3.Image = bitmaps.ElementAt(1);
            pictureBox4.Image = bitmaps.ElementAt(2);
            pictureBox5.Image = bitmaps.ElementAt(3);
            pictureBox6.Image = bitmaps.ElementAt(4);
            pictureBox7.Image = bitmaps.ElementAt(5);

            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox5.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox6.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox7.SizeMode = PictureBoxSizeMode.Zoom;

        }

        private List<Locations> FineTuneResult(List<Locations> segmantationLocations)
        {
            var normalizedList = new List<Locations>();

            foreach (var item in segmantationLocations)
            {
                var diff = item.End - item.Start;
                if (diff > 59)
                {
                    var end = diff / 2 + item.Start;
                    normalizedList.Add(new Locations
                    {
                        Start = item.Start,
                        End = end
                    });
                    normalizedList.Add(new Locations
                    {
                        Start = end + 1,
                        End = item.End
                    });
                }
                else
                {
                    normalizedList.Add(item);
                }
            }

            return normalizedList;
        }

        struct Locations
        {
            public int Start { get; set; }
            public int End { get; set; }
        }
        public List<Dictionary<string, int>> HistoList { get; set; } = new List<Dictionary<string, int>>();
        private void HistoGram(Bitmap bm)
        {
            // Get your image in a bitmap; this is how to get it from a picturebox
            // Store the histogram in a dictionary   

            string colorType = "";
            for (int x = 0; x < bm.Width; x++)
            {
                Dictionary<string, int> histo = new Dictionary<string, int>();
                for (int y = 0; y < bm.Height; y++)
                {
                    // Get pixel color 
                    Color c = bm.GetPixel(x, y);
                    // If it exists in our 'histogram' increment the corresponding value, or add new
                    if (c.B > 120 && c.G > 120 && c.R > 120)
                    {
                        colorType = "white";
                    }
                    else
                    {
                        colorType = "black";
                    }
                    if (histo.ContainsKey(colorType))
                        histo[colorType] = histo[colorType] + 1;
                    else
                        histo.Add(colorType, 1);
                }

                //Console.WriteLine($"------------------------");
                //Console.WriteLine($"Width: {x}");
                //foreach (string key in histo.Keys)
                //{
                //    Console.WriteLine(key.ToString() + ": " + histo[key]);
                //}
                //Console.WriteLine($"------------------------");
                HistoList.Add(histo);
            }
        }

        public Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
    }
}
