﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
//using DirectShowLib;

namespace WPF_Cameras_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        readonly Сamera_stream_inf[] quality_inf = new Сamera_stream_inf[3];//массив в котором хранятся 3 типа возможных разрешений для стрима
        Thread play_video;//поток для проигрывания стрима
        const int times_for_reconnect = 30; //30 раз в течении 30 секунд пытаемся переподключиться
        readonly List<Сamera_id_and_name> available_cameras_list = new List<Сamera_id_and_name>();//список доступных камер из xml документа
        Сurrent_selected_camera current_camera = new Сurrent_selected_camera();
        DateTime time_of_last_switch_cam = DateTime.Now;
        struct Сamera_stream_inf
        {
            public string quality_degree;//качество стрима 
            public int X_resolution;//разрешение X и Y
            public int Y_resolution;
            
        }
        struct Сamera_id_and_name
        {
            public string camera_id; //id камеры по которому мы подключаемся
            public string camera_name;//название ккамеры 
        }
        struct Сurrent_selected_camera
        {
          public int camera_order_id;//порядковый индекс в списке конкретной камеры 
          public  Сamera_id_and_name cam_id_name;
          public  Сamera_stream_inf cam_stream_inf;
        }

        string URL = "http://demo.macroscop.com:8080/mobile?login=root&channelid=e6f2848c-f361-44b9-bbec-1e54eae777c0&resolutionX=640&resolutionY=480&fps=25";
        public void Start_mjpeg_stream()//метод для воспроизведения потока mjpeg картинок
        {      
            var request = (HttpWebRequest)WebRequest.Create(URL);
            request.KeepAlive = true;//для более эффективной работы с сетью
            Stream stream; 
            try
            {
               stream = request.GetResponse().GetResponseStream(); //TODO: System.Net.WEb Exception                
            }
            catch (WebException)
            {                
                MessageBox.Show("Server not respond");
                Reconnect_to_camera();
                return;//выходим из функции так как сервер не отвечает
            }
            
            while (true)
            {
                var byte_buff = new byte[1024];
                var image_jpeg = new byte[170000];//Размер после сжатия примерно - 160 Кб берем буфер под макс разрешение в сжатом виде MJPEG 1280*720 * 24;  Степень сжатия 17.4
                int jpeg_i = 0;//индекс для движения по массиву  image_jpeg
                int start_jpeg_index = 0;//индекс байтов начала 0xff 0xd8

                bool is_end_of_frame = false; 
            
                while (!is_end_of_frame)
                {
                   int actual_number_of_bytes = 0;//хранит число байт реально считанных потоком                        
                    try
                    {                    
                       actual_number_of_bytes = stream.Read(byte_buff, 0, byte_buff.Length);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Connection Lost! We are trying to reconnect!");
                        Reconnect_to_camera();
                        return; //выходим из функции т.к пользователь разорвал соединение                           
                    }                  

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
                
                //Обновим полученную картинку в UI потоке
                Dispatcher.Invoke(() => 
                {
                    ImageSource img_source;
                    BitmapImage bmp_img = new BitmapImage();
                    MemoryStream ms = new MemoryStream(image_jpeg, 0, jpeg_i - start_jpeg_index);
                    bmp_img.BeginInit();
                    bmp_img.StreamSource = ms;
                    bmp_img.EndInit();
                    img_source = bmp_img as ImageSource;
                    picture.Source = img_source;
                });
            }
        }

        public void  Get_list_of_cameras()//Заполняет список доступных камер из XML документа
        {
            XmlDocument xml_doc = new XmlDocument();
            const string configs_url = "http://demo.macroscop.com:8080/configex?login=root";
 
            try
            {
                xml_doc.Load(configs_url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("NO Internet Connection or xml file is damaged! " + ex.Message);
                return;
            }
     
            var xml_nodes_list = xml_doc.GetElementsByTagName("ChannelInfo");
            for (int i = 0; i < xml_nodes_list.Count; i++)
            {
                string camera_id = xml_nodes_list[i].Attributes.GetNamedItem("Id").Value;
                string camera_name = xml_nodes_list[i].Attributes.GetNamedItem("Name").Value;
                var next_elt = new Сamera_id_and_name
                {
                    camera_id = camera_id,
                    camera_name = camera_name
                };
                available_cameras_list.Add(next_elt);
               
                #if DEBUG
                Debug.WriteLine(camera_id);  
                Debug.WriteLine(camera_name);
                #endif
            }

            if (available_cameras_list.Count > 0)
            {
                current_camera.camera_order_id = 0;//индекс 0 так как мы инициализируем текущую камеру как первую из списка всех доступных

                current_camera.cam_id_name.camera_id = available_cameras_list.First().camera_id;
                current_camera.cam_id_name.camera_name = available_cameras_list.First().camera_name;

                current_camera.cam_stream_inf.quality_degree = "Low";
                current_camera.cam_stream_inf.X_resolution = 640;
                current_camera.cam_stream_inf.Y_resolution = 480;
            }
            else
            {
                MessageBox.Show("We have not found available cameras");
            }
        }
        async void Reconnect_to_camera()//асинхронный метод для восстановления соединения  при потери 
        {
            await Task.Run(() =>
            {
                int i = 0;
                for (; i < times_for_reconnect; i++)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://www.google.ru/");
                        request.Timeout = 10000;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        Stream ReceiveStream1 = response.GetResponseStream();
                        StreamReader stream = new StreamReader(ReceiveStream1, true);
                        var responseFromServer = stream.ReadToEnd();
                        response.Close();
                        
                        Dispatcher.Invoke(() =>//заново вызовем в основном потоке проигрывание стрима
                        {
                            Button_Click_Play(this, null);
                        });
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(1000);
                    }
                }
                if (i == times_for_reconnect)
                {
                    MessageBox.Show("We couldn't reconnect to camera, check please your connection and press PLAY");
                }
                else
                {
                    MessageBox.Show("We have succesfully reconnected!");
                }
                    
                           
                
            });
        }
        public MainWindow()
        {
            InitializeComponent();
            Get_list_of_cameras();
           
            //Инициализация массива со списком возможного качества изображения и разрешения 
            quality_inf[0].quality_degree = "Low";
            quality_inf[0].X_resolution = 640;
            quality_inf[0].Y_resolution = 480;
            
            quality_inf[1].quality_degree = "Middle";
            quality_inf[1].X_resolution = 800;
            quality_inf[1].Y_resolution = 480;

            quality_inf[2].quality_degree = "High";
            quality_inf[2].X_resolution = 1280;
            quality_inf[2].Y_resolution = 720;

            //Преобразуем иконки для левой и правой кнопки -стрелочки к ImageSource 
            Bitmap bmp_l_arrow = Properties.Resources.left_arrow.ToBitmap();
            IntPtr h_bmp_l_arrow = bmp_l_arrow.GetHbitmap();
            img_left_arrow.Source = Imaging.CreateBitmapSourceFromHBitmap(h_bmp_l_arrow, IntPtr.Zero, Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());
            Bitmap bmp_r_arrow = Properties.Resources.right_arrow.ToBitmap();
            IntPtr h_bmp_r_arrow = bmp_r_arrow.GetHbitmap();
            img_right_arrow.Source = Imaging.CreateBitmapSourceFromHBitmap(h_bmp_r_arrow, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        }

        private void Button_Click_Play(object sender, RoutedEventArgs e)
        {
            if (play_video == null || !play_video.IsAlive)
            {
               // Сamera_stream_inf selected_stream_configs;
               // selected_stream_configs = combox_quality.SelectedIndex > 0?  quality_inf[combox_quality.SelectedIndex] : quality_inf[0];

                URL =  $"http://demo.macroscop.com:8080/mobile?login=root" +
                    $"&channelid=801e28a4-1711-4ab2-b7fd-a4d9687a471c" +
                    $"&resolutionX={current_camera.cam_stream_inf.X_resolution}" +
                    $"&resolutionY={current_camera.cam_stream_inf.Y_resolution}&fps=25";
                play_video = new Thread(new ThreadStart(Start_mjpeg_stream));
                play_video.Start();
            }
            else;//видео и так уже проигрывается
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (play_video!=null && play_video.IsAlive)
            {
                play_video.Abort(); //остановим поток отвечающий за отображение картинки 
            }
            else;            
        }
        private void Button_right_arrow_Click(object sender, RoutedEventArgs e)
        {
            
            if (play_video != null && DateTime.Now.Subtract(time_of_last_switch_cam).TotalSeconds>=3)//мы переключаем видео раз в секунду чтобы предотвратить ОЧЕНЬ частых нажатий               
            {

                if (current_camera.camera_order_id < available_cameras_list.Count - 1)
                {
                    current_camera.camera_order_id++;
                    current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_id;
                    current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_name;
                }
                else//идем по кругу т.е. после последней камеры откроем вновь первую из списка
                {
                    current_camera.camera_order_id = 0;
                    current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(0).camera_id;
                    current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(0).camera_name;
                }

                play_video.Abort();
                while (play_video.IsAlive)
                {
                    //ждем пока наш поток завершится и тогда мы уже откроем новый стрим с другой камеры по списку
                }
                Button_Click_Play(this, null);
                time_of_last_switch_cam = DateTime.Now;                
            }
            else;//поток не был создан еще следовательно ни одного стрима не было
            
        }

        private void Button_left_arrow_Click(object sender, RoutedEventArgs e)
        {
            if (play_video != null && DateTime.Now.Subtract(time_of_last_switch_cam).TotalSeconds >= 3)//мы переключаем видео раз в секунду чтобы предотвратить ОЧЕНЬ частых нажатий
            {
                if (current_camera.camera_order_id > 0)
                {
                    current_camera.camera_order_id--;
                    current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_id;
                    current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_name;
                }
                else//идем по кругу т.е. после последней камеры откроем вновь первую из списка
                {
                    current_camera.camera_order_id = available_cameras_list.Count-1;
                    current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(0).camera_id;
                    current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(0).camera_name;
                }

                play_video.Abort();
                while (play_video.IsAlive)
                {
                    //ждем пока наш поток завершится и тогда мы уже откроем новый стрим с другой камеры по списку
                }
                Button_Click_Play(this, null);
                time_of_last_switch_cam = DateTime.Now;
            }
            else;//поток не был создан еще следовательно ни одного стрима не было
        }
    }
}
