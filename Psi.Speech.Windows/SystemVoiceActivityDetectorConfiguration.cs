﻿using System;
using Microsoft.Psi;
using Microsoft.Psi.Speech;
using OpenSense.Component.Contract;

namespace OpenSense.Component.Psi.Speech {
    [Serializable]
    public class SystemVoiceActivityDetectorConfiguration : ConventionalComponentConfiguration {

        private Microsoft.Psi.Speech.SystemVoiceActivityDetectorConfiguration raw = new Microsoft.Psi.Speech.SystemVoiceActivityDetectorConfiguration();

        public Microsoft.Psi.Speech.SystemVoiceActivityDetectorConfiguration Raw {
            get => raw;
            set => SetProperty(ref raw, value);
        }

        public override IComponentMetadata GetMetadata() => new SystemVoiceActivityDetectorMetadata();

        protected override object Instantiate(Pipeline pipeline) => new SystemVoiceActivityDetector(pipeline, Raw);
    }
}