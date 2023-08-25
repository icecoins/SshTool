namespace SshTool
{
    class Config
    {
        public Config(int index, string label, string ssh_host, string ssh_user, 
            string ssh_password, string ssh_port, string app_port, string local_port)
        {
            this.index = index;
            this.label = label;
            this.ssh_host = ssh_host;
            this.ssh_user = ssh_user;
            this.ssh_password = ssh_password;
            this.ssh_port = ssh_port;
            this.app_port = app_port;
            this.local_port = local_port;
        }
        public int index;
        public string label,
            ssh_host,
            ssh_user,
            ssh_password,
            ssh_port,
            app_port,
            local_port;
    }
}
