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
using static System.Windows.Forms.ListView;

namespace Client
{
    public partial class LoginForm : Form
    {
        // 服务器端口
        int port;
        // 定义变量
        private UdpClient sendUdpClient;
        private UdpClient receiveUdpClient;
        private IPEndPoint clientIPEndPoint;
        private TcpClient tcpClient;
        private TcpListener tcpListener;
        private NetworkStream networkStream;
        private BinaryReader binaryReader;
        private string userListstring;
        private List<ChatFormcs> chatFormList = new List<ChatFormcs>();
        private Dictionary<string, List<Dictionary<string, string>>> unReadMsg = new Dictionary<string, List<Dictionary<string, string>>>();

        public LoginForm()
        {
            InitializeComponent();
            IPAddress[] localIP = Dns.GetHostAddresses("localhost");
            txtserverIP.Text = localIP[1].ToString();
            txtLocalIP.Text = localIP[1].ToString();
            // 随机指定本地端口
            Random random = new Random();
            port = random.Next(1024, 65500);
            txtlocalport.Text = port.ToString();

            // 随机生成用户名
            Random random2 = new Random((int)DateTime.Now.Ticks);
            txtusername.Text = "user" + random2.Next(100, 999);
            btnLogout.Enabled = false;
        }

        // 登录服务器
        private void btnlogin_Click(object sender, EventArgs e)
        {
            // 创建接受套接字
            IPAddress clientIP = IPAddress.Parse(txtLocalIP.Text);
            clientIPEndPoint = new IPEndPoint(clientIP, int.Parse(txtlocalport.Text));
            receiveUdpClient = new UdpClient(clientIPEndPoint);
            // 启动接收线程
            Thread receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();

            // 打开TCP监听器（用于文件传输）
            tcpListener = new TcpListener(IPAddress.Parse(txtLocalIP.Text), int.Parse(txtlocalport.Text) + 1);//端口直接使用上面udp的端口+1
            tcpListener.Start();
            // 启动TCP监听器
            Thread listenThread = new Thread(ListenTcpConnect);
            listenThread.Start();


            // 匿名发送
            sendUdpClient = new UdpClient();
            // 启动发送线程
            Thread sendThread = new Thread(SendMessage);
            sendThread.Start(string.Format("login,{0},{1}", txtusername.Text, clientIPEndPoint));

            btnlogin.Enabled = false;
            btnLogout.Enabled = true;
            this.Text = txtusername.Text;
        }

