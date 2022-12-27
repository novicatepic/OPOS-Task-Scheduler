using System.Drawing;
using TaskScheduler;
using System.Windows;
using System.Drawing.Imaging;
using System;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms.VisualStyles;

namespace PROJEKAT.Jobs
{
    public class ImageNormalizationJob : IUserJob
    {
        public String Name { get; init; } = "Image";
        public int parallelism { get; init; } = 2;

        private string pathAttr;
        private string[] filePaths;
        private string output_Path = "Images/OutputImages/";
        private const float MIN_BRIGHTNESS = 0;
        private const float MAX_BRIGHTNESS = 1;
        private int NumIterations = 3;
        public int SleepTime = 500;

        private string[] inputPaths;
        private string[] outputPaths;
 
        public ImageNormalizationJob(string path)
        {
            pathAttr = path;
            filePaths = Directory.GetFiles(path);
            //image = (Bitmap)System.Drawing.Image.FromFile("Images/InputImages/cat.jpg");
        }

        public ImageNormalizationJob(List<(string, string)> paths)
        {
            inputPaths = new string[paths.Count];
            //inputPathsSet = new string[paths.Count];
            outputPaths = new string[paths.Count];
            for (int i = 0; i < paths.Count; i++)
            {
                inputPaths[i] = paths[i].Item1.ToString();
                outputPaths[i] = paths[i].Item2.ToString();
            }
        }

        void IUserJob.Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");

            for (int i = 0; i < inputPaths.Length; i++)
            {
                string[] pictures = Directory.GetFiles(inputPaths[i]);

                Parallel.ForEach(pictures, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, picture =>
                {
                    Bitmap image = (Bitmap)System.Drawing.Image.FromFile(picture);

                    float minBrightness = MAX_BRIGHTNESS;
                    float maxBrightness = MIN_BRIGHTNESS;
                    Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, x =>
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            float pixelBrightness = image.GetPixel(x, y).GetBrightness();
                            minBrightness = Math.Min(minBrightness, pixelBrightness);
                            maxBrightness = Math.Max(maxBrightness, pixelBrightness);
                        }
                    });

                    Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, x =>
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            Color pixelColor = image.GetPixel(x, y);
                            float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                            Color normalizedPixelColor = ColorConverter.ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                                pixelColor.GetSaturation(), normalizedPixelBrightness);
                            image.SetPixel(x, y, normalizedPixelColor);
                        }
                    });

                    string[] splitPic = picture.Split('/');
                    image.Save(outputPaths[i] + splitPic.ElementAt(splitPic.Length - 1));
                });

                Console.WriteLine($"{Name}: {i}");

                

                Thread.Sleep(SleepTime);

                if (jobApi.StoppageConfirmed())
                {
                    break;
                }

                jobApi.CheckAll();

                if (jobApi.CheckConditions())
                {
                    break;
                }   
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








        /*void IUserJob.Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");

            if (filePaths.Length == 1)
            {
                Bitmap image = (Bitmap)System.Drawing.Image.FromFile(filePaths[0]);

                float minBrightness = MAX_BRIGHTNESS;
                float maxBrightness = MIN_BRIGHTNESS;
                //Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, x =>
                //{
                //int y;
                Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, x =>
                {
                    //for (int x = 0; x < image.Width; x++)
                    //{
                    lock (pictureLock)
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            //Console.WriteLine("X = " + x);
                            //Console.WriteLine("Y = " + y);
                            float pixelBrightness = image.GetPixel(x, y).GetBrightness();
                            minBrightness = Math.Min(minBrightness, pixelBrightness);
                            maxBrightness = Math.Max(maxBrightness, pixelBrightness);
                        }
                    }

                    //}

                    lock (pictureLock)
                    {
                        for (int y = 0; y < image.Height; y++)
                        {
                            Color pixelColor = image.GetPixel(x, y);
                            float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                            Color normalizedPixelColor = ColorConverter.ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                            pixelColor.GetSaturation(), normalizedPixelBrightness);
                            image.SetPixel(x, y, normalizedPixelColor);
                        }
                    }

                });

                /*for(int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        Color pixelColor = image.GetPixel(x, y);
                        float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                        Color normalizedPixelColor = ColorConverter.ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                            pixelColor.GetSaturation(), normalizedPixelBrightness);
                        image.SetPixel(x, y, normalizedPixelColor);
                    }
                }

                image.Save(output_Path + filePaths[0].Substring(pathAttr.Length));
                //});
            }
            else
            {
                int k = 0;
                int sumIters = 0;
                for (int i = 0; i < NumIterations; i++)
                {

                    string[] array;
                    if (i == NumIterations - 1)
                    {
                        int len = filePaths.Length - sumIters;
                        array = new string[len];
                        for (int j = 0; j < len; j++)
                        {
                            Console.WriteLine(filePaths.Length - 1 - j);
                            array[j] = filePaths[filePaths.Length - 1 - j];
                        }
                    }
                    else
                    {
                        int len = filePaths.Length / NumIterations;
                        array = new string[len];
                        for (int j = 0; j < len; j++)
                        {
                            array[j] = filePaths[j + k * len];
                            Console.WriteLine(j + k * len);
                        }

                        sumIters += len;
                        k++;
                    }

                    Parallel.ForEach(array, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, filePath =>
                    {
                        Bitmap image = (Bitmap)System.Drawing.Image.FromFile(filePath);

                        float minBrightness = MAX_BRIGHTNESS;
                        float maxBrightness = MIN_BRIGHTNESS;


                        //Parallel.For(0, image.Width, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, x =>
                        //{
                            //try
                            //{
                            for(int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                float pixelBrightness = image.GetPixel(x, y).GetBrightness();
                                minBrightness = Math.Min(minBrightness, pixelBrightness);
                                maxBrightness = Math.Max(maxBrightness, pixelBrightness);
                            }
                        }
                                
                            //}
                            //catch (Exception e) { }
                            //try
                            //{
                            for(int x = 0; x < image.Width; x++)
                        {
                            for (int y = 0; y < image.Height; y++)
                            {
                                Color pixelColor = image.GetPixel(x, y);
                                float normalizedPixelBrightness = (pixelColor.GetBrightness() - minBrightness) / (maxBrightness - minBrightness);
                                Color normalizedPixelColor = ColorConverter.ColorFromAhsb(pixelColor.A, pixelColor.GetHue(),
                                    pixelColor.GetSaturation(), normalizedPixelBrightness);
                                image.SetPixel(x, y, normalizedPixelColor);
                            }
                        }
                               
                            //} catch(Exception e) { }
                            

                        //});

                        image.Save(output_Path + filePath.Substring(pathAttr.Length));
                    });

                    Console.WriteLine($"{Name}: {i}");

                    Thread.Sleep(SleepTime);

                    if (jobApi.StoppageConfirmed())
                    {
                        break;
                    }

                    jobApi.CheckAll();

                    if (jobApi.CheckConditions())
                    {
                        break;
                    }
                }


            }

            if (!jobApi.StoppageConfirmed())
            {
                Console.WriteLine($"{Name} finished.");
            }
            else
            {
                Console.WriteLine($"{Name} stopped.");
            }
        }*/


        private class ColorConverter
        {
            public static Color ColorFromAhsb(int a, float h, float s, float b)
            {
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
            }
        }

    }
}
