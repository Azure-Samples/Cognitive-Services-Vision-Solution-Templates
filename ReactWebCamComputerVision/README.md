For the experienced React devloper:
1. The ClientApp folder has all the components required to run this demo. It is the equivalent of the folder created by `create-react-app` where you can run your npm commands. You can use your favourite framework and IDE. 
2. Go to step 7 below.

For everyone else: 
3. This was tested for Visual Studio Enterprise 2019 version 16.3.2. Install it from [here](https://visualstudio.microsoft.com/downloads/). Ensure you have this workload checked during the installation process: ASP.NET and web development under the tab Web & Cloud. This should install ASP.NET core version 2.2 which this was tested on. You can install this from [here](https://dotnet.microsoft.com/download/dotnet-core/2.2)
4. Clone this repository and open it on Visual Studio

If you have your own version of Visual Studio and ASP.NET Core and want to use that instead, 
5. Create a new project in Visual Studio. This is an ASP.NET Core Web Application. Choose React.js when prompted for a template.
6. Delete the ClientApp folder created and all its contents and move the ClientApp folder from this repository to its place

7. Go to [Azure Cognitive Services](https://azure.microsoft.com/en-us/try/cognitive-services/my-apis/?api=computer-vision) to get your Cognitive Services subscription key.
8. Replace the '' in line in ClientApp\src\components\WebCamCV.js with the subscription key.
9. Hit F5 or run IIS express 

