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
using static SkiaSharp.HarfBuzz.SKShaper;

namespace SignalGenerator.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SignalChannelSignal sig;
        private List<SignalChannelSignal> signalList = new List<SignalChannelSignal>();
        public MainWindow()
        {
            InitializeComponent();
            
        }
        private void addSignal(SignalChannelSignal obj)
        {

            signalList.Add(obj);
            WpfPlot1.Plot.Add.ScatterLine(obj.TimeTick, obj.Value);
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
                SignalChannelSignalWriter.Write(signalList.Last(), filePath);
            }


        }
        private void Button_Load(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл(*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                addSignal(SignalChannelSignalLoader.Load(filePath));
                
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
            SignalChannelSignal sig_summ = Tools.Summ(signalList);
            string text = "Lehfr";
            if(sig_summ == null)
            {
                MessageBox.Show(text);
            }
            else
            {
                addSignal(sig_summ);
            }
                
        }
        private void Button_Click_Concat(object sender, RoutedEventArgs e)
        {
            SignalChannelSignal sig_con = Tools.Concat(signalList);
            string text = "Lehfr";
            if (sig_con == null)
            {
                MessageBox.Show(text);
            }
            else
            {
                addSignal(sig_con);
            }

        }

        private void Button_Click_Offline(object sender, RoutedEventArgs e)
        {
            float window_size = (float.Parse(Window_textBox.Text, CultureInfo.InvariantCulture));
            int intWindowSize = (int)(window_size * signalList.Last().Freq + 0.5);
            float[] result = OfflineFilter.Filter(signalList.Last().Value, intWindowSize);
            SignalChannelSignal newData = new SignalChannelSignal(signalList.Last().Freq, result);
            addSignal(newData);
        }

        private void Button_Click_Online(object sender, RoutedEventArgs e)
        {
            //Tools.Online;
        }
        private void Button_Click_CutUp(object sender, RoutedEventArgs e)
        {
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            SignalChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            addSignal(sig);




        }
        private void Button_Click_CutDown(object sender, RoutedEventArgs e)
        {
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SignalChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutDown(downCut);
            addSignal(sig);
        }
        private void Button_Click_CutUpAndDown(object sender, RoutedEventArgs e)
        {
            float upCut = (float.Parse(upCut_textBox.Text, CultureInfo.InvariantCulture));
            float downCut = (float.Parse(downCut_textBox.Text, CultureInfo.InvariantCulture));
            SignalChannelSignal sig = signalList.Last();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
            ThresholdCutter thre = new ThresholdCutter();
            thre.SetSignal(sig);
            thre.CutUp(upCut);
            thre.CutDown(downCut);
            addSignal(sig);
        }

    }
}
