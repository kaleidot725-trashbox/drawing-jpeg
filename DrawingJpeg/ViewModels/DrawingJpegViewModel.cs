using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Prism.Mvvm;
using System.Threading;
using Prism.Commands;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;
using System.Reactive.Linq;

namespace PhotoFrame.ViewModels
{
    class DrawingJpegViewModel : BindableBase
    {
        private static readonly int INDEX_MIN = 0;
        private static readonly int INDEX_MAX = 60;

        private WriteableBitmap writableBitmap;
        public WriteableBitmap WritableBitmap
        {
            get { return writableBitmap; }
            set { SetProperty(ref writableBitmap, value); }
        }

        private int writableBitmapCount;
        public int WritableBitmapCount
        {
            get { return writableBitmapCount; }
            set { SetProperty(ref writableBitmapCount, value); }
        }

        private BitmapFrame bitmapFrame;
        public BitmapFrame BitmapFrame
        {
            get { return bitmapFrame; }
            set { SetProperty(ref bitmapFrame, value); }
        }

        private int bitmapFrameCount;
        public int BitmapFrameCount
        {
            get { return bitmapFrameCount; }
            set { SetProperty(ref bitmapFrameCount, value); }
        }

        private int bitmapIndex = INDEX_MIN;
        private int writableBitmapIndex = INDEX_MIN;
        private bool writableBitmapAlive;
        private Thread writableBitmapThread;
        private bool bitmapFrameAlive;
        private Thread bitmapFrameThread;
        private List<BitmapFrame> Frames;

        public DrawingJpegViewModel()
        {
            WritableBitmapCount = 0;
            BitmapFrameCount = 0;

            LoadFrames();

            WritableBitmap = new WriteableBitmap(3841, 2162, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
            writableBitmapThread = new Thread(WritableBitmapTask);
            writableBitmapAlive = true;
            writableBitmapThread.Start();

            bitmapFrameThread = new Thread(BitmapFrameTask);
            bitmapFrameAlive = true;
            bitmapFrameThread.Start();
        }

        ~DrawingJpegViewModel()
        {
            writableBitmapAlive = false;
            bitmapFrameAlive = false;

            writableBitmapThread.Join();
            bitmapFrameThread.Join();

            Frames = null;
        }

        public void LoadFrames()
        {
            Frames = new List<BitmapFrame>();
            for (var i = INDEX_MIN; i <= INDEX_MAX; i++)
            {
                using (var stream = new FileStream(GetUri(i), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    var image = decoder.Frames[0];
                    Frames.Add(image);
                }
            }
        }

        private string GetUri(int index)
        {
            var directory = "pic\\";
            var extention = ".jpg";

            var fileName = bitmapIndex.ToString();
            bitmapIndex = (INDEX_MAX == bitmapIndex) ? (INDEX_MIN) : (bitmapIndex + 1);

            return directory + fileName + extention;
        }

        public void WritableBitmapTask()
        {
            while (writableBitmapAlive)
            {
                UpdateWritableBitmap(Frames[writableBitmapIndex]);
                WritableBitmapCount = (INDEX_MAX == writableBitmapIndex) ? (WritableBitmapCount + 1) : (WritableBitmapCount);
                writableBitmapIndex = (INDEX_MAX == writableBitmapIndex) ? (INDEX_MIN) : (writableBitmapIndex + 1);
            }
        }

        public void BitmapFrameTask()
        {
            while (bitmapFrameAlive)
            {
                UpdateBitmapFrame(Frames[bitmapIndex]);
                BitmapFrameCount = (INDEX_MAX == writableBitmapIndex) ? (BitmapFrameCount + 1) : (BitmapFrameCount);
                bitmapIndex = (INDEX_MAX == bitmapIndex) ? (INDEX_MIN) : (bitmapIndex + 1);
            }
        }

        private void UpdateBitmapFrame(BitmapFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BitmapFrame = frame;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateWritableBitmap(BitmapFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                writableBitmap.Lock();
                frame.CopyPixels(new System.Windows.Int32Rect(0, 0, 3841, 2162), writableBitmap.BackBuffer, (int)writableBitmap.BackBufferStride * (int)writableBitmap.Height, (int)writableBitmap.BackBufferStride);
                writableBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, 3841, 2162));
                writableBitmap.Unlock();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
