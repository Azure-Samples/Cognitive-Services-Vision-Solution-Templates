using Microsoft.Rest;
using Newtonsoft.Json;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace DigitalAssetManagementTemplate
{
    static partial class Util
    {
        public static string CapitalizeString(string s)
        {
            return string.Join(" ", s.Split(' ').Select(word => !string.IsNullOrEmpty(word) ? char.ToUpper(word[0]) + word.Substring(1) : string.Empty));
        }

        internal static async Task GenericApiCallExceptionHandler(Exception ex, string errorTitle)
        {
            string errorDetails = GetMessageFromException(ex);

            await new MessageDialog(errorDetails, errorTitle).ShowAsync();
        }
        internal static async Task GenericApiCallExceptionHandler(Exception ex, string errorTitle, bool log = false)
        {
            //log error
            if (log)
            {
                AppInsightsHelper.TrackException(ex, errorTitle);
            }

            await GenericApiCallExceptionHandler(ex, errorTitle);
        }

        internal static string GetMessageFromException(Exception ex)
        {
            string errorDetails = ex.Message;

            Face.APIErrorException faceApiException = ex as Face.APIErrorException;
            if (faceApiException?.Message != null)
            {
                errorDetails = faceApiException.Message;
            }

            var visionException = ex as Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ComputerVisionErrorException;
            if (!string.IsNullOrEmpty(visionException?.Body?.Message))
            {
                errorDetails = visionException.Body.Message;
            }

            HttpOperationException httpException = ex as HttpOperationException;
            if (httpException?.Response?.ReasonPhrase != null)
            {
                string errorReason = $"\"{httpException.Response.ReasonPhrase}\".";
                if (httpException?.Response?.Content != null)
                {
                    errorReason += $" Some more details: {httpException.Response.Content}";
                }

                errorDetails = $"{ex.Message}. The error was {errorReason}.";
            }

            return errorDetails;
        }

        internal static Face.DetectedFace FindFaceClosestToRegion(IEnumerable<Face.DetectedFace> faces, BitmapBounds region)
        {
            return faces?.Where(f => Util.AreFacesPotentiallyTheSame(region, f.FaceRectangle))
                                  .OrderBy(f => Math.Abs(region.X - f.FaceRectangle.Left) + Math.Abs(region.Y - f.FaceRectangle.Top)).FirstOrDefault();
        }

        internal static bool AreFacesPotentiallyTheSame(BitmapBounds face1, Face.FaceRectangle face2)
        {
            return CoreUtil.AreFacesPotentiallyTheSame((int)face1.X, (int)face1.Y, (int)face1.Width, (int)face1.Height, face2.Left, face2.Top, face2.Width, face2.Height);
        }

        public static async Task ConfirmActionAndExecute(string message, Func<Task> action,
            Func<Task> cancelAction = null, string confirmActionLabel = "Yes", string cancelActionLabel = "Cancel")
        {
            var messageDialog = new MessageDialog(message);

            messageDialog.Commands.Add(new UICommand(confirmActionLabel, new UICommandInvokedHandler(async (c) => await action())));

            if (cancelAction != null)
            {
                messageDialog.Commands.Add(new UICommand(cancelActionLabel, new UICommandInvokedHandler(async (c) => { await cancelAction(); })));
            }
            else
            {
                messageDialog.Commands.Add(new UICommand(cancelActionLabel, new UICommandInvokedHandler((c) => { })));
            }

            messageDialog.DefaultCommandIndex = 1;
            messageDialog.CancelCommandIndex = 0;

            await messageDialog.ShowAsync();
        }

        public static async Task<IEnumerable<string>> GetAvailableCameraNamesAsync()
        {
            DeviceInformationCollection deviceInfo = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return deviceInfo.Select(d => GetCameraName(d, deviceInfo)).OrderBy(name => name);
        }

        public static KeyValuePair<string, double>[] EmotionToRankedList(Face.Emotion emotion)
        {
            return new KeyValuePair<string, double>[]
            {
                new KeyValuePair<string, double>("Anger", emotion.Anger),
                new KeyValuePair<string, double>("Contempt", emotion.Contempt),
                new KeyValuePair<string, double>("Disgust", emotion.Disgust),
                new KeyValuePair<string, double>("Fear", emotion.Fear),
                new KeyValuePair<string, double>("Happiness", emotion.Happiness),
                new KeyValuePair<string, double>("Neutral", emotion.Neutral),
                new KeyValuePair<string, double>("Sadness", emotion.Sadness),
                new KeyValuePair<string, double>("Surprise", emotion.Surprise)
            }
            .OrderByDescending(e => e.Value)
            .ToArray();
        }

        public static Microsoft.Azure.CognitiveServices.Vision.Face.Models.Gender? GetFaceGender(Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender? gender)
        {
            switch (gender)
            {
                case Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender.Male:
                    return Face.Gender.Male;
                case Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.Gender.Female:
                    return Face.Gender.Female;
                default:
                    return null;
            }
        }

        public static bool ExtractFileFromZipArchive(StorageFile zipFile, string extractedFileName, StorageFile newFile)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFile.Path))
                {
                    ZipArchiveEntry entry = archive.Entries.FirstOrDefault(e => e.FullName != null && e.FullName.Contains(extractedFileName, StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                    {
                        entry.ExtractToFile(newFile.Path, true);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static async Task<StorageFile> SaveDataToJsonFileAsync<T>(T data, string jsonFileName)
        {
            StorageFile jsonFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(jsonFileName, CreationCollisionOption.ReplaceExisting);
            using (StreamWriter writer = new StreamWriter(await jsonFile.OpenStreamForWriteAsync()))
            {
                string jsonStr = JsonConvert.SerializeObject(data, Formatting.Indented);
                await writer.WriteAsync(jsonStr);
            }
            return jsonFile;
        }

        /// <summary>
        /// Returns the camera name. It is the same underlying name if only one camera exist with the same name, otherwise it is a combination
        /// of the underlying name and unique Id.
        /// </summary>
        internal static string GetCameraName(DeviceInformation cameraInfo, DeviceInformationCollection allCameras)
        {
            bool isCameraNameUnique = allCameras.Count(c => c.Name == cameraInfo.Name) == 1;
            return isCameraNameUnique ? cameraInfo.Name : string.Format("{0} [{1}]", cameraInfo.Name, cameraInfo.Id);
        }

        async public static Task<ImageSource> GetCroppedBitmapAsync(Func<Task<Stream>> originalImgFile, Rect rectangle)
        {
            try
            {
                using (IRandomAccessStream stream = (await originalImgFile()).AsRandomAccessStream())
                {
                    return await GetCroppedBitmapAsync(stream, rectangle);
                }
            }
            catch
            {
                // default to no image if we fail to crop the bitmap
                return null;
            }
        }

        async public static Task<ImageSource> GetCroppedBitmapAsync(IRandomAccessStream stream, Rect rectangle)
        {
            var pixels = await GetCroppedPixelsAsync(stream, rectangle);

            // Stream the bytes into a WriteableBitmap 
            WriteableBitmap cropBmp = new WriteableBitmap((int)pixels.Item2.Width, (int)pixels.Item2.Height);
            cropBmp.FromByteArray(pixels.Item1);

            return cropBmp;
        }

        async private static Task CropBitmapAsync(Stream localFileStream, Rect rectangle, StorageFile resultFile)
        {
            //Get pixels of the crop region
            var pixels = await GetCroppedPixelsAsync(localFileStream.AsRandomAccessStream(), rectangle);

            // Save result to new image
            using (Stream resultStream = await resultFile.OpenStreamForWriteAsync())
            {
                IRandomAccessStream randomAccessStream = resultStream.AsRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        pixels.Item2.Width, pixels.Item2.Height,
                                        DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixels.Item1);

                await encoder.FlushAsync();
            }
        }

        public static async Task DownloadAndSaveBitmapAsync(string imageUrl, StorageFile resultFile)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                await SaveBitmapToStorageFileAsync(stream, resultFile);
            }
        }

        public static async Task SaveBitmapToStorageFileAsync(Stream localFileStream, StorageFile resultFile)
        {
            // Get pixels
            var pixels = await GetPixelsAsync(localFileStream.AsRandomAccessStream());

            // Save result to new image
            using (Stream resultStream = await resultFile.OpenStreamForWriteAsync())
            {
                IRandomAccessStream randomAccessStream = resultStream.AsRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        pixels.Item2.ScaledWidth, pixels.Item2.ScaledHeight,
                                        DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixels.Item1);

                await encoder.FlushAsync();
            }
        }

        async public static Task CropBitmapAsync(Func<Task<Stream>> localFile, Rect rectangle, StorageFile resultFile)
        {
            await CropBitmapAsync(await localFile(), rectangle, resultFile);
        }

        async public static Task DownloadAndCropBitmapAsync(string imageUrl, Rect rectangle, StorageFile resultFile)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                await CropBitmapAsync(stream, rectangle, resultFile);
            }
        }

        async public static Task<ImageSource> DownloadAndCropBitmapAsync(string imageUrl, Rect rectangle)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                return await GetCroppedBitmapAsync(stream.AsRandomAccessStream(), rectangle);
            }
        }

        public static bool FileExists(StorageFolder folder, string fileName)
        {
            var result = folder?.TryGetItemAsync(fileName);
            result.AsTask().Wait();
            var storageFile = result.GetResults();
            return storageFile != null;
        }

        public static string UppercaseFirst(string str)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return str.Length > 1 ? char.ToUpper(str[0]) + str.Substring(1) : str.ToUpper();
        }

        public static void CopyToClipboard(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // send text to clipboard
                var dataPackage = new DataPackage();
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);
            }
        }

        public static async Task<StorageFile> PickSingleFileAsync(string[] fileTypeFilter = null)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
            fileTypeFilter?.ToList().ForEach(f => fileOpenPicker.FileTypeFilter.Add(f));
            return await fileOpenPicker.PickSingleFileAsync();
        }

        public static async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(string[] fileTypeFilter = null)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
            fileTypeFilter?.ToList().ForEach(f => fileOpenPicker.FileTypeFilter.Add(f));
            return await fileOpenPicker.PickMultipleFilesAsync();
        }

        public static void DisplayFaceLandmarks(Grid grid, Face.FaceRectangle faceRect, Face.FaceLandmarks landmarks, 
            double scaleX, double scaleY, SolidColorBrush color = null)
        {
            // Mouth (6)
            SolidColorBrush colorBrush = color ?? new SolidColorBrush(Colors.White);
            AddFacialLandmark(grid, faceRect, landmarks.MouthLeft,      scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.MouthRight,     scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.UpperLipBottom, scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.UpperLipTop,    scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.UnderLipBottom, scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.UnderLipTop,    scaleX, scaleY, colorBrush);

            // Eyes (10)
            colorBrush = color ?? new SolidColorBrush(Colors.Red);
            AddFacialLandmark(grid, faceRect, landmarks.EyeLeftBottom,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeLeftTop,     scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeLeftInner,   scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeLeftOuter,   scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeRightBottom, scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeRightTop,    scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeRightInner,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyeRightOuter,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.PupilLeft,      scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.PupilRight,     scaleX, scaleY, colorBrush);

            // nose (7)
            colorBrush = color ?? new SolidColorBrush(Colors.LimeGreen);
            AddFacialLandmark(grid, faceRect, landmarks.NoseLeftAlarOutTip,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseLeftAlarTop,     scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseRightAlarOutTip, scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseRightAlarTop,    scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseRootLeft,        scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseRootRight,       scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.NoseTip,             scaleX, scaleY, colorBrush);

            // eyebrows (4)
            colorBrush = color ?? new SolidColorBrush(Colors.Yellow);
            AddFacialLandmark(grid, faceRect, landmarks.EyebrowLeftInner,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyebrowLeftOuter,  scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyebrowRightInner, scaleX, scaleY, colorBrush);
            AddFacialLandmark(grid, faceRect, landmarks.EyebrowRightOuter, scaleX, scaleY, colorBrush);
        }

        private static void AddFacialLandmark(Grid grid, Face.FaceRectangle rect, Face.Coordinate coordinate, 
            double scaleX, double scaleY, SolidColorBrush color)
        {
            double dotSize = 3;
            Rectangle b = new Rectangle
            {
                Fill = color,
                Width = dotSize,
                Height = dotSize,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
            };
            b.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
            b.Margin = new Thickness(
                ((coordinate.X - rect.Left) * scaleX) - dotSize / 2,
                ((coordinate.Y - rect.Top) * scaleY) - dotSize / 2,
                0, 0);
            grid.Children.Add(b);
        }

        async private static Task<Tuple<byte[], BitmapBounds>> GetCroppedPixelsAsync(IRandomAccessStream stream, Rect rectangle)
        {
            // Create a decoder from the stream. With the decoder, we can get  
            // the properties of the image. 
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create cropping BitmapTransform and define the bounds. 
            BitmapTransform transform = new BitmapTransform();
            BitmapBounds bounds = new BitmapBounds();
            bounds.X = (uint)Math.Max(0, rectangle.Left);
            bounds.Y = (uint)Math.Max(0, rectangle.Top);
            bounds.Height = bounds.Y + rectangle.Height <= decoder.PixelHeight ? (uint)rectangle.Height : decoder.PixelHeight - bounds.Y;
            bounds.Width = bounds.X + rectangle.Width <= decoder.PixelWidth ? (uint)rectangle.Width : decoder.PixelWidth - bounds.X;
            transform.Bounds = bounds;

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return new Tuple<byte[], BitmapBounds>(pix.DetachPixelData(), transform.Bounds);
        }

        private static async Task<Tuple<byte[], BitmapTransform>> GetPixelsAsync(IRandomAccessStream stream)
        {
            // Create a decoder from the stream. With the decoder, we can get the properties of the image.
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create BitmapTransform and define the bounds.
            BitmapTransform transform = new BitmapTransform
            {
                ScaledHeight = decoder.PixelHeight,
                ScaledWidth = decoder.PixelWidth
            };

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return new Tuple<byte[], BitmapTransform>(pix.DetachPixelData(), transform);
        }

        internal static Color ColorFromString(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length != 8)
            {
                return Colors.Gray;
            }

            try
            {
                return Color.FromArgb(byte.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                                    byte.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                                    byte.Parse(str.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                                    byte.Parse(str.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
            }
            catch (Exception)
            {
                return Colors.Gray;
            }
        }

        internal static async Task DownloadFileASync(string link, StorageFile destination, IProgress<DownloadOperation> progress, CancellationToken cancellationToken)
        {
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(new Uri(link), destination);
            await download.StartAsync().AsTask(cancellationToken, progress);
        }

        internal static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                // Read the pixel bytes from the memory stream
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }

        internal static async Task<Stream> ResizePhoto(Stream photo, int height)
        {
            InMemoryRandomAccessStream result = new InMemoryRandomAccessStream();
            await ResizePhoto(photo, height, result);
            return result.AsStream();
        }

        internal static async Task<Tuple<double, double>> ResizePhoto(Stream photo, int height, StorageFile resultFile)
        {
            var resultStream = (await resultFile.OpenStreamForWriteAsync()).AsRandomAccessStream();
            var result = await ResizePhoto(photo, height, resultStream);
            resultStream.Dispose();

            return result;
        }

        private static async Task<Tuple<double, double>> ResizePhoto(Stream photo, int height, IRandomAccessStream resultStream)
        {
            WriteableBitmap wb = new WriteableBitmap(1, 1);
            wb = await wb.FromStream(photo.AsRandomAccessStream());

            int originalWidth = wb.PixelWidth;
            int originalHeight = wb.PixelHeight;

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

        static HashSet<string> GenderKeywords = new HashSet<string>(new string[] { "man", "woman", "boy", "girl", "male", "female" }, StringComparer.InvariantCultureIgnoreCase);
        internal static bool ContainsGenderRelatedKeyword(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word => GenderKeywords.Contains(word));
        }

        public static bool IsPointInsideVisualElement(Visual visual, Point point)
        {
            Vector3 offset = visual.Offset;
            Vector2 size = visual.Size;
            Vector2 anchor = visual.AnchorPoint;

            double xMargin = size.X * anchor.X;
            double yMargin = size.Y * anchor.Y;

            double visualX1 = offset.X - xMargin;
            double visualX2 = offset.X - xMargin + size.X;
            double visualY1 = offset.Y - yMargin;
            double visualY2 = offset.Y - yMargin + size.Y;

            return point.X >= visualX1 && point.X <= visualX2 &&
                   point.Y >= visualY1 && point.Y <= visualY2;
        }

        public static string StringToDateFormat(string date, string format = "")
        {
            bool isDate = DateTime.TryParse(date, out DateTime datetime);
            return isDate ? datetime.ToString(format) : string.Empty;
        }
    }

    public static class WinRTExtentions
    {
        public static async Task RunAsync(this CoreDispatcher dispatcher, Func<Task> callback)
        {
            Task task = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                task = callback();
            });
            await task;
        }

        public static async Task<T> RunAsync<T>(this CoreDispatcher dispatcher, Func<Task<T>> callback)
        {
            Task<T> task = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                task = callback();
            });
            return await task;
        }

        public static async Task RunAsync(this CoreDispatcher dispatcher, DispatchedHandler callback)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, callback);
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }

    public static class ScrollViewerExtensions
    {
        public static ScrollViewer GetScrollViewer(this UIElement element)
        {
            if (element == null)
            {
                return null;
            }

            ScrollViewer scrollViewer = null;
            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count && scrollViewer == null; i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                {
                    scrollViewer = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
                }
                else
                {
                    scrollViewer = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);
                }
            }
            return scrollViewer;
        }
    }
}