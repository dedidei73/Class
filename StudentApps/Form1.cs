using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace StudentApp
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread sendThread;
        private bool isConnected = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIP.Text.Trim();  // �� TextBox ��ȡ��ʦ�� IP
                client = new TcpClient(ip, 5000);  // Ĭ�� 5000 �˿�
                stream = client.GetStream();

                isConnected = true;

                // ��ʼ������Ļ�߳�
                sendThread = new Thread(SendLoop) { IsBackground = true };
                sendThread.Start();

                MessageBox.Show("�����ӽ�ʦ�ˣ����ڹ�����Ļ��");
            }
            catch
            {
                MessageBox.Show("����ʧ�ܣ������ʦ���Ƿ�����");
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (!isConnected) return;

            string inputPwd = Microsoft.VisualBasic.Interaction.InputBox("������Ͽ����룺", "��֤", "");
            if (inputPwd == "Dedidei73")
            {
                isConnected = false;          // ֪ͨ�����߳��Լ��˳�
                sendThread?.Join();           // �ȴ��߳̽���
                sendThread = null;

                stream?.Close();
                client?.Close();

                MessageBox.Show("�ѶϿ����ӡ�");
            }
            else
            {
                MessageBox.Show("��������޷��Ͽ����ӣ�");
            }
        }

        private void SendLoop()
        {
            try
            {
                while (isConnected)
                {
                    // ����
                    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }

                    // ѹ��ΪJPEG�ֽ��� 
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byte[] data = ms.ToArray();

                        // �������ݳ���
                        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
                        stream.Write(lengthBytes, 0, lengthBytes.Length);

                        // ��������
                        stream.Write(data, 0, data.Length);
                    }

                    Thread.Sleep(200); // ���Ʒ���Ƶ�ʣ�����ռ������
                }
            }
            catch
            {
                // �����ʦ�˶Ͽ����Զ��˳�
                isConnected = false;
            }
        }


    }
}