using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace TeacherApp
{
    public partial class Form1 : Form
    {
        private TcpListener? server = null;
        private Thread? listenThread = null;
        private volatile bool isRunning = false;

        private readonly Dictionary<string, Panel> studentPanels = new Dictionary<string, Panel>();
        private readonly object panelsLock = new object();

        private const int MAX_CLIENTS = 64;
        private const int MAX_IMAGE_BYTES = 10 * 1024 * 1024; // 10 MB
        private const int PANEL_WIDTH = 280;                // 16:9 ����ͼ����
        private const int PANEL_HEIGHT = 157;               // �߶� = 280/16*9

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            isRunning = true;

            listenThread = new Thread(ListenLoop) { IsBackground = true };
            listenThread.Start();
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = server!.AcceptTcpClient();
                    Thread t = new Thread(HandleClient) { IsBackground = true };
                    t.Start(client);
                }
                catch { }
            }
        }

        private void HandleClient(object? obj)
        {
            TcpClient client = (TcpClient)obj!;
            IPEndPoint remoteEp = (IPEndPoint)client.Client.RemoteEndPoint!;
            string internalKey = remoteEp.ToString();
            string ipText = remoteEp.Address.ToString();

            NetworkStream ns = client.GetStream();
            BinaryReader br = new BinaryReader(ns, System.Text.Encoding.UTF8, true);

            Invoke((Action)(() =>
            {
                lock (panelsLock)
                {
                    if (!studentPanels.ContainsKey(internalKey) && studentPanels.Count < MAX_CLIENTS)
                    {
                        Panel panel = new Panel
                        {
                            Width = 280,
                            Height = 157, // 16:9
                            BorderStyle = BorderStyle.FixedSingle,
                            Margin = new Padding(5)
                        };

                        PictureBox pb = new PictureBox
                        {
                            Dock = DockStyle.Fill,
                            SizeMode = PictureBoxSizeMode.Zoom // ���ֱ���������ʾ
                        };

                        panel.Controls.Add(pb);


                        // ˫���¼�����ȫ��
                        pb.DoubleClick += (s, e) =>
                        {
                            if (pb.Image != null)
                            {
                                Image copy = new Bitmap(pb.Image); // ������ֹ���޸�
                                FullScreenForm fs = new FullScreenForm(copy);
                                fs.ShowDialog();
                                copy.Dispose();
                            }
                        };


                        Label lbl = new Label
                        {
                            Dock = DockStyle.Bottom,
                            Height = 20,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Text = ipText
                        };

                        panel.Controls.Add(pb);
                        panel.Controls.Add(lbl);
                        flowLayoutPanel1.Controls.Add(panel);
                        studentPanels[internalKey] = panel;
                    }
                }
            }));

            try
            {
                while (client.Connected)
                {
                    int len = br.ReadInt32();
                    if (len <= 0 || len > MAX_IMAGE_BYTES)
                        throw new InvalidDataException($"�Ƿ�ͼƬ����: {len}");

                    byte[] data = ReadExactly(ns, len);

                    using (var ms = new MemoryStream(data))
                    using (Image temp = Image.FromStream(ms))
                    {
                        Image copy = new Bitmap(temp);
                        Invoke((Action)(() =>
                        {
                            lock (panelsLock)
                            {
                                if (studentPanels.TryGetValue(internalKey, out Panel panel))
                                {
                                    PictureBox pb = panel.Controls.OfType<PictureBox>().FirstOrDefault();
                                    if (pb != null)
                                    {
                                        if (pb.Image != null)
                                            try { pb.Image.Dispose(); } catch { }
                                        pb.Image = copy;
                                    }
                                }
                            }
                        }));
                    }
                }
            }
            catch
            {
                try { client.Close(); } catch { }

                Invoke((Action)(() =>
                {
                    lock (panelsLock)
                    {
                        if (studentPanels.TryGetValue(internalKey, out Panel panel))
                        {
                            flowLayoutPanel1.Controls.Remove(panel);
                            studentPanels.Remove(internalKey);
                            try { panel.Dispose(); } catch { }
                        }
                    }
                }));
            }
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    throw new EndOfStreamException("�Զ˹رջ������쳣");
                offset += read;
            }
            return buffer;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRunning = false;
            try { server?.Stop(); } catch { }
            try { listenThread?.Join(500); } catch { }
        }
    }
}