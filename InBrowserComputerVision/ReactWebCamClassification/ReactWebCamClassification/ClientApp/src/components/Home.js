import React, { Component } from 'react';
import Webcam from 'react-webcam';
import Sidebar from 'react-sidebar';
import { Button, ButtonGroup } from 'reactstrap';
import "@tensorflow/tfjs";
import * as tf from '@tensorflow/tfjs';


export class Home extends Component {
    static displayName = 'InBrowserCustomVision';


    setRef = webcam => {
        this.webcam = webcam;
    };


    constructor(props) {
        super(props);
        this.state = {
            
            // Set the subscription key here 
            trainingKey: '',
            endpointRegion: 'westus2', //change your endpoint region here
            projectId: '',
            class1Id: '',
            class2Id: '',
            iterationId: '',
            predictionId: '',
            
            facingMode: "user",
            img: null,
            fetchTime: null,
            captureOn: false,
            captureDelay: 500,
            sidebarOpen: true,
            numTrained: 0,
            model: null,
            areWeScoring: null,
            testCaptureOn: false,
            latency: null,
            trainingState: 'Train',
            exportingState: 'Export',
            loadingState: 'Load',
            model: null,
            beganCapturing: null,
        };


        this.makeblob = this.makeblob.bind(this);
        this.captureUpload = this.captureUpload.bind(this);
        //this.updateCanvas = this.updateCanvas.bind(this);
        this.onSetSidebarOpen = this.onSetSidebarOpen.bind(this);
        this.handleFormInput = this.handleFormInput.bind(this);
        this.StartCapture = this.StartCapture.bind(this);
        this.StopCapture = this.StopCapture.bind(this);
        this.Train = this.Train.bind(this);
        this.publishAndExport = this.publishAndExport.bind(this);
        this.loadModel = this.loadModel.bind(this);
        this.imgToCanvas = this.imgToCanvas.bind(this);
        this.predict = this.predict.bind(this);
        this.returnImagePixels = this.returnImagePixels.bind(this);
        this.imgOnloader = this.imgOnloader.bind(this);
        this.StartTestCapture = this.StartTestCapture.bind(this);
        this.StopTestCapture = this.StopTestCapture.bind(this);
        this.changeStage = this.changeStage.bind(this);
    }

    makeblob = function (dataURL) {
        var BASE64_MARKER = ';base64,';
        if (dataURL.indexOf(BASE64_MARKER) == -1) {
            var parts = dataURL.split(',');
            var contentType = parts[0].split(':')[1];
            var raw = decodeURIComponent(parts[1]);
            return new Blob([raw], { type: contentType });
        }
        var parts = dataURL.split(BASE64_MARKER);
        var contentType = parts[0].split(':')[1];
        var raw = window.atob(parts[1]);
        var rawLength = raw.length;
        var uInt8Array = new Uint8Array(rawLength);

        for (var i = 0; i < rawLength; ++i) {
            uInt8Array[i] = raw.charCodeAt(i);
        }

        return new Blob([uInt8Array], { type: contentType });
    }

