using System;
using System.Collections.Generic;
using PLC.IBase;

namespace PLC.Models {
    /// <summary>
    /// 一台 PLC 的完整配置:连接 + 轨道 + 自定义寄存器 + 运行时错误信息。
    /// </summary>
    public class TcpPlcConfig {
        /// <summary>连接配置层(后续可扩展串口层)</summary>
        public TcpLinkConfig Link { get; set; } = new TcpLinkConfig();

        /// <summary>轨道配置</summary>
        public TrackConfig Track { get; set; } = new TrackConfig();

        /// <summary>用户自定义的读写寄存器表</summary>
        public List<IRegister> RegisterSheet { get; set; } = new List<IRegister>();

        /// <summary>协议扩展参数</summary>
        public Dictionary<string, string> ExtraParams { get; set; } = new Dictionary<string, string>();

        /// <summary>最后一次错误信息</summary>
        public string LastError { get; set; } = "";

        /// <summary>最后一次错误时间</summary>
        public DateTime LastErrorTime { get; set; } = DateTime.MinValue;
    }
}