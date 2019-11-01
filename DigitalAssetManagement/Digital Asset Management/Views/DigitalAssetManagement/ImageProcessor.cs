using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace DigitalAssetManagementTemplate.Views.DigitalAssetManagement
{
    public class ImageProcessor
    {
        static readonly VisualFeatureTypes[] _visionFeatures = new[]
        {
            VisualFeatureTypes.Tags,
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Objects,
            VisualFeatureTypes.Brands,
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Adult,
            VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Color
        };
        static readonly FaceAttributeType[] _faceFeatures = new[]
        {
            FaceAttributeType.Accessories,
            FaceAttributeType.Age,
            FaceAttributeType.Blur,
            FaceAttributeType.Emotion,
            FaceAttributeType.Exposure,
            FaceAttributeType.FacialHair,
            FaceAttributeType.Gender,
            FaceAttributeType.Glasses,
            FaceAttributeType.Hair,
            FaceAttributeType.HeadPose,
            FaceAttributeType.Makeup,
            FaceAttributeType.Noise,
            FaceAttributeType.Occlusion,
            FaceAttributeType.Smile
        };

        public async Task<DigitalAssetData> ProcessImagesAsync(ImageProcessorSource source, ImageProcessorService? services, int? fileLimit, int? startingFileIndex, Func<ImageInsights, Task> callback)
        {
            //validate
            if (services == null)
            {
                throw new ApplicationException("No Azure services provided for image processing pipeline. Need at least 1.");
            }

            //get images
            var insights = new List<ImageInsights>();
            var (filePaths, reachedEndOfFiles) = await source.GetFilePaths(fileLimit, startingFileIndex);

            //process each image - in batches
            var tasks = new List<Task<ImageInsights>>();
            var lastFile = filePaths.Last();
            foreach (var filePath in filePaths)
            {
                tasks.Add(ProcessImageAsync(filePath, services.Value));
                if (tasks.Count == 8 || filePath == lastFile)
                {
                    var results = await Task.WhenAll(tasks);
                    foreach (var task in tasks)
                    {
                        var insight = task.Result;
                        insights.Add(insight);
                        await callback(insight);
                    }
                    tasks.Clear();
                }
            }

            return new DigitalAssetData() { Info = new DigitalAssetInfo { Path = source.Path, FileLimit = fileLimit, Services = services.Value, Name = source.GetName(), LastFileIndex = filePaths.Count() + (startingFileIndex ?? 0), ReachedEndOfFiles = reachedEndOfFiles, Source = source.GetType().Name } , Insights = insights.ToArray() };
        }

        async Task<ImageInsights> ProcessImageAsync(Uri filePath, ImageProcessorService services)
        {
            ImageAnalyzer analyzer = null;
            if (filePath.IsFile)
            {
                analyzer = new ImageAnalyzer((await StorageFile.GetFileFromPathAsync(filePath.LocalPath)).OpenStreamForReadAsync);
            }
            else
            {
                analyzer = new ImageAnalyzer(filePath.AbsoluteUri);
            }
            analyzer.ShowDialogOnFaceApiErrors = true;

            // trigger vision, face, emotion and OCR requests
            var tasks = new List<Task>();
            if (services.HasFlag(ImageProcessorService.ComputerVision))
            {
                tasks.Add(analyzer.AnalyzeImageAsync(null, visualFeatures: _visionFeatures));
                tasks.Add(analyzer.RecognizeTextAsync());
            }
            if (services.HasFlag(ImageProcessorService.Face))
            {
                tasks.Add(analyzer.DetectFacesAsync(true, false, _faceFeatures));
            }
            await Task.WhenAll(tasks);

            // trigger face match against previously seen faces
            if (services.HasFlag(ImageProcessorService.Face))
            {
                await analyzer.FindSimilarPersistedFacesAsync();
            }

            ImageInsights result = new ImageInsights { ImageUri = filePath };

            // assign computer vision results
            result.VisionInsights = new VisionInsights
            {
                Caption = analyzer.AnalysisResult?.Description?.Captions.FirstOrDefault()?.Text,
                Tags = analyzer.AnalysisResult?.Tags != null ? analyzer.AnalysisResult.Tags.Select(t => t.Name).ToArray() : new string[0],
                Objects = analyzer.AnalysisResult?.Objects != null ? analyzer.AnalysisResult.Objects.Select(t => t.ObjectProperty).ToArray() : new string[0],
                Celebrities = analyzer.AnalysisResult?.Categories != null ? analyzer.AnalysisResult.Categories.Where(i => i.Detail?.Celebrities != null && i.Detail.Celebrities.Count != 0).SelectMany(i => i.Detail.Celebrities).Select(i => i.Name).ToArray() : new string[0],
                Landmarks = analyzer.AnalysisResult?.Categories != null ? analyzer.AnalysisResult.Categories.Where(i => i.Detail?.Landmarks != null && i.Detail.Landmarks.Count != 0).SelectMany(i => i.Detail.Landmarks).Select(i => i.Name).ToArray() : new string[0],
                Brands = analyzer.AnalysisResult?.Brands != null ? analyzer.AnalysisResult.Brands.Select(t => t.Name).ToArray() : new string[0],
                Adult = analyzer.AnalysisResult?.Adult,
                Color = analyzer.AnalysisResult?.Color,
                ImageType = analyzer.AnalysisResult?.ImageType,
                Metadata = analyzer.AnalysisResult?.Metadata,
                Words = analyzer.TextOperationResult?.RecognitionResult?.Lines != null ? analyzer.TextOperationResult.RecognitionResult.Lines.SelectMany(i => i.Words).Select(i => i.Text).ToArray() : new string[0],
            };

            // assign face api results
            List<FaceInsights> faceInsightsList = new List<FaceInsights>();
            foreach (var face in analyzer.DetectedFaces ?? Array.Empty<DetectedFace>())
            {
                FaceInsights faceInsights = new FaceInsights { FaceRectangle = face.FaceRectangle, FaceAttributes = face.FaceAttributes };

                SimilarFaceMatch similarFaceMatch = analyzer.SimilarFaceMatches.FirstOrDefault(s => s.Face.FaceId == face.FaceId);
                if (similarFaceMatch != null)
                {
                    faceInsights.UniqueFaceId = similarFaceMatch.SimilarPersistedFace.PersistedFaceId.GetValueOrDefault();
                }

                faceInsightsList.Add(faceInsights);
            }
            result.FaceInsights = faceInsightsList.ToArray();

            return result;
        }
    }

    public abstract class ImageProcessorSource
    {
        protected string[] ValidExtentions = { ".png", ".jpg", ".bmp", ".jpeg", ".gif" };
        public Uri Path { get; }

        public ImageProcessorSource(Uri path)
        {
            //set fields
            Path = path;
        }

        public abstract Task<(IEnumerable<Uri>, bool)> GetFilePaths(int? fileLimit, int? startingIndex);
        public abstract string GetName();
    }

    public class StorageSource : ImageProcessorSource
    {
        public StorageSource(Uri path) : base(path) { }

        public override async Task<(IEnumerable<Uri>,bool)> GetFilePaths(int? fileLimit, int? startingIndex)
        {
            //calculate max results
            var maxResults = fileLimit;
            if (fileLimit != null && startingIndex != null)
            {
                maxResults = fileLimit + startingIndex;
            }

            var container = new CloudBlobContainer(Path);
            var results = new List<Uri>();
            var reachedFileLimit = false;
            var skipped = 0;
            var files = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, maxResults, null, null, null);
            BlobContinuationToken continuationToken = null;
            do
            {
                foreach (var file in files.Results)
                {
                    //skip if not the right extention
                    var extentionIndex = file.Uri.LocalPath.LastIndexOf('.');
                    if (extentionIndex >= 0 && ValidExtentions.Contains(file.Uri.LocalPath.Substring(extentionIndex), StringComparer.OrdinalIgnoreCase))
                    {
                        //skip
                        if (startingIndex != null && skipped != startingIndex)
                        {
                            skipped++;
                            continue;
                        }

                        //create file URI
                        var root = Path.AbsoluteUri.Remove(Path.AbsoluteUri.Length - Path.PathAndQuery.Length);
                        var query = Path.Query;
                        var path = file.Uri.LocalPath;
                        var fileUri = new Uri(root + path + query);
                        results.Add(fileUri);
                        
                        //at file limit
                        if (fileLimit != null && results.Count >= fileLimit.Value)
                        {
                            reachedFileLimit = true;
                            break;
                        }
                    }
                }

                //get more results
                continuationToken = files.ContinuationToken;
                if (files.ContinuationToken != null && !reachedFileLimit)
                {
                    files = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.Metadata, maxResults, files.ContinuationToken, null, null);
                }
                else
                {
                    break;
                }
            } while (continuationToken != null);

            //determine if we reached the end of all files
            var reachedEndOfFiles = !reachedFileLimit;
            if (reachedEndOfFiles && files.ContinuationToken != null)
            {
                reachedEndOfFiles = false;
            }

            return (results.ToArray(), reachedEndOfFiles);
        }

        public override string GetName()
        {
            return Uri.UnescapeDataString(Path.LocalPath.Replace(@"/",string.Empty));
        }
    }

    public class FileSource : ImageProcessorSource
    {
        public FileSource(Uri path) : base(path) { }

        public override async Task<(IEnumerable<Uri>, bool)> GetFilePaths(int? fileLimit, int? startingIndex)
        {
            //calulate new file limit
            if (fileLimit != null && startingIndex != null)
            {
                fileLimit = fileLimit + startingIndex;
            }

            var folder = await StorageFolder.GetFolderFromPathAsync(Path.LocalPath);
            var query = folder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.DefaultQuery, ValidExtentions));
            var files = fileLimit != null ? await query.GetFilesAsync(0, (uint)fileLimit.Value) : await query.GetFilesAsync();
            var filePaths = files.Select(i => new Uri(i.Path));
            if (startingIndex != null)
            {
                filePaths = filePaths.Skip(startingIndex.Value);
            }
            var result = filePaths.ToArray();
            var reachedEndOfFiles = fileLimit == null || result.Length < fileLimit;
            return (result, reachedEndOfFiles);
        }

        public override string GetName()
        {
            return Uri.UnescapeDataString(Path.Segments.Last());
        }
    }

    [Flags]
    public enum ImageProcessorService
    {
        Face = 1,
        ComputerVision = 2,
        CustomVision = 4
    }
}
