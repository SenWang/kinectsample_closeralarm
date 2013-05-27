using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Speech.Synthesis;


namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.DepthStream.Disable();
                kinect.AllFramesReady -= mykinect_AllFramesReady;
                kinect.Stop();
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private WriteableBitmap _DepthImageBitmap;
        private Int32Rect _DepthImageBitmapRect;
        private int _DepthImageStride;
        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                ColorImageStream colorStream = kinect.ColorStream;
                kinect.ColorStream.Enable();
                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgra32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;

                DepthImageStream depthStream = kinect.DepthStream;
                kinect.DepthStream.Enable();   
                _DepthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _DepthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _DepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthData.Source = _DepthImageBitmap;

                kinect.SkeletonStream.Enable();

                kinect.AllFramesReady += mykinect_AllFramesReady;

                kinect.Start();
            }
        }

        DepthImageFrame depthframe;
        short[] depthpixelData;
        DepthImagePixel[] depthPixel;

        ColorImageFrame colorframe;
        byte[] colorpixelData;
        void mykinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            #region 基本彩色影像與深度影像處理
            depthframe = e.OpenDepthImageFrame();          
            colorframe = e.OpenColorImageFrame();

            if (depthframe == null || colorframe == null)
                return;

            depthpixelData = new short[depthframe.PixelDataLength];                
            depthframe.CopyPixelDataTo(depthpixelData);
            _DepthImageBitmap.WritePixels(_DepthImageBitmapRect, depthpixelData, _DepthImageStride, 0);
            depthPixel = new DepthImagePixel[depthframe.PixelDataLength];
            depthframe.CopyDepthImagePixelDataTo(depthPixel);
            
            colorpixelData = new byte[colorframe.PixelDataLength];
            colorframe.CopyPixelDataTo(colorpixelData);
            #endregion

            if (depthpixelData != null)
            {
                PlayerFilter();
                Alarm();
            }
            
            _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, colorpixelData, _ColorImageStride, 0);
            depthframe.Dispose();
            colorframe.Dispose();        
        }

        ColorImagePoint[] colorpoints;
        void PlayerFilter()
        {
            colorpoints = new ColorImagePoint[depthpixelData.Length] ;
            kinect.CoordinateMapper.MapDepthFrameToColorFrame(
                depthframe.Format, 
                depthPixel, 
                colorframe.Format, 
                colorpoints);

            for (int i = 0; i < depthpixelData.Length; i++)
            {
                PlayerColor(i);           
            }
        }
        void PlayerColor(int i)
        {
            int playerIndex = depthPixel[i].PlayerIndex; 
            ColorImagePoint p = colorpoints[i];
            int colorindex = (p.X + p.Y * colorframe.Width) * colorframe.BytesPerPixel;
            if (playerIndex > 0)
            {               
                colorpixelData[colorindex+1] = 0x00;
                if (nearst > ALARM_RANGE)
                    colorpixelData[colorindex + 2] = 0x00;
                else
                    colorpixelData[colorindex] = 0x00;

                colorpixelData[colorindex + 3] = 128;

                int depth = depthPixel[i].Depth ;
                Nearst(depth);
            }else
                colorpixelData[colorindex + 3] = 0xFF;
        }

        const int MAX_RANGE = 8000;
        const int ALARM_RANGE = 1000;
        int nearst = ALARM_RANGE;
        void Nearst(int depth)
        {
            if (depth < nearst)
                nearst = depth;
        }
        void Alarm()
        {
            if (nearst < ALARM_RANGE)
            {
                Title = "有人接近";
                Speak();
            }
            else
                Title = "監視中";
            nearst = MAX_RANGE;
        }

        SpeechSynthesizer synthesizer = new SpeechSynthesizer() {  Rate = 0 , Volume = 100};
        void Speak()
        {
            if(synthesizer.State != SynthesizerState.Speaking)
                synthesizer.SpeakAsync("有不速之客");
        }
    }
    
}
