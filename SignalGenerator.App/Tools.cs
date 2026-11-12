using HarfBuzzSharp;
using OpenTK;
using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace SignalGenerator.App
{
    internal class Tools
    {

        public static SingleChannelSignal Line(float len, float freq, float k, float b)
        {
            int count = (int)(len * freq + 0.5);
            
            float[]x = new float[count];
            float[]y = new float[count];
            for (int i = 0; i < count; i++) { 

                    x[i] = i;
                    y[i] = k * x[i] + b;

        }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Sin(float len, float freq, float period, float phase)
        {
            int count = (int)(len * freq + 0.5);
            float[]x = new float[count];
            float[]y = new float[count];
            float diff = 1 / freq;
            for (int i = 0; i < count; i++)
            {
              
                    x[i] = i * diff;
                    y[i] = (float)Math.Sin((x[i] - phase) * 2 * Math.PI / period);
               
                
            }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Noise(float len, float freq,float amplitude)
        {
            Random random = new Random();
            int count = (int)(len * freq + 0.5);
            float[] x = new float[count];
            float[] y = new float[count];
            float diff = 1 / freq;
            for (int i = 0; i < count; i++)
            {

                x[i] = i;
                y[i] = (float)random.NextDouble()*amplitude - amplitude/2;


            }

            return new SingleChannelSignal(freq, y);
        }
        public static SingleChannelSignal Summ(List<SingleChannelSignal> signalList)
        {

            if (signalList.Count == 0)
            {
                return null;
                
            }

            float freq = signalList.First().Frequency;

            int max_len = signalList.Select(el => el.Data.Length).Max();
            float[] summ_signals = new float[max_len];
            double eps = 0.0000001;
            for (int i = 0; i < signalList.Count; i++) {
                if (Math.Abs(signalList[i].Frequency - freq) > eps)
                {
                    return null;
                }
            }
                for (int i = 0; i< signalList.Count; i++)
            {
                {
                    for(int j = 0; j < max_len; j++)
                    {
                        summ_signals[j] += signalList[i].Data[j];
                    }
                }
            }
            
            return new SingleChannelSignal(freq, summ_signals);
        }

        public static SingleChannelSignal Concat(List<SingleChannelSignal> signalList)
        {

            if (signalList.Count == 0)
            {
                return null;

            }

            float freq = signalList.First().Frequency;

            IEnumerable<float> concat_signals = new float[0];
            double eps = 0.0000001;
            for (int i = 0; i < signalList.Count; i++)
            {
                if (Math.Abs(signalList[i].Frequency - freq) > eps)
                {
                    return null;
                }
            }
            for (int i = 0; i < signalList.Count; i++)
            {
                concat_signals = concat_signals.Concat(signalList[i].Data);
            }

            return new SingleChannelSignal(freq, concat_signals.ToArray());
        }
    }
}
