using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SignalProcessing.Core;

namespace SignalGenerator.App
{
    public partial class MainWindow : Window
    {
        private List<SingleChannelSignal> signalList = new List<SingleChannelSignal>();
        private readonly ThresholdAnalyzer _thresholdAnalyzer = new ThresholdAnalyzer();
        private SignalMarkerRenderer _markerRenderer;
        private int _markerCount = 0;
        private SignalAnalysisResult _analysisResult;

        public MainWindow()
        {
            InitializeComponent();

            _markerRenderer = new SignalMarkerRenderer(WpfPlot1.Plot);

            _thresholdAnalyzer.MarkerDetected += ThresholdAnalyzer_MarkerDetected_Draw;

            _thresholdAnalyzer.MarkerDetected += ThresholdAnalyzer_MarkerDetected_AddToList;

            SignalTypeComboBox.ItemsSource = Enum.GetValues(typeof(SignalType));
            SignalTypeComboBox.SelectedIndex = 0;
        }

        private void addSignal(SingleChannelSignal obj)
        {
            ClearAnalysis();
            signalList.Add(obj);
            WpfPlot1.Plot.Add.ScatterLine(obj.TimeTick, obj.Data);
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            ClearAnalysis();
            signalList.Clear();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
        }

        private void Button_Click_Concat(object sender, RoutedEventArgs e)
        {
            try
            {
                addSignal(SignalOperations.Concat(signalList));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_Generate(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(SignalTypeComboBox.SelectedItem is SignalType selectedType))
                {
                    MessageBox.Show("Выберите тип сигнала.", "Ошибка");
                    return;
                }

                float samplingRate = ReadFloat(
                    SamplingRateTextBox, "Частота квантования");

                float duration = ReadFloat(
                    DurationTextBox, "Длительность");

                var parameters = ReadParameters(selectedType);

                var generator = SignalGeneratorFactory.Create(parameters);

                var signal = generator.Generate(
                    new SamplingParameters(samplingRate, duration));

                addSignal(signal);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка генерации",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Button_Click_Median_Offline(object sender, RoutedEventArgs e)
        {
            try
            {
                if (signalList == null || signalList.Count == 0)
                {
                    MessageBox.Show("Нет сигналов для фильтрации.", "Ошибка");
                    return;
                }

                if (!float.TryParse(Window_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float windowSizeSec))
                {
                    MessageBox.Show("Некорректный размер окна. Введите число.", "Ошибка");
                    return;
                }

                var lastSignal = signalList.Last();
                int intWindowSize = (int)(windowSizeSec * lastSignal.Frequency + 0.5);

                if (intWindowSize <= 0 || intWindowSize % 2 == 0)
                {
                    MessageBox.Show("Для медианного фильтра размер окна в отсчетах должен быть положительным и нечётным.", "Ошибка");
                    return;
                }

                float[] result = OfflineMedianFilter.Filter(lastSignal.Data, intWindowSize);
                SingleChannelSignal newData = new SingleChannelSignal(lastSignal.Frequency, result);
                addSignal(newData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при медианной офлайн-фильтрации: {ex.Message}", "Ошибка");
            }
        }

        private void Button_Click_Median_Online(object sender, RoutedEventArgs e)
        {
            try
            {
                if (signalList == null || signalList.Count == 0)
                {
                    MessageBox.Show("Нет сигналов для фильтрации.", "Ошибка");
                    return;
                }

                if (!float.TryParse(Window_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float windowSizeSec))
                {
                    MessageBox.Show("Некорректный размер окна. Введите число.", "Ошибка");
                    return;
                }

                var lastSignal = signalList.Last();
                int intWindowSize = (int)(windowSizeSec * lastSignal.Frequency + 0.5);

                if (intWindowSize <= 0 || intWindowSize % 2 == 0)
                {
                    MessageBox.Show("Для медианного фильтра размер окна в отсчетах должен быть положительным и нечётным.", "Ошибка");
                    return;
                }

                var medianFilter = new OnlineMedianFilter(intWindowSize);
                float[] data = lastSignal.Data;
                float[] result = new float[data.Length];

                int chunkSize = 7;
                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    int currentChunkSize = Math.Min(chunkSize, data.Length - i);
                    float[] chunk = new float[currentChunkSize];
                    Array.Copy(data, i, chunk, 0, currentChunkSize);

                    float[] filteredChunk = medianFilter.Filter(chunk);
                    Array.Copy(filteredChunk, 0, result, i, currentChunkSize);
                }

                SingleChannelSignal newData = new SingleChannelSignal(lastSignal.Frequency, result);
                addSignal(newData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при медианной онлайн-фильтрации: {ex.Message}", "Ошибка");
            }
        }

        private void Button_Click_Offline(object sender, RoutedEventArgs e)
        {
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Frequency + 0.5);
            float[] result = OfflineFilter.Filter(signalList.Last().Data, intWindowSize);
            SingleChannelSignal newData = new SingleChannelSignal(signalList.Last().Frequency, result);
            addSignal(newData);
        }

        private void Button_Click_Online(object sender, RoutedEventArgs e)
        {
            try
            {
                if (signalList == null || signalList.Count == 0)
                {
                    MessageBox.Show("Нет сигналов для фильтрации.", "Ошибка");
                    return;
                }

                if (!float.TryParse(Window_textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float windowSizeSec))
                {
                    MessageBox.Show("Некорректный размер окна. Введите число.", "Ошибка");
                    return;
                }

                var lastSignal = signalList.Last();

                int intWindowSize = (int)(windowSizeSec * lastSignal.Frequency + 0.5);

                if (intWindowSize <= 0)
                {
                    MessageBox.Show("Размер окна должен быть больше нуля.", "Ошибка");
                    return;
                }

                var onlineFilter = new OnlineFilter(intWindowSize);

                float[] data = lastSignal.Data;
                float[] result = new float[data.Length];

                int chunkSize = 5;
                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    int currentChunkSize = Math.Min(chunkSize, data.Length - i);
                    float[] chunk = new float[currentChunkSize];

                    Array.Copy(data, i, chunk, 0, currentChunkSize);

                    float[] filteredChunk = onlineFilter.Filter(chunk);

                    Array.Copy(filteredChunk, 0, result, i, currentChunkSize);
                }

                SingleChannelSignal newData = new SingleChannelSignal(lastSignal.Frequency, result);
                addSignal(newData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при онлайн-фильтрации: {ex.Message}", "Ошибка");
            }
        }

        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            ClearAnalysis();
            if (signalList.Count == 0)
            {
                MessageBox.Show("yt elfkzq");
            }
            else
            {
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                signalList.Remove(signalList.Last());
                WpfPlot1.Refresh();
            }
        }

        private void Button_Click_Resample(object sender, RoutedEventArgs e)
        {
            try
            {
                if (signalList.Count == 0)
                {
                    MessageBox.Show("Нет сигналов.");
                    return;
                }
                float newFreq = float.Parse(NewFreq_textBox.Text, CultureInfo.InvariantCulture);
                SingleChannelSignal resampled = Resampler.Resample(signalList[signalList.Count - 1], newFreq);
                ClearAnalysis();
                signalList.RemoveAt(signalList.Count - 1);
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                addSignal(resampled);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_Summ(object sender, RoutedEventArgs e)
        {
            try
            {
                addSignal(SignalOperations.Sum(signalList));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Load(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog myDialog = new OpenFileDialog();
                myDialog.Filter = "csv-файл(*.csv)|*.csv";
                myDialog.CheckFileExists = true;
                myDialog.Multiselect = false;
                if (myDialog.ShowDialog() == true)
                {
                    string filePath = myDialog.FileName;
                    addSignal(SingleChannelSignalReader.Read(filePath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Write(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csv-файл (*.csv)|*.csv";
            saveFileDialog.FileName = "MySignal";
            saveFileDialog.DefaultExt = "csv";
            if (saveFileDialog.ShowDialog() == true && signalList.Count() != 0)
            {
                string filePath = saveFileDialog.FileName;
                SingleChannelSignalWriter.Write(signalList.Last(), filePath);
            }
        }

        private void ThresholdAnalyzer_MarkerDetected_Draw(
    object sender,
    MarkerDetectedEventArgs e)
        {
            _markerRenderer.AddMarker(
                e.Time,
                e.Value,
                e.Marker.Direction);

            WpfPlot1.Refresh();
        }

        private void ThresholdAnalyzer_MarkerDetected_AddToList(
    object sender,
    MarkerDetectedEventArgs e)
        {
            string direction = e.Marker.Direction == CrossingDirection.Up
                ? "вверх"
                : "вниз";

            string time = e.Time.ToString(
                "F3",
                CultureInfo.GetCultureInfo("ru-RU"));

            string value = e.Value.ToString(
                CultureInfo.GetCultureInfo("ru-RU"));

            MarkerListBox.Items.Add(
                $"{time} с; {value}; {direction}");

            _markerCount++;

            SummaryTextBlock.Text =
                $"Найдено пересечений: {_markerCount}";
        }

        private void Button_FindMarkers_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (signalList.Count == 0)
            {
                MessageBox.Show("Нет сигналов для анализа.", "Ошибка");
                return;
            }

            if (!float.TryParse(
                ThresholdTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float threshold)
                || float.IsNaN(threshold)
                || float.IsInfinity(threshold))
            {
                MessageBox.Show(
                    "Введите корректное пороговое значение.",
                    "Ошибка");

                return;
            }

            // Очищаем результаты предыдущего анализа.
            ClearAnalysis();

            try
            {
                // Анализируем последнюю добавленную кривую.
                _analysisResult = _thresholdAnalyzer.Analyze(
                    signalList.Last(),
                    threshold);

                SummaryTextBlock.Text =
                    $"Найдено пересечений: {_analysisResult.Markers.Count}";
            }
            catch (ArgumentException ex)
            {
                ClearAnalysis();
                MessageBox.Show(ex.Message, "Ошибка анализа");
            }

            WpfPlot1.Refresh();
        }

        private void Button_ClearMarkers_Click(
    object sender,
    RoutedEventArgs e)
        {
            ClearAnalysis();

            WpfPlot1.Refresh();
        }
        private void ClearAnalysis()
        {
            if (_markerRenderer != null)
            {
                _markerRenderer.ClearMarkers();
            }

            MarkerListBox.Items.Clear();

            SummaryTextBlock.Text = "Найдено пересечений: 0";

            _analysisResult = null;
            _markerCount = 0;
        }

        private float ReadFloat(TextBox textBox, string parameterName)
        {
            if (!float.TryParse(
                textBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value)
                || float.IsNaN(value)
                || float.IsInfinity(value))
            {
                throw new FormatException(
                    $"Некорректное значение параметра: {parameterName}");
            }

            return value;
        }

        private GeneratorParameters ReadParameters(SignalType type)
        {
            switch (type)
            {
                case SignalType.Sine:

                    return new SineParameters
                    {
                        Period = ReadFloat(PeriodTextBox, "Период"),
                        Phase = ReadFloat(PhaseTextBox, "Фаза"),
                        Amplitude = ReadFloat(AmplitudeTextBox, "Амплитуда")
                    };

                case SignalType.Line:

                    return new LineParameters
                    {
                        K = ReadFloat(LineKTextBox, "K"),
                        B = ReadFloat(LineBTextBox, "B")
                    };

                case SignalType.Noise:

                    return new NoiseParameters
                    {
                        Range = ReadFloat(NoiseRangeTextBox, "Размах шума")
                    };

                case SignalType.Triangle:

                    return new TriangleParameters
                    {
                        Period = ReadFloat(TrianglePeriodTextBox, "Период"),
                        Phase = ReadFloat(TrianglePhaseTextBox, "Фаза"),
                        Amplitude = ReadFloat(TriangleAmplitudeTextBox, "Амплитуда")
                    };
                case SignalType.Saw:

                    return new SawParameters
                    {
                        Period = ReadFloat(SawPeriodTextBox, "Период"),
                        Phase = ReadFloat(SawPhaseTextBox, "Фаза"),
                        Amplitude = ReadFloat(SawAmplitudeTextBox, "Амплитуда")
                    };
                case SignalType.Constant:

                    return new ConstantParameters
                    {
                        Value = ReadFloat(ConstantValueTextBox, "Период")
                    };

                default:

                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private void SignalTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SinePanel.Visibility = Visibility.Collapsed;
            LinePanel.Visibility = Visibility.Collapsed;
            NoisePanel.Visibility = Visibility.Collapsed;
            TrianglePanel.Visibility = Visibility.Collapsed;
            SawPanel.Visibility = Visibility.Collapsed;
            ConstantPanel.Visibility = Visibility.Collapsed;

            if (!(SignalTypeComboBox.SelectedItem is SignalType type))
                return;

            switch (type)
            {
                case SignalType.Sine:
                    SinePanel.Visibility = Visibility.Visible;
                    break;

                case SignalType.Line:
                    LinePanel.Visibility = Visibility.Visible;
                    break;

                case SignalType.Noise:
                    NoisePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Triangle:
                    TrianglePanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Saw:
                    SawPanel.Visibility = Visibility.Visible;
                    break;
                case SignalType.Constant:
                    ConstantPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}