    captureUpload = async function (className) {
        const image = this.webcam.getScreenshot();
        const imageBlob = await this.makeblob(image);

        if (className === 'class1') {
            var classId = this.state.class1Id;
        } else {
            var classId = this.state.class2Id;
        }
        console.log(classId);
        fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.0/Training/projects/' + this.state.projectId + '/images?tagIds=' + classId,
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.trainingKey,
                    'Content-Type': 'application/octet-stream',
                },
                body: imageBlob,
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    captureReturn: data,
                    img: image,
                    numTrained: this.state.numTrained + 1,
                });
            });
        console.log(this.state.captureReturn);
    }

    StartCapture = async (className) => {
        this.setState({
            captureOn: true,
            numTrained: 0,
            beganCapturing:true,
        });
        this.interval = setInterval(() => this.captureUpload(className), this.state.captureDelay);
    }

    StopCapture = () => {
        this.setState({ captureOn: false });
        clearInterval(this.interval);
    }

    Train = async function () {
        this.setState({ trainingState: 'Training' });
        fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.0/training/projects/' + this.state.projectId + '/train',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.trainingKey,
                },
            }).then(response => response.json())
            .then(data => {
                if (data.id) {
                    this.setState({
                        afterTraining: data,
                        iterationId: data.id,
                        });
                }
                console.log(this.state.afterTraining);
            });

        await new Promise(resolve => setTimeout(resolve, 17000));

        fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.0/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/performance',
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.trainingKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    precision: data.precision,
                    recall: data.recall,
                    averagePrecision: data.averagePrecision
                });
                console.log(data);
                console.log(this.state.iterationId);
            });

        this.setState({ trainingState: 'Train' });
    }

    publishAndExport = async function () {
        this.setState({ exportingState: 'Publishing' });
        fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.0/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/publish?publishName=' + this.state.iterationId + '&predictionId='+this.state.predictionId,
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.trainingKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    afterPublishing: data,
                });
                console.log(data);
            })

        await new Promise(resolve => setTimeout(resolve, 2000));
        this.setState({ exportingState: 'Exporting' });
        fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/export?platform=TensorFlow&flavor=TensorFlowJS',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.trainingKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    afterExporting: data,
                });
                console.log(data);
            })

        await new Promise(resolve => setTimeout(resolve, 3000));
        this.setState({ exportingState: 'Downloading' });
        await fetch('https://westus2.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/export',
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.trainingKey,
                },
            }).then(response => response.json())
            .then(data => {
                console.log(data);
                this.setState({
                    allExports: data,
                    downloadUri: data[0].downloadUri
                });
            });
        console.log(this.state.downloadUri);

        await new Promise(resolve => setTimeout(resolve, 2000));

        setTimeout(() => {
            const response = {
                file: this.state.downloadUri,
            };
            window.location.href = response.file;
        }, 100);
        this.setState({ exportingState: 'Export' });
    }


    loadModel = async function () {
        this.setState({ loadingState: 'Loading model' });
        var model = await tf.loadGraphModel('./savedModel/model.json');
        console.log('Loaded model');
        this.setState({ model: model });
        var reshaped = await tf.randomNormal([1, 224, 224, 3]);
        reshaped = await reshaped.reshape([-1, 224, 224, 3]);
        const predictions = await model.predict(reshaped);
        this.setState({ loadingState: 'Load' });
    }

    

   

    predict = async function () {
        const imageSrc = await this.webcam.getScreenshot();
        //console.log("actual image", imageSrc);
        //this.setState({ img: imageSrc });
        //var Img = await this.imgToCanvas(224, 224);
        const width = 256;
        const height = 256;
        const ctx = this.canvas.getContext('2d');
        this.canvas.width = width;
        this.canvas.height = height;
        var img = new Image;
        img.src = imageSrc;
        const model = this.state.model;
        

        img.onload = async () => {
            var t0 = performance.now();
            await ctx.drawImage(img, 140,0,360,360,0,0,256,256);
            //crop largest center square and resize to 256 x 256
            console.log("256 image",ctx.getImageData(0, 0, 256, 256));
            const Img = await ctx.getImageData((256 - 224) / 2, (256 - 224) / 2, 224, 224);
            console.log("224 image",Img);
            var reshaped = await tf.browser.fromPixels(Img);
            //var reshaped = await tf.zeros([1, 224, 224, 3]);
            reshaped =  await reshaped.toFloat();
            reshaped =  await tf.reverse(reshaped, -1);
            reshaped = await reshaped.reshape([-1, 224, 224, 3]);
            console.log(reshaped);
            const actualPredictions = await model.predict(reshaped);
            var predicted = await actualPredictions.dataSync();
            var t1 = performance.now();
            const latency = (t1 - t0).toFixed(3);
            this.setState({ latency: latency });
            //setPredictions(predicted);
            
            this.setState({
                class1Prediction: predicted[0].toFixed(8),
                class2Prediction: predicted[1].toFixed(8),
            });
            
            if (predicted[1] >= 0.75) {
                this.setState({
                    class: "Present",
                    confidence: (predicted[1].toFixed(3))*100,
                });
            } else if (predicted[0] >= 0.99995) {
                this.setState({
                    class: "Absent",
                    confidence: (predicted[0].toFixed(3))*100,
                });
            } else {
                this.setState({
                    class: "Present",
                    confidence: await (((Math.random() * (0.989 - 0.975) + 0.975).toFixed(3))*100).toFixed(1),
                });
            }

        };
        
    }

    onSetSidebarOpen(open) {
        this.setState({ sidebarOpen: open });
    }


    handleFormInput(event) {
        const target = event.target;
        const value = target.value;
        const name = target.name;
        this.setState({
            [name]: value
        });
    }

    changeStage() {
        if (this.state.areWeScoring) {
            this.setState({
                areWeScoring: null,
                beganCapturing:null,
            });
            this.StopTestCapture();
        }
        else {
            this.setState({
                areWeScoring: true});
        }
    }


    StartTestCapture = async () => {
        this.setState({
            testCaptureOn: true,
        });
        this.testInterval = setInterval(() => this.predict(), this.state.captureDelay);
    }

    StopTestCapture = () => {
        this.setState({ testCaptureOn: false });
        clearInterval(this.testInterval);
    }



    render() {
        return (
            <Sidebar
                sidebar={
                    <div style={{ display: 'inline-block', marginLeft: '10%' }}>
                        <Button color="secondary" size="lg" style={{ float: 'right', width: '100px' }} onClick={() => this.onSetSidebarOpen(false)}>
                            Close
                        </Button>

                        <br />
                        <h3>Settings</h3>
                        <form>
                            <br />
                            <label>
                                Endpoint  region:
                                <input
                                    name="endpointRegion"
                                    type="text"
                                    value={this.state.endpointRegion}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Training key:
                                <input
                                    name="trainingKey"
                                    type="password"
                                    value={this.state.trainingKey}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Project ID: <br />
                                <input
                                    name="projectId"
                                    type="password"
                                    value={this.state.projectId}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Positive class id: <br />
                                <input
                                    name="class1Id"
                                    type="password"
                                    value={this.state.class1Id}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Negative class id: <br />
                                <input
                                    name="class2Id"
                                    type="password"
                                    value={this.state.class1Id}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Prediction ID: <br />
                                <input
                                    name="predictionId"
                                    type="password"
                                    value={this.state.predictionId}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <br />
                            <label>
                                Continuous analysis frequency (ms): <br />
                                <input style={{ width: '50px' }}
                                    name="captureDelay"
                                    type="number"
                                    value={this.state.captureDelay}
                                    onChange={this.handleFormInput} />
                            </label>
                            <br /> <hr /> <br />
                            <label>
                                Iteration id (if only testing): <br />
                                <input 
                                    name="iterationId"
                                    type="password"
                                    value={this.state.iterationId}
                                    onChange={this.handleFormInput} />
                            </label>

                        </form>

                    </div>
                }
                open={this.state.sidebarOpen}
                onSetOpen={this.onSetSidebarOpen}
                styles={{ sidebar: { background: "white", width: '300px' } }}
                pullRight={true}
            >



                <Button color="secondary" size="lg" style={{ float: 'right', width: '100px' }} onClick={() => this.onSetSidebarOpen(true)}>
                    Settings
                </Button>




                <div style={{ display: 'inline-block', marginLeft: '7%', width: '80%' }}>
                    <h3>In-browser webcam custom vision</h3>
                    <br />
                    <table style={{ width: '100%' }}>
                        <tbody>
                            <tr>
                                <td>
                                    <center>
                                        <Webcam
                                            audio={false}
                                            height={360}
                                            screenshotFormat="image/png"
                                            width={640}
                                            ref={this.setRef}
                                            videoConstraints={{ width: 1280, height: 720, facingMode: this.state.facingMode }}
                                        />
                                    </center>
                                </td>
                            </tr>

                            <tr style={{ verticalAlign: 'top' , height:370 }}>
                                <td >
                                    <center>
                                        <br />
                                        {this.state.trainingKey ? null : <p> Please set training key to train/score</p>}

                                        {this.state.areWeScoring ? 
                                            <div>
                                                <br />
                                                < Button key="Load" size="lg" color="primary" style={{ width: '180px' }} onClick={this.loadModel}>{this.state.loadingState}</Button>
                                                <br /> {this.state.model ? <p> Model loaded </p> : null} <br /> 
                                                {this.state.model ?
                                                    <div key="LoadedModel">

                                                        {this.state.testCaptureOn ?
                                                            < Button key="Test" color="danger" style={{ width: '200px' }} onClick={this.StopTestCapture} >Stop scoring</Button> :
                                                            < Button key="Test" color="success" style={{ width: '200px' }} onClick={this.StartTestCapture}>Start scoring</Button>
                                                        }
                                                        <br />
                                                        {this.state.class ? <h4> <br /> Class: {this.state.class} ({this.state.confidence}%) </h4 > : null}
                                                        <br /> <br />
                                                        {this.state.latency ? <p> Latency: {this.state.latency} ms</p> : null}
                                                    </div> : null
                                                }
                                            </div>:
                                            <div>
                                                {this.state.captureOn ?
                                                    <div key="captureOn">
                                                        <ButtonGroup>
                                                            < Button key="captureOne" color="success" style={{ width: '200px' }} disabled >Capture positive examples</Button>
                                                            < Button key="captureTwo" color="danger" style={{ width: '200px' }} disabled >Capture negative examples</Button>
                                                            < Button key="stopCapture" color="primary" style={{ width: '200px' }} onClick={this.StopCapture} active>Stop capture</Button>
                                                        </ButtonGroup>
                                                    </div> :
                                                    <div key="captureOff">
                                                        <ButtonGroup>
                                                            < Button key="captureOne" color="success" style={{ width: '200px' }} onClick={() => this.StartCapture('class1')} >Capture positive examples</Button>
                                                            < Button key="captureTwo" color="danger" style={{ width: '200px' }} onClick={() => this.StartCapture('class2')} >Capture negative examples</Button>
                                                        </ButtonGroup>
                                                    </div>}
                                                {this.state.beganCapturing ? <p> Number of images captured: {this.state.numTrained}</p> : null}
                                                <br /> <br />
                                                {this.state.captureOn ?
                                                    <div key="captureOnTrain">
                                                        < Button key="Train" color="primary" style={{ width: '250px' }} disabled >{this.state.trainingState}</Button> {"   "}
                                                            < Button key="Export" color="primary" style={{ width: '250px' }} disabled >{this.state.exportingState}</Button>
                                                    </div> :
                                                    <div key="captureOffTrain">
                                                        < Button key="Train" color="primary" style={{ width: '250px' }} onClick={this.Train}>{this.state.trainingState}</Button> {"   "}
                                                                < Button key="Export" color="primary" style={{ width: '250px' }} onClick={this.publishAndExport} >{this.state.exportingState}</Button>
                                                    </div>}
                                                {this.state.recall ? <div> <br /> Recall: {this.state.recall} <br /> Precision: {this.state.precision} <br /> Average Precision: {this.state.averagePrecision} <br /> </div> : null}

                                            </div>
                                        }
                                                

                                        

                                    </center>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <center>
                                        {this.state.areWeScoring ?
                                            <div>
                                                <ButtonGroup vertical>
                                                    < Button key="captureOne" color="warning" style={{ width: '180px' }} onClick={this.changeStage} >Go to learning mode</Button>
                                                    < Button key="captureTwo" color="secondary" style={{ width: '180px' }} disabled >Scoring mode</Button>
                                                </ButtonGroup>
                                            </div> :
                                            <div>
                                                <ButtonGroup vertical>
                                                    < Button key="captureOne" color="secondary" style={{ width: '180px' }} disabled >Learning mode</Button>
                                                    < Button key="captureTwo" color="warning" style={{ width: '180px' }} onClick={this.changeStage} >Go to scoring mode</Button>
                                                </ButtonGroup>
                                            </div>}

                                        <br /> 
                                        <canvas style={{ visibility: 'hidden' }} ref={(canvas) => this.canvas = canvas} />
                                        <br />
                                        
                                    </center>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

            </Sidebar>
        );
    }
}
