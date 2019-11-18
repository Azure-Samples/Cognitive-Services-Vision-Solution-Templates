import React, { Component, useState } from 'react';
import Webcam from 'react-webcam';
import * as cvstfjs from 'customvision-tfjs';
import * as tf from '@tensorflow/tfjs';

import { Button, ButtonGroup, Col, Row, Form, FormGroup, Label, Input, Spinner } from 'reactstrap';
import model from './savedModel/model.json';
import JSZip from 'jszip';


export class VisualAlerts extends Component {
    displayName = VisualAlerts.name;
    
    constructor(props) {
        super(props);
        this.state = {
            subscriptionKey: '',
            endpoint: '',
            tag: '',
            captureOn: false,
            numPositive: 0,
            numNegative: 0,
            finishedCapturing: false,
            trainingState: 'Train',
            trainingComplete: true,
            exportingState: 'Export',
            loadingState: 'Load',
            finishedTraining: false,
            finishedExporting: false,
            displayProjectCreationMode: true,
            displayTrainingMode: false,
            displayScoringMode: false,
            stage: 'begin',
        };

        this.handleFormInput = this.handleFormInput.bind(this);
        this.makeblob = this.makeblob.bind(this);
        this.StartCapture = this.StartCapture.bind(this);
        this.StopCapture = this.StopCapture.bind(this);
        this.Train = this.Train.bind(this);
        this.GetPerformance = this.GetPerformance.bind(this);
        this.GetTrainStatus = this.GetTrainStatus.bind(this);
        this.CheckTrainStatus = this.CheckTrainStatus.bind(this);
        this.GetTrainStatus = this.GetTrainStatus.bind(this);
        this.Export = this.Export.bind(this);
        this.renderProjectCreation = this.renderProjectCreation.bind(this);
        this.renderTrainingMode = this.renderTrainingMode.bind(this);
        this.renderScoringMode = this.renderScoringMode.bind(this);
        this.renderCamera = this.renderCamera.bind(this);
        this.renderCaptureNegativeClass = this.renderCaptureNegativeClass.bind(this);
        this.renderCapturePositiveClass = this.renderCapturePositiveClass.bind(this);
        this.renderTrain = this.renderTrain.bind(this);
        this.deleteProject = this.deleteProject.bind(this);
    }

    setRef = webcam => {
        this.webcam = webcam;
    };

