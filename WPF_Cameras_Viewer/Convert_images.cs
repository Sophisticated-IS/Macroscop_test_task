using System;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;

namespace WPF_Cameras_Viewer
{
     class Convert_images//располагаются функции для конвертирования байтов и битовых карт  к ImageSource
    {
        public ImageSource Convert_to_ImageSource (byte[] image_bytes,int index_of_end)//принимает массив байт и индекс конца интервала  массива для преобразования
        {
            BitmapImage bmp_img = new BitmapImage();
            MemoryStream ms = new MemoryStream(image_bytes, 0, index_of_end);
            bmp_img.BeginInit();
            bmp_img.StreamSource = ms;
            bmp_img.EndInit();
            return bmp_img as ImageSource;
        }

        public ImageSource Convert_to_ImageSource(Bitmap bmp)//преобразует bitmap к ImageSource
        {
            IntPtr h_bmp = bmp.GetHbitmap();
            return Imaging.CreateBitmapSourceFromHBitmap(h_bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
    }
}
