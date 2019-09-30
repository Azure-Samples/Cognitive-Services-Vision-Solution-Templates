import React, { Component } from 'react';
import Webcam from 'react-webcam';
import JSONViewer from 'react-json-viewer';


export class WebcamCapture extends Component {
    setRef = webcam => {
        this.webcam = webcam;
    };

    constructor(props)
    {
        super(props);
        this.state = {
            clicked: 'False', retdata: null, img: null
        };
        this.makeblob = this.makeblob.bind(this);
    }

    makeblob = function (dataURL)
    {
        var BASE64_MARKER = ';base64,';
        if (dataURL.indexOf(BASE64_MARKER) == -1)
        {
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


    capture = () => {
        const imageSrc = this.webcam.getScreenshot();
        this.setState({ clicked: 'True', retdata: {}, img: null });
        fetch('https://westus.api.cognitive.microsoft.com/vision/v2.0/detect/', {
            method: 'POST',
            headers: {
                'Ocp-Apim-Subscription-Key': 'xxxxxxxxxxxx',
                'Content-Type': 'application/octet-stream',
            },

            body: this.makeblob(imageSrc),

        }).then(response => response.json())
            .then(data => {
                this.setState({ clicked: 'Returned', retdata: data, img: imageSrc });
            })
    };

    render() {
        const videoConstraints = {
            width: 1280,
            height: 720,
            facingMode: "user"
        };

        return (
            <div>
                <Webcam
                    audio={false}
                    height={350}
                    ref={this.setRef}
                    screenshotFormat="image/png"
                    width={350}
                    videoConstraints={videoConstraints}
                />
                <button onClick={this.capture}>Take a photo</button>
                <p> Photo captured? = {this.state.clicked} </p>
                {this.state.retdata ?
                    <JSONViewer
                        json={this.state.retdata}
                    /> : null}
                {this.state.img ? <img src={this.state.img} /> : null}

            </div>
        );
    }
}
