﻿namespace OpenSense.Components.OpenFace {
    using System;
    using System.Windows;
    using System.Collections.Generic;

    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    using OpenFaceInterop;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Collections.Immutable;
    using OpenSense.DataStructure;
    using OpenSense.Utilities.DataWriter;
    using MathNet.Spatial.Euclidean;

    public class OpenFace : IConsumer<Shared<Image>>, INotifyPropertyChanged {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (!EqualityComparer<T>.Default.Equals(field, value)) {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private CLNF landmarkDetector;
        private FaceDetector faceDetector;
        private FaceAnalyser faceAnalyser;
        private GazeAnalyser gazeAnalyser;
        private FaceModelParameters faceModelParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFace"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public OpenFace(Pipeline pipeline) {
            // Image receiver.
            In = pipeline.CreateReceiver<Shared<Image>>(this, ReceiveImage, nameof(In));

            // Pose data emitter.
            HeadPoseOut = pipeline.CreateEmitter<HeadPose>(this, nameof(HeadPoseOut));

            // Gaze data emitter.
            GazeOut = pipeline.CreateEmitter<Gaze>(this, nameof(GazeOut));

            HeadPoseAndGazeOut = pipeline.CreateEmitter<HeadPoseAndGaze>(this, nameof(HeadPoseAndGazeOut));

            pipeline.PipelineRun += Initialize;
            pipeline.PipelineCompleted += OnPipelineCompleted;
        }

        private void Initialize(object sender, PipelineRunEventArgs e) {

            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            faceModelParameters = new FaceModelParameters(rootDirectory, true, false, false);
            faceModelParameters.optimiseForVideo();

            faceDetector = new FaceDetector(faceModelParameters.GetHaarLocation(), faceModelParameters.GetMTCNNLocation());
            if (!faceDetector.IsMTCNNLoaded()) {
                faceModelParameters.SetFaceDetector(false, true, false);
            }

            landmarkDetector = new CLNF(faceModelParameters);
            faceAnalyser = new FaceAnalyser(rootDirectory, true, 112, true);
            gazeAnalyser = new GazeAnalyser();

            landmarkDetector.Reset();
            faceAnalyser.Reset();
        }

        private float cameraCalibFx = 500;

        public float CameraCalibFx {
            get => cameraCalibFx;
            set => SetProperty(ref cameraCalibFx, value);
        }

        private float cameraCalibFy = 500;

        public float CameraCalibFy {
            get => cameraCalibFy;
            set => SetProperty(ref cameraCalibFy, value);
        }

        private float cameraCalibCx = 640 / 2f;

        public float CameraCalibCx {
            get => cameraCalibCx;
            set => SetProperty(ref cameraCalibCx, value);
        }

        private float cameraCalibCy = 480 / 2f;

        public float CameraCalibCy {
            get => cameraCalibCy;
            set => SetProperty(ref cameraCalibCy, value);
        }

        private bool mute = false;

        public bool Mute {
            get => mute;
            set => SetProperty(ref mute, value);
        }

        private IDataWriter dataWriter;

        public IDataWriter DataWriter {
            get => dataWriter;
            set => SetProperty(ref dataWriter, value);
        }

        /// <summary>
        /// Gets. Receiver that encapsulates the shared image input stream.
        /// </summary>
        public Receiver<Shared<Image>> In { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the pose data output stream.
        /// </summary>
        public Emitter<HeadPose> HeadPoseOut { get; private set; }

        /// <summary>
        /// Gets. Emitter that encapsulates the gaze data output stream.
        /// </summary>
        public Emitter<Gaze> GazeOut { get; private set; }

        public Emitter<HeadPoseAndGaze> HeadPoseAndGazeOut { get; private set; }

        /// <summary>
        /// The receive method for the ImageIn receiver.
        /// This executes every time a message arrives on ImageIn.
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        private void ReceiveImage(Shared<Image> input, Envelope envelope) {
            if (Mute) {
                return;
            }
            try {
                using (var colorSharedImage = ImagePool.GetOrCreate(input.Resource.Width, input.Resource.Height, input.Resource.PixelFormat))
                using (var graySharedImage = ImagePool.GetOrCreate(input.Resource.Width, input.Resource.Height, PixelFormat.Gray_8bpp)) {
                    input.Resource.CopyTo(colorSharedImage.Resource);
                    var colorImageBuffer = new ImageBuffer(colorSharedImage.Resource.Width, colorSharedImage.Resource.Height, colorSharedImage.Resource.ImageData, colorSharedImage.Resource.Stride);
                    var grayImageBuffer = new ImageBuffer(graySharedImage.Resource.Width, graySharedImage.Resource.Height, graySharedImage.Resource.ImageData, graySharedImage.Resource.Stride);
                    Methods.ToGray(colorImageBuffer, grayImageBuffer);
                    using (var colorRawImage = Methods.ToRaw(colorImageBuffer))
                    using (var grayRawImage = Methods.ToRaw(grayImageBuffer)) {
                        if (landmarkDetector.DetectLandmarksInVideo(colorRawImage, faceModelParameters, grayRawImage)) {

                            //double confidence = landmarkDetector.GetConfidence();
                            //float scale = landmarkDetector.GetRigidParams()[0];

                            //List<float> nonRigidParameters = landmarkDetector.GetNonRigidParams();
                            //List<bool> visibilities = landmarkDetector.GetVisibilities();
                            //faceAnalyser.AddNextFrame(colorRawImage, landmarkDetector.CalculateAllLandmarks(), detected, true);
                            //Dictionary<string, double> aus = faceAnalyser.GetCurrentAUsReg();

                            // Pose.
                            var landmarks = landmarkDetector.CalculateAllLandmarks().Select(m => new Point2D(m.Item1, m.Item2));
                            var visiableLandmarks = landmarkDetector.CalculateVisibleLandmarks().Select(m => new Point2D(m.Item1, m.Item2));
                            var landmarks3D = landmarkDetector.Calculate3DLandmarks(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy).Select(m => new Point3D(m.Item1, m.Item2, m.Item3));
                            var poseData = new List<float>();
                            landmarkDetector.GetPose(poseData, CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var box = landmarkDetector.CalculateBox(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var boxConverted = box.Select(line => new Tuple<Point2D, Point2D>(new Point2D((float)line.Item1.X, (float)line.Item1.Y), new Point2D((float)line.Item2.X, (float)line.Item2.Y)));
                            var headPose = new HeadPose(poseData, landmarks, visiableLandmarks, landmarks3D, boxConverted);
                            HeadPoseOut.Post(headPose, envelope.OriginatingTime);

                            // Gaze.
                            gazeAnalyser.AddNextFrame(landmarkDetector, true, CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var eyeLandmarks = landmarkDetector.CalculateAllEyeLandmarks().Select(m => new Point2D(m.Item1, m.Item2));
                            var visiableEyeLandmarks = landmarkDetector.CalculateVisibleEyeLandmarks().Select(m => new Point2D(m.Item1, m.Item2));
                            var eyeLandmarks3D = landmarkDetector.CalculateAllEyeLandmarks3D(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy).Select(m => new Point3D(m.Item1, m.Item2, m.Item3));
                            var (leftPupil, rightPupil) = gazeAnalyser.GetGazeCamera();
                            var (angleX, angleY) = gazeAnalyser.GetGazeAngle();//Not accurate
                            var gazeLines = gazeAnalyser.CalculateGazeLines(CameraCalibFx, CameraCalibFy, CameraCalibCx, CameraCalibCy);
                            var gazeLinesConverted = gazeLines.Select(line => new Tuple<Point2D, Point2D>(new Point2D((float)line.Item1.X, (float)line.Item1.Y), new Point2D((float)line.Item2.X, (float)line.Item2.Y)));
                            var gaze = new Gaze(
                                    new Pupil(
                                            new Point3D(leftPupil.Item1, leftPupil.Item2, leftPupil.Item3),
                                            new Point3D(rightPupil.Item1, rightPupil.Item2, rightPupil.Item3)
                                        ),
                                    new Point2D(angleX, angleY),
                                    eyeLandmarks,
                                    visiableEyeLandmarks,
                                    eyeLandmarks3D,
                                    gazeLinesConverted
                                );
                            GazeOut.Post(gaze, envelope.OriginatingTime);

                            var headPoseAndGaze = new HeadPoseAndGaze(headPose.DeepClone(), gaze.DeepClone());
                            DataWriter?.Write(headPoseAndGaze, envelope);
                            HeadPoseAndGazeOut.Post(headPoseAndGaze, envelope.OriginatingTime);

                        }
                    }
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"An exception is raised in {GetType().Name}: {ex.ToString()}");
                Mute = true;
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e) {
        }
    }
}
