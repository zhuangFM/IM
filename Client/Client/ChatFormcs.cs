using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// 添加额外命名空间
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Client
{
    public partial class ChatFormcs : Form
    {
        private string selfUserName;
        public string peerUserName { get; set; }
        private IPEndPoint peerUserIPEndPoint;
        private UdpClient sendUdpClient;

        public ChatFormcs()
        {
            InitializeComponent();
        }

        public void SetUserInfo(string selfName, string peerName, IPEndPoint peerIPEndPoint)
        {
            selfUserName = selfName;
            peerUserName = peerName;
            peerUserIPEndPoint = peerIPEndPoint;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txbSend.Text.Trim() == "")
            {
                MessageBox.Show("空白消息");
                return;
            }
            // 匿名发送
            sendUdpClient = new UdpClient();
            // 启动发送线程
            Thread sendThread = new Thread(SendMessage);
            sendThread.Start(string.Format("talk,{0},{1},{2}", DateTime.Now.ToLongTimeString(), selfUserName, txbSend.Text));
            richtxbTalkinfo.AppendText(selfUserName + "    " + DateTime.Now.ToLongTimeString() + " 说" + Environment.NewLine + txbSend.Text);
            richtxbTalkinfo.AppendText(Environment.NewLine);
            // 将控件内容滚动到当前插入符的位置
            richtxbTalkinfo.ScrollToCaret();
            txbSend.Text = "";
            txbSend.Focus();

        }
        private void SendMessage(object obj)
        {
            string message = (string)obj;
            byte[] sendbytes = Encoding.Unicode.GetBytes(message);
            sendUdpClient.Send(sendbytes, sendbytes.Length, peerUserIPEndPoint);
            sendUdpClient.Close();
        }

        public void ShowTalkInfo(string peerName, string time, string content)
        {
            richtxbTalkinfo.AppendText(peerName + "    " + time + " 说" + Environment.NewLine + content);
            richtxbTalkinfo.AppendText(Environment.NewLine);
            richtxbTalkinfo.ScrollToCaret();
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string filePath = "";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                // 匿名发送
                sendUdpClient = new UdpClient();
                // 启动发送线程
                Thread sendThread = new Thread(SendMessage);
                sendThread.Start(string.Format("file,{0},{1},{2}", DateTime.Now.ToLongTimeString(), selfUserName, selfUserName + "向你发送了文件"));
                //启动TCP发送文件
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(peerUserIPEndPoint.Address, peerUserIPEndPoint.Port + 1);
                NetworkStream ns = tcpClient.GetStream();
                FileStream fs = new FileStream(filePath, FileMode.Open);
                int size = 0;//初始化读取的流量为0 
                long len = 0;//初始化已经读取的流量   
                while (len < fs.Length)
                {
                    byte[] buffer = new byte[512];
                    size = fs.Read(buffer, 0, buffer.Length);
                    ns.Write(buffer, 0, size);
                    len += size;
                }
                fs.Flush();
                ns.Flush();
                fs.Close();
                ns.Close();
            }
        }
    }
}
