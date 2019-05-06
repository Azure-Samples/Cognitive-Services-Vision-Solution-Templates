using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Storage;

/// <summary>
/// See Custom Vision ONNX UWP sample https://github.com/Azure-Samples/Custom-Vision-ONNX-UWP/blob/master/VisionApp/ONNXModel.cs
/// </summary>

namespace IoTVisualAlerts
{
    public sealed class CustomVisionModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class CustomVisionModelOutput
    {
        // The label returned by the model
        public TensorString classLabel = TensorString.Create(new long[] { 1, 1 });

        // The loss returned by the model
        public IList<IDictionary<string, float>> loss = new List<IDictionary<string, float>>();

        public List<Tuple<string, float>> GetPredictionResult()
        {
            List<Tuple<string, float>> result = new List<Tuple<string, float>>();
            foreach (IDictionary<string, float> dict in loss)
            {
                foreach (var item in dict)
                {
                    result.Add(new Tuple<string, float>(item.Key, item.Value));
                }
            }
            return result;
        }
    }

    public sealed class CustomVisionONNXModel
    {
        private LearningModel _learningModel = null;
        private LearningModelSession _session;
        public int InputImageWidth { get; private set; }
        public int InputImageHeight { get; private set; }

        // Create a model from an ONNX 1.2 file
        public static async Task<CustomVisionONNXModel> CreateONNXModel(StorageFile file)
        {
            LearningModel learningModel = null;
            try
            {
                learningModel = await LearningModel.LoadFromStorageFileAsync(file);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            var inputFeatures = learningModel.InputFeatures;
            ImageFeatureDescriptor inputImageDescription = inputFeatures?.FirstOrDefault(feature => feature.Kind == LearningModelFeatureKind.Image) as ImageFeatureDescriptor;
            uint inputImageWidth = 0, inputImageHeight = 0;
            if (inputImageDescription != null)
            {
                inputImageHeight = inputImageDescription.Height;
                inputImageWidth = inputImageDescription.Width;
            }

            return new CustomVisionONNXModel()
            {
                _learningModel = learningModel,
                _session = new LearningModelSession(learningModel),
                InputImageWidth = (int)inputImageWidth,
                InputImageHeight = (int)inputImageHeight
            };
        }

        public async Task<CustomVisionModelOutput> EvaluateAsync(CustomVisionModelInput input)
        {
            var output = new CustomVisionModelOutput();
            var binding = new LearningModelBinding(_session);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResult result = await _session.EvaluateAsync(binding, "0");
            return output;
        }
    }
}