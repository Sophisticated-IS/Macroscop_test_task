using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;

namespace WPF_Cameras_Viewer
{
     class Convert_images
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
    }
}
