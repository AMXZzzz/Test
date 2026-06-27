using PLC.IBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


    

namespace PLC {
    /// <summary>
    /// ModbusTcp 协议内置插件, 继承IPlcBase
    /// </summary>
    public class ModbusTcp : IPlcBase {

        //! **************************** 私有属性 ****************************
        //! ModbusTcp 协议基础对象
        private ModbusTcpLib _modbus = new ModbusTcpLib();

        //! ModbusTcp 独有调试日志
        private string _debugLogHeader = "[ModbusTCP调试]";


        //! **************************** 公开属性 ****************************
        //! 构造函数
        public ModbusTcp () { }

        public const string ProtocolName = "Modbus TCP";

        //! 获取当前协议字符串
        public override string GetProtocolName () => ProtocolName;

        //! 获取链接状态
        public override bool IsConnected => _modbus.IsConnected;

        /// <summary>
        /// ModbusTcp 异步链接
        /// </summary>
        /// <param name="ip">链接ip</param>
        /// <param name="port">链接端口</param>
        /// <param name="localIP">链接适配器</param>
        /// <param name="extraParams">扩展参数</param>
        /// <returns></returns>
        public override async Task<bool> ConnectAsync (
             string ip, int port, string localIP = null,
             Dictionary<string, string> extraParams = null)
             => await _modbus.ConnectAsync(ip, port);   // 保持异步,不要 .Result

        /// <summary>
        /// 断开链接
        /// </summary>
        public override void Disconnect () => _modbus.Disconnect();

        /// <summary>
        /// 异步读取寄存器,值传递
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>返回修改后的新寄存器</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override async Task<IRegister> ReadAsync (PLC.IBase.IRegister dt) {

            //! debug
            LogHelper.DebugLog($"ReadAsync 被调用，地址={dt.Address.ToString()}, 类型={dt.DataType.ToString()}");

            //! 链接判断
            if (!_modbus.IsConnected) {
                PLC.LogHelper.Log("Modbus 未连接或连接已失效");
                dt.CurrentValue = 0;
            }
            //! 对象判空
            if (dt == null)  throw new ArgumentNullException(nameof(dt));

            // ==================== 尝试解析 ====================
            try {

                switch (dt.DataType) {
                    //! 线圈类型
                    case RegisterDataType.Coil:
                        bool[] coils = await _modbus.ReadCoilsAsync(dt.Address.ToString(), 1);
                        dt.CurrentValue = (coils.Length > 0 && coils[0]) ? 1 : 0;
                        break;

                    //! 16位Int
                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16:
                        ushort[] uint16Regs = await _modbus.ReadHoldingRegistersAsync(dt.Address.ToString(), 1);
                        dt.CurrentValue = uint16Regs[0];
                        break;
                    
                    //! 32位Int
                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32:
                        ushort[] uint32Regs = await _modbus.ReadHoldingRegistersAsync(dt.Address.ToString(), 2);
                        dt.CurrentValue = (uint)((uint32Regs[0] << 16) | uint32Regs[1]);
                        break;

                    //! 32位float
                    case RegisterDataType.Float:
                        dt.CurrentValue = await _modbus.ReadFloatAsync(dt.Address.ToString(), highWordFirst: true);
                        break;

                    //! 64位float
                    case RegisterDataType.Double:
                        dt.CurrentValue = await _modbus.ReadDoubleAsync(dt.Address.ToString(), highWordFirst: true);
                        break;

                    //! 32字符串
                    case RegisterDataType.String32:
                        ushort[] strRegs = await _modbus.ReadHoldingRegistersAsync(dt.Address.ToString(), 16);
                        dt.CurrentValue = _modbus.RegistersToString(strRegs);
                        break;

                    //! 不在范围内的数据类型
                    default:
                        throw new NotSupportedException($"不支持的数据类型: {dt.DataType}");
                }
            
            //! 异常处理
            } catch (ObjectDisposedException) {
                PLC.LogHelper.DebugLog($"{_debugLogHeader} Modbus 连接已断开（Stream已释放）");
            } catch (Exception ex) {
                PLC.LogHelper.DebugLog($"{_debugLogHeader} ReadAsync 异常类型: {ex.GetType().Name}  异常信息:  {ex.Message}");
                if (ex.InnerException != null) PLC.LogHelper.DebugLog($"{_debugLogHeader} InnerException: {ex.InnerException.Message}");
            }

            //! 返回地址
            return dt.Clone();
        }



