using S7.Net;
using System;
using System.Collections.Generic;

namespace PLC {

    // ==================== 读写点 ====================


    // ==================== 配置 ====================
    /// <summary>
    /// 轨道配置结构
    /// </summary>
    public class TrackConfig {

        //! 轨道数量
        public int TrackCount { get; set; } = 1;

        //! 一轨就绪地址
        public PLC.IBase.IRegister OneReady = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 一轨宽度值
        public PLC.IBase.IRegister OneWidthAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 一轨触发地址
        public PLC.IBase.IRegister OneTriggerAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 一轨道有完成状态
        public bool OneHasStatus = false;
        //! 一轨完成地址
        public PLC.IBase.IRegister OneStatusAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 一轨完成标定状态,即读取的值与该状态一致,为完成状态
        public int OneStatusDoneValue = 1;

        //! 二轨就绪地址
        public PLC.IBase.IRegister TwoReady = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 二轨宽度值
        public PLC.IBase.IRegister TwoWidthAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 二轨触发地址
        public PLC.IBase.IRegister TwoTriggerAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 二轨道有完成状态
        public bool TwoHasStatus = false;
        //! 二轨完成地址
        public PLC.IBase.IRegister TwoStatusAddr = new PLC.IBase.IRegister { DataType = PLC.IBase.RegisterDataType.Int32 };
        //! 二轨完成标定状态,即读取的值与该状态一致,为完成状态
        public int TwoStatusDoneValue = 1;

    }

    /// <summary>
    /// 链接配置结构
    /// </summary>
    public class TcpLinkConfig {
        //! PLC的名称
        public string PlcName { get; set; } = "PLC";
        //! PLC的链接IP
        public string Ip { get; set; }
        //! PLC的链接端口
        public int Port { get; set; }
        //! PLC的通讯协议, 默认ModbusTcp协议
        public string Protocol { get; set; } = ModbusTcp.ProtocolName;
        //! 协议对象
        public object Client { get; set; }

    }

    /// <summary>
    //  PLC 使用的资源
    /// </summary>
    public class TcpPlcConfig {
        //! 链接层, 后续增加串口层
        public TcpLinkConfig Link = new TcpLinkConfig();    //! 1个以太网接口

        //! 轨道配置
        public TrackConfig Track = new TrackConfig();   //! 轨道配置

        //! 可选, 用户添加的寄存器表格
        public List<PLC.IBase.IRegister> RegisterSheet { get; set; } = new List<PLC.IBase.IRegister>();

        //! 可选, 需要使用的扩展参数
        public Dictionary<string, string> ExtraParams { get; set; } = new Dictionary<string, string>();

        //! 该PLC最后的错误信息
        public string LastError { get; set; } = "";

        //! 该PLC最后触发错误的时间
        public DateTime LastErrorTime { get; set; } = DateTime.MinValue;
    }
}
