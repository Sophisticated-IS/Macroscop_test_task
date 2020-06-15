using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF_Cameras_Viewer
{
    class IP_cameras_list//Получает Listview и список камер ,затем опрашивает каждую и записывает 
    {
      struct Сamera_name_and_frame{
            public string Camera_Name;
            public ImageSource Camera_Frame; 
      }
        //private URL =http://demo.macroscop.com:8080/mobile?login=root&channelid=2016897c-8be5-4a80-b1a3-7f79a9ec729c&resolutionX=640&resolutionY=480&fps=25";      
        public void  test_method(ListView list_view_availab_cameras, List<Сamera_id_and_name> available_cameras)//получает список камер и опрашивает каждую беря 1 кадр и вставляя картинку
        {
            if (available_cameras.Count > 0)
            {
                List<Сamera_name_and_frame> list_cam_name_and_frame = new List<Сamera_name_and_frame>();
                for (int i = 0; i < available_cameras.Count; i++)
                {
                    string URL = $"http://demo.macroscop.com:8080/mobile?login=root&channelid=" +
                                 $"{available_cameras.ElementAt(i).camera_id}&resolutionX=640&resolutionY=480&fps=25";
                    var request = (HttpWebRequest)WebRequest.Create(URL);
                    Stream stream;
             
                    try
                    {
                        stream = request.GetResponse().GetResponseStream(); //TODO: System.Net.WEb Exception                
                        var byte_buff = new byte[1024];//буфер для считывания потока байтов из ответа сервера
                        var image_jpeg = new byte[170000];//Размер после сжатия примерно - 160 Кб берем буфер под макс разрешение в сжатом виде MJPEG 1280*720 * 24;  Степень сжатия 17.4
                        int jpeg_i = 0;//индекс для движения по массиву  image_jpeg
                        int start_jpeg_index = 0;//индекс байтов начала 0xff 0xd8

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

            }
            else
            {
                //TODO:
                throw new Exception("Нет доступных камер!");
            }
            

            //опросили и заполнили лист
            
          //  ImageSource img_s;
          //  IntPtr h_bmp_stream_background = Properties.Resources.Stream_default_img.GetHbitmap();
          //  img_s = Imaging.CreateBitmapSourceFromHBitmap(h_bmp_stream_background, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            //привязали результат в проге
         //   list_view_availab_cameras.ItemsSource = new[] { new { Camera_Name = "camera1", Camera_Frame =img_s  }, new { Camera_Name = "camera2", Camera_Frame = img_s } };

        }
    }
}
