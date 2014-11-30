using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;


namespace sharp1._1
{
    public class C_SendFile
    {

        public int port;
        public string ip;
        


        

        public C_SendFile(string ip , int port)
        {
            this.port = port;
            this.ip = ip;
            //Listener = new TcpListener(port);
        }



        public void SendFile(string FileName , int port , string ip)
        {
            //Коннектимся
            TcpListener Listener;
            Listener = new TcpListener(port);
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket Connector = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Connector.Connect(EndPoint);
            
            //Получаем имя из полного пути к файлу
            //StringBuilder FileName = new StringBuilder(openFileDialog1.FileName);
            //Выделяем имя файла
           
            //Получаем имя файла

            System.IO.FileInfo file = new System.IO.FileInfo(FileName);
            long size = file.Length;

            String resFileName = System.IO.Path.GetFileName(FileName);

            //Записываем в лист
            List<Byte> First256Bytes = Encoding.Default.GetBytes(resFileName).ToList();
            List<Byte> Second256Bytes = Encoding.Default.GetBytes(size.ToString()).ToList();
            Int32 Diff = 256 - First256Bytes.Count;
            Int32 Diff2 = 256 - Second256Bytes.Count;
            //Остаток заполняем нулями
            for (int i = 0; i < Diff; i++)
                First256Bytes.Add(0);
            for (int i = 0; i < Diff2; i++)
                Second256Bytes.Add(0);
            //Начинаем отправку данных
            Byte[] ReadedBytes = new Byte[256];
            using (var FileStream = new FileStream(FileName, FileMode.Open))
            {
                using (var Reader = new BinaryReader(FileStream))
                {
                    Int32 CurrentReadedBytesCount;
                    //Вначале отправим название файла
                    Connector.Send(First256Bytes.ToArray());
                    Connector.Send(Second256Bytes.ToArray());
                    do
                    {
                        //Затем по частям - файл
                        CurrentReadedBytesCount = Reader.Read(ReadedBytes, 0, ReadedBytes.Length);
                        Connector.Send(ReadedBytes, CurrentReadedBytesCount, SocketFlags.None);
                    }
                    while (CurrentReadedBytesCount == ReadedBytes.Length);
                }
            }
            //Завершаем передачу данных
            Connector.Close();
        }

    
        

    }
}
