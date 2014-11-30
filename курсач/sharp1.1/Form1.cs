using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace sharp1._1
{
    public partial class Form1 : Form
    {

        public bool send_allow = true;
        // C_SendFile Send;

        string broadcast_check = "some_word";
        string host;
        int port;


        public string MyIp()
        {
            string myHost = System.Net.Dns.GetHostName();
            string myIP = System.Net.Dns.GetHostByName(myHost).AddressList[0].ToString();
            return myIP;
        }


        public void toLog(string text)
        {
            string line = Environment.NewLine + System.DateTime.Now.ToUniversalTime() + " : " + text;
            richTextBox1.Text += line;

            System.IO.File.AppendAllText("log.txt", line);

        }

        public Form1()
        {
            auth form = new auth();
            form.ShowDialog();
            InitializeComponent();
            comboBox1.SelectedIndex = 0;

            TreadIn.WorkerReportsProgress = true;
            TreadIn.WorkerSupportsCancellation = true;
            TreadIn.RunWorkerAsync();
            broadcast_listen_thread.RunWorkerAsync();
            richTextBox1.Text = "Программа запущена успешно!";


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (System.Windows.Forms.DialogResult.OK == openFileDialog1.ShowDialog())
            {
                textBox3.Text = openFileDialog1.FileName;
            }


        }


        private void SendBtn_Click(object sender, EventArgs e)
        {
            host = comboBox2.Text;

            try
            {
                port = int.Parse(textBox2.Text);
            }
            catch
            {
                MessageBox.Show("Введите корректный порт!");
                return;
            }
            //Send = new C_SendFile(host, port);
            Thread_SendFile.RunWorkerAsync();
        }

        //private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (comboBox1.SelectedIndex == 0)
        //    {
        //        send_allow = true;
        //        ToLogSafe("ok");
        //        return;
        //    }
        //    send_allow = false;
        //}

        // Приём файла
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            BackgroundWorker worker = sender as BackgroundWorker;
            TcpListener Listener = new TcpListener(5678);
            string filename;
            Listener.Start();
            Socket ReceiveSocket;
            int progress = 0;
            while (send_allow)
            {
                try
                {
                    ReceiveSocket = Listener.AcceptSocket();
                    System.Windows.Forms.SaveFileDialog savedial = new SaveFileDialog();
                    //if (System.Windows.Forms.DialogResult.OK == savedial.ShowDialog())
                    // {
                    filename = "C:/test.txt";//savedial.FileName;
                    //}
                    Byte[] Receive = new Byte[256];

                    //using (MemoryStream MessageR = new MemoryStream())
                    {

                        //Количество считанных байт
                        Int32 ReceivedBytes;
                        Int32 Firest256Bytes = 0;
                        String FilePath = "";
                        Int64 filesize = 0;
                        Int64 all_size = -1;
                        FileStream fl = null;
                        String resFilePath = "";
                        Stopwatch stopWatch = new Stopwatch();
                        Stopwatch stopWatch2 = new Stopwatch();
                        string speed = "0";

                        int speed_last = 0;
                        int count = 0;
                        do
                        {//Собственно читаем
                            if (count == 0)
                            {
                                stopWatch.Restart();

                            }
                            ReceivedBytes = ReceiveSocket.Receive(Receive, Receive.Length, 0);
                            all_size += ReceivedBytes;
                            //Разбираем первые 256 байт
                            if (Firest256Bytes < 256)
                            {

                                Firest256Bytes += ReceivedBytes;
                                Byte[] ToStr = Receive;
                                all_size -= 256;

                                //Накапливаем имя файла
                                FilePath += Encoding.Default.GetString(ToStr);
                                resFilePath = FilePath.Substring(0, FilePath.IndexOf('\0'));
                                fl = new FileStream(resFilePath, FileMode.Create);
                            }
                            else
                                if (Firest256Bytes < 512)
                                {
                                    all_size -= 256;
                                    Firest256Bytes += ReceivedBytes;
                                    Byte[] ToStr = Receive;
                                    string str_size = "";

                                    str_size += Encoding.Default.GetString(ToStr);
                                    String res_size = str_size.Substring(0, str_size.IndexOf('\0'));
                                    filesize = Convert.ToInt64(res_size);

                                    all_size = 0;

                                    file_desc f = new file_desc(resFilePath, filesize);
                                    byte[] mess = new byte[1];

                                    if (!AcessFileSafe(f))
                                    {
                                        ToLogSafe("Передача отклонена пользователем!");
                                        mess[0] = 0;
                                        ReceiveSocket.Send(mess);
                                        return;
                                    }
                                    else
                                    {
                                        mess[0] = 1;
                                        ReceiveSocket.Send(mess);
                                        stopWatch.Start();
                                        stopWatch2.Start();
                                    }


                                }
                                else
                                {

                                    count++;
                                    if (count == 100)
                                    {

                                        TimeSpan ts = stopWatch.Elapsed;

                                        int progres = (int)(all_size / (filesize / 100));

                                        int speed_cur = Convert.ToInt32((25600 * 1000 / 1024) / ts.TotalMilliseconds);
                                        TimeSpan ts2 = stopWatch2.Elapsed;

                                        if (ts2.Seconds > 1)
                                        {
                                            stopWatch2.Restart();
                                            speed = Convert.ToString(speed_cur);
                                            speed_last = speed_cur;
                                        }


                                        TreadIn.ReportProgress(progres, speed);
                                        count = 0;
                                    }
                                    //и записываем в поток


                                    fl.Write(Receive, 0, ReceivedBytes);

                                }

                            //Читаем до тех пор, пока в очереди не останется данных
                        } while (all_size != filesize && !Thread_SendFile.CancellationPending);

                        if (TreadIn.CancellationPending)
                        {
                            ReceiveSocket.Close();
                            TreadIn.ReportProgress(0, "0");
                        }

                        ReceiveSocket.Close();

                        ToLogSafe("Приём " + resFilePath + " завершен!");

                    }

                }
                catch (System.Exception er)
                {

                    ToLogSafe("Ошибка : " + er.Message);
                }
            }


        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            speedText.Text = e.UserState.ToString();
        }

        private void Thread_SendFile_DoWork(object sender, DoWorkEventArgs e)
        {
            SendFile(textBox3.Text, 5678, host);
            Thread_SendFile.CancelAsync();
        }

        private void ToLogSafe(string text)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action<string>((s) =>
                {
                    toLog(s);
                }), text);
            else toLog(text);
        }


        private bool AcessFileSafe(file_desc file)
        {
            int result = 0;
            DialogResult dialogResult = DialogResult.None;
            if (InvokeRequired)
                this.BeginInvoke(new Action<file_desc>((s) =>
                {
                    dialogResult = MessageBox.Show("Хотите принять файл " +
                s.file_name + ", размером " + s.file_size.ToString() + " байт ?", "Проверка", MessageBoxButtons.YesNo);
                }), file);
            while (dialogResult == System.Windows.Forms.DialogResult.None)
            {
            }
            if (dialogResult == DialogResult.Yes)
            {
                return true;
            }
            else if (dialogResult == DialogResult.No)
            {
                return false;
            }
            return false;


        }

        private void SetSpeedSafe(string text)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action<string>((s) =>
                {
                    SetSpeed(s);
                }), text);
            else SetSpeed(text);
        }

        private void SetSpeed(string speed)
        {
            speedText.Text = speed;
        }



        public void SendFile(string FileName, int port, string ip)
        {
            //Коннектимся
            bool access = false;
            TcpListener Listener;
            Listener = new TcpListener(port);
            IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket Connector = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ToLogSafe("Устанавливается соединение.");
            try
            {
                Connector.Connect(EndPoint);
            }
            catch (Exception e)
            {
                ToLogSafe("Ошибка");
                ToLogSafe(e.Message.ToString());
                return;
            }
            ToLogSafe("Соединение установлено!");

            //Получаем имя из полного пути к файлу
            //StringBuilder FileName = new StringBuilder(openFileDialog1.FileName);
            //Выделяем имя файла

            //Получаем имя файла

            System.IO.FileInfo file = new System.IO.FileInfo(FileName);
            long size = file.Length;

            String resFileName = System.IO.Path.GetFileName(FileName);
            long filesize = size;
            long all_size = 0;
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
                    ToLogSafe("Ожидаем разрешения");
                    while (!access) // Ожидаем подтвердения
                    {
                        byte[] mess = new byte[1];
                        Connector.Receive(mess);
                        access = mess[0] == 0 ? false : true;
                    }

                    ToLogSafe("Разрешение получено. Начинается передача файла");
                    Stopwatch stopWatch = new Stopwatch();
                    Stopwatch stopWatch2 = new Stopwatch();
                    int count = 0;
                    CurrentReadedBytesCount = 0;
                    int speed_last = 0;
                    string speed = "0";
                    stopWatch2.Start();
                    do
                    {
                        if (count == 0)
                        {
                            stopWatch.Reset();
                            stopWatch.Start();
                        }


                        //Затем по частям - файл
                        try
                        {
                            CurrentReadedBytesCount = Reader.Read(ReadedBytes, 0, ReadedBytes.Length);
                            Connector.Send(ReadedBytes, CurrentReadedBytesCount, SocketFlags.None);
                        }
                        catch (Exception e)
                        {
                            ToLogSafe(e.Message);
                            return;
                        }
                        all_size += ReadedBytes.Length;

                        count++;
                        if (count == 100)
                        {
                            stopWatch.Stop();
                            TimeSpan ts = stopWatch.Elapsed;
                            int progres = (int)(all_size / (filesize / 100));

                            int speed_cur = Convert.ToInt32((25600 * 1000 / 1024) / ts.TotalMilliseconds);
                            TimeSpan ts2 = stopWatch2.Elapsed;

                            if (ts2.Seconds > 1)
                            {
                                stopWatch2.Reset();
                                stopWatch2.Start();
                                speed = Convert.ToString(speed_cur);
                                speed_last = speed_cur;
                            }
                            Thread_SendFile.ReportProgress(progres, speed);

                            count = 0;
                        }


                    }
                    while (CurrentReadedBytesCount == ReadedBytes.Length);
                }
            }
            //Завершаем передачу данных
            ToLogSafe("Передача завершена!");
            Connector.Close();
        }

        private void Thread_SendFile_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            speedText.Text = e.UserState.ToString();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Thread_SendFile.CancelAsync();
                TreadIn.CancelAsync();
            }
            catch
            {
            }

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            sendBroadCast(broadcast_check);
        }

        private void sendBroadCast(string message)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, 9050);
            string hostname = Dns.GetHostName();
            byte[] data = Encoding.ASCII.GetBytes(message);
            sock.SendTo(data, iep);
            sock.Close();


        }

        private void toComboSafe(string ip)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action<string>((s) =>
                {

                    comboBox2.Items.Add(s);
                }), ip);
        }

        private void sendMes(string ip, string message)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(ip), 9050);
            //string hostname = Dns.GetHostName();
            byte[] data = Encoding.ASCII.GetBytes(message);
            sock.SendTo(data, iep);
            sock.Close();
        }

        private void sendMesSafe(string ip)
        {
            if (InvokeRequired)
                this.BeginInvoke(new Action<string>((s) =>
                {
                    sendMes(s, "ans");
                }), ip);
        }


        private void broadcast_listen_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9050);
            sock.Bind(iep);
            EndPoint ep = (EndPoint)iep;
            ToLogSafe("Готов к приёму широковещательных сообщений!");

            byte[] data = new byte[256];
            while (true)
            {
                int recv = sock.ReceiveFrom(data, ref ep);
                string stringData = Encoding.ASCII.GetString(data, 0, recv);
                string ip = ep.ToString();
                ip = ip.Split(':')[0];
                if (stringData == broadcast_check)
                {
                    ToLogSafe("Пришло широковещательное сообщение от " + ip);
                    //comboBox1.Items.Add(ip);
                    sendMesSafe(ip);
                }
                if (stringData == "ans")
                {
                    toComboSafe(ip);
                }


            }
            sock.Close();
        }

        private void оПрограммеToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            f_about form = new f_about();
            form.ShowDialog();
        }
    }
}
