﻿using System;
using Microsoft.Psi;
using OpenSense.Component.Contract;
using OpenSense.Component.OpenPose.PInvoke.Configuration;

namespace OpenSense.Component.OpenPose {
    [Serializable]
    public class OpenPoseConfiguration : ConventionalComponentConfiguration {

        private AggregateStaticConfiguration raw = new AggregateStaticConfiguration();

        public AggregateStaticConfiguration Raw {
            get => raw;
            set => SetProperty(ref raw, value);
        }

        public override IComponentMetadata GetMetadata() => new OpenPoseMetadata();

        protected override object Instantiate(Pipeline pipeline) => new OpenPose(pipeline, Raw);
    }
}