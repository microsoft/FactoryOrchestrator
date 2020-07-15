// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// A result entry page that queries the user if a given TaskRun for an external/UWP task passed or failed on the DUT.
    /// The page is automatically navigated from if the TaskRun is "completed" by a remote FO client.
    /// </summary>
    public sealed partial class ExternalTestResultPage : Page
    {
        public ExternalTestResultPage()
        {
            this.InitializeComponent();
            updateLock = new object();
            testReportReady = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;

            // Get TaskRun we are reporting results for
            taskRun = ((App)Application.Current).RunWaitingForResult;

            taskRunPoller = new ServerPoller(taskRun.Guid, typeof(TaskRun), 1000);
            taskRunPoller.OnUpdatedObject += OnUpdatedRun;
            taskRunPoller.OnException += TaskRunPoller_OnException;
            taskRunPoller.StartPolling(Client);

            // Append task details to UI
            string taskRunText = taskRun.TaskName;
            if (!String.IsNullOrEmpty(taskRunText))
            {
                TestText.Text = taskRunText;
                TestText.Visibility = Visibility.Visible;
            }
            
            if (taskRun.TaskPath != taskRun.TaskName)
            {
                string taskRunPath = taskRun.TaskPath;
                if (!String.IsNullOrEmpty(taskRunPath)) 
                {
                    PathText.Text = taskRunPath;
                    PathText.Visibility = Visibility.Visible;
                    PathTextLabel.Visibility = Visibility.Visible;
                }
                
                string mediaPath = taskRun.TaskPath;
                MediaType mediaType = GetInstructionalMediaType(mediaPath);                                                                    
                if (mediaType == MediaType.Image)
                {               
                    AddSourceToImage(mediaPath, InstructionalImage, MediaProblems);                    
                }
                else if (mediaType == MediaType.Video)
                {                                                         
                    AddSourceToVideoAndDisplay(mediaPath, InstructionalVideo, MediaProblems);                                                                 
                }
            }

            string argsString = taskRun.Arguments;
            string taskRunString = taskRun.Guid.ToString();
            if (!String.IsNullOrEmpty(argsString)) 
            {
                ArgsText.Text = argsString;
                ArgsText.Visibility = Visibility.Visible;
                ArgsTextLabel.Visibility = Visibility.Visible;
            }           
            
            if (!String.IsNullOrEmpty(taskRunString)) 
            {
                TaskRunText.Text = taskRunString;
                TaskRunText.Visibility = Visibility.Visible;
                TaskRunTextLabel.Visibility = Visibility.Visible;
            }
            base.OnNavigatedTo(e);
        }
   
        /// <summary>
        /// Given the file path to the media, it will determine if the file extenstion is one of the supported image file extensions or video file extensions, and return string "image" or "video", respectively
        /// </summary>
        /// <param name="mediaPath"></param>
        /// <returns></returns>
        private MediaType GetInstructionalMediaType(string mediaPath) 
        {         
            // TODO: Import all of the supported extensions from config.json, and store it in a scope outside of this function and page so it doesn't parse a json tree every time this is run
            
            // Strip the file path to be just the file extension
            string mediaType = Path.GetExtension(mediaPath);

            // Check if the path ends in a supported image extension
            if (SupportedImageExtensions.Any(x => x.Equals(mediaType, StringComparison.OrdinalIgnoreCase))) 
            {
                return MediaType.Image;
            }
            if (SupportedVideoExtensions.Any(x => x.Equals(mediaType, StringComparison.OrdinalIgnoreCase)))
            {
                return MediaType.Video;
            }
   
            return MediaType.None;
        }

        private async void AddSourceToImage(string devicePath, Image img, TextBlock errorText) 
        {
            string desiredName = Path.GetFileName(devicePath);

            try
            {
                string newPath = Path.Combine(localFolder.Path, desiredName);
                await Client.GetFileFromDevice(devicePath, newPath);               
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(img.BaseUri, newPath);
                img.Source = bitmapImage;
                img.Visibility = Visibility.Visible;
            }
            catch (Exception addImage)
            {
                errorText.Text = addImage.ToString();
                errorText.Visibility = Visibility.Visible;
            }
        }
        
        private async void AddSourceToVideoAndDisplay(string devicePath, MediaPlayerElement mediaPlayer, TextBlock errorText) 
        {
            if (devicePath != null)
            {
                try
                {
                    // Create a new file in the current folder.
                    string desiredName = Path.GetFileName(devicePath);
                    string newPath = Path.Combine(localFolder.Path, desiredName);
                    await Client.GetFileFromDevice(devicePath, newPath);                  
                    StorageFile videoFile = await StorageFile.GetFileFromPathAsync(newPath);
                    mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(newPath));
                    mediaPlayer.MediaPlayer.IsLoopingEnabled = true;
                    mediaPlayer.Visibility = Visibility.Visible;
                    VideoButtonTray.Visibility = Visibility.Visible;
                    mediaPlayer.MediaPlayer.Play();
                }
                catch(Exception videoException) 
                {
                    errorText.Text = videoException.ToString();
                    errorText.Visibility = Visibility.Visible;
                }
            }
        }
        
        
        private void TaskRunPoller_OnException(object source, ServerPollerExceptionHandlerArgs e)
        {
            if (e.Exception.GetType() == typeof(FactoryOrchestratorUnkownGuidException))
            {
                // Run no longer valid, mark as aborted
                taskRun.TaskStatus = TaskStatus.Aborted;

                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ExitPage();
                });
            }
            else
            {
                // Call global error handler
                ((App)Application.Current).OnServerPollerException(source, e);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (taskRunPoller != null)
            {
                taskRunPoller.StopPolling();
                taskRunPoller = null;
            }
            InstructionalVideo.MediaPlayer.Dispose();
            base.OnNavigatedFrom(e);            
        }

        /// <summary>
        /// Periodically checks if the TaskRun has been completed.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnUpdatedRun(object source, ServerPollerEventArgs e)
        {
            lock (updateLock)
            {
                if (!testReportReady)
                {
                    taskRun = (TaskRun)e.Result;

                    if (taskRun.TaskRunComplete)
                    {
                        _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                ExitPage();
                            });
                    }
                }
            }
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Aborted);
        }

        private void FailButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Failed);
        }

        private void PassButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Passed);
        }

        private async void ReportTaskRunResultAsync(TaskStatus result)
        {
            lock (updateLock)
            {   
                // Prevent OnUpdatedRun from firing
                taskRunPoller.StopPolling();

                if (taskRun.TaskRunComplete)
                {
                    // The task was finished right before user interaction. Return, the poll event handler will exit the page.
                    return;
                }

                testReportReady = true;
                taskRun.TaskStatus = result;
                if (!String.IsNullOrWhiteSpace(CommentBox.Text))
                {
                    taskRun.TaskOutput.Add("------- Start Comments -------");
                    foreach (var line in CommentBox.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        taskRun.TaskOutput.Add(line);
                    }
                    taskRun.TaskOutput.Add("------- End Comments -------");
                }

                if (result != TaskStatus.Aborted)
                {
                    // Don't consider the task "done" until the task passed/failed and that result was chosen by the user.
                    // This is consistent with how FactoryOrchestratorServer handles exe & TAEF tests.
                    taskRun.TimeFinished = DateTime.Now;

                    // Set the exit code
                    taskRun.ExitCode = result == (TaskStatus.Passed) ? 0 : -1;
                }

            }

            // Report selected result to server
            bool updated = false;
            while (!updated)
            {
                try
                {
                    await Client.UpdateTaskRun(taskRun);
                    updated = true;
                }
                catch (FactoryOrchestratorUnkownGuidException)
                {
                    // Run no longer valid, mark as aborted
                    taskRun.TaskStatus = TaskStatus.Aborted;
                }
                catch (FactoryOrchestratorConnectionException)
                {
                    ((App)Application.Current).OnConnectionFailure();
                    while ((((App)Application.Current).OnConnectionPage) || (!Client.IsConnected))
                    {
                        await Task.Delay(1000);
                    }
                }
            }
            ExitPage();
        }

        private void ExitPage()
        {   
            // Update App task, so the ServiceEvent code knows we finished
            ((App)Application.Current).RunWaitingForResult.TaskStatus = taskRun.TaskStatus;

            if (this.Frame.CanGoBack)
            {
                // Return to last page
                this.Frame.GoBack();
            }
            else
            {
                // Return to MainPage
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private void InstructionalVideoPause_Click(object sender, RoutedEventArgs e)
        {
            InstructionalVideo.MediaPlayer.Pause();
        }

        private void InstructionalVideoPlay_Click(object sender, RoutedEventArgs e)
        {
            InstructionalVideo.MediaPlayer.Play();
        }

        private enum MediaType
        {
            Image,
            Video,
            None
        }
        private readonly StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        // List of all supported image extensions
        private readonly List<string> SupportedImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
        // List of all supported video extensions
        private readonly List<string> SupportedVideoExtensions = new List<string> { ".MP4", ".AVI", ".WMV" };
        private bool testReportReady;
        private TaskRun taskRun;
        private ServerPoller taskRunPoller;
        private object updateLock;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;

       
    }
}
