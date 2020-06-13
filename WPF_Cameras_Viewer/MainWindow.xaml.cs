using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using DirectShowLib;

namespace WPF_Cameras_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {  
        public void load_image()
        {
          
            string example_url = "http://demo.macroscop.com:8080/mobile?login=root&channelid=e6f2848c-f361-44b9-bbec-1e54eae777c0&resolutionX=640&resolutionY=480&fps=25";

            var request = (HttpWebRequest)WebRequest.Create(example_url);
            request.KeepAlive = true;//для более эффективной работы с сетью
            var stream = request.GetResponse().GetResponseStream(); //TODO: System.Net.WEb Exception                
            //StreamReader reader = new StreamReader(stream);
            for(int t=0;t<20;t++)
            { 
            var byte_buff = new byte[1024];
            byte[] image_jpeg = new byte[30000];
            // int image_i = 0;
            int jpeg_i = 0;
            int start_jpeg = 0;
           // using (FileStream fstream = new FileStream(@"C:\Users\Sova IS\Desktop\analstream.bin", FileMode.Create))
           //    {
                    bool flag = false; 
            
                    while (true)
                    {
                        int actual_number_of_bytes = 0;//хранит число байт реально считанных потоком
                        actual_number_of_bytes = stream.Read(byte_buff, 0, byte_buff.Length);
                         byte_buff.CopyTo(image_jpeg, jpeg_i);
                        jpeg_i += actual_number_of_bytes;
                 
                       
                        for (int j = 0; j < actual_number_of_bytes - 1; j++)
                        {                        

                            if (byte_buff[j] == 0xff && byte_buff[j + 1] == 0xd8)
                            {
                            
                                 start_jpeg = j;                          
                            }

                            if (byte_buff[j] == 0xff && byte_buff[j + 1] == 0xd9)
                            {
                                flag = true;
                                break;
                            }
                        }


                        if (flag)
                        {
                            //fstream.Write(image_jpeg, 0, image_i);
                            break;
                        }
                    }

                image_jpeg = image_jpeg.Skip(start_jpeg).ToArray();
              //  fstream.Write(image_jpeg, 0, jpeg_i);
                


            //}
            BitmapImage biImg = new BitmapImage();
            MemoryStream ms = new MemoryStream(image_jpeg,0,jpeg_i-start_jpeg);
            biImg.BeginInit();
            biImg.StreamSource = ms;
            biImg.EndInit();
            ImageSource imgSrc = biImg as ImageSource;

            picture.Source = imgSrc;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        
            //char[] buff = new char[1];
            
            // using (FileStream fstream = new FileStream(@"C:\Users\Sova IS\Desktop\analstream.txt", FileMode.OpenOrCreate))
             {


                //int i = 0;
                //string line = "";
                //int counter = 0;
                //while (counter < 2)
                //{
                //    line = reader.readline();
                //    reader.readblock(buff, 0, 1);
                //    tmp1 = (byte)buff[0];
                //    fstream.writebyte(tmp1);

                //    i++;

                //    if (line.contains("--myboundary"))
                //    {
                //        debug.write("--myboundary\r\n");
                //        counter++;
                //        if (counter == 2)
                //        {
                //            line = line.trimend("--myboundary".toarray());
                //        }
                //    }
                //    if (line.contains("content-type: image/jpeg"))
                //    {
                //        debug.write("content-type:image/jpeg\r\n");
                //    }
                //    if (line.contains("content-length: "))
                //    {
                //        debug.write("content-length:");
                //        line = line.replace("content-length: ", "");

                //        debug.write($"{line}");

                //        bitmapimage biimg = new bitmapimage();
                //        memorystream ms = new memorystream(buffer);
                //        biimg.begininit();
                //        biimg.streamsource = ms;
                //        biimg.endinit();

                //        imagesource imgsrc = biimg as imagesource;
                //        var bmp = (bitmap)system.drawing.image.fromstream(new memorystream(buffer, 0, buf_len));
                //        var handle = bmp.gethbitmap();

                //        picture.source = imaging.createbitmapsourcefromhbitmap(handle, intptr.zero, int32rect.empty, bitmapsizeoptions.fromemptyoptions());
                //    }
                //    fstream.(encoding.ascii.getbytes(line + "\r\n"), 0, line.length);

                //    foreach (var item in line)
                //    {
                //        fstream.writebyte((byte)item);
                //    }
                //    file.writealltext(@"c:\users\sova is\desktop\analstream.txt", line);

                //}

            }

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            load_image();
        }
    }
}
