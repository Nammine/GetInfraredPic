using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Kinect;
using Windows.UI.Xaml.Shapes;
using Windows.UI;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace App2
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        KinectSensor sensor;
        InfraredFrameReader irReader;
        MultiSourceFrameReader msfr;
        ushort[] irData;
        byte[] irDataConvert;
        WriteableBitmap irBitmap;
        Body[] bodies;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();
            irReader = sensor.InfraredFrameSource.OpenReader();
            msfr = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Infrared);
            FrameDescription fd = sensor.InfraredFrameSource.FrameDescription;
            bodies = new Body[6];
            irData = new ushort[fd.LengthInPixels];
            irDataConvert = new byte[fd.LengthInPixels * 4];
            irBitmap = new WriteableBitmap(fd.Width, fd.Height);
            image.Source = irBitmap;
            sensor.Open();
            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
        }

        void msfr_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
            using (MultiSourceFrame msf = args.FrameReference.AcquireFrame()) {
                if (msf != null) {
                    using (BodyFrame bodyFrame = msf.BodyFrameReference.AcquireFrame()) {
                        using (InfraredFrame irFrame = msf.InfraredFrameReference.AcquireFrame()) {
                            if (bodyFrame != null && irFrame != null) {
                                irFrame.CopyFrameDataToArray(irData);
                                for (int i = 0; i < irData.Length; i++) {
                                    byte intensity = (byte)(irData[i] >> 8);
                                    irDataConvert[i * 4] = intensity;
                                    irDataConvert[i * 4 + 1] = intensity;
                                    irDataConvert[i * 4 + 2] = intensity;
                                    irDataConvert[i * 4 + 3] = 255;
                                }
                                irDataConvert.CopyTo(irBitmap.PixelBuffer);
                                irBitmap.Invalidate();

                                bodyFrame.GetAndRefreshBodyData(bodies);
                                bodyCanvas.Children.Clear();
                                foreach (Body body in bodies) {
                                    if (body.IsTracked) {
                                        Joint headJoint = body.Joints[JointType.Head];
                                        if (headJoint.TrackingState == TrackingState.Tracked) {
                                            DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(headJoint.Position);
                                            Ellipse headCircle = new Ellipse() { Width = 100, Height = 100, Fill = new SolidColorBrush(Color.FromArgb(255,255,0,0))};
                                            bodyCanvas.Children.Add(headCircle);
                                            Canvas.SetLeft(headCircle, dsp.X - 50);
                                            Canvas.SetTop(headCircle, dsp.Y - 50);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
