using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTVisualAlerts
{
    class CustomVisionServiceWrapper
    {
        CustomVisionTrainingClient customVisionTrainingClient = new CustomVisionTrainingClient
        {
            ApiKey = "{The training key for your Custom Vision Service instance}",
            Endpoint = "https://westus2.api.cognitive.microsoft.com" // update with the region of your key
        };

        Guid targetCVSProjectGuid = new Guid("{Your Custom Vision Service target project id}");

        public async Task PrepTargetProjectForTrainingAsync()
        {
            // delete tags
            foreach (var tag in await customVisionTrainingClient.GetTagsAsync(targetCVSProjectGuid))
            {
                await customVisionTrainingClient.DeleteTagAsync(targetCVSProjectGuid, tag.Id);
            }

            // delete untagged images
            var untaggedImages = await customVisionTrainingClient.GetUntaggedImagesAsync(targetCVSProjectGuid, iterationId: null, take: 256);
            await customVisionTrainingClient.DeleteImagesAsync(targetCVSProjectGuid, untaggedImages.Select(i => i.Id).ToList());

            // delete iterations
            foreach (var iteration in await customVisionTrainingClient.GetIterationsAsync(targetCVSProjectGuid))
            {
                if (iteration.PublishName != null)
                {
                    // we need to unpublish before we can delete it
                    await customVisionTrainingClient.UnpublishIterationAsync(targetCVSProjectGuid, iteration.Id);
                }

                await customVisionTrainingClient.DeleteIterationAsync(targetCVSProjectGuid, iteration.Id);
            }
        }

        public async Task UploadTrainingImageAsync(Stream stream)
        {
            await customVisionTrainingClient.CreateImagesFromDataAsync(targetCVSProjectGuid, stream);
        }

        public async Task<Export> GetTrainedONNXExportIfAvailableAsync(DateTime minIterationTrainedTime)
        {
            var iterations = await customVisionTrainingClient.GetIterationsAsync(targetCVSProjectGuid);
            Iteration targetIteration = iterations.Where(i => i.Status == "Completed" && i.TrainedAt > minIterationTrainedTime).FirstOrDefault();

            if (targetIteration == null)
            {
                // no trained iteration to export at this point
                return null;
            }

            // Trigger ONNX export and wait until it finishes
            Export onnxExport;
            while (true)
            {
                IList<Export> exports = await customVisionTrainingClient.GetExportsAsync(targetCVSProjectGuid, targetIteration.Id);
                onnxExport = exports.Where(e => e.Platform == "ONNX").FirstOrDefault();
                if (onnxExport == null)
                {
                    onnxExport = await customVisionTrainingClient.ExportIterationAsync(targetCVSProjectGuid, targetIteration.Id, "onnx", flavor: "onnx12");
                }

                if (onnxExport.Status == "Exporting")
                {
                    await Task.Delay(1000);
                    continue;
                }
                else
                {
                    break;
                }
            }

            return onnxExport;
        }

    }
}
