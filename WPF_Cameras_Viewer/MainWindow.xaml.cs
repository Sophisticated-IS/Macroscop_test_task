using System;
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
        struct camera_stream_inf
        {
            public string quality_degree;
            public int X_resolution;
            public int Y_resolution;
            public string _quality   { get { return quality_degree; } set { quality_degree = value; } }
            public int _X_resolution { get { return X_resolution; } set { X_resolution = value; } }
            public int _Y_resolution { get { return Y_resolution; } set { Y_resolution = value; } }
        }
        struct camera_id_and_name
        {
            public string camera_id;
            public string camera_name;
        }
        camera_stream_inf[] quality_inf = new camera_stream_inf[3];
        Thread play_video;

        public void Load_image(/*string URL,string quality,string*/ )
        {          
            string example_url = "http://demo.macroscop.com:8080/mobile?login=root&channelid=e6f2848c-f361-44b9-bbec-1e54eae777c0&resolutionX=640&resolutionY=480&fps=25";
            //StringBuilder
            var request = (HttpWebRequest)WebRequest.Create(example_url);
            request.KeepAlive = true;//для более эффективной работы с сетью
            Stream stream;
            try
            {
               stream = request.GetResponse().GetResponseStream(); //TODO: System.Net.WEb Exception                
            }
            catch (WebException)
            {                
                MessageBox.Show("Server not respond");
                return;//выходим из функции так как сервер не отвечает
            }
            
            
            while (true)
            {
                var byte_buff = new byte[1024];
                var image_jpeg = new byte[50000];//TODO: Вопросы по размеру буффера частенько вылетает
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
                        MessageBox.Show("Connection Lost");                        
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
                            break;                             
                        }
                    }
                }
                image_jpeg = image_jpeg.Skip(start_jpeg_index).ToArray();//пропустим заголовок до начала кадра
                
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

        public void  Get_list_of_cameras()//Хранится в XML документе
        {
            XmlDocument xml_doc = new XmlDocument();
            const string configs_url = "http://demo.macroscop.com:8080/configex?login=root";

            //TODO:try
            try
            {
                xml_doc.Load(configs_url);
            }
            catch (XamlParseException)
            {
                MessageBox.Show("NO Internet Connection or xml file is damaged");
                return;
            }
     
            XmlNodeList list; 
            list = xml_doc.GetElementsByTagName("ChannelInfo");
            for (int i = 0; i < list.Count; i++)
            {
                string camera_id = list[i].Attributes.GetNamedItem("Id").Value;
                string camera_name = list[i].Attributes.GetNamedItem("Name").Value;
                Debug.WriteLine(camera_id);  
                Debug.WriteLine(camera_name);
            }
            
        }
        public MainWindow()
        {
            InitializeComponent();
            Get_list_of_cameras();
            quality_inf[0]._quality = "Low";
            quality_inf[0].X_resolution = 640;
            quality_inf[0].Y_resolution = 480;
            
            quality_inf[1]._quality = "Middle";
            quality_inf[1].X_resolution = 800;
            quality_inf[1].Y_resolution = 480;

            quality_inf[2]._quality = "High";
            quality_inf[2].X_resolution = 1280;
            quality_inf[2].Y_resolution = 720;

         //   larrow.Source = ;

        }

        private void Button_Click_Play(object sender, RoutedEventArgs e)
        {
            
            play_video = new Thread(new ThreadStart(Load_image));
            play_video.Start();
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (play_video!=null && play_video.IsAlive)
            {
                play_video.Abort(); //остановим поток отвечающий за отображение картинки 
            }
            else;
            
        }
    }
}
