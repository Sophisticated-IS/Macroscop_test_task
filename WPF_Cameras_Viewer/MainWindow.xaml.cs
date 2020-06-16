using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml;

namespace WPF_Cameras_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    struct Сamera_id_and_name
    {
        public string camera_id; //id камеры по которому мы подключаемся
        public string camera_name;//название ккамеры 
    }
    public partial class MainWindow : Window
    {
        string URL = "http://demo.macroscop.com:8080/mobile?login=root&channelid=e6f2848c-f361-44b9-bbec-1e54eae777c0&resolutionX=640&resolutionY=480&fps=25";
        readonly Сamera_stream_inf[] quality_inf_array = new Сamera_stream_inf[3];//массив в котором хранятся 3 типа возможных разрешений для стрима
        Thread play_video;//поток для проигрывания стрима
        const int times_for_reconnect = 30; //30 раз в течении 30 секунд пытаемся переподключиться
        readonly List<Сamera_id_and_name> available_cameras_list = new List<Сamera_id_and_name>();//список доступных камер из xml документа
        Сurrent_selected_camera current_camera = new Сurrent_selected_camera();//структура камеры с которой стрим вещается в данный момент
        DateTime time_of_last_switch_cam = DateTime.Now;//время последнего переключения камеры
        const int switch_cameras_delay = 2;
        struct Сamera_stream_inf
        {
            public string quality_degree;//качество стрима 
            public int X_resolution;//разрешение X и Y
            public int Y_resolution;

        }
     
        struct Сurrent_selected_camera
        {
            public int camera_order_id;//порядковый индекс в списке конкретной камеры 
            public Сamera_id_and_name cam_id_name;
            public Сamera_stream_inf cam_stream_inf;
        }

        public void Start_mjpeg_stream()//метод для воспроизведения потока mjpeg картинок
        {
            var request = (HttpWebRequest)WebRequest.Create(URL);
            request.KeepAlive = true;//для поддержки соединения
            Stream stream;
            Convert_images cnvrt_images = new Convert_images();
            try
            {
                stream = request.GetResponse().GetResponseStream();             
            }
            catch (WebException)
            {
                MessageBox.Show("Server not respond");
                Reconnect_to_camera();
                return;//выходим из функции так как сервер не отвечает
            }
            var jpeg_frame = new byte[180000];// MJPEG 1280*720 * 24 /15.4(compression) = 1 436 260/8 = 180 000 bytes; Степень сжатия 15.4
            while (true)
            {
                var byte_buff = new byte[1024];//буфер для считывания потока байтов из ответа сервера
                var jpeg_skipped_header = new byte[180000];//байты jpeg картинки без заголовка
                int jpeg_i = 0;//индекс для движения по массиву  image_jpeg
                int start_jpeg_index = 0;//индекс байтов начала 0xff 0xd8

                bool is_end_of_frame = false;
                bool is_bad_image = false;
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
                    try
                    {
                        byte_buff.CopyTo(jpeg_frame, jpeg_i);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        is_bad_image = true;
                        break;//выйдем из обработки кадра так как он ошибочно передался по сети
                    }
                 
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
                if (!is_bad_image)
                {
                    //пропустим заголовок у jpeg кадра
                    Array.Copy(jpeg_frame, start_jpeg_index, jpeg_skipped_header, 0, jpeg_skipped_header.Length - start_jpeg_index);

                    //Обновим полученную картинку в UI потоке
                    Dispatcher.Invoke(() =>
                    {
                        img_stream_picture.Source = cnvrt_images.Convert_to_ImageSource(jpeg_skipped_header, jpeg_i); ;
                    });
                }
                else;//пропустим картинку
            }
        }

        public void Get_list_of_cameras()//Заполняет список доступных камер из XML документа
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
        async void Show_data_and_time()//асинхроннный метод - таймер
        {
            while (true)
            {
                await Task.Delay(400);
                txtblock_time.Text = DateTime.Now.ToString();
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            Get_list_of_cameras();
           
            //Инициализация массива со списком возможного качества изображения и разрешения 
            quality_inf_array[0].quality_degree = "Low";
            quality_inf_array[0].X_resolution = 640;
            quality_inf_array[0].Y_resolution = 480;
            
            quality_inf_array[1].quality_degree = "Middle";
            quality_inf_array[1].X_resolution = 800;
            quality_inf_array[1].Y_resolution = 480;

            quality_inf_array[2].quality_degree = "High";
            quality_inf_array[2].X_resolution = 1280;
            quality_inf_array[2].Y_resolution = 720;
            
            var cnvrt_to_img_source = new Convert_images();
            //Преобразуем иконки для левой и правой кнопки -стрелочки к ImageSource 
            img_left_arrow.Source = cnvrt_to_img_source.Convert_to_ImageSource(Properties.Resources.left_arrow.ToBitmap());
            img_right_arrow.Source = cnvrt_to_img_source.Convert_to_ImageSource(Properties.Resources.right_arrow.ToBitmap());

            //Зададим фон по дефолту для стрима 
            img_stream_picture.Source = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.Stream_default_img.GetHbitmap(), IntPtr.Zero, 
                                                                              Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
           
            Show_data_and_time();

             var test = new IP_cameras_get_picture();
             test.Load_images_from_ip_cameras(list_view_availab_cameras,available_cameras_list);            
        }

        private void Button_Click_Play(object sender, RoutedEventArgs e)
        {
            if (play_video == null || !play_video.IsAlive)
            {              
                URL =  $"http://demo.macroscop.com:8080/mobile?login=root" +
                    $"&channelid={current_camera.cam_id_name.camera_id}" +
                    $"&resolutionX={current_camera.cam_stream_inf.X_resolution}" +
                    $"&resolutionY={current_camera.cam_stream_inf.Y_resolution}&fps=25";
                play_video = new Thread(new ThreadStart(Start_mjpeg_stream));
                play_video.Start();
                
                //обновим UI 
                txtblock_camera_name.Text = current_camera.cam_id_name.camera_name;
                if (combox_quality.SelectedIndex < 0)//если пользователь не выбрал качество то мы ставим низкое по дефолту
                {
                    combox_quality.SelectedIndex = 0;
                }
                else;//уже было выбрано качество из списка комбобокса
                
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
            
            if (play_video != null && DateTime.Now.Subtract(time_of_last_switch_cam).TotalSeconds>=switch_cameras_delay)//мы переключаем видео раз в секунду чтобы предотвратить ОЧЕНЬ частых нажатий               
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
            if (play_video != null && DateTime.Now.Subtract(time_of_last_switch_cam).TotalSeconds >= switch_cameras_delay)//мы переключаем видео раз в секунду чтобы предотвратить ОЧЕНЬ частых нажатий
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
                    current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_id;
                    current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(current_camera.camera_order_id).camera_name;
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



        private void Combox_quality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var selected_Item = (ComboBoxItem)comboBox.SelectedItem;
            //ищем нужное качество в массиве
            var quality_inf_elt = Array.Find(quality_inf_array, delegate (Сamera_stream_inf stream_inf) { return stream_inf.quality_degree ==selected_Item.Content.ToString(); });

            if (quality_inf_elt.ToString() != null)
            {
                current_camera.cam_stream_inf.quality_degree = quality_inf_elt.quality_degree;
                current_camera.cam_stream_inf.X_resolution = quality_inf_elt.X_resolution;
                current_camera.cam_stream_inf.Y_resolution = quality_inf_elt.Y_resolution;
            }
            else
            {
                //элемент не был найден, следовательно, лучше оставим настройки стрима по дефолту
            }

            if (play_video != null)
            {
                play_video.Abort();
                while (play_video.IsAlive)
                {
                    //ждем завершения стрима
                }

            }
            else//поток еще не был запущен и стрима не было ни разу
            {
                //значит останавливать текущий стрим не нужно
            }
            Button_Click_Play(this, null);
        }

        private void List_view_availab_cameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listview = (ListView)sender;
            if (DateTime.Now.Subtract(time_of_last_switch_cam).TotalSeconds >= switch_cameras_delay)
            {

                //Выберем текущей камеру, на которую кликнул пользователь
                current_camera.camera_order_id = listview.SelectedIndex;
                current_camera.cam_id_name.camera_id = available_cameras_list.ElementAt(listview.SelectedIndex).camera_id;
                current_camera.cam_id_name.camera_name = available_cameras_list.ElementAt(listview.SelectedIndex).camera_name;

                if (play_video != null)
                {
                    play_video.Abort();
                    while (play_video.IsAlive)
                    {
                        //ждем завершения потока стрима
                    }
                }
                else;//поток не создавался

                Button_Click_Play(this, null);
                time_of_last_switch_cam = DateTime.Now;
            }
            else//мы не реагируем на такие частые клики
            {
                listview.SelectedIndex = current_camera.camera_order_id; // вернем обратно на ту камеру которая была выбрана           
            }
    
        }

   //TODO: сделать переключение кнопками
    }
}
