﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler;

namespace PROJEKAT.Jobs
{
    public class NormalizeImageJob : IUserJob
    {

        public String Name { get; init; } = "Image";
        public int Parallelism { get; init; } = 2;
        public int SingleParralelism { get; init; } = 2;
        private const float MIN_BRIGHTNESS = 0;
        private const float MAX_BRIGHTNESS = 1;
        public int SleepTime { get; init; } = 500;
        //public List<(string, string)> PathTupple { get; init; } = new();

        public List<string> InputPaths { get; init; } = new List<string>() { "Images/InputImages/" };
        public string OutputPath { get; init; } = "Images/OutputImages/";
        /*private string[] inputPaths;
        private string[] outputPaths;*/
        public NormalizeImageJob() { }
        public NormalizeImageJob(List<string> inputPaths, string outputPath)
        {
            this.InputPaths = inputPaths;
            this.OutputPath = outputPath;
        }

        public int NumOfPictures()
        {
            int number = 0;
            foreach (var file in InputPaths)
            {
                number += Directory.GetFiles(file).Length;
            }
            return number;
        }

        object _lock = new();
        void IUserJob.Run(IJobContext jobApi)
        {
            for (int i = 0; i < InputPaths.Count; i++)
            {

                string[] pictures = Directory.GetFiles(InputPaths[i]);
                int numPictures = NumOfPictures();
                bool breakFromParralel = false;
                int k = 0;
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                Parallel.ForEach(pictures, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, picture =>
                {
                    //Load image that's going to be processed
                    Bitmap image = new Bitmap(picture);
                    if (breakFromParralel == false)
                    {
                        //Standard setters for min and max values, so they can be easily overwritten
                        int minRed = 255, minGreen = 255, minBlue = 255;
                        int maxRed = 0, maxGreen = 0, maxBlue = 0;
                        //SingleParallelismLevel implemented
                        Parallel.For(0, image.Height, new ParallelOptions { MaxDegreeOfParallelism = SingleParralelism }, y =>
                        {
                            //Need to have this lock because errors otherwise (multiple threads are going to modify the same picture) :/
                            lock(_lock)
                            {
                                //Go through image width to find maximum and minimum values
                                for (int x = 0; x < image.Width; x++)
                                {
                                    Color pixel;
                                    //Get corresponding pixel
                                    pixel = image.GetPixel(x, y);
                                    //Find minimum possible values for RGB, comparing it to values that were declared before
                                    minRed = Math.Min(minRed, pixel.R);
                                    minGreen = Math.Min(minGreen, pixel.G);
                                    minBlue = Math.Min(minBlue, pixel.B);
                                    maxRed = Math.Max(maxRed, pixel.R);
                                    maxGreen = Math.Max(maxGreen, pixel.G);
                                    maxBlue = Math.Max(maxBlue, pixel.B);
                                }
                                //Go through image width again
                                for (int x = 0; x < image.Width; x++)
                                {
                                    //Get the corresponding pixel that's going to be normalized
                                    Color pixel = image.GetPixel(x, y);
                                    //Use the formula to calculate new red, green and blue components, and the formula is ->
                                    //normalizedValue = (originalValue - minValue) / (maxValue - minValue) * 255
                                    int red = (int)((pixel.R - minRed) / (float)(maxRed - minRed) * 255);
                                    int green = (int)((pixel.G - minGreen) / (float)(maxGreen - minGreen) * 255);
                                    int blue = (int)((pixel.B - minBlue) / (float)(maxBlue - minBlue) * 255);
                                    //Set the new pixel as the image pixel
                                    image.SetPixel(x, y, Color.FromArgb(red, green, blue));
                                }
                            }
                        });

                        //HOW THE FORMULA WORKS:
                        //Scale intensity from 0-255 (so, from original range to full range)
                        //We have originalValue - minValue because we need to fit component of a pixel between MAX and MIN value
                        //That's why we divide
                        //And then we multiply with 255
                        //Multiplication with 255 -> so we stretch the picture from normal range to full range (0-255), for example, if the range was 50-100, it will be 0-255
                        //We want the range to start at '0', which is not 0, but the first possible value that a pixel can have in it's current scale
                        //After that we divide with the old range of the corresponding color
                        //Basically, we do this to prepare for multiplication with 255, so we can normalize the image by stretching the range

                        // Save the normalized image to a file
                        // Splitter because I want to save it with the same name in the output folder
                        string[] splitPic = picture.Split('/');
                        image.Save(OutputPath + splitPic.ElementAt(splitPic.Length - 1));

                        Console.WriteLine($"{Name}: {++k}");

                        //Check stuff
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

                stopwatch.Stop();
                //Check to see if faster -> it is on a small sample
                Console.WriteLine("Elapsed Time is {0} ms", stopwatch.ElapsedMilliseconds);

            }


        }
    }
}