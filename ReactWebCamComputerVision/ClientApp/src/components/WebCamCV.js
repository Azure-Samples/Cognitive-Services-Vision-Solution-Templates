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
        };
        this.makeblob = this.makeblob.bind(this);
        this.capture = this.capture.bind(this);
        this.toggle = this.toggle.bind(this)
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

    capture = function () {
        const image = this.webcam.getScreenshot();
        this.setState({ img: image });


        fetch('https://westus.api.cognitive.microsoft.com/vision/v2.0/detect/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': '',
                'Content-Type': 'application/octet-stream',
            },

            body: this.makeblob(image),

        }).then(response => response.json())
            .then(data => {
                console.log(data);
                this.setState({ objects: data.objects });
            })

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

                <h1>In-browser webcam computer vision with Cognitive Services and React</h1>

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
                                {this.state.img ? <img ref="image" src={this.state.img} className="hidden" /> : null}
                            </td>
                        </tr>
                    </tbody>
                </table>
                {this.state.objects ? console.log(this.state.objects) : null}
                <p>The <code>ClientApp</code> subdirectory is a standard React application based on the <code>create-react-app</code> template. If you open a command prompt in that directory, you can run <code>npm</code> commands such as <code>npm test</code> or <code>npm install</code>.</p>
          </div>
        );
  }
}
