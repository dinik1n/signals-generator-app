using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SignalProcessing.Core;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace SignalGenerator.App
{
    public partial class MainWindow : Window
    {
        private SingleChannelSignal sig;
        private List<SingleChannelSignal> signalList = new List<SingleChannelSignal>();
        public MainWindow()
        {
            InitializeComponent();
            
        }
        private void addSignal(SingleChannelSignal obj)
        {

            signalList.Add(obj);
            WpfPlot1.Plot.Add.ScatterLine(obj.TimeTick, obj.Data);
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }


        private void Button_Click_Sin(object sender, RoutedEventArgs e)
        {
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            float period = (float.Parse(period_textBox.Text, CultureInfo.InvariantCulture));
            float phase = (float.Parse(phase_textBox.Text, CultureInfo.InvariantCulture));

            addSignal(Tools.Sin(len, freq, period, phase));
            

        }
        private void Button_Click_Line(object sender, RoutedEventArgs e)
        {
            float k = (float.Parse(k_textBox.Text, CultureInfo.InvariantCulture));
            float b = (float.Parse(b_textBox.Text, CultureInfo.InvariantCulture));
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            addSignal(Tools.Line(len,freq,k, b));


        }
        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            signalList.Clear();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
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
        private void Button_Click_Noise(object sender, RoutedEventArgs e)
        {
            float amplitude = (float.Parse(Amplitude_textBox.Text, CultureInfo.InvariantCulture));
            float len = (float.Parse(len_textBox.Text, CultureInfo.InvariantCulture));
            float freq = (float.Parse(freq_textBox.Text, CultureInfo.InvariantCulture));
            addSignal(Tools.Noise(len, freq, amplitude));
        }
        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
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
        private void Button_Click_Summ(object sender, RoutedEventArgs e)
        {

                try {
                    addSignal(SignalOperations.Sum(signalList));
                    }
                catch (Exception ex){
                    MessageBox.Show(ex.Message);
                }
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
                signalList.RemoveAt(signalList.Count - 1);
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                addSignal(resampled);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
