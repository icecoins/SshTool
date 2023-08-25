using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SshTool
{
    public partial class MainWindow : Window
    {
        SshClient client;
        ForwardedPortLocal forwardedPortLocal;
        Config[] configs;
        int last_selected = -1;


        public MainWindow()
        {
            InitializeComponent();
            ReadConfig();
        }

        public void ReadConfig()
        {
            if (File.Exists("config"))
            {
                byte[] bytes = File.ReadAllBytes("config");
                string[] info = System.Text.Encoding.Default.GetString(bytes).Split('\n');
                // Console.WriteLine(info.Length);
                if (info.Length > 1)
                {
                    // MsgAppend("已读取配置");
                    Console.WriteLine(info[0]);
                    last_selected = int.Parse(info[0]);
                    configs = new Config[info.Length - 1];
                    for(int i = 1; i < info.Length; i++)
                    {
                        // Console.WriteLine("ID: " + i + " MSG:" + info[i]);
                        string[] items = info[i].Split('\t');
                        // Console.WriteLine(info[i]);
                        if (items.Length == 7)
                        {
                            configs[i-1] = new Config(i-1, items[0], items[1], items[2], items[3], items[4], items[5], items[6]);
                        }
                    }

                    if (configs.Length < 1)
                    {
                        MsgAppend("配置信息为空");
                    }
                    else
                    {
                        if (last_selected < 0)
                        {
                            last_selected = 0;
                        }
                        config_info.ItemsSource = GetConfigLabels();
                        config_info.SelectionChanged += ConfigOnSelect;
                        config_info.SelectedIndex = last_selected;
                        MsgAppend("发现配置，数量：" + configs.Length);
                    }
                }
                else
                {
                    configs = null;
                    config_info.SelectedItem = null;
                    MsgAppend("配置内容为空");
                }
            }
            else
            {
                MsgAppend("未发现配置文件");
            }
        }

        byte[] ConfigToBytes(Config config)
        {
            return System.Text.Encoding.Default.GetBytes("\n" + config.label + "\t" + config.ssh_host + "\t" + 
                config.ssh_user + "\t" + config.ssh_password + "\t" + config.ssh_port + "\t" + config.app_port + "\t" + config.local_port);
        }

        public void SaveConfig()
        {
            if (File.Exists("config"))
            {
                File.Delete("config");
            }
            FileStream f = File.Create("config");
            byte[] bytes = System.Text.Encoding.Default.GetBytes(last_selected.ToString());
            f.Write(bytes, 0, bytes.Length);
            for (int i = 0; i < configs.Length; i++)
            {
                if(-999 == configs[i].index)
                {
                    continue;
                }
                bytes = ConfigToBytes(configs[i]);
                f.Write(bytes, 0, bytes.Length);
            }
            f.Flush();
            f.Close();
            //MsgAppend("已保存配置");
        }

        List<string> GetConfigLabels()
        {
            List<string> list = new List<string>();
            foreach(Config cfg in configs)
            {
                list.Add(cfg.index + "\t名称：" + GetLimitedString(cfg.label) + "\t地址：" + 
                    GetLimitedString(cfg.ssh_host));
            }
            return list;
        }

        string GetLimitedString(string str)
        {
            int limited_len = 15;
            if(str.Length < limited_len)
            {
                return str;
            }
            return str.Substring(0, limited_len) + "...";
        }

        void ConfigOnSelect(object sender, SelectionChangedEventArgs e)
        {
            if(null == configs)
            {
                config_info.ItemsSource = null;
                config_label.Text = "";
                ssh_host.Text = "";
                ssh_user.Text = "";
                ssh_password.Text = "";
                ssh_port.Text = "";
                app_port.Text = "";
                local_port.Text = "";
                return;
            }
            if(null == config_info.SelectedItem)
            {
                last_selected = 0;
            }
            else
            {
                last_selected = int.Parse(config_info.SelectedItem.ToString().Split('\t')[0]);
            }
            Config selectedOption = configs[last_selected];
            config_label.Text = selectedOption.label;
            ssh_host.Text = selectedOption.ssh_host;
            ssh_user.Text = selectedOption.ssh_user;
            ssh_password.Text = selectedOption.ssh_password;
            ssh_port.Text = selectedOption.ssh_port;
            app_port.Text = selectedOption.app_port;
            local_port.Text = selectedOption.local_port;
            //MsgAppend("载入配置: " + selectedOption.label);
            SaveConfig();
        }

        public void MsgAppend(string msg)
        {
            info.Text = info.Text + "\n" + msg;
            info.ScrollToEnd();
        }

        bool IsLabelExist(string label)
        {
            foreach(Config cfg in configs)
            {
                if (cfg.label.Equals(label))
                {
                    return true;
                }
            }
            return false;
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
            //string config_temp = host_s + "\n" + user_s + "\n" + password_s + "\n" + server_port_s + "\n" + app_port_s + "\n" + local_port_s;
            //SaveConfig();
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

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            NewConfigFromInput();
        }

        void NewConfigFromInput()
        {
            var host = ssh_host.Text.Trim();
            var label = config_label.Text.Trim();
            var ssh_u = ssh_user.Text.Trim();
            var ssh_p = ssh_port.Text.Trim();
            var pwd = ssh_password.Text.Trim();
            var app_p = app_port.Text.Trim();
            var local_p = local_port.Text.Trim();
            if (IsEmpty(host) || IsEmpty(label) || IsEmpty(label) || IsEmpty(ssh_u) || IsEmpty(pwd) || IsEmpty(app_p) || IsEmpty(local_p))
            {
                MessageBox.Show("所有参数均为必填项");
                return;
            }

            if (null == configs)
            {
                configs = new Config[1];
                configs[0] = new Config(configs.Length, label, host, ssh_u, pwd, ssh_p, app_p, local_p);
            }
            else if (IsLabelExist(label))
            {
                MessageBox.Show("配置名称已存在，请重新填写配置信息后再次点击新增");
                return;
            }
            else
            {
                Config cfg = new Config(configs.Length, label, host, ssh_u, pwd, ssh_p, app_p, local_p);
                Config[] Cfgs = new Config[configs.Length + 1];
                configs.CopyTo(Cfgs, 0);
                Cfgs[Cfgs.Length - 1] = cfg;
                configs = Cfgs;
                last_selected = Cfgs.Length - 1;
            }
            SaveConfig();
            ReadConfig();
            MsgAppend("添加成功");
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if(null == configs || configs.Length < 1)
            {
                MsgAppend("当前配置信息为空");
            }
            else
            {
                configs[last_selected].index = -999;
                last_selected = 0;
                MsgAppend("删除成功");
                SaveConfig();
                ReadConfig();
            }
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