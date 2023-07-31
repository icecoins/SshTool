using Renci.SshNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
namespace SshTool
{
    public partial class MainWindow : Window
    {
        SshClient client;
        ForwardedPortLocal forwardedPortLocal;

        public MainWindow()
        {
            InitializeComponent();
            ReadConfig();
        }

        public void ReadConfig()
        {
            if (File.Exists("config") && (File.ReadAllBytes("config").Length > 10))
            {
                byte[] bytes = File.ReadAllBytes("config");
                string[] configs = System.Text.Encoding.Default.GetString(bytes).Split('\n');
                if (configs.Length == 6)
                {
                    MsgAppend("已读取配置");
                    ssh_host.Text = configs[0];
                    ssh_user.Text = configs[1];
                    ssh_password.Text = configs[2];
                    ssh_port.Text = configs[3];
                    app_port.Text = configs[4];
                    local_port.Text = configs[5];
                }
                else
                {
                    MsgAppend("未发现配置");
                }
            }
        }

        public void SaveConfig(string config)
        {
            if (File.Exists("config"))
            {
                File.Delete("config");
            }
            FileStream f = File.Create("config");
            byte[] bytes = System.Text.Encoding.Default.GetBytes(config);
            f.Write(bytes, 0, bytes.Length);
            f.Flush();
            f.Close();
            MsgAppend("已保存配置");
        }

        public void MsgAppend(string msg)
        {
            info.Text = info.Text + "\n" + msg;
            info.ScrollToEnd();
        }

        public void Ssh_Start(string host_s, string user_s, string password_s,
            string server_port_s, string app_port_s, string local_port_s)
        {
            if (host_s.Contains("@"))
            {
                user_s = host_s.Substring(0, host_s.IndexOf("@"));
                ssh_user.Text = user_s;
                host_s = host_s.Substring(host_s.IndexOf("@") + 1);
                ssh_host.Text = host_s;
            }
            string config_temp = host_s + "\n" + user_s + "\n" + password_s + "\n" + server_port_s + "\n" + app_port_s + "\n" + local_port_s;
            SaveConfig(config_temp);
            MsgAppend("服务器地址：" + host_s);
            MsgAppend("ssh端口：" + server_port_s);
            MsgAppend("ssh用户名：" + user_s);
            MsgAppend("ssh密码：" + password_s);
            MsgAppend("应用运行端口：" + app_port_s);
            MsgAppend("本地浏览端口：" + local_port_s);
            client = new SshClient(host_s, int.Parse(server_port_s), user_s, password_s);
            client.KeepAliveInterval = new TimeSpan(0, 0, 5);
            client.ConnectionInfo.Timeout = new TimeSpan(0, 0, 20);
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                string err = "未知错误";
                Console.WriteLine(ex);
                if(ex.ToString().Contains("Permission denied"))
                {
                    err = "用户名或密码错误";
                }
                if (ex.ToString().Contains("actively refused"))
                {
                    err = "服务器异常或ssh端口错误";
                }
                if (ex.ToString().Contains("No such host"))
                {
                    err = "服务器地址错误";
                }
                MsgAppend("连接异常： " + err);
            }
            MsgAppend("连接状态：" + client.IsConnected);
            if (client.IsConnected)
            {
                try
                {
                    forwardedPortLocal = new ForwardedPortLocal("127.0.0.1", uint.Parse(local_port_s), "localhost", uint.Parse(app_port_s));
                    client.AddForwardedPort(forwardedPortLocal);
                    forwardedPortLocal.Start();
                    MsgAppend("转发状态：" + forwardedPortLocal.IsStarted);
                    MsgAppend("本地浏览地址：http://127.0.0.1:" + local_port_s);
                    Process.Start("http://127.0.0.1:" + local_port_s);
                    connect.IsEnabled = false;
                    disconnect.IsEnabled = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    MsgAppend("转发异常");
                }
            }
        }

        public void Ssh_Stop()
        {
            if (!(null == forwardedPortLocal) && forwardedPortLocal.IsStarted)
            {
                forwardedPortLocal.Stop();
                MsgAppend("转发状态： " + forwardedPortLocal.IsStarted);
            }
            if (!(null == client) && client.IsConnected)
            {
                client.Disconnect();
                MsgAppend("连接状态： " + client.IsConnected);
            }
            MsgAppend("连接已断开");
        }

        public void Exit()
        {
            forwardedPortLocal.Dispose();
            client.Dispose();
            Environment.Exit(0);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            var host = ssh_host.Text.Trim();
            var ssh_u = ssh_user.Text.Trim();
            var ssh_p = ssh_port.Text.Trim();
            var pwd = ssh_password.Text.Trim();
            var app_p = app_port.Text.Trim();
            var local_p = local_port.Text.Trim();
            if (IsEmpty(host) || IsEmpty(ssh_p) || IsEmpty(pwd) || IsEmpty(app_p) || IsEmpty(local_p))
            {
                MessageBox.Show("所有参数均为必填项");
            }
            else
            {
                Ssh_Start(host, ssh_u, pwd, ssh_p, app_p, local_p);
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            Ssh_Stop();
            connect.IsEnabled = true;
            disconnect.IsEnabled = false;
        }

        bool IsEmpty(string s)
        {
            return string.IsNullOrEmpty(s);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if ((null == forwardedPortLocal) || (null == client))
            {
                Environment.Exit(0);
            }
            if (forwardedPortLocal.IsStarted || client.IsConnected)
            {
                Console.WriteLine("自动关闭连接");
                Ssh_Stop();
            }
            Exit();
        }
    }
}