        // 客户端接受服务器回应消息 
        private void ReceiveMessage()
        {
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // 关闭receiveUdpClient时会产生异常
                    byte[] receiveBytes = receiveUdpClient.Receive(ref remoteIPEndPoint);
                    string message = Encoding.Unicode.GetString(receiveBytes, 0, receiveBytes.Length);

                    // 处理消息
                    string[] splitstring = message.Split(',');
                    //MessageBox.Show(message);

                    switch (splitstring[0])
                    {
                        case "Accept":
                            try
                            {
                                tcpClient = new TcpClient();
                                tcpClient.Connect(remoteIPEndPoint.Address, int.Parse(splitstring[1]));
                                if (tcpClient != null)
                                {
                                    // 表示连接成功
                                    networkStream = tcpClient.GetStream();
                                    binaryReader = new BinaryReader(networkStream);
                                }
                            }
                            catch
                            {
                                MessageBox.Show("连接失败", "异常");
                            }

                            Thread getUserListThread = new Thread(GetUserList);
                            getUserListThread.Start();
                            break;
                        case "login":
                            string userItem = splitstring[1] + "," + splitstring[2];
                            AddItemToListView(userItem);
                            break;
                        case "logout":
                            RemoveItemFromListView(splitstring[1]);
                            break;
                        case "talk":
                            if (chatFormList.Count == 0)
                            {
                                for (int j = 0; j < lstviewOnlineUser.Items.Count; j++)
                                {
                                    if (lstviewOnlineUser.Items[j].SubItems[1].Text == splitstring[2])
                                    {
                                        List<Dictionary<string, string>> list = null;
                                        Dictionary<string, string> dict = new Dictionary<string, string>();
                                        if (unReadMsg.ContainsKey(splitstring[2]))
                                        {
                                            list = unReadMsg[splitstring[2]];
                                            dict.Add("time", splitstring[1]);
                                            dict.Add("content", splitstring[3]);
                                            list.Add(dict);
                                        }
                                        else
                                        {
                                            list = new List<Dictionary<string, string>>();
                                            dict.Add("time", splitstring[1]);
                                            dict.Add("content", splitstring[3]);
                                            list.Add(dict);
                                            unReadMsg.Add(splitstring[2], list);
                                        }
                                        lstviewOnlineUser.Items[j].SubItems[0].Text = "有" + list.Count + "条未读消息！";
                                        //break;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < chatFormList.Count; i++)
                                {
                                    //MessageBox.Show(chatFormList[i].Text);
                                    if (chatFormList[i].Text == splitstring[2])
                                    {
                                        chatFormList[i].ShowTalkInfo(splitstring[2], splitstring[1], splitstring[3]);
                                        break;
                                    }
                                    if (i == chatFormList.Count - 1)
                                    {
                                        for (int j = 0; j < lstviewOnlineUser.Items.Count; j++)
                                        {
                                            if (lstviewOnlineUser.Items[j].SubItems[1].Text == splitstring[2])
                                            {
                                                List<Dictionary<string, string>> list = null;
                                                Dictionary<string, string> dict = new Dictionary<string, string>();
                                                if (unReadMsg.ContainsKey(splitstring[2]))
                                                {
                                                    list = unReadMsg[splitstring[2]];
                                                }
                                                else
                                                {
                                                    list = new List<Dictionary<string, string>>();
                                                }
                                                dict.Add("time", splitstring[1]);
                                                dict.Add("content", splitstring[3]);
                                                list.Add(dict);
                                                unReadMsg.Add(splitstring[2], list);
                                                lstviewOnlineUser.Items[j].SubItems[0].Text = "有" + list.Count + "条未读消息！";
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case "file":
                            if (chatFormList.Count == 0)
                            {

                                for (int j = 0; j < lstviewOnlineUser.Items.Count; j++)
                                {
                                    if (lstviewOnlineUser.Items[j].SubItems[1].Text == splitstring[2])
                                    {
                                        //MessageBox.Show(splitstring[2] + "对话框未打开，自动打开接收文件");
                                        showDialogChatToReceiveFile(lstviewOnlineUser.Items[j]);//打开对话框接收文件
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < chatFormList.Count; i++)
                                {
                                    if (chatFormList[i].Text == splitstring[2])
                                    {
                                        //MessageBox.Show(splitstring[2]+"对话框已经打开，接收文件");
                                        chatFormList[i].ShowTalkInfo(splitstring[2], splitstring[1], splitstring[3]);
                                        break;
                                    }
                                    if (i == chatFormList.Count - 1)
                                    {
                                        for (int j = 0; j < lstviewOnlineUser.Items.Count; j++)
                                        {
                                            if (lstviewOnlineUser.Items[j].SubItems[1].Text == splitstring[2])
                                            {
                                                //MessageBox.Show(splitstring[2] + "对话框未打开，自动打开接收文件");
                                                showDialogChatToReceiveFile(lstviewOnlineUser.Items[j]);//打开对话框接收文件
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                catch
                {
                    //MessageBox.Show("ERROR!");
                    break;
                }
            }
        }

        private void ListenTcpConnect()
        {
            // 接受客户端的连接(传输文件)
            TcpClient newClient = null;
            while (true)
            {
                try
                {
                    newClient = tcpListener.AcceptTcpClient();
                    if (newClient.Connected)
                    {
                        Thread receiveFileThread = new Thread(new ParameterizedThreadStart(ReceiveFileThread));
                        receiveFileThread.SetApartmentState(ApartmentState.STA);
                        receiveFileThread.Start(newClient);
                    }

                    //AddItemToListBox(string.Format("接受客户端{0}的TCP请求", newClient.Client.RemoteEndPoint));
                }
                catch
                {
                    //AddItemToListBox(string.Format("监听线程({0}:{1})", serverIp, tcpPort));
                    break;
                }

                //Thread sendThread = new Thread(SendData);
                //sendThread.Start(newClient);
            }
        }

        private void ReceiveFileThread(object fileClient)
        {
            TcpClient client = (TcpClient)fileClient;

            if (MessageBox.Show("是否要接收文件？", "提示", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                NetworkStream stream = client.GetStream();
                if (stream != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string fileSavePath = saveFileDialog.FileName;//获得用户保存文件的路径
                        FileStream fs = new FileStream(fileSavePath, FileMode.Create, FileAccess.Write);
                        int size = 0;
                        byte[] buffer = new byte[512];
                        while ((size = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, size);
                        }
                        MessageBox.Show("文件接收完毕！");
                        fs.Flush();
                        stream.Flush();
                        stream.Close();
                        client.Close();
                    }
                }
            }
            else
            {
                client.Close();
            }

        }

        // 从服务器获取在线用户列表
        private void GetUserList()
        {
            while (true)
            {
                userListstring = null;
                try
                {
                    userListstring = binaryReader.ReadString();
                    if (userListstring.EndsWith("end"))
                    {
                        string[] splitstring = userListstring.Split(';');
                        for (int i = 0; i < splitstring.Length - 1; i++)
                        {
                            AddItemToListView(splitstring[i]);
                        }

                        binaryReader.Close();
                        tcpClient.Close();
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // 用委托机制来操作界面上控件
        private delegate void AddItemToListViewDelegate(string str);

        /// <summary>
        /// 在ListView中追加用户信息
        /// </summary>
        /// <param name="userinfo">要追加的信息</param>
        private void AddItemToListView(string userinfo)
        {
            if (lstviewOnlineUser.InvokeRequired)
            {
                AddItemToListViewDelegate adddelegate = AddItemToListView;
                lstviewOnlineUser.Invoke(adddelegate, userinfo);
            }
            else
            {
                string[] splitstring = userinfo.Split(',');
                ListViewItem item = new ListViewItem();
                item.SubItems.Add(splitstring[0]);
                item.SubItems.Add(splitstring[1]);
                lstviewOnlineUser.Items.Add(item);
            }
        }

        private delegate void RemoveItemFromListViewDelegate(string str);

        /// <summary>
        /// 从ListView中删除用户信息
        /// </summary>
        /// <param name="str">要删除的信息</param>
        private void RemoveItemFromListView(string str)
        {
            if (lstviewOnlineUser.InvokeRequired)
            {
                RemoveItemFromListViewDelegate removedelegate = RemoveItemFromListView;
                lstviewOnlineUser.Invoke(removedelegate, str);
            }
            else
            {
                for (int i = 0; i < lstviewOnlineUser.Items.Count; i++)
                {
                    if (lstviewOnlineUser.Items[i].SubItems[1].Text == str)
                    {
                        lstviewOnlineUser.Items[i].Remove();
                    }
                }
            }
        }

        // 发送登录或者注销请求
        private void SendMessage(object obj)
        {
            string message = (string)obj;
            byte[] sendbytes = Encoding.Unicode.GetBytes(message);
            IPAddress remoteIp = IPAddress.Parse(txtserverIP.Text);
            IPEndPoint remoteIPEndPoint = new IPEndPoint(remoteIp, int.Parse(txtServerport.Text));
            sendUdpClient.Send(sendbytes, sendbytes.Length, remoteIPEndPoint);
            sendUdpClient.Close();
        }

        // 退出
        private void btnLogout_Click(object sender, EventArgs e)
        {
            // 匿名发送
            sendUdpClient = new UdpClient();
            //启动发送线程
            Thread sendThread = new Thread(SendMessage);
            sendThread.Start(string.Format("logout,{0},{1}", txtusername.Text, clientIPEndPoint));
            receiveUdpClient.Close();
            lstviewOnlineUser.Items.Clear();
            btnlogin.Enabled = true;
            btnLogout.Enabled = false;
            this.Text = "Client";
        }

        // 双击打开与某个用户聊天的子窗口 
        private void lstviewOnlineUser_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string peerName = lstviewOnlineUser.SelectedItems[0].SubItems[1].Text;
            if (peerName == txtusername.Text)
            {
                return;
            }
            lstviewOnlineUser.SelectedItems[0].SubItems[0].Text = "";//清空未读消息提示

            string ipEndPoint = lstviewOnlineUser.SelectedItems[0].SubItems[2].Text;
            //MessageBox.Show("ipEndPoint : " + ipEndPoint);
            string[] splitString = ipEndPoint.Split(':');
            IPAddress peerIP = IPAddress.Parse(splitString[0]);
            IPEndPoint peerIPEndPoint = new IPEndPoint(peerIP, int.Parse(splitString[1]));
            ChatFormcs dialogChat = new ChatFormcs();
            dialogChat.SetUserInfo(txtusername.Text, peerName, peerIPEndPoint);
            dialogChat.Text = peerName;
            chatFormList.Add(dialogChat);

            if (unReadMsg.ContainsKey(peerName))
            {
                foreach (Dictionary<string, string> msgItem in unReadMsg[peerName])
                {
                    dialogChat.ShowTalkInfo(peerName, msgItem["time"], msgItem["content"]);
                }
                unReadMsg[peerName].Clear();
            }
            dialogChat.FormClosing += closeDialogChat;
            dialogChat.Show();
        }

        private void showDialogChatToReceiveFile(ListViewItem electedItem)
        {
            string peerName = electedItem.SubItems[1].Text;
            electedItem.SubItems[0].Text = "";//清空未读消息de提示
            string ipEndPoint = electedItem.SubItems[2].Text;
            //MessageBox.Show("ipEndPoint : " + ipEndPoint);
            string[] splitString = ipEndPoint.Split(':');
            IPAddress peerIP = IPAddress.Parse(splitString[0]);
            IPEndPoint peerIPEndPoint = new IPEndPoint(peerIP, int.Parse(splitString[1]));
            ChatFormcs dialogChat = new ChatFormcs();
            dialogChat.SetUserInfo(txtusername.Text, peerName, peerIPEndPoint);
            dialogChat.Text = peerName;
            chatFormList.Add(dialogChat);

            dialogChat.ShowTalkInfo(peerName, DateTime.Now.ToLongTimeString(), "向你发送了文件");
            dialogChat.FormClosing += closeDialogChat;
            dialogChat.Show();
        }

        private void closeDialogChat(object sender, FormClosingEventArgs e)
        {
            ChatFormcs chatFormcs = (ChatFormcs)sender;
            string peerName = chatFormcs.peerUserName;
            foreach (ChatFormcs item in this.chatFormList)
            {
                if (item.peerUserName == peerName)
                {
                    this.chatFormList.Remove(item);
                    break;
                }
            }
        }
    }
}
