using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspect_View
{
    public struct ColorMaskData
    {
        public int redMin;
        public int redMax;
        public int greenMin;
        public int greenMax;
        public int blueMin;
        public int blueMax;

        public ColorMaskData()
        {
            redMin = 0;
            redMax = 255;
            greenMin = 0;
            greenMax = 255;
            blueMin = 0;
            blueMax = 255;
        }
    }

    public enum Color
    {
        Red,
        Green,
        Blue
    }

    public struct ColorStatistics
    {
        public double sum;
        public List<double> vals;
        public double avg;
        public double variation;

        public int index;

        public ColorStatistics()
        {
            sum = 0;
            vals = [];
            avg = 0;
            variation = 0;

            index = 0;
        }
    }

    public class ImageInspectionLimiter
    {
        public static int index = 0;

        public bool rectangle; //false - circle ; true - rectangle
        public bool isSelected;
        public int width;
        public int height;
        public System.Drawing.Point position;

        public String name;

        public ColorMaskData colorData;
        public int pixelCount;
        public int delta;

        public Mat limiterMask;


        public ImageInspectionLimiter()
        {
            this.rectangle = false;
            this.isSelected = false;
            this.width = 0;
            this.height = 0;
            this.position = new System.Drawing.Point(0, 0);

            this.name = "Empty limiter";

            this.colorData = new();

            pixelCount = 0;
            delta = 0;
        }

        public ImageInspectionLimiter(Mat currentFrame)
        {
            this.rectangle = false;
            this.isSelected = false;
            this.width = 0;
            this.height = 0;
            this.position = new System.Drawing.Point(0, 0);

            this.name = "Limiter #" + index.ToString();

            this.colorData = new();

            ZeroMask(currentFrame);

            pixelCount = 0;
            delta = 0;

            index++;
        }


        public ImageInspectionLimiter(Mat currentFrame, bool rectangle, bool isSelected, int width, int height, System.Drawing.Point position)
        {
            this.rectangle = rectangle;
            this.isSelected = isSelected;
            this.width = width;
            this.height = height;
            this.position = position;

            this.name = "Limiter #" + index.ToString();

            this.colorData = new();

            ZeroMask(currentFrame);

            pixelCount = 0;
            delta = 0;

            index++;
        }


        public void ZeroMask(Mat currentFrame)
        {
            limiterMask = Mat.Zeros(currentFrame.Rows, currentFrame.Cols, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
        }

        public void RefreshMask(Mat currentFrame)
        {
            this.ZeroMask(currentFrame);

            if (this.rectangle == false)
            {
                RotatedRect rect = new RotatedRect(this.position, new SizeF(this.width, this.height), 0);
                CvInvoke.Ellipse(this.limiterMask, rect, new MCvScalar(255, 255, 255), -1);
            }
            else
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(this.position, new System.Drawing.Size(this.width, this.height));
                CvInvoke.Rectangle(this.limiterMask, rect, new MCvScalar(255, 255, 255), -1);
            }
        }

        public ColorStatistics GetColorStats(Mat currentFrame, Inspect_View.Color color)
        {
            double sum = 0;
            List<double> vals = [];
            double avg;
            double variation = 0;

            int index = 0;

            Image<Gray, byte> maskGray = limiterMask.ToImage<Gray, byte>();
            Image<Bgr, byte> frame = currentFrame.ToImage<Bgr, byte>();

            for (int i = 0; i < maskGray.Cols; i++)
            {
                for (int j = 0; j < maskGray.Rows; j++)
                {
                    if (maskGray[j, i].Intensity > 0)
                    {
                        switch(color)
                        {
                            case Inspect_View.Color.Red:
                                sum += frame[j, i].Red;
                                vals.Add(frame[j, i].Red);
                                break;

                            case Inspect_View.Color.Green:
                                sum += frame[j, i].Green;
                                vals.Add(frame[j, i].Green);
                                break;

                            case Inspect_View.Color.Blue:
                                sum += frame[j, i].Blue;
                                vals.Add(frame[j, i].Blue);
                                break;
                        }

                        index++;
                    }
                }
            }

            avg = sum / index;

            foreach (double x in vals)
            {
                variation += Math.Sqrt(Math.Pow(x - avg, 2));
            }
            variation /= index;


            ColorStatistics stats = new();

            stats.sum = sum;
            stats.vals = vals;
            stats.avg = avg;
            stats.variation = variation;
            stats.index = index;

            return stats;
        }
    }
}
