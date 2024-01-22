# Description

This application is a real-time object detection system developed using Unity and integrated with a server for processing the detection. It's designed to capture images from a device's camera, process them through an object detection model on a server, and display the detected objects with bounding boxes on the user's screen.

# Usage

## Server side setting
Upload ServerSideDetection.py to your server, and change the ip address to your own.

## Unity side setting

Copy the entire repo to your local, use unity (2020) with android platform to open it as a project;

In Asset/ServerCommunicator.cs, change the ip address into what you get from your server.

## How to start

In your server, run the ServerSideDetection.py;

In unity/your android device (if you have built the project onto it, and it should be a compatible device with a camera), click the button to start/stop detection.

<img src="https://github.com/YM-Xiu/Object_detection_stream/blob/master/demo.png?raw=true" width="300">

