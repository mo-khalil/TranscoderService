# Windows Transcoder Service using HandBrakeCLI developed for Wowza Streaming Engine

## What is this?

It's a cut down version of a Windows service application I developed for an education client. The requirements for the brief were:

### Server-side:

1. We have a Windows 2012 R2 streaming server with FTP access for video editors and Wowza Streaming Engine;

2. When a video file is uploaded via FTP, the server should transcode the video in to multiple file formats;

3. When transcoding is finished, the server should neatly organise the files in to folders belonging to each of the streaming server's "applications" and delete the original;

4. We don't have time to monitor this server for new files so all of this needs to be automated.

### Client-side:

1. Video editors should simply be required to upload full HD videos to the server. That's it! Nothing more should be expected of them.

## The solution

This Windows service does everything required of the brief. It runs at intervals specified in the `App.config` file, watching the FTP upload folder (also in `App.config`) for new video files. If any are found, the service transcodes them using [Handbrake's CLI](http://handbrake.fr) with [arguments](https://trac.handbrake.fr/wiki/CLIGuide) specified in the `TranscoderArguments.txt` file.

To keep the video files organised, the video uploader should follow the file naming convention: WowzaApplicationName.SomeFolder.My video file.mp4

The service will place the transcoded video file in to the folder and file name: WowzaApplicationName/SomeFolder/My-video-file.mp4

Only the WowzaApplicationName and file name (including extension) are mandatory; sub-folders are optional. If folders don't exist, the service will create them. Any number of folders can be created (limited only by OS file name character lengths) and if no folders are specified, then the file will be saved under the [WowzaApplicationName] root.
