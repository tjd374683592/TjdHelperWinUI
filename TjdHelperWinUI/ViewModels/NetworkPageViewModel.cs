using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TjdHelperWinUI.Tools;

namespace TjdHelperWinUI.ViewModels
{
    public class NetworkPageViewModel : INotifyPropertyChanged
    {
        #region TCP 列表
        private ObservableCollection<NetworkPortModel> _tcpPortList;
        public ObservableCollection<NetworkPortModel> TcpPortList
        {
            get => _tcpPortList;
            set
            {
                _tcpPortList = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region UDP 列表
        private ObservableCollection<NetworkPortModel> _udpPortList;
        public ObservableCollection<NetworkPortModel> UdpPortList
        {
            get => _udpPortList;
            set
            {
                _udpPortList = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region TCP 状态列表
        private ObservableCollection<TcpStateModel> _tcpStateList;
        public ObservableCollection<TcpStateModel> TcpStateList
        {
            get => _tcpStateList;
            set
            {
                _tcpStateList = value;
                OnPropertyChanged();
            }
        }
        #endregion


        public NetworkPageViewModel()
        {
            TcpPortList = new ObservableCollection<NetworkPortModel>();
            UdpPortList = new ObservableCollection<NetworkPortModel>();
            TcpStateList = new ObservableCollection<TcpStateModel>();

            LoadPorts();
            LoadTcpStates(); // 加载 TCP 状态表
        }

        #region 加载端口
        private void LoadPorts()
        {
            TcpPortList.Clear();
            UdpPortList.Clear();

            foreach (var item in NetworkPortHelper.GetAllTcpPorts())
                TcpPortList.Add(item);

            foreach (var item in NetworkPortHelper.GetAllUdpPorts())
                UdpPortList.Add(item);
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region 加载 TCP 状态
        private void LoadTcpStates()
        {
            TcpStateList.Clear();

            TcpStateList.Add(new TcpStateModel { Status = "LISTENING", Meaning = "监听状态", Description = "本机在某个端口上等待别人连接（通常是服务器端口，如 80/443/3389）" });
            TcpStateList.Add(new TcpStateModel { Status = "ESTABLISHED", Meaning = "已建立连接", Description = "本机和远端的 TCP 连接已经建立，可以传输数据（典型客户端-服务器连接）" });
            TcpStateList.Add(new TcpStateModel { Status = "SYN_SENT", Meaning = "已发送 SYN", Description = "本机向远端请求建立连接，发送了 SYN 包，等待回应（客户端发起连接）" });
            TcpStateList.Add(new TcpStateModel { Status = "SYN_RECEIVED", Meaning = "已收到 SYN", Description = "收到远端的 SYN 请求并回应了 ACK，本机也准备建立连接（服务器端半连接）" });
            TcpStateList.Add(new TcpStateModel { Status = "FIN_WAIT_1", Meaning = "关闭请求发出", Description = "本机主动关闭连接，已经发送 FIN 包，等待对方 ACK" });
            TcpStateList.Add(new TcpStateModel { Status = "FIN_WAIT_2", Meaning = "等待关闭", Description = "对方 ACK 了本机的 FIN，本机等待远端 FIN 包" });
            TcpStateList.Add(new TcpStateModel { Status = "CLOSE_WAIT", Meaning = "等待关闭", Description = "对方已经关闭连接，本机还没关闭，等待应用程序关闭套接字" });
            TcpStateList.Add(new TcpStateModel { Status = "CLOSING", Meaning = "双方同时关闭", Description = "双方同时发送 FIN，正在等待确认" });
            TcpStateList.Add(new TcpStateModel { Status = "LAST_ACK", Meaning = "等待最后 ACK", Description = "本机发送 FIN 后收到对方 FIN，等待最后确认" });
            TcpStateList.Add(new TcpStateModel { Status = "TIME_WAIT", Meaning = "等待超时关闭", Description = "本机主动关闭连接后，等待足够时间确保远端收到 ACK（2MSL），防止旧数据包干扰新连接" });
            TcpStateList.Add(new TcpStateModel { Status = "CLOSED", Meaning = "已关闭", Description = "连接完全关闭" });
        }
        #endregion
    }

    public class TcpStateModel
    {
        public string Status { get; set; }      // 状态
        public string Meaning { get; set; }     // 含义
        public string Description { get; set; } // 说明
    }

}
