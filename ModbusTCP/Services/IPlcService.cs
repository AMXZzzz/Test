using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTCP.Services {
    //! PLC服务接口
    public interface IPlcService {
        //! 连接PLC
        bool Connect (string ip, int port);
        //! 断开PLC连接
        void Disconnect ();
        //! 读取寄存器
        int[] ReadRegisters (int startAddress, int count);
        //! 写入寄存器
        void WriteRegister (int address, int value);
        //! 是否已连接
        bool IsConnected { get; }
    }
}
