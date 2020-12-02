using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace P2PRedes
{
    public partial class Form1 : Form
    {
        private static string shortFileName = "";
        private static string fileName = "";

        public delegate void FileRecievedEventHandler(object source, string fileName);

        public event FileRecievedEventHandler NewFileRecieved;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.NewFileRecieved += new FileRecievedEventHandler(Form1_NewFileRecieved);
        }

        private void Form1_NewFileRecieved(object sender, string fileName)
        {
            this.BeginInvoke(
            new Action(
            delegate()
            {
                MessageBox.Show("Novo arquivo recebido\n" + fileName);
                System.Diagnostics.Process.Start("explorer", @"c:\");
            }));
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            int port = int.Parse(txtHost.Text);
            Task.Factory.StartNew(() => HandleIncomingFile(port));
            MessageBox.Show("Escutando na porta : " + port);
        }

        public void HandleIncomingFile(int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(port);
                tcpListener.Start();
                while (true)
                {
                    Socket handlerSocket = tcpListener.AcceptSocket();
                    if (handlerSocket.Connected)
                    {
                        string fileName = string.Empty;
                        NetworkStream networkStream = new NetworkStream(handlerSocket);
                        int thisRead = 0;
                        int blockSize = 1024;
                        Byte[] dataByte = new Byte[blockSize];
                        lock (this)
                        {
                            string folderPath = @"c:\";
                            handlerSocket.Receive(dataByte);
                            int fileNameLen = BitConverter.ToInt32(dataByte, 0);
                            fileName = Encoding.ASCII.GetString(dataByte, 4, fileNameLen);
                            Stream fileStream = File.OpenWrite(folderPath + fileName);
                            fileStream.Write(dataByte, 4 + fileNameLen, (1024 - (4 + fileNameLen)));
                            while (true)
                            {
                                thisRead = networkStream.Read(dataByte, 0, blockSize);
                                fileStream.Write(dataByte, 0, thisRead);
                                if (thisRead == 0)
                                    break;
                            }
                            fileStream.Close();
                        }
                        if (NewFileRecieved != null)
                        {
                            NewFileRecieved(this, fileName);
                        }
                        handlerSocket = null;
                    }
                }
            }
            catch
            {
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Selecionar arquivo:";
            dlg.ShowDialog();
            txtFile.Text = dlg.FileName;
            fileName = dlg.FileName;
            shortFileName = dlg.SafeFileName;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string ipAddress = txtIPAddress.Text;
            int port = int.Parse(txtPort.Text);
            string fileName = txtFile.Text;
            Task.Factory.StartNew(() => SendFile(ipAddress, port, fileName, shortFileName));
            MessageBox.Show("Arquivo enviado");
        }

        public void SendFile(string remoteHostIP, int remoteHostPort, string longFileName, string shortFileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(remoteHostIP))
                {
                    byte[] fileNameByte = Encoding.ASCII.GetBytes(shortFileName);
                    byte[] fileData = File.ReadAllBytes(longFileName);
                    byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                    byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
                    fileNameLen.CopyTo(clientData, 0);
                    fileNameByte.CopyTo(clientData, 4);
                    fileData.CopyTo(clientData, 4 + fileNameByte.Length);
                    TcpClient clientSocket = new TcpClient(remoteHostIP, remoteHostPort);
                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Write(clientData, 0, clientData.GetLength(0));
                    networkStream.Close();
                }
            }
            catch
            {
            }
        }
    }
}