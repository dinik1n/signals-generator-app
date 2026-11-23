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
    }
}
