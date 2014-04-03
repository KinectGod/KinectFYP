using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaiChiLearning.Recorder
{
    public static class CompressImage
    {
        /*
        public static byte[] ToJpegBytes(this byte[] data, int compressionRate)
        {
            // On the event of no data being passed, return null;
            if (data == null)
                return null;
            
            //TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
            using (Bitmap bmp = ByteToImage(data))
            {
                
                System.Drawing.Imaging.ImageCodecInfo myImageCodecInfo;
                System.Drawing.Imaging.Encoder myEncoder;
                System.Drawing.Imaging.EncoderParameter myEncoderParameter;
                System.Drawing.Imaging.EncoderParameters myEncoderParameters;
                myImageCodecInfo = GetEncoderInfo("image/jpeg");
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);

                // Save the bitmap as a JPEG file with quality level 25.
                myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 50L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                using (var stream = new MemoryStream())
                {
                    bmp.Save(stream, myImageCodecInfo, myEncoderParameters);
                    return stream.ToArray();
                }
            }
            return data;
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        */
        public static byte[] BytesToBitmapImage(byte[] data, int ratio)
        {
            WriteableBitmap bmp = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);
            bmp.WritePixels(new Int32Rect(0, 0, 640, 480), data, bmp.PixelWidth * sizeof(int), 0);
            var bitmapSource = new WriteableBitmap(bmp);

            var enc = new JpegBitmapEncoder();
            enc.QualityLevel = ratio;

            var bf = BitmapFrame.Create(bitmapSource); // will throw InvalidOperationException (without WriteableBitmap trick)
            enc.Frames.Add(bf);

            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                BitmapImage NewBitmap = new BitmapImage();
                NewBitmap.BeginInit();
                NewBitmap.StreamSource = new MemoryStream(ms.ToArray());
                NewBitmap.EndInit();
                return ms.ToArray();
            }
        }

    }
}
