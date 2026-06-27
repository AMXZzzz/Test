using PLC.IBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PLC
{
    /// <summary>
    /// Modbus RTU (over TCP) 协议插件，实现当前 IPlcBase 接口
    /// </summary>
    public class ModbusRtuProtocol : IPlcBase
    {
        private ModbusRtuOverTcpClient _client;
        private bool _isConnected = false;

        //! 协议名称
        public const string ProtocolName = "Modbus RTU (over TCP)";

        public ModbusRtuProtocol() { }

        public override string GetProtocolName() => ProtocolName;

        public override bool IsConnected => _isConnected && _client?.IsConnected == true;

        public override Dictionary<string, string> GetRecommendedExtraParams()
            => new Dictionary<string, string>();

        public override async Task<bool> ConnectAsync(string ip, int port,
            string localIP = null, Dictionary<string, string> extraParams = null)
        {
            try
            {
                _client = new ModbusRtuOverTcpClient();
                await _client.ConnectAsync(ip, port, localIP);   //! 传递 localIP
                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Debug.WriteLine($"[Modbus RTU] 连接失败: {ex.Message}");
                return false;
            }
        }

        public override void Disconnect()
        {
            try { _client?.Disconnect(); } catch { }
            _isConnected = false;
        }

        public override async Task<IRegister> ReadAsync(IRegister dt)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected)
            {
                Debug.WriteLine("[Modbus RTU] 未连接，跳过读取");
                return dt.Clone();
            }

            int addr = ConvertToInt(dt.Address);

            try
            {
                switch (dt.DataType)
                {
                    case RegisterDataType.Coil:
                        bool[] coils = await _client.ReadCoilsAsync(addr, 1);
                        dt.CurrentValue = (coils.Length > 0 && coils[0]) ? 1 : 0;
                        break;

                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16:
                        ushort[] r1 = await _client.ReadHoldingRegistersAsync(addr, 1);
                        dt.CurrentValue = dt.DataType == RegisterDataType.Int16
                            ? (object)(short)r1[0]
                            : r1[0];
                        break;

                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32:
                        //! 读 2 个保持寄存器，高字在前
                        ushort[] r2 = await _client.ReadHoldingRegistersAsync(addr, 2);
                        uint raw = (uint)((r2[0] << 16) | r2[1]);
                        dt.CurrentValue = dt.DataType == RegisterDataType.UInt32 ? (object)raw : (int)raw;
                        break;

                    default:
                        throw new NotSupportedException($"Modbus RTU 不支持的数据类型: {dt.DataType}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Debug.WriteLine($"[Modbus RTU] 读取异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        public override async Task<IRegister> WriteAsync(IRegister dt)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected)
            {
                Debug.WriteLine("[Modbus RTU] 未连接，跳过写入");
                return dt.Clone();
            }

            int addr = ConvertToInt(dt.Address);
            int value = Convert.ToInt32(dt.TargetValue);

            try
            {
                switch (dt.DataType)
                {
                    case RegisterDataType.Coil:
                        await _client.WriteSingleCoilAsync(addr, value != 0);
                        break;

                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16:
                        await _client.WriteSingleRegisterAsync(addr, (ushort)value);
                        break;

                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32:
                        //! 写 2 个寄存器，高字在前（与读取一致）
                        ushort hi = (ushort)(value >> 16);
                        ushort lo = (ushort)(value & 0xFFFF);
                        await _client.WriteMultipleRegistersAsync(addr, new ushort[] { hi, lo });
                        break;

                    default:
                        throw new NotSupportedException($"Modbus RTU 不支持写入的数据类型: {dt.DataType}");
                }

                //! 写入后回读最新值
                dt = await ReadAsync(dt);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Debug.WriteLine($"[Modbus RTU] 写入异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        private static int ConvertToInt(object addr)
        {
            if (addr is int i) return i;
            if (addr is string s && int.TryParse(s, out int result)) return result;
            return 0;
        }
    }
}
