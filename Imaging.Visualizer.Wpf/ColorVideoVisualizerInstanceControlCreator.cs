﻿using System.Composition;
using System.Windows;
using OpenSense.Wpf.Component.Contract;

namespace OpenSense.Component.Imaging.Visualizer {
    [Export(typeof(IInstanceControlCreator))]
    public class ColorVideoVisualizerInstanceControlCreator : IInstanceControlCreator {

        public bool CanCreate(object instance) => instance is ColorVideoVisualizer;

        public UIElement Create(object instance) => new ColorVideoVisualizerInstanceControl((ColorVideoVisualizer)instance);
    }
}
