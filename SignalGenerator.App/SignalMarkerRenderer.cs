
using System;
using System.Collections.Generic;
using ScottPlot;
using SignalProcessing.Core;

namespace SignalGenerator.App
{
    public sealed class SignalMarkerRenderer
    {
        private readonly Plot _plot;

        private readonly List<ScottPlot.Plottables.Marker> _markers =
            new List<ScottPlot.Plottables.Marker>();

        public SignalMarkerRenderer(Plot plot)
        {
            if (plot == null)
                throw new ArgumentNullException(nameof(plot));

            _plot = plot;
        }

        public void AddMarker(
            float time,
            float value,
            CrossingDirection direction)
        {
            var marker = _plot.Add.Marker(time, value);

            marker.MarkerStyle.Shape = MarkerShape.FilledCircle;
            marker.MarkerStyle.Size = 9;

            if (direction == CrossingDirection.Up)
            {
                marker.MarkerStyle.FillColor = Colors.Green;
            }
            else
            {
                marker.MarkerStyle.FillColor = Colors.Red;
            }

            _markers.Add(marker);
        }

        public void ClearMarkers()
        {
            foreach (var marker in _markers)
            {
                _plot.Remove(marker);
            }

            _markers.Clear();
        }
    }
}
