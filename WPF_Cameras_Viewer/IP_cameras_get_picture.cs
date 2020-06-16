using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF_Cameras_Viewer
{
    class IP_cameras_get_picture//Опрашивает все камеры, получая с них изображение для отображение в UI 
    {
      struct Сamera_name_and_frame{
            public string Camera_Name;
            public ImageSource Camera_Frame; 
      }
        private const int camera_interrogation_rate_ms = 30000;
        public void  Load_images_from_ip_cameras(ListView list_view_availab_cameras, List<Сamera_id_and_name> available_cameras)//опрашивает каждую камеру из списка доступных и получает с неё изображение, в конце обновляя UI
        {
            if (available_cameras.Count > 0)
            {
                List<Сamera_name_and_frame> list_cam_name_and_frame = new List<Сamera_name_and_frame>();//Список названия камеры и её изображения
                var cnvrt_images = new Convert_images();
                for (int i = 0; i < available_cameras.Count; i++)
                {
                    string URL = $"http://demo.macroscop.com:8080/mobile?login=root&channelid=" +
                                 $"{available_cameras.ElementAt(i).camera_id}&resolutionX=640&resolutionY=480&fps=25";

                    var request = (HttpWebRequest)WebRequest.Create(URL);
                    request.Timeout = 10000;
                    Stream stream;
                    try
                    {
                        stream = request.GetResponse().GetResponseStream();           
                        var byte_buff = new byte[1024];//буфер для считывания потока байтов из ответа сервера
                        var image_jpeg = new byte[170000];//Размер после сжатия примерно - 160 Кб берем буфер под макс разрешение в сжатом виде MJPEG 1280*720 * 24;  Степень сжатия 17.4
                        int jpeg_i = 0;//индекс для движения по массиву  image_jpeg
                        int start_jpeg_index = 0;//индекс байта 0xff из двух байтов начала 0xff 0xd8

                        bool is_end_of_frame = false;

                        while (!is_end_of_frame)
                        {
                            int actual_number_of_bytes = 0;//хранит число байт реально считанных потоком                        
                            actual_number_of_bytes = stream.Read(byte_buff, 0, byte_buff.Length);
                            byte_buff.CopyTo(image_jpeg, jpeg_i);
                            jpeg_i += actual_number_of_bytes;

                            for (int j = 0; j < actual_number_of_bytes - 1; j++)
                            {

                                if (byte_buff[j] == 0xff && byte_buff[j + 1] == 0xd8)//начало кадра
                                {
                                    start_jpeg_index = j;
                                }

                                if (byte_buff[j] == 0xff && byte_buff[j + 1] == 0xd9)//конец кадра
                                {
                                    is_end_of_frame = true;

                                }
                            }
                        }
                        image_jpeg = image_jpeg.Skip(start_jpeg_index).ToArray();//пропустим заголовок до начала jpeg кадра

                        list_cam_name_and_frame.Add(new Сamera_name_and_frame()
                        {
                            Camera_Name = available_cameras.ElementAt(i).camera_name,
                            Camera_Frame = cnvrt_images.Convert_to_ImageSource(image_jpeg, jpeg_i - start_jpeg_index)
                        }
                        );

                        stream.Close();
                    }
                    catch (Exception)//если камера не отвечает(IOException, WebException) то мы вместо первого кадра от неё поставим картинку - ошибка (no signal)
                    {
                        IntPtr h_bmp_stream_background = Properties.Resources.no_signal.GetHbitmap();
                        var img_no_signal = (ImageSource)Imaging.CreateBitmapSourceFromHBitmap(h_bmp_stream_background, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                        list_cam_name_and_frame.Add(new Сamera_name_and_frame()
                        {
                            Camera_Name = available_cameras.ElementAt(i).camera_name,
                            Camera_Frame = img_no_signal
                        }
                        );
                    }

                }
                
                foreach (var name_and_frame in list_cam_name_and_frame) 
                {
                    list_view_availab_cameras.Items.Add(new { name_and_frame.Camera_Name, name_and_frame.Camera_Frame });
                }
                
             }
            else
            {
                MessageBox.Show("No one camera is online!");
                //TODO: Доработать и подумать что делать если ни одна камера не отвечает
                //throw new Exception("Нет доступных камер!");
            }

        }

        private async void Try_to_recconect_to_not_responding_cameras()//пытается опросить камеры, которые не отвечали при загрузке программы TODO: дописать
        {
            await Task.Run(() =>
            {
                bool all_cameras_online = false;

                while (!all_cameras_online)
                {
                   Task.Delay(camera_interrogation_rate_ms);

                }

            });
        }
    }
}