        /// <summary>
        /// 写入寄存器,值传递
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override async Task<IRegister> WriteAsync (PLC.IBase.IRegister dt) {
            //! 地址判空
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            LogHelper.DebugLog($"{_debugLogHeader} WriteAsync 被调用，地址={dt.Address}, 类型={dt.DataType}");

            // 检查连接状态
            if (!_modbus.IsConnected) {
                LogHelper.Log($"{_debugLogHeader} Modbus 未连接，跳过写入");
                return dt;
            }

            // ==================== 尝试写入 ====================
            try {

                switch (dt.DataType) {
                    case RegisterDataType.Coil:
                        bool coilValue = Convert.ToBoolean(dt.TargetValue);
                        await _modbus.WriteSingleCoilAsync(dt.Address.ToString(), coilValue);
                        break;

                    case RegisterDataType.Int16:
                        short int16Value = Convert.ToInt16(dt.TargetValue);
                        await _modbus.WriteSingleRegisterAsync(dt.Address.ToString(), int16Value);
                        break;

                    case RegisterDataType.UInt16:
                        ushort uint16Value = Convert.ToUInt16(dt.TargetValue);
                        await _modbus.WriteSingleRegisterAsync(dt.Address.ToString(), uint16Value);
                        break;

                    case RegisterDataType.Int32:
                        int int32Value = Convert.ToInt32(dt.TargetValue);
                        ushort high = (ushort)(int32Value >> 16);
                        ushort low = (ushort)(int32Value & 0xFFFF);
                        await _modbus.WriteMultipleRegistersAsync(dt.Address.ToString(), new ushort[] { high, low });
                        break;
                    case RegisterDataType.UInt32:
                        uint uint32Value = Convert.ToUInt32(dt.TargetValue);
                        ushort uhigh = (ushort)(uint32Value >> 16);
                        ushort ulow = (ushort)(uint32Value & 0xFFFF);
                        await _modbus.WriteMultipleRegistersAsync(dt.Address.ToString(), new ushort[] { uhigh, ulow });
                        break;

                    case RegisterDataType.Float:
                        float floatValue = Convert.ToSingle(dt.TargetValue);
                        await _modbus.WriteFloatAsync(dt.Address.ToString(), floatValue, highWordFirst: true);
                        break;

                    case RegisterDataType.Double:
                        double doubleValue = Convert.ToDouble(dt.TargetValue);
                        await _modbus.WriteDoubleAsync(dt.Address.ToString(), doubleValue, highWordFirst: true);
                        break;

                    case RegisterDataType.String32:
                        // String32 一般不建议直接写入，如需写入可扩展
                        throw new NotSupportedException($"{_debugLogHeader} String32 类型暂不支持写入");

                    default:
                        throw new NotSupportedException($"{_debugLogHeader} 不支持的数据类型: {dt.DataType}");
                }

                LogHelper.DebugLog($"{_debugLogHeader} 写入成功 地址={dt.Address}");

                // ==================== 写入后读取最新值 ====================
                dt = await ReadAsync(dt);
               
            } catch (Exception ex) {
                LogHelper.DebugLog($"{_debugLogHeader} 写入异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        /// <summary>
        /// 获取扩展参数
        /// </summary>
        /// <returns>无</returns>
        public override Dictionary<string, string> GetRecommendedExtraParams () {
            return new Dictionary<string, string>();
        }
    }
}

