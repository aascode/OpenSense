﻿using System;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using OpenSense.Component.Contract;

namespace OpenSense.Component.Psi.Audio {
    [Serializable]
    public class WaveFileAudioSourceConfiguration : ConventionalComponentConfiguration {

        private string filename;

        public string Filename {
            get => filename;
            set => SetProperty(ref filename, value);
        }

        private DateTime? audioStartTime = null;

        public DateTime? AudioStartTime {
            get => audioStartTime;
            set => SetProperty(ref audioStartTime, value);
        }

        private int targetLatencyMs = 20;

        public int TargetLatencyMs {
            get => targetLatencyMs;
            set => SetProperty(ref targetLatencyMs, value);
        }

        public override IComponentMetadata GetMetadata() => new WaveFileAudioSourceMetadata();

        protected override object Instantiate(Pipeline pipeline) => new WaveFileAudioSource(pipeline, Filename, AudioStartTime, TargetLatencyMs);
    }
}