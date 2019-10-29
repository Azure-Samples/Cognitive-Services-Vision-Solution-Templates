# Digital Asset Management
This sample illustrates how Computer Vision can add a layer of insights to a collection of images. 

<p align="center">
  <img src="ReadmeAssets/Screenshot.jpg" />
</p>

## Prerequisites

* PC with Windows 10 version 17763 or higher
* [Visual Studio 2017](https://visualstudio.microsoft.com/) or higher
* Azure Subscription (you will need a [Cognitive Service](https://ms.portal.azure.com/#create/Microsoft.CognitiveServicesAllInOne) resource)

## How it works

Each image from a local folder or a blob collection are prossessed through the Computer Vision Api and/or the Face Api.  The results are saved in a json file.
The json file is then used to create a filter on the images.

