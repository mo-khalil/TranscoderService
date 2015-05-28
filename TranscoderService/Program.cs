using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace TranscoderService
{
	public static class Program
	{
		static void Main()
		{
			var ServicesToRun = new ServiceBase[] 
			{ 
				new TranscoderService() 
			};
			ServiceBase.Run(ServicesToRun);
		}

		public static void Transcode()
		{
			/*
			 * Create an array and fill it with each transcode setting:
			 * Quality on the left and arguments on the right
			*/
			var arguments = new Dictionary<string, string>();
			using (var streamReader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + ConfigurationManager.AppSettings["TranscoderArguments"]))
			{
				string line;
				while ((line = streamReader.ReadLine()) != null) arguments.Add(line.Split('>')[0].Trim(), line.Split('>')[1].Trim());
			}
			// Get the temporary folder where transcoding should take place
			var transcodeFolder = ConfigurationManager.AppSettings["TranscodingFolder"] + "\\";
			// Get the "watch" folder, e.g. an FTP upload folder
			var watchFolder = ConfigurationManager.AppSettings["WatchFolder"] + "\\";
			DirectoryInfo dirInfo = new DirectoryInfo(watchFolder);
			/*
			 * Look for any MP4 files in the watch folder.
			 * See here if you want to get files of varying extensions:
			 * http://stackoverflow.com/questions/3527203/getfiles-with-multiple-extentions
			*/
			FileInfo[] files = dirInfo.GetFiles("*.mp4");
			// If there aren't any files, there's nothing more to do.
			if (files.Length == 0)
			{
				WriteLog("No files found.");
				return;
			}
			int count = 0;
			/*
			 * If the watch folder is an FTP upload folder, then this service
			 * should only work on video files with a length > 0 bytes.
			 * "Upload in progress" files on a Windows FTP server will retain
			 * a file size of 0 bytes until the file has finished uploading.
			 * So, this service should ignore these files.
			*/
			foreach (FileInfo file in files.Where(file => file.Length > 0))
			{
				// Pause the service timer so that it doesn't run while we are transcoding
				if (TranscoderService.TimerStatus()) TranscoderService.DisableTimer();
				WriteLog("Found file " + file.Name);
				var outputFolder = ConfigurationManager.AppSettings["OutputFolder"] + "\\";
				var outputfile = file.Name.Remove(file.Name.Length - 4);
				/*
				 * I'm using Wowza Media Server with multiple VOD applications.
				 * I want to keep the media folder organised using the following
				 * folder and file naming convention:
				 * [OutputFolder]/VodApplicationName/CategoryName/VideoFile.mp4
				 * 
				 * When the editing department uploads a video file via FTP, I've told them
				 * to include the application name and (optional) folder structure in the
				 * file name so that I don't have to monitor the media server for new files
				 * and manually organise them if there are any.
				 * 
				 * So, if a file is uploaded with the file name:
				 * MyVodApp.Latest-Videos.My latest video.mp4
				 * Then after transcoding, this service will move the file in to the folder
				 * [OutputFolder]/MYVODAPP/LATEST-VIDEOS/My-latest-video.mp4
				*/
				var array = outputfile.Split('.');
				for (int i = 0; i < array.Length; i++)
				{
					if (i == array.Length - 1) outputfile = array[i];
					else
					{
						outputFolder += array[i].Replace(" ", "-").Replace(".", "").ToUpper() + "\\";
						DirectoryInfo di = new DirectoryInfo(outputFolder);
						if (!di.Exists) di.Create();
					}
				}
				outputfile = outputfile.Replace(" ", "-").Replace(".", "") + "-";
				foreach (KeyValuePair<string, string> argument in arguments)
				{
					outputfile += argument.Key + ".mp4";
					// Create a new process using HandBrakeCLI.exe and pass the transcoding arguments to it
					var process = new Process
					{
						StartInfo =
						{
							FileName = @"C:\Program Files\Handbrake\HandBrakeCLI.exe",
							Arguments = argument.Value.Replace("##INPUT##", file.FullName).Replace("##OUTPUT##", transcodeFolder + outputfile),
							UseShellExecute = false,
							RedirectStandardOutput = true,
							WorkingDirectory = watchFolder
						}
					};
					WriteLog("Starting transcoding file " + file.FullName);
					// Start the process
					process.Start();
					var output = process.StandardOutput.ReadToEnd();
					process.WaitForExit();
					// Process has ended
					WriteLog("Finished transcoding file " + file.FullName);
					// If this is a re-upload, delete the existing file before saving the new file
					if (File.Exists(outputFolder + outputfile)) File.Delete(outputFolder + outputfile);
					File.Move(transcodeFolder + outputfile, outputFolder + outputfile);
					WriteLog("File moved from " + transcodeFolder + outputfile + " to " + outputFolder + outputfile);
					outputfile = outputfile.Remove(outputfile.LastIndexOf("-", StringComparison.Ordinal) + 1);
				}
				WriteLog("Deleting file " + file.Name);
				// The uploaded file in the watch folder is no longer required so delete it
				file.Delete();
				count++;
			}
			if (count > 0) WriteLog("Files processed: " + count);
			if (!TranscoderService.TimerStatus()) TranscoderService.EnableTimer();
		}

		public static void WriteLog(string message)
		{
			try
			{
				var sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\TranscoderService.log", true);
				sw.WriteLine(DateTime.Now + ": " + message);
				sw.Flush();
				sw.Close();
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}