    async createProject() {
        console.log("Creating new project");
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects?name=' + this.state.tag + '&classificationType=Multilabel&domainId=0732100f-1a38-4e49-a514-c9b44c697ab5',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    projectId: data.id,
                });
                console.log("Project with id "+data.id+" created");
            });

        await new Promise(resolve => setTimeout(resolve, 1000));

        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/tags?name=' + this.state.tag+'&type=Regular',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    positiveTagId: data.id,
                });
                console.log("Positive class tag with id " + data.id + " created");
            });


        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/tags?name=Negative&type=Negative',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    negativeTagId: data.id,
                });
                console.log("Negative class tag with id " + data.id + " created");
            });

        this.setState({
            displayProjectCreationMode: false,
            displayTrainingMode: true,
            stage: 'capture positive',
        });
    }

    makeblob (dataURL) {
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

    async captureAndUpload (className) {
        const image = this.webcam.getScreenshot();
        const imageBlob = await this.makeblob(image);

        if (className === 'positive') {
            var classId = this.state.positiveTagId;
            this.setState({ numPositive : this.state.numPositive + 1 });
        } else {
            var classId = this.state.negativeTagId;
            this.setState({ numNegative : this.state.numNegative + 1 });
        }
        if (this.state.numPositive >= 5 && this.state.numNegative >= 5) {
            this.setState({ finishedCapturing: true });
        }
        console.log(classId);
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/images?tagIds=' + classId,
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                    'Content-Type': 'application/octet-stream',
                },
                body: imageBlob,
            });
    }

    StartCapture = async (className) => {
        this.setState({
            captureOn: true,
            beganCapturing: true,
        });
        this.interval = setInterval(() => this.captureAndUpload(className), 600);
    }

    StopCapture = async() => {
        this.setState({ captureOn: false });
        clearInterval(this.interval);
        if (this.state.numPositive >= 5 && this.state.numNegative < 5) {
            await new Promise(resolve => setTimeout(resolve, 3000));
            this.setState({ stage: 'capture negative' });
        }
        else if (this.state.numPositive >= 5 && this.state.numNegative >= 5) {
            await new Promise(resolve => setTimeout(resolve, 3000));
            this.setState({ stage: 'train' });
        }
        
    }

    GetTrainStatus() {
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId,
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                console.log(data.status);
                if (data.status != 'Training') {
                    this.setState({ trainingComplete: true });
                }
            });        
    }

    CheckTrainStatus() {
        this.trainStatusInterval = setInterval(() => this.GetTrainStatus(), 1000);
        if (this.state.trainingComplete) {
            clearInterval(this.trainStatusInterval);
            return
        }
    } 

    GetPerformance() {
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.0/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/performance',
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                if (data.precision) {
                    this.setState({
                        precision: data.precision.toFixed(3),
                        recall: data.recall.toFixed(3),
                        averagePrecision: data.averagePrecision.toFixed(3)
                    });
                }
            });

        this.setState({
            trainingState: 'Train',
            finishedTraining: true,
        });
    }


    async Train () {
        this.setState({
            trainingState: 'Training',
            finishedTraining: false,
        });
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/train',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                if (data.id) {
                    this.setState({
                        iterationId: data.id,
                    });
                    console.log("Training began with iteration id = " + this.state.iterationId);
                }
            });

        
        await new Promise(resolve => setTimeout(resolve, 5000));

        this.setState({
            trainingState: 'Training',
            finishedTraining: false,
        });
        var trainingFlag = false;
        while (true) {
            console.log("Enquiring");
            fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId,
                {
                    method: 'GET',
                    headers: {
                        'Training-key': this.state.subscriptionKey,
                    },
                }).then(response => response.json())
                .then(data => {
                    console.log(data.status);
                    if (data.status != 'Training') {
                        this.setState({ trainingComplete: true });
                        trainingFlag = true;
                        this.GetPerformance();
                    }
                });
            await new Promise(resolve => setTimeout(resolve, 600));
            if (trainingFlag) {
                break;
            }
        };
        this.setState({
            trainingComplete: true,
            finishedTraining: true,
        });
    }

    async Export() {
        this.setState({ exportingState: 'Exporting' });

        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/export?platform=TensorFlow&flavor=TensorFlowJS',
            {
                method: 'POST',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                console.log("Exported");
            });

        await new Promise(resolve => setTimeout(resolve, 3000));
        this.setState({ exportingState: 'Downloading' });
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId + '/iterations/' + this.state.iterationId + '/export',
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.json())
            .then(data => {
                this.setState({
                    downloadUri: data[0].downloadUri
                });
            });
        console.log(this.state.downloadUri);

        await new Promise(resolve => setTimeout(resolve, 2000));

        setTimeout(() => {
            const response = {
                file: this.state.downloadUri,
            };
            window.open(response.file);
        }, 100);
        this.setState({
            exportingState: 'Export',
            finishedExporting: true,
        });

        await new Promise(resolve => setTimeout(resolve, 3000));
        this.deleteProject();

        fetch("https://cors-anywhere.herokuapp.com/" + this.state.downloadUri,
            {
                method: 'GET',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            }).then(response => response.blob()).then(response => {
                console.log(response);

                var new_zip = new JSZip();
                new_zip.loadAsync(response)
                    .then(async function (zip) {
                        // you now have every files contained in the loaded zip
                        console.log("UNZIPPED!!!!");
                        console.log(zip);
                        var labels = zip.file("labels.txt").async("text");
                        var model = zip.file("model.json").async("blob");
                        var weights = await zip.file("weights.bin").async("blob");
                        return [labels, model, weights];
                    }).then(unzipped => {
                        this.setState({
                            labels: unzipped[0],
                            model: unzipped[1],
                            weights: unzipped[2],
                        });
                        console.log(unzipped[1]);
                    });/*.then(output => {
                        return tf.loadLayersModel(tf.io.browserFiles([this.state.model, this.state.weights]));
                    }).then(model => {
                        console.log(model);
                    });*/
            });

    }

    async Load() {
        console.log("loading model");
        this.deleteProject();
        this.setState({
            displayProjectCreationMode: false,
            displayTrainingMode: false,
            displayScoringMode: true,
            numNegative: 0,
            numPositive: 0,
            finishedCapturing: false,
            finishedExporting: false,
            finishedTraining: false,
            testCaptureOn: false,
            stage : 'scoring',
        });

        this.setState({ loadingState: 'Loading model' });
        let model = new cvstfjs.ClassificationModel();
        await model.loadModelAsync('./savedModel/model.json');
        var reshaped = tf.randomNormal([1, 224, 224, 3]);
        reshaped = reshaped.reshape([-1, 224, 224, 3]);
        const predictions = await model.executeAsync(reshaped);
        console.log({predictions});
        this.setState({ loadingState: 'Model loaded' });
        console.log('Loaded model');
        this.setState({ model: model });
        this.testInterval = setInterval(() => this.Predict(), 300);

        this.setState({
            loadingState: 'Loaded model',
            testCaptureOn: true,
        });

    }

    async Predict() {
        const imageSrc = await this.webcam.getScreenshot();
        
        const ctx = this.canvas.getContext('2d');
        this.canvas.width = 640;
        this.canvas.height = 360;
        var img = new Image;
        img.src = imageSrc;
        const model = this.state.model;

        img.onload = async () => {
            var t0 = performance.now();
            await ctx.drawImage(img, 0, 0, 640, 360, 0, 0, 640, 360);
            const Img = await ctx.getImageData(0, 0, 640, 360);
            const actualPredictions = await model.executeAsync(Img);
            var predicted = actualPredictions[0];
            var t1 = performance.now();
            const latency = (1000 / (0.1 + t1 - t0)).toFixed(1);
            this.setState({ latency: latency });
            const present_score = await predicted[0].toFixed(3);
            const absent_score = 1 - present_score;
            if (present_score >= 0.15) {
                this.setState({
                    class: this.state.tag,
                    confidence: (present_score * 100).toFixed(2),
                });
            } else {
                this.setState({
                    class: "Not detected",
                    confidence: (absent_score * 100).toFixed(2),
                });
            };
        }
    }

    StartTestCapture = async () => {
        this.setState({
            testCaptureOn: true,
        });
        this.testInterval = setInterval(() => this.Predict(), 300);
    }

    StopTestCapture = () => {
        this.setState({ testCaptureOn: false });
        clearInterval(this.testInterval);
    }

    deleteProject() {
        console.log("Deleting project");
        fetch('https://' + this.state.endpoint + '.api.cognitive.microsoft.com/customvision/v3.1/training/projects/' + this.state.projectId ,
            {
                method: 'DELETE',
                headers: {
                    'Training-key': this.state.subscriptionKey,
                },
            });
    }

    startOver() {
        this.deleteProject();
        this.setState({
            displayProjectCreationMode: true,
            displayScoringMode: false,
            displayTrainingMode: false,
            stage: 'begin'
        });
    }

    handleFormInput(event) {
        const target = event.target;
        const value = target.value;
        const name = target.name;
        this.setState({
            [name]: value
        });
    }

    renderCamera() {
        return (
            <Webcam
                audio={false}
                height={360}
                screenshotFormat="image/png"
                width={640}
                ref={this.setRef}
                videoConstraints={{ width: 1280, height: 720, facingMode: 'user' }}
            />
        );
    }


    renderProjectCreation() {
        return (
            <div>
                <h3> Please enter your credentials to continue </h3>
                <br /> <br /> 
                
                <Form>
                    <FormGroup row>
                        <Col sm={1}> </Col>
                        <Label for="subscriptionKey" sm={4}>Subscription Key</Label>
                        <Col sm={5}>
                            <Input type="password"
                                name="subscriptionKey"
                                id="subscriptionKey"
                                placeholder="0123456789abcdef0123456789abcdef"
                                onChange={this.handleFormInput}
                            />
                        </Col>
                    </FormGroup>
                    <FormGroup row>
                        <Col sm={1}> </Col>
                        <Label for="endpoint" sm={4}>Endpoint</Label>
                        <Col sm={5}>
                            <Input type="text"
                                name="endpoint"
                                id="endpoint"
                                placeholder="westus2"
                                onChange={this.handleFormInput}
                            />
                        </Col>
                    </FormGroup>
                    
                    <FormGroup row>
                        <Col sm={1}> </Col>
                        <Label for="tag" sm={4}>What are you creating <br/> a visual alert for?</Label>
                        <Col sm={5}>
                            <Input type="text"
                                name="tag"
                                id="tag"
                                placeholder="your name perhaps?"
                                onChange={this.handleFormInput}
                            />
                        </Col>
                    </FormGroup>
                    <br /> 
                    <FormGroup row>
                        <Button color="primary" size="lg"  onClick={() => this.createProject()}>
                            Begin
                        </Button>

                        <br /> <br />
                        <h4> If you already have a project exported, downloaded and unzipped with model.json and weights.bin in ClientApp/public/savedModel, click on load </h4>
                        <br />
                        <Button color="primary" size="lg" onClick={() => this.Load()}>
                            Load
                        </Button>

                    </FormGroup>
                </Form>
            </div>
        );
    }

    renderTrainingMode() {
        return (
            <div>
                <p> It's recommended to have at least 5 images that have {this.state.tag} present and 5 images that have {this.state.tag} absent</p>
                {this.state.captureOn ?
                    <div key="captureOn">
                        <ButtonGroup>
                            < Button key="captureOne" color="success" style={{ width: '250px' }} disabled >Capture positive examples</Button>
                            < Button key="captureTwo" color="danger" style={{ width: '250px' }} disabled >Capture negative examples</Button>
                            < Button key="stopCapture" color="primary" style={{ width: '250px' }} onClick={this.StopCapture} active>Stop capture</Button>
                        </ButtonGroup>
                    </div> :
                    <div key="captureOff">
                        <ButtonGroup>
                            < Button key="captureOne" color="success" style={{ width: '250px' }} onClick={() => this.StartCapture('positive')} >Capture presence samples </Button>
                            < Button key="captureTwo" color="danger" style={{ width: '250px' }} onClick={() => this.StartCapture('negative')} >Capture absence samples</Button>
                        </ButtonGroup>
                    </div>
                }
                {this.state.finishedCapturing && !this.state.captureOn ? null : <p> Number of presence samples captured: {this.state.numPositive} /5 <br />
                    Number of absence  samples captured: {this.state.numNegative} /5 </p>}

                 <br /> <br />
                {this.state.captureOn ?
                    null :
                    <div key="captureOffTrain">
                        {this.state.finishedCapturing ?
                            <div>
                                < Button key="Train" color="primary" style={{ width: '250px' }} onClick={this.Train}>{this.state.trainingState}</Button> {"   "}
                                {this.state.finishedTraining ?
                                    < Button key="Export" color="primary" style={{ width: '250px' }} onClick={this.Export} >{this.state.exportingState}</Button>
                                    : < Button key="Export" color="primary" style={{ width: '250px' }} disabled >{this.state.exportingState}</Button>}
                            </div>
                            :
                            null
                        }

                    </div>
                }
                {this.state.finishedExporting ?
                    <div>
                        <br /> <br />
                        <p> Download and unzip the downloaded file in ClientApp/public/savedModel </p>
                        <Button color="warning" size="lg" onClick={() => this.Load()}>
                            Load
                        </Button>
                    </div> :
                    <div>
                        {this.state.recall ? <div> <br /> Recall: {this.state.recall} <br /> Precision: {this.state.precision} <br /> Average Precision: {this.state.averagePrecision} <br /> </div> : null}
                    </div>
                }
            </div>
            )
    }

    renderScoringMode() {
        return (
            <div>
                {this.state.loadingState} <br /> <br />
                {this.state.confidence ? <h4> <br /> {this.state.class} ({this.state.confidence}%) </h4 > : null}
                <br /> <br />
                {this.state.latency ? <p> Frame-rate: {this.state.latency} fps</p> : null}

                {this.state.loadingState === 'Loaded model' ? <div> {this.state.testCaptureOn ?
                    < Button key="Test" color="danger" style={{ width: '200px' }} onClick={this.StopTestCapture} >Pause scoring</Button> :
                    < Button key="Test" color="success" style={{ width: '200px' }} onClick={this.StartTestCapture}>Resume scoring</Button>
                } </div> : null}
                

            </div>
        )
    }

    renderCapturePositiveClass() {
        return (
            <div>
                <p> It's recommended to train the model with at least 5 images that have {this.state.tag} present and 5 images that have {this.state.tag} absent. Start by capturing some positive samples. </p>
                {this.state.captureOn ?
                    <div key="captureOn">
                        <ButtonGroup>
                            < Button key="stopCapture" color="primary" size="lg" style={{ width: '350px' }} onClick={this.StopCapture} active>Stop capture</Button>
                        </ButtonGroup>
                    </div> :
                    <div key="captureOff">
                        <ButtonGroup>
                            < Button key="captureOne" color="success" size="lg" style={{ width: '350px' }} onClick={() => this.StartCapture('positive')} >Capture {this.state.tag} </Button>
                        </ButtonGroup>
                    </div>
                }
                <br />
                Number of images captured: {this.state.numPositive}
                <br />
                
            </div>
            )
    }

    renderCaptureNegativeClass() {
        return (
            <div>
                <p>Done with the positive samples. Capture some negative samples now by pointing the camera to anything else as long as {this.state.tag} isn't in the frame. </p>
                {this.state.captureOn ?
                    <div key="captureOn">
                        <ButtonGroup>
                            < Button key="stopCapture" color="primary" size="lg" style={{ width: '350px' }} onClick={this.StopCapture} active>Stop capture</Button>
                        </ButtonGroup>
                    </div> :
                    <div key="captureOff">
                        <ButtonGroup>
                            < Button key="captureTwo" color="danger" size="lg" style={{ width: '350px' }} onClick={() => this.StartCapture('negative')} >Capture absence of {this.state.tag} </Button>
                        </ButtonGroup>
                    </div>
                }
                <br />
                Number of images captured: {this.state.numNegative}
            </div>
        )
    }

    renderTrain() {
        return (
            <div>
                {this.state.finishedTraining ?
                    < Button key="Train" color="primary" size="lg" style={{ width: '250px' }} disabled> Training complete </Button> :
                    <div>
                        < Button key="Train" color="primary" size="lg" style={{ width: '250px' }} onClick={this.Train}>{this.state.trainingState + ' '} </Button>
                        {this.state.trainingState === 'Training' ? <Spinner color="success" /> : null}
                    </div>}

                <br />
                {this.state.recall ? <div> <br /> Recall: {this.state.recall} <br /> Precision: {this.state.precision} <br /> Average Precision: {this.state.averagePrecision} <br /> </div> : null}
                <br />
                {this.state.finishedTraining ?
                    < Button key="Export" color="primary" size="lg" style={{ width: '250px' }} onClick={this.Export} >{this.state.exportingState}</Button>
                    : null }
                {this.state.finishedExporting ?
                    <div>
                        <br /> 
                        <p> Download and unzip the downloaded file in ClientApp/public/savedModel . The page will automatically refresh when there's a change in the folder. Click load model in the landing page once you've replaced the existing model.json and weights.bin in the ClientApp/public/savedModel folder with the weights of the file just downloaded.</p>
                    </div>: null}

            </div>
            )
    }

    



    render() {
        return (
            <div>
                <h3>In-Browser Visual Alerts using Azure Custom Vision</h3>
                <br/> <br/>
                <center>
                    <this.renderCamera />
                    <br /><br />

                    {this.state.stage === 'begin' ? < this.renderProjectCreation /> : null}
                    {this.state.stage === 'capture positive' ? <this.renderCapturePositiveClass /> : null}
                    {this.state.stage === 'capture negative' ? <this.renderCaptureNegativeClass /> : null}
                    {this.state.stage === 'train' ? <this.renderTrain /> : null}
                    {this.state.stage === 'scoring' ? <this.renderScoringMode /> : null}
                    {/*this.state.displayProjectCreationMode ? < this.renderProjectCreation /> :
                        [this.state.displayTrainingMode ? <this.renderTrainingMode /> : <this.renderScoringMode /> ]
                    }
                    <br /> <br />
                    {this.state.displayProjectCreationMode ? null :
                        <Button color="secondary" size="lg" onClick={() => this.startOver()}>
                            Start Over
                        </Button>
                    */}
                    <br /> <br />
                    {this.state.stage === 'begin' ? null :
                        <Button color="secondary" size="lg" onClick={() => this.startOver()}>
                        Start Over
                        </Button>}
                </center>
                <canvas style={{ visibility: 'hidden' }} ref={(canvas) => this.canvas = canvas} />
            </div>
        );
    }
}
