using PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Presenters.Views {
    /// <summary>
    /// PLC 列表视图契约。Presenter 通过它操作界面,不直接碰控件。
    /// </summary>
    public interface IPlcListView {
        //! ============== 属性 :界面当前状态,只读   =============
        //! 选择的 PLC 索引
        int ISelectedPlcIndex { get; }

        //! IP
        string IIpText { get; }

        //! 端口
        string IPortText { get; }

        //! 协议
        string ISelectedProtocol { get; }

        //! ============== 方法 :界面操作,由 Presenter 调用   =============
        //! 显示 PLC 列表
        void IShowPlcList (IReadOnlyList<TcpPlcConfig> plcNames);

        //! 显示 PLC 详细信息
        void IShowPlcDetail (string ip, string port, string protocol);

        //! 当前协议可选列表
        void IShowProtocolList (IReadOnlyList<string> protocols);

        //! 选的的PLC
        void ISelectPlc (int index);

        //! 确认按钮
        bool IConfirm(string message,string caption);

        //! 日志记录
        void ILog (string message);


        //! ============== 事件 :界面操作,由 Presenter 订阅   =============
        event EventHandler AddPlcEvent;                 //! 添加 PLC
        event EventHandler DeletePlcEvent;              //! 删除 PLC
        event EventHandler ConnectRequested;            //! 连接 PLC
        event EventHandler ConnectAllRequested;         //! 连接所有 PLC
        event EventHandler DisconnectRequested;         //! 断开 PLC
        event EventHandler DisconnectAllRequested;      //! 断开所有 PLC
        event EventHandler MoveUpRequested;             //! 上移 PLC
        event EventHandler MoveDownRequested;           //! 下移 PLC
        event EventHandler ApplyIpRequested;            //! 应用 IP
        event EventHandler ApplyPortRequested;          //! 应用端口
        event EventHandler PlcSelectionChanged;            //! 选择 PLC 改变
        event EventHandler ProtocolChangeRequested;     //! 协议改变
    }
}
