using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Threading.Tasks;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ServiceHelpers
{
    public static class CoreUtil
    {
        public static uint MinDetectableFaceCoveragePercentage = 0;

        public static bool IsFaceBigEnoughForDetection(int faceHeight, int imageHeight)
        {
            if (imageHeight == 0)
            {
                // sometimes we don't know the size of the image, so we assume the face is big enough
                return true;
            }

            double faceHeightPercentage = 100 * ((double) faceHeight / imageHeight);

            return faceHeightPercentage >= MinDetectableFaceCoveragePercentage;
        }

        public static bool AreFacesPotentiallyTheSame(Face.FaceRectangle face1, Face.FaceRectangle face2)
        {
            return AreFacesPotentiallyTheSame(face1.Left, face1.Top, face1.Width, face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
        }

        public static bool AreFacesPotentiallyTheSame(Rect face1, Rect face2)
        {
            return AreFacesPotentiallyTheSame(face1.Left, face1.Top, face1.Width, face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
        }

        public static bool AreFacesPotentiallyTheSame(double face1X, double face1Y, double face1Width, double face1Height,
                                                       double face2X, double face2Y, double face2Width, double face2Height)
        {
            double distanceThresholdFactor = 1;
            double sizeThresholdFactor = 0.5;

            // See if faces are close enough from each other to be considered the "same"
            if (Math.Abs(face1X - face2X) <= face1Width * distanceThresholdFactor &&
                Math.Abs(face1Y - face2Y) <= face1Height * distanceThresholdFactor)
            {
                // See if faces are shaped similarly enough to be considered the "same"
                if (Math.Abs(face1Width - face2Width) <= face1Width * sizeThresholdFactor &&
                    Math.Abs(face1Height - face2Height) <= face1Height * sizeThresholdFactor)
                {
                    return true;
                }
            }

            return false;
        }

        public static Rect ToRect(this FaceRectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static Rect ToRect(this BoundingRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.W, rect.H);
        }

        public static Rect ToRect(this Face.FaceRectangle rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static Rect Inflate(this Rect rect, double inflatePercentage)
        {
            var width = rect.Width * inflatePercentage;
            var height = rect.Height * inflatePercentage;
            return new Rect(rect.X - ((width - rect.Width)/2), rect.Y - ((height - rect.Height)/2), width, height);
        }

        public static Rect Scale(this Rect rect, double scale)
        {
            return new Rect(rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        }

        public static async Task<Tuple<double, double>> ResizeBitmapAsync(WriteableBitmap wb, int width, int height, IRandomAccessStream resultStream)
        {
            int originalWidth = wb.PixelWidth;
            int originalHeight = wb.PixelHeight;

            if (wb.PixelWidth > width)
            {
                wb = wb.Resize(width, (int)(((double)wb.PixelHeight / wb.PixelWidth) * width), WriteableBitmapExtensions.Interpolation.Bilinear);
            }

            if (wb.PixelHeight > height)
            {
                wb = wb.Resize((int)(((double)wb.PixelWidth / wb.PixelHeight) * height), height, WriteableBitmapExtensions.Interpolation.Bilinear);
            }

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resultStream);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Ignore,
                                    (uint)wb.PixelWidth, (uint)wb.PixelHeight,
                                    DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, wb.PixelBuffer.ToArray());

            await encoder.FlushAsync();

            return new Tuple<double, double>((double)originalWidth / wb.PixelWidth, (double)originalHeight / wb.PixelHeight);
        }
    }


    public class CelebritiesComparer : IEqualityComparer<CelebritiesModel>
    {
        public bool Equals(CelebritiesModel x, CelebritiesModel y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x != null && y != null)
            {
                return x.Name == y.Name && x.FaceRectangle.ToRect() == y.FaceRectangle.ToRect();
            }
            return false;
        }

        public int GetHashCode(CelebritiesModel obj)
        {
            return obj.Name.GetHashCode() ^ obj.FaceRectangle.ToRect().GetHashCode();
        }
    }
}
