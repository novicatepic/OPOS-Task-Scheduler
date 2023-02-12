using System.Drawing;
using TaskScheduler;
using System.Windows;
using System.Drawing.Imaging;
using System;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms.VisualStyles;
using System.IO;

namespace PROJEKAT.Jobs
{
    public class ImageNormalizationJob : IUserJob
    {
        public String Name { get; init; } = "Image";
        public int Parallelism { get; init; } = 2;
        private const float MIN_BRIGHTNESS = 0;
        private const float MAX_BRIGHTNESS = 1;
        public int SleepTime { get; init; } = 500;
        //public List<(string, string)> PathTupple { get; init; } = new();

        public List<string> InputPaths { get; init; } = new List<string>() { "Images/InputImages/" };
        public string OutputPath { get; init; } = "Images/OutputImages/";
        /*private string[] inputPaths;
        private string[] outputPaths;*/
        public ImageNormalizationJob() { }
        public ImageNormalizationJob(List<string> inputPaths, string outputPath)
        {
            this.InputPaths = inputPaths;
            this.OutputPath = outputPath;
        }

        object picLock = new();
        private void HelpMethodForProcessingImage(Bitmap image, float minBrightness, float maxBrightness)
        {
            if(Parallelism > 1)
            {
                Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, x =>
                {
                    //Console.WriteLine("ID: " + Task.CurrentId);
                    Bitmap imageClone = null;
                    lock (picLock)
                    {
                        imageClone = (Bitmap)image.Clone();
                    }
                    for (int y = 0; y < imageClone.Height; y++)
                    {
                        float pixelBrightness;
                        pixelBrightness = imageClone.GetPixel(x, y).GetBrightness();
                        minBrightness = Math.Min(minBrightness, pixelBrightness);
                        maxBrightness = Math.Max(maxBrightness, pixelBrightness);
                    }
                    for (int y = 0; y < imageClone.Height; y++)
                    {
                        Color pixelColor = imageClone.GetPixel(x, y);
                        float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                        Color normalizedPixelColor = ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                            pixelColor.GetSaturation(), normalizedPixelBrightness);
                        imageClone.SetPixel(x, y, normalizedPixelColor);
                    }
                });
            }
           else
            {
                for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        float pixelBrightness;
                        pixelBrightness = image.GetPixel(x, y).GetBrightness();
                        minBrightness = Math.Min(minBrightness, pixelBrightness);
                        maxBrightness = Math.Max(maxBrightness, pixelBrightness);
                    }
                }
                for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        Color pixelColor = image.GetPixel(x, y);
                        float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                        Color normalizedPixelColor = ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                            pixelColor.GetSaturation(), normalizedPixelBrightness);
                        image.SetPixel(x, y, normalizedPixelColor);
                    }
                }
                
            }
        }

        public int NumOfPictures()
        {
            int number = 0;
            foreach(var file in InputPaths)
            {
                number += Directory.GetFiles(file).Length;
            }
            return number;
        }

        void IUserJob.Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");
            bool breakFromParralel = false;
            int k = 0;
            int numPictures = NumOfPictures();
            for (int i = 0; i < InputPaths.Count; i++)
            {
                //Console.WriteLine($"{Name}: {i}");                         
                string[] pictures = Directory.GetFiles(InputPaths[i]);

                Parallel.ForEach(pictures, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, picture =>
                {
                    if(breakFromParralel == false)
                    {
                        Bitmap image = (Bitmap)System.Drawing.Image.FromFile(picture);

                        float minBrightness = MAX_BRIGHTNESS;
                        float maxBrightness = MIN_BRIGHTNESS;
                        HelpMethodForProcessingImage(image, minBrightness, maxBrightness);

                        string[] splitPic = picture.Split('/');
                        image.Save(OutputPath + splitPic.ElementAt(splitPic.Length - 1));

                        Console.WriteLine($"{Name}: {++k}");
                        
                        jobApi.CheckAll();
                        if (jobApi.StoppageConfirmed())
                        {
                            breakFromParralel = true;
                        }

                        if (jobApi.CheckConditions())
                        {
                            breakFromParralel = true;
                        }

                        if (k == numPictures)
                        {
                            jobApi.SetProgress(1);
                        }
                        else
                        {
                            double progress = (double)k / (double)numPictures;
                            jobApi.SetProgress(progress);
                        }

                        Thread.Sleep(SleepTime);
                    }                  
                });               
            }

            if (!jobApi.StoppageConfirmed())
            {
                Console.WriteLine($"{Name} finished.");
            }
            else
            {
                Console.WriteLine($"{Name} stopped.");
            }

        }

        //private static object myLock = new();
        //private class ColorConverter
        //{
        private Color ColorFromAhsb(int a, float h, float s, float b)
        {
            //lock(myLock)
            //{
            if (0 > a || 255 < a)
            {
                throw new Exception("a");
            }
            if (0f > h || 360f < h)
            {
                throw new Exception("h");
            }
            if (0f > s || 1f < s)
            {
                throw new Exception("s");
            }
            if (0f > b || 1f < b)
            {
                throw new Exception("b");
            }

            if (0 == s)
            {
                return Color.FromArgb(a, Convert.ToInt32(b * 255),
                  Convert.ToInt32(b * 255), Convert.ToInt32(b * 255));
            }

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < b)
            {
                fMax = b - (b * s) + s;
                fMin = b + (b * s) - s;
            }
            else
            {
                fMax = b + (b * s);
                fMin = b - (b * s);
            }

            iSextant = (int)Math.Floor(h / 60f);
            if (300f <= h)
            {
                h -= 360f;
            }
            h /= 60f;
            h -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = h * (fMax - fMin) + fMin;
            }
            else
            {
                fMid = fMin - h * (fMax - fMin);
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant)
            {
                case 1:
                    return Color.FromArgb(a, iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(a, iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(a, iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(a, iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(a, iMax, iMin, iMid);
                default:
                    return Color.FromArgb(a, iMax, iMid, iMin);
            }
            // }

        }
        //}

    }
}
