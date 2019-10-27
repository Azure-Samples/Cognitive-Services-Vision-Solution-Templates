//browser compatibility http://caniuse.com/#feat=stream

import React, { Component } from 'react';
import Webcam from 'react-webcam';
import Sidebar from 'react-sidebar';
import { Button, ButtonGroup } from 'reactstrap';
import Switch from 'react-switch';


export class WebCamCV extends Component {
    static displayName = WebCamCV.name;
    

    setRef = webcam => {
        this.webcam = webcam;
    };


    constructor(props) {
        super(props);
        this.state = {

            /* Set the subscription key here */
            subscriptionKey: '' , 
            /* For example, if your subscription key is ABCDE12345, the line should look like
             * subscriptionKey: 'ABCDE12345' , */
            endpointRegion: 'westus', //change your endpoint region here

            facingMode: "user",
            img: null,
            fetchTime: null,
            objects: null,
            tags: null,
            caption: null,
            captureOn: false,
            captureDelay: 500,
            sidebarOpen: true,
        };
        this.makeblob = this.makeblob.bind(this);
        this.capture = this.capture.bind(this);
        this.updateCanvas = this.updateCanvas.bind(this);
        this.onSetSidebarOpen = this.onSetSidebarOpen.bind(this);
        this.handleFormInput = this.handleFormInput.bind(this);
        this.handleSwitchChange = this.handleSwitchChange.bind(this);

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

    updateCanvas() {
        const ctx = this.canvas.getContext('2d');
        ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        var img = new Image;
        let objects = this.state.objects;

        img.onload = function () {
            ctx.drawImage(img, 0, 0);

            ctx.lineWidth = 4;
            ctx.lineJoin = 'bevel';
            const font = "16px sans-serif"
            ctx.font = font;
            ctx.textBaseline = "top";

            objects.forEach(object => {
                console.log(object.confidence, object.object, object.rectangle);

                ctx.strokeStyle = "rgba(255,0,0," + String((object.confidence-0.4)*2) +")";
                ctx.lineWidth = 4;
                ctx.strokeRect(object.rectangle.x, object.rectangle.y, object.rectangle.w, object.rectangle.h);

                ctx.fillStyle = "rgba(255,255,255," + String(object.confidence) + ")";;
                const textWidth = ctx.measureText(object.object + " (" + object.confidence + ")").width;
                const textHeight = parseInt(font, 10); // base 10
                ctx.fillRect(object.rectangle.x, object.rectangle.y - (textHeight + 4), textWidth + 4, textHeight + 4);

                ctx.fillStyle = "rgb(0,0,0)";
                ctx.fillText(object.object + " (" + object.confidence + ")", object.rectangle.x, (object.rectangle.y - textHeight - 2));

            });
        };

        img.src = this.state.img;
    }

    capture = function () {
        const image = this.webcam.getScreenshot();
        const imageBlob = this.makeblob(image);
        this.setState({ img: image });
        var t0 = performance.now();
        //Object Detection
        fetch('https://' + this.state.endpointRegion + '.api.cognitive.microsoft.com/vision/v2.0/detect/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': this.state.subscriptionKey,
                'Content-Type': 'application/octet-stream',
            },

            body: imageBlob,

        }).then(response => response.json())
            .then(data => {
                var t1 = performance.now();
                this.setState({ objects: data.objects, fetchTime: (t1 - t0).toFixed(3) });
            }).then(returnValue => this.updateCanvas());


        //Image description
        fetch('https://' + this.state.endpointRegion + '.api.cognitive.microsoft.com/vision/v2.0/describe/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': this.state.subscriptionKey,
                'Content-Type': 'application/octet-stream',
            },

