using PLC.IBase;

namespace PLC.Models {
    /// <summary>
    /// 连接配置。描述一台 PLC 的网络连接信息与当前连接实例。
    /// </summary>
    public class TcpLinkConfig {
        /// <summary>PLC 名称</summary>
        public string PlcName { get; set; } = "PLC";

        /// <summary>连接 IP</summary>
        public string Ip { get; set; }

        /// <summary>连接端口</summary>
        public int Port { get; set; }

        /// <summary>通讯协议名,默认 Modbus TCP</summary>
        public string Protocol { get; set; } = "";

        /// <summary>当前连接的协议实例;null 表示未连接</summary>
        public IPlcBase Client { get; set; }
    }
}