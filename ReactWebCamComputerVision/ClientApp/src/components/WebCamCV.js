//browser compatibility http://caniuse.com/#feat=stream

import React, { Component } from 'react';
import Webcam from 'react-webcam';

export class WebCamCV extends Component {
    static displayName = WebCamCV.name;
    

    setRef = webcam => {
        this.webcam = webcam;
    };


    constructor(props) {
        super(props);
        this.state = {
            facingMode: "environment",
            img: null,
            objects: null,
            tags: null,
            caption: null,
        };
        this.makeblob = this.makeblob.bind(this);
        this.capture = this.capture.bind(this);
        this.toggle = this.toggle.bind(this);
        this.updateCanvas = this.updateCanvas.bind(this);
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
                ctx.fillRect(object.rectangle.x, object.rectangle.y, textWidth + 4, textHeight + 4);

                ctx.fillStyle = "rgb(0,0,0)";
                ctx.fillText(object.object + " (" + object.confidence + ")", object.rectangle.x, object.rectangle.y);

            });
        };

        img.src = this.state.img;
    }

    capture = function () {
        const image = this.webcam.getScreenshot();
        const imageBlob = this.makeblob(image);
        this.setState({ img: image });

        //Object Detection
        fetch('https://westus.api.cognitive.microsoft.com/vision/v2.0/detect/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': '',
                'Content-Type': 'application/octet-stream',
            },

            body: imageBlob,

        }).then(response => response.json())
            .then(data => {
                this.setState({ objects: data.objects });
            }).then(returnValue => this.updateCanvas());


        //Image description
        fetch('https://westus.api.cognitive.microsoft.com/vision/v2.0/describe/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': '',
                'Content-Type': 'application/octet-stream',
            },

            body: imageBlob,

        }).then(response => response.json())
            .then(data => {
                console.log(data.description);
                this.setState({ tags: data.description.tags });
                if (data.description.captions.length >= 1) {
                    this.setState({ caption: data.description.captions[0].text });
                    this.setState({ captionConfidence: data.description.captions[0].confidence.toFixed(3) });
                }
            });

    }


    toggle = () => {
        if (this.state.facingMode == "user") {
            this.setState({ facingMode: "environment" })
            return
        }
        this.setState({ facingMode: "user" })
        return
    }

    render() {

        return (
            <div>

                <h1>In-browser webcam computer vision with Microsoft Azure Cognitive Services and React</h1>

                <p></p>
                <table>
                    <tbody>
                        <tr>
                            <td>
                                <Webcam
                                    audio={false}
                                    height={290}
                                    screenshotFormat="image/png"
                                    width={512}
                                    ref={this.setRef}
                                    videoConstraints={{ width: 1280, height: 720, facingMode: this.state.facingMode }}
                                />
                            </td>
                            <td>
                                <center>
                                    <button onClick={this.capture}>Capture and analyze</button> <br></br>
                                    <button onClick={this.toggle}>Toggle camera</button>
                                </center>
                            </td>
                            <td>
                                <canvas ref={(canvas) => this.canvas = canvas} width="512" height="300" />
                                

                            </td>
                        </tr>
                        <tr>
                            <td></td>
                            <td></td>
                            <td>
                                {this.state.caption ? <p>Caption: {this.state.caption} ({this.state.captionConfidence}) </p> : null}
                                {this.state.tags ? <div> <h3> Tags </h3> <ul>
                                    {this.state.tags.map(function (tag, index) {
                                        return <li key={index}>{tag}</li>;
                                    })}
                                </ul> </div> : null}
                            </td>
                        </tr>
                    </tbody>
                </table>

          </div>
        );
  }
}
