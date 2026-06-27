using PLC.IBase;
using Sharp7;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PLC.SiemensS7 {
    /// <summary>
    /// 西门子 S7 协议插件（基于 Sharp7），实现当前 IPlcBase 接口
    /// </summary>
    public class SiemensS7ClientImpl : IPlcBase {
        private S7Client _client;
        //! DB 块号（可由扩展参数 DbNumber 覆盖）
        private int _dbNumber = 1;

        //! 协议名称
        public const string ProtocolName = "SiemensS7";

        public SiemensS7ClientImpl () { }

        public override string GetProtocolName () => ProtocolName;

        public override bool IsConnected => _client != null && _client.Connected;

        //! 推荐扩展参数
        public override Dictionary<string, string> GetRecommendedExtraParams () {
            return new Dictionary<string, string>
            {
                { "Rack", "0" },
                { "Slot", "1" },
                { "DbNumber", "1" }
            };
        }

        public override async Task<bool> ConnectAsync (string ip, int port,
            string localIP = null, Dictionary<string, string> extraParams = null) {
            if (!string.IsNullOrEmpty(localIP)) {
                //! Sharp7 暂不支持强制绑定指定网卡
                Debug.WriteLine("[S7] Sharp7 暂不支持强制绑定指定网卡，已忽略 localIP");
            }

            short rack = 0;
            short slot = 1;
            _dbNumber = 1;

            if (extraParams != null) {
                if (extraParams.TryGetValue("Rack", out string rackStr) && short.TryParse(rackStr, out short r))
                    rack = r;
                if (extraParams.TryGetValue("Slot", out string slotStr) && short.TryParse(slotStr, out short s))
                    slot = s;
                if (extraParams.TryGetValue("DbNumber", out string dbStr) && int.TryParse(dbStr, out int db))
                    _dbNumber = db;
            }

            try {
                _client = new S7Client();
                //! Sharp7 为阻塞式 API，放到线程池执行（注意：S7 固定使用 102 端口，port 参数不生效）
                int result = await Task.Run(() => _client.ConnectTo(ip, rack, slot));
                return result == 0;
            } catch (Exception ex) {
                Debug.WriteLine($"[S7] 连接失败: {ex.Message}");
                return false;
            }
        }

        public override void Disconnect () {
            try { _client?.Disconnect(); } catch { }
        }

        public override async Task<IRegister> ReadAsync (IRegister dt) {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected) {
                Debug.WriteLine("[S7] 未连接，跳过读取");
                return dt.Clone();
            }

            int start = ConvertToInt(dt.Address);

            try {
                switch (dt.DataType) {
                    case RegisterDataType.Coil: {
                        byte[] buf = new byte[1];
                        int rc = await Task.Run(() =>
                            _client.ReadArea(S7Area.DB, _dbNumber, start, 1, S7WordLength.Bit, buf));
                        dt.CurrentValue = (rc == 0 && (buf[0] & 0x01) == 1) ? 1 : 0;
                        break;
                    }

                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32: {
                        byte[] buf = new byte[4];
                        int rc = await Task.Run(() =>
                            _client.ReadArea(S7Area.DB, _dbNumber, start, 4, S7WordLength.Byte, buf));
                        if (rc != 0) throw new Exception($"S7 读取失败 code=0x{rc:X}");
                        //! S7 为大端
                        int v = (buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3];
                        dt.CurrentValue = dt.DataType == RegisterDataType.UInt32 ? (object)(uint)v : v;
                        break;
                    }

                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16: {
                        byte[] buf = new byte[2];
                        int rc = await Task.Run(() =>
                            _client.ReadArea(S7Area.DB, _dbNumber, start, 2, S7WordLength.Byte, buf));
                        if (rc != 0) throw new Exception($"S7 读取失败 code=0x{rc:X}");
                        //! S7 为大端：高字节在前
                        short v = (short)((buf[0] << 8) | buf[1]);
                        dt.CurrentValue = dt.DataType == RegisterDataType.UInt16 ? (object)(ushort)v : v;
                        break;
                    }

                    default:
                        throw new NotSupportedException($"S7 不支持的数据类型: {dt.DataType}");
                }
            } catch (Exception ex) {
                Debug.WriteLine($"[S7] 读取异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        public override async Task<IRegister> WriteAsync (IRegister dt) {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected) {
                Debug.WriteLine("[S7] 未连接，跳过写入");
                return dt.Clone();
            }

            int start = ConvertToInt(dt.Address);
            int value = Convert.ToInt32(dt.TargetValue);

            try {
                switch (dt.DataType) {
                    case RegisterDataType.Coil: {
                        byte[] buf = new byte[1];
                        buf[0] = (byte)(value != 0 ? 1 : 0);
                        int rc = await Task.Run(() =>
                            _client.WriteArea(S7Area.DB, _dbNumber, start, 1, S7WordLength.Bit, buf));
                        if (rc != 0) throw new Exception($"S7 写入失败 code=0x{rc:X}");
                        break;
                    }

                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32: {
                        //! S7 大端
                        byte[] buf = new byte[4];
                        buf[0] = (byte)(value >> 24);
                        buf[1] = (byte)(value >> 16);
                        buf[2] = (byte)(value >> 8);
                        buf[3] = (byte)value;
                        int rc = await Task.Run(() =>
                            _client.WriteArea(S7Area.DB, _dbNumber, start, 4, S7WordLength.Byte, buf));
                        if (rc != 0) throw new Exception($"S7 写入失败 code=0x{rc:X}");
                        break;
                    }

                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16: {
                        //! S7 大端：高字节在前
                        byte[] buf = new byte[2];
                        buf[0] = (byte)(value >> 8);
                        buf[1] = (byte)value;
                        int rc = await Task.Run(() =>
                            _client.WriteArea(S7Area.DB, _dbNumber, start, 2, S7WordLength.Byte, buf));
                        if (rc != 0) throw new Exception($"S7 写入失败 code=0x{rc:X}");
                        break;
                    }

                    default:
                        throw new NotSupportedException($"S7 不支持写入的数据类型: {dt.DataType}");
                }

                //! 写入后回读最新值
                dt = await ReadAsync(dt);
            } catch (Exception ex) {
                Debug.WriteLine($"[S7] 写入异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        private static int ConvertToInt (object addr) {
            if (addr is int i) return i;
            if (addr is string s && int.TryParse(s, out int result)) return result;
            return 0;
        }
    }
}