            body: imageBlob,

        }).then(response => response.json())
            .then(data => {
                console.log(data.description);
                if (data.description.captions.length >= 1) {
                    this.setState({ caption: data.description.captions[0].text });
                    this.setState({ captionConfidence: data.description.captions[0].confidence.toFixed(3) });
                    this.setState({ tags: data.description.tags });
                }
            });

    }

    StartCapture = async () => {
        this.setState({ captureOn: true });
        this.interval = setInterval(() => this.capture(), this.state.captureDelay);
    }

    StopCapture = () => {
        this.setState({ captureOn: false });
        clearInterval(this.interval);
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

    handleSwitchChange(captureOn) {
        this.setState({ captureOn });
        if (captureOn) {
            this.interval = setInterval(() => this.capture(), this.state.captureDelay);
        }
        else {
            clearInterval(this.interval);
        }
    }


    render() {
        return (
            <Sidebar
                sidebar={
                    <div style={{ display: 'inline-block', marginLeft: '10%' }}>
                        <Button color="primary" size="lg" style={{ float: 'right', width: '100px' }} onClick={() => this.onSetSidebarOpen(false)}>
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
                                Subscription API key:
                                <input
                                    name="subscriptionKey"
                                    type="password"
                                    value={this.state.subscriptionKey}
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

                        </form>

                    </div>
                }
                open={this.state.sidebarOpen}
                onSetOpen={this.onSetSidebarOpen}
                styles={{ sidebar: { background: "white" , width: '300px' } }}
                pullRight={true}
            >


                
                <Button color="primary" size="lg" style={{ float: 'right', width: '100px'}} onClick={() => this.onSetSidebarOpen(true)}>
                    Settings
                </Button>

                <div style={{ display: 'inline-block', marginLeft: '10%' }}>
                <h3>In-browser webcam computer vision with Microsoft Azure Cognitive Services and React</h3>
                <br />

                <table>
                    <tbody>
                        <tr>
                            <td style={{ width: '620px' }}>
                                <center>
                                    <Webcam
                                        audio={false}
                                        height={292}
                                        screenshotFormat="image/png"
                                        width={512}
                                        ref={this.setRef}
                                        videoConstraints={{ width: 1280, height: 720, facingMode: this.state.facingMode }}
                                    />
                                </center>
                            </td>

                            <td style={{ width: '580px' }}>
                                 <canvas ref={(canvas) => this.canvas = canvas} width="512" height="290" /> 
                            </td>

                        </tr>
                        <tr style={{ verticalAlign: 'top' }}>
                                <td >
                                    <center>
                                        {this.state.subscriptionKey ? null : <p> Please set subscription key to analyze</p>}
                                        
                                        <br />
                                        {this.state.subscriptionKey ?
                                            [this.state.captureOn ?
                                                <div key="captureOn">
                                                    < Button key="captureOnce" color="primary" style={{ width: '200px' }} onClick={this.capture} disabled >Analyze Single Frame</Button>
                                                    <label style={{ float: 'right', marginRight: 50 }}>
                                                        <span style={{ fontSize: 20, verticalAlign: 'top' }}>Analyze Continuously {}</span>
                                                        <Switch onChange={this.handleSwitchChange} checked={this.state.captureOn} />
                                                    </label>
                                                </div> :
                                                <div key="captureOff">
                                                        < Button key="captureOnce" color="primary" style={{ width: '200px' }} onClick={this.capture}>Analyze Single Frame</Button> 
                                                    <label style={{ float: 'right', marginRight: 50}}>
                                                        <span style={{ fontSize: 20, verticalAlign: 'top' }}>Analyze Continuously {  }</span>
                                                        <Switch onChange={this.handleSwitchChange} checked={this.state.captureOn} />
                                                    </label>
                                                </div>]
                                            : null}

                                    </center>
                                </td>

                            <td>
                                {this.state.caption ? <div> <h3>Caption </h3> <p> {this.state.caption} ({this.state.captionConfidence}) </p> </div> : null}
                                {this.state.tags ?
                                    <div> <h3> Tags </h3> <ul>
                                        {this.state.tags.map(function (tag, index) {
                                            return <li key={index}>{tag}</li>;
                                        })}
                                        </ul>
                                    </div> : null
                                    }

                                    {this.state.fetchTime ? <div> <p> <b>Latency: </b>  {this.state.fetchTime} milliseconds</p> </div> : null}

                            </td>
                        </tr>
                    </tbody>
                    </table>
                </div>

            </Sidebar>
        );
  }
}
