using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Face = Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;

namespace DigitalAssetManagementTemplate.Views.DigitalAssetManagement
{
    public class ImageInsights
    {
        public Uri ImageUri { get; set; }
        public FaceInsights[] FaceInsights { get; set; }
        public VisionInsights VisionInsights { get; set; }
    }

    public class VisionInsights
    {
        public string Caption { get; set; }
        public string[] Tags { get; set; }
        public string[] Objects { get; set; }
        public string[] Landmarks { get; set; }
        public string[] Celebrities { get; set; }
        public string[] Brands { get; set; }
        public string[] Words { get; set; }
        public AdultInfo Adult { get; set; }
        public ColorInfo Color { get; set; }
        public ImageType ImageType { get; set; }
        public ImageMetadata Metadata { get; set; }
    }

    public class FaceInsights
    {
        public Guid UniqueFaceId { get; set; }
        public Face.FaceRectangle FaceRectangle { get; set; }
        public Face.FaceAttributes FaceAttributes { get; set; }
    }
}
