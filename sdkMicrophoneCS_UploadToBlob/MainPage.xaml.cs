/* 
    Copyright (c) 2011 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using System;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Windows;
using System.Globalization;
using Microsoft.WindowsAzure.Samples.Phone.Storage;
using sdkMicrophoneCS.Tables;

namespace sdkMicrophoneCS
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Microphone microphone = Microphone.Default;     // Object representing the physical microphone on the device
        private byte[] buffer;                                  // Dynamic buffer to retrieve audio data from the microphone
        private MemoryStream stream = new MemoryStream();       // Stores the audio data for later playback
        private SoundEffectInstance soundInstance;              // Used to play back audio
        private bool soundIsPlaying = false;                    // Flag to monitor the state of sound playback
        private bool soundIsUploading = false;                  // Flag to monitor the state of sound upload

        private string applicationId = "sdkMicrophoneCS_UploadToBlob";
        private string containerName = "notes";
        private readonly ICloudBlobClient blobClient;
        //private readonly ICloudNoteClient cloudNoteClient;


        // Status images
        private BitmapImage blankImage;
        private BitmapImage microphoneImage;
        private BitmapImage speakerImage;

        /// <summary>
        /// Constructor 
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Timer to simulate the XNA Framework game loop (Microphone is 
            // from the XNA Framework). We also use this timer to monitor the 
            // state of audio playback so we can update the UI appropriately.
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(33);
            dt.Tick += new EventHandler(dt_Tick);
            dt.Start();

            // Event handler for getting audio data when the buffer is full
            microphone.BufferReady += new EventHandler<EventArgs>(microphone_BufferReady);

            blankImage = new BitmapImage(new Uri("Images/blank.png", UriKind.RelativeOrAbsolute));
            microphoneImage = new BitmapImage(new Uri("Images/microphone.png", UriKind.RelativeOrAbsolute));
            speakerImage = new BitmapImage(new Uri("Images/speaker.png", UriKind.RelativeOrAbsolute));

            // Initialize storage
            this.blobClient = CloudStorageContext.Current.Resolver.CreateCloudBlobClient();
        }

        /// <summary>
        /// Updates the XNA FrameworkDispatcher and checks to see if a sound is playing.
        /// If sound has stopped playing, it updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dt_Tick(object sender, EventArgs e)
        {
            try { FrameworkDispatcher.Update(); }
            catch { }

            if (true == soundIsPlaying)
            {
                if (soundInstance.State != SoundState.Playing)
                {
                    // Audio has finished playing
                    soundIsPlaying = false;

                    // Update the UI to reflect that the 
                    // sound has stopped playing
                    SetButtonStates(true, true, false, true);
                    UserHelp.Text = "press play\nor record";
                    StatusImage.Source = blankImage;
                }
            }
        }

        /// <summary>
        /// The Microphone.BufferReady event handler.
        /// Gets the audio data from the microphone and stores it in a buffer,
        /// then writes that buffer to a stream for later playback.
        /// Any action in this event handler should be quick!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void microphone_BufferReady(object sender, EventArgs e)
        {
            // Retrieve audio data
            microphone.GetData(buffer);

            // Store the audio data in a stream
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Handles the Click event for the record button.
        /// Sets up the microphone and data buffers to collect audio data,
        /// then starts the microphone. Also, updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void recordButton_Click(object sender, EventArgs e)
        {
            // Get audio data in 1/2 second chunks
            microphone.BufferDuration = TimeSpan.FromMilliseconds(500);

            // Allocate memory to hold the audio data
            buffer = new byte[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];

            // Set the stream back to zero in case there is already something in it
            stream.SetLength(0);

            // Write WAV header information
            Helper.WriteWavHeader(stream, microphone.SampleRate);

            // Start recording
            microphone.Start();

            SetButtonStates(false, false, true, false);
            UserHelp.Text = "record";
            StatusImage.Source = microphoneImage;
        }

        /// <summary>
        /// Handles the Click event for the stop button.
        /// Stops the microphone from collecting audio and updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopButton_Click(object sender, EventArgs e)
        {
            if (microphone.State == MicrophoneState.Started)
            {
                // In RECORD mode, user clicked the 
                // stop button to end recording
                microphone.Stop();

                // Updated stream with WAV header information
                Helper.UpdateWavHeader(stream);
            }
            else if (soundInstance.State == SoundState.Playing)
            {
                // In PLAY mode, user clicked the 
                // stop button to end playing back
                soundInstance.Stop();
            }

            SetButtonStates(true, true, false, true);
            UserHelp.Text = "ready";
            StatusImage.Source = blankImage;
        }

        /// <summary>
        /// Handles the Click event for the play button.
        /// Plays the audio collected from the microphone and updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playButton_Click(object sender, EventArgs e)
        {
            if (stream.Length > 0)
            {
                // Update the UI to reflect that
                // sound is playing
                SetButtonStates(false, false, true, false);
                UserHelp.Text = "play";
                StatusImage.Source = speakerImage;

                // Play the audio in a new thread so the UI can update.
                Thread soundThread = new Thread(new ThreadStart(playSound));
                soundThread.Start();
            }
        }

        /// <summary>
        /// Plays the audio using SoundEffectInstance 
        /// so we can monitor the playback status.
        /// </summary>
        private void playSound()
        {
            // Play audio using SoundEffectInstance so we can monitor it's State 
            // and update the UI in the dt_Tick handler when it is done playing.
            SoundEffect sound = new SoundEffect(stream.ToArray(), microphone.SampleRate, AudioChannels.Mono);
            soundInstance = sound.CreateInstance();
            soundIsPlaying = true;
            soundInstance.Play();
        }

        /// <summary>
        /// Helper method to change the IsEnabled property for the ApplicationBarIconButtons.
        /// </summary>
        /// <param name="recordEnabled">New state for the record button.</param>
        /// <param name="playEnabled">New state for the play button.</param>
        /// <param name="stopEnabled">New state for the stop button.</param>
        private void SetButtonStates(bool recordEnabled, bool playEnabled, bool stopEnabled, bool uploadEnabled)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = recordEnabled;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = playEnabled;
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = stopEnabled;
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = uploadEnabled;
        }


        /// <summary>
        /// Handles the Click event for the stop button.
        /// Stops the microphone from collecting audio and updates the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uploadButton_Click(object sender, EventArgs e)
        {
            UploadAudio(
                successMessage =>
                {
                    MessageBox.Show("Upload successful", "upload result message", MessageBoxButton.OK);
                    //this.NavigationService.GoBack();
                },
                errorMessage =>
                {
                    MessageBox.Show(errorMessage, "upload result message", MessageBoxButton.OK);
                    //this.NavigationService.GoBack();
                });
        }

        public void UploadAudio(Action<string> successCallback, Action<string> failureCallback)
        {
            successCallback = successCallback ?? (r => { });
            failureCallback = failureCallback ?? (r => { });

            soundIsUploading = true;

            var container = this.blobClient.GetContainerReference(this.containerName);
            container.CreateIfNotExist(
                BlobContainerPublicAccessType.Container,
                r =>
                {
                    // Successfull container creation.
                    if (r.Success)
                    {
                        string blobName = string.Format(CultureInfo.InvariantCulture, "{0}-{1}.wav", DateTime.Now.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture), Guid.NewGuid());

                        var blob = container.GetBlobReference(blobName);

                        blob.Properties.ContentType = "audio/wav";

                        //blob.Metadata["alias"] = userAlias;
                        //blob.Metadata["date"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);

                        this.stream.Position = 0;

                        blob.UploadFromStream(
                            this.stream,
                            response =>
                            {
                                if (response.Success)
                                {
                                    CloudNote cloudNote = new CloudNote();
                                    cloudNote.ApplicationId = this.applicationId;
                                    cloudNote.DeviceId = Helper.DeviceId;
                                    cloudNote.PartitionKey = "a";
                                    cloudNote.Time = DateTime.Now;
                                    cloudNote.Uri = blob.Uri.AbsoluteUri;

                                    var tableClient = CloudStorageContext.Current.Resolver.CreateCloudTableClient();
                                    var tableName = "CloudNotes";

                                    tableClient.CreateTableIfNotExist(
                                        tableName,
                                        p =>
                                        {
                                            var context = CloudStorageContext.Current.Resolver.CreateTableServiceContext();

                                            context.AddObject(tableName, cloudNote);
                                            context.BeginSaveChanges(
                                                asyncResult =>
                                                {
                                                    var response2 = context.EndSaveChanges(asyncResult);

                                                    // Some logic here.
                                                },
                                                null);
                                        });
                                }
                                else
                                {
                                    soundIsUploading = false;

                                    failureCallback(string.Concat("Error: ", response.ErrorMessage));
                                }

                                stream.Close();
                            });
                    }
                    else
                    {
                        soundIsUploading = false;

                        // The container was not created successfully, some error has ocurred.
                        failureCallback(string.Concat("Error: ", r.ErrorMessage));
                    }
                });
        }
    }
}
