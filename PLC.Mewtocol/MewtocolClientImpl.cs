using PLC.IBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PLC.Mewtocol {
    /// <summary>
    /// Mewtocol 协议插件，对外实现当前的 IPlcBase 接口
    /// </summary>
    public class MewtocolClientImpl : IPlcBase {
        //! 内部客户端
        private MewtocolClient _client;
        //! 连接标志位
        private bool _isConnected = false;
        //! 链接信息
        private string _ip = "";
        private int _port = 0;
        //! 站号
        private int _stationNo = 1;

        //! 协议名称
        public const string ProtocolName = "Mewtocol";

        //! 无参构造（插件加载器要求）
        public MewtocolClientImpl () { }

        //! 连接状态
        public override bool IsConnected => _isConnected && (_client?.IsConnected ?? false);

        //! 协议名
        public override string GetProtocolName () => ProtocolName;

        //! 推荐扩展参数：站号
        public override Dictionary<string, string> GetRecommendedExtraParams () {
            return new Dictionary<string, string> {
                { "StationNo", "1" }
            };
        }

        /// <summary>
        /// 异步连接，签名与 IPlcBase 一致：(ip, port, localIP, extraParams)
        /// </summary>
        public override async Task<bool> ConnectAsync (string ip,
                                                       int port,
                                                       string localIP = null,
                                                       Dictionary<string, string> extraParams = null) {
            _ip = ip;
            _port = port;
            _stationNo = 1;

            //! 可选站号
            if (extraParams != null &&
                extraParams.TryGetValue("StationNo", out string snStr) &&
                int.TryParse(snStr, out int sn)) {
                _stationNo = sn;
            }

            try {
                _client = new MewtocolClient(ip, port, _stationNo);
                await _client.ConnectAsync(localIP);   //! 传递本地网卡
                _isConnected = true;
                return true;
            } catch (Exception ex) {
                _isConnected = false;
                Debug.WriteLine($"[Mewtocol] 连接失败: {ex.Message}");
                return false;
            }
        }

        //! 断开
        public override void Disconnect () {
            try { _client?.Disconnect(); } catch { }
            _isConnected = false;
        }

        /// <summary>
        /// 异步读取
        /// </summary>
        public override async Task<IRegister> ReadAsync (IRegister dt) {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected) {
                Debug.WriteLine("[Mewtocol] 未连接，跳过读取");
                return dt.Clone();
            }

            try {
                switch (dt.DataType) {
                    //! 线圈/触点：地址是字符串，支持 R10A、R20F 等十六进制位地址
                    case RegisterDataType.Coil:
                        bool on = await _client.ReadContactAsync(dt.Address);
                        dt.CurrentValue = on ? 1 : 0;
                        break;

                    //! 32 位（DDT 双字）
                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32:
                        int dval = await _client.ReadDDTAsync(ParseNumericAddress(dt.Address));
                        dt.CurrentValue = dt.DataType == RegisterDataType.UInt32 ? (object)(uint)dval : dval;
                        break;

                    //! 16 位（DT 单字）
                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16:
                        short wval = await _client.ReadDTAsync(ParseNumericAddress(dt.Address));
                        dt.CurrentValue = dt.DataType == RegisterDataType.UInt16 ? (object)(ushort)wval : wval;
                        break;

                    default:
                        throw new NotSupportedException($"Mewtocol 不支持的数据类型: {dt.DataType}");
                }
            } catch (Exception ex) {
                _isConnected = false;
                Debug.WriteLine($"[Mewtocol] 读取异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        /// <summary>
        /// 异步写入
        /// </summary>
        public override async Task<IRegister> WriteAsync (IRegister dt) {
            if (dt == null) throw new ArgumentNullException(nameof(dt));

            if (!IsConnected) {
                Debug.WriteLine("[Mewtocol] 未连接，跳过写入");
                return dt.Clone();
            }

            try {
                switch (dt.DataType) {
                    case RegisterDataType.Coil:
                        bool on = Convert.ToInt32(dt.TargetValue) != 0;
                        await _client.WriteContactAsync(dt.Address, on);
                        break;

                    case RegisterDataType.Int32:
                    case RegisterDataType.UInt32:
                        await _client.WriteDDTAsync(ParseNumericAddress(dt.Address), Convert.ToInt32(dt.TargetValue));
                        break;

                    case RegisterDataType.Int16:
                    case RegisterDataType.UInt16:
                        await _client.WriteDTAsync(ParseNumericAddress(dt.Address), (short)Convert.ToInt32(dt.TargetValue));
                        break;

                    default:
                        throw new NotSupportedException($"Mewtocol 不支持写入的数据类型: {dt.DataType}");
                }

                //! 写入后回读最新值
                dt = await ReadAsync(dt);
            } catch (Exception ex) {
                _isConnected = false;
                Debug.WriteLine($"[Mewtocol] 写入异常: {ex.Message}");
                throw;
            }

            return dt.Clone();
        }

        /// <summary>
        /// DT/DDT 字数据寄存器地址按十进制解析（线圈走字符串，不经过这里）
        /// </summary>
        private static int ParseNumericAddress (string addr) {
            if (string.IsNullOrWhiteSpace(addr)) return 0;
            return int.TryParse(addr.Trim(), out int n) ? n : 0;
        }
    }
}
