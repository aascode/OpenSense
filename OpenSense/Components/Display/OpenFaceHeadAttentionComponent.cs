﻿using Microsoft.Psi;
using OpenSense.DataStructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenSense.Components {
    public class OpenFaceHeadAttentionComponent : IConsumer<HeadPose>, INotifyPropertyChanged {

        public Receiver<HeadPose> In { get; private set; }

        public OpenFaceHeadAttentionComponent(Pipeline pipeline) {
            In = pipeline.CreateReceiver<HeadPose>(this, Porcess, nameof(In));
            pipeline.PipelineCompleted += PipelineCompleted;
        }

        // copied from previous impl
        private void Porcess(HeadPose pose, Envelope envelope) {
            // pose: T_x, T_y, T_z, R_x, R_y, R_z
            var angleX = pose.Angle.X;
            var angleY = pose.Angle.Y;

            var angleXConf = 0.75;
            var angleYConf = 0.75;

            // Maximum possible confidence corresponds to this gradient width.
            const double minGradientWidth = 0.04;

            // Set width of mark based on confidence.
            // A confidence of 0 would give us a gradient that fills whole area diffusely.
            // A confidence of 1 would give us the narrowest allowed gradient width.
            var halfWidthX = Math.Max(1 - angleXConf, minGradientWidth) / 2;
            var halfWidthY = Math.Max(1 - angleYConf, minGradientWidth) / 2;

            //// Update the gradient to reflect confidence.
            //this.beamBarGsPre.Offset = Math.Max(this.beamBarGsMain.Offset - halfWidth, 0);
            //this.beamBarGsPost.Offset = Math.Min(this.beamBarGsMain.Offset + halfWidth, 1);

            // Convert from radians to degrees for display purposes.
            AngleXDeg = angleX * 180.0f / Math.PI;
            AngleYDeg = angleY * 180.0f / Math.PI;

            HeadAttentionStats = AngleXDeg >= -5 && AngleXDeg <= 5 && AngleYDeg >= -5 && AngleYDeg <= 5;

            //// Rotate gradient to match angle.
            //beamBarRotation.Angle = -beamAngleInDeg;
            //beamNeedleRotation.Angle = -beamAngleInDeg;
        }

        private void PipelineCompleted(object sender, PipelineCompletedEventArgs e) {
            AngleXDeg = 0;
            AngleYDeg = 0;
            HeadAttentionStats = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (!EqualityComparer<T>.Default.Equals(field, value)) {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private double angleXDeg;

        public double AngleXDeg {
            get => angleXDeg;
            private set => SetProperty(ref angleXDeg, value);
        }

        private double angleYDeg;

        public double AngleYDeg {
            get => angleYDeg;
            private set => SetProperty(ref angleYDeg, value);
        }

        private bool headAttentionStats;

        public bool HeadAttentionStats {
            get => headAttentionStats;
            set => SetProperty(ref headAttentionStats, value);
        }
    }
}
