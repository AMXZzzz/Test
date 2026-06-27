using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLC.Mewtocol
{
    /// <summary>
    /// Mewtocol-COM(ASCII) 底层客户端
    /// </summary>
    public class MewtocolClient : IDisposable
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private readonly string _ip;
        private readonly int _port;
        private readonly int _stationNo;

        //! 串行化收发，避免轮询定时器与手动读写并发时帧交错
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);

        public MewtocolClient(string ip, int port, int stationNo = 1)
        {
            _ip = ip;
            _port = port;
            _stationNo = stationNo;
        }

        //! 连接状态
        public bool IsConnected => _tcp?.Connected == true;

        public async Task ConnectAsync(string localIP = null)
        {
            _tcp = new TcpClient();

            if (!string.IsNullOrEmpty(localIP))
            {
                try
                {
                    var localEndPoint = new System.Net.IPEndPoint(
                        System.Net.IPAddress.Parse(localIP), 0);
                    _tcp.Client.Bind(localEndPoint);
                }
                catch
                {
                    //! 绑定失败则使用默认网卡
                }
            }

            await _tcp.ConnectAsync(_ip, _port);
            _stream = _tcp.GetStream();
            _stream.ReadTimeout = 3000;
            _stream.WriteTimeout = 3000;
        }

        public void Disconnect()
        {
            try { _stream?.Close(); _tcp?.Close(); } catch { }
            _stream = null;
            _tcp = null;
        }

        public void Dispose() => Disconnect();

        // ==================== 读取 ====================

        /// <summary>
        /// 读取触点（支持 "101"、"A5"、"R101"、"R10A"、"X10" 等；位地址按十六进制）
        /// 默认当做 R 系列（不需要输入 R）
        /// </summary>
        public async Task<bool> ReadContactAsync(string coilAddress)
        {
            var (contactType, numericAddress) = ParseCoilAddress(coilAddress);
            string addrStr = FormatContactAddress(numericAddress);
            string text = $"RCS{contactType}{addrStr}";
            string resp = await SendCommandAsync(text);

            int idx = resp.IndexOf("$RC");
            if (idx < 0 || idx + 3 >= resp.Length) throw new Exception($"读触点失败: {resp}");
            return resp[idx + 3] == '1';
        }

        /// <summary>
        /// 写入触点（支持字符串地址）
        /// </summary>
        public async Task WriteContactAsync(string coilAddress, bool value)
        {
            var (contactType, numericAddress) = ParseCoilAddress(coilAddress);
            string addrStr = FormatContactAddress(numericAddress);
            string text = $"WCS{contactType}{addrStr}{(value ? "1" : "0")}";
            string resp = await SendCommandAsync(text);
            if (!resp.Contains("$WC")) throw new Exception($"写触点失败: {resp}");
        }

        // ==================== 数据寄存器（十进制地址） ====================

        public async Task<short> ReadDTAsync(int address)
        {
            string addrStr = address.ToString("D5");
            string text = $"RDD{addrStr}{addrStr}";
            string resp = await SendCommandAsync(text);
            int idx = resp.IndexOf("$RD");
            if (idx < 0) throw new Exception($"读DT失败: {resp}");
            string hex = resp.Substring(idx + 3, 4);
            byte lo = Convert.ToByte(hex.Substring(0, 2), 16);
            byte hi = Convert.ToByte(hex.Substring(2, 2), 16);
            return (short)(lo | (hi << 8));
        }

        public async Task<int> ReadDDTAsync(int address)
        {
            string addr1 = address.ToString("D5");
            string addr2 = (address + 1).ToString("D5");
            string text = $"RDD{addr1}{addr2}";
            string resp = await SendCommandAsync(text);
            int idx = resp.IndexOf("$RD");
            if (idx < 0) throw new Exception($"读DDT失败: {resp}");
            string hex1 = resp.Substring(idx + 3, 4);
            string hex2 = resp.Substring(idx + 7, 4);
            short lo = ParseSwappedShort(hex1);   //! 低字在前
            short hi = ParseSwappedShort(hex2);   //! 高字在后
            return (int)((uint)(ushort)lo | ((uint)(ushort)hi << 16));
        }

        public async Task WriteDTAsync(int address, short value)
        {
            string addrStr = address.ToString("D5");
            byte lo = (byte)(value & 0xFF);
            byte hi = (byte)((value >> 8) & 0xFF);
            string data = $"{lo:X2}{hi:X2}";
            string text = $"WDD{addrStr}{addrStr}{data}";
            string resp = await SendCommandAsync(text);
            if (!resp.Contains("$WD")) throw new Exception($"写DT失败: {resp}");
        }

        public async Task WriteDDTAsync(int address, int value)
        {
            string addr1 = address.ToString("D5");
            string addr2 = (address + 1).ToString("D5");
            short lo = (short)(value & 0xFFFF);
            short hi = (short)((value >> 16) & 0xFFFF);
            string data1 = $"{(byte)(lo & 0xFF):X2}{(byte)((lo >> 8) & 0xFF):X2}";
            string data2 = $"{(byte)(hi & 0xFF):X2}{(byte)((hi >> 8) & 0xFF):X2}";
            string text = $"WDD{addr1}{addr2}{data1}{data2}";
            string resp = await SendCommandAsync(text);
            if (!resp.Contains("$WD")) throw new Exception($"写DDT失败: {resp}");
        }

        // ==================== 解析 + 辅助方法 ====================

        /// <summary>
        /// 解析触点地址：可带类型前缀(R/X/Y/L/T/C)，位地址按十六进制
        /// 例如 R10A → 类型R + 0x10A，FormatContactAddress 再补成 4 位 "010A"
        /// </summary>
        private (char contactType, int numericAddress) ParseCoilAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ('R', 0);

            input = input.Trim().ToUpper();

            char type = 'R';
            string addrPart = input;

            if (input.Length > 1 && "RXYLTC".Contains(input[0]))
            {
                type = input[0];
                addrPart = input.Substring(1);
            }

            //! 按十六进制解析（R10A / R20F 等位地址）
            if (!int.TryParse(addrPart, System.Globalization.NumberStyles.HexNumber, null, out int numeric))
            {
                numeric = 0;
            }

            return (type, numeric);
        }

        /// <summary>
        /// 转换为 MEWTOCOL 要求的 4 字符格式（输入 0x10A → "010A"）
        /// </summary>
        private static string FormatContactAddress(int address)
        {
            if (address < 0) address = 0;
            return address.ToString("X4");
        }

        private async Task<string> SendCommandAsync(string textCode)
        {
            await _ioLock.WaitAsync();
            try
            {
                if (_stream == null || _tcp?.Connected != true)
                    throw new InvalidOperationException("Mewtocol 未连接");

                string stationStr = _stationNo.ToString("D2");
                string body = $"%{stationStr}#{textCode}";
                string bcc = CalcBcc(body);
                string cmd = body + bcc + "\r";

                byte[] sendBytes = Encoding.ASCII.GetBytes(cmd);
                await _stream.WriteAsync(sendBytes, 0, sendBytes.Length);

                var sb = new StringBuilder();
                byte[] buf = new byte[256];
                while (true)
                {
                    int n = await _stream.ReadAsync(buf, 0, buf.Length);
                    if (n == 0) break;
                    sb.Append(Encoding.ASCII.GetString(buf, 0, n));
                    if (sb.ToString().Contains("\r")) break;
                }

                string resp = sb.ToString().TrimEnd('\r', '\n');
                if (resp.Contains("!")) throw new Exception($"Mewtocol错误: {resp}");
                return resp;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        private static string CalcBcc(string data)
        {
            byte xor = 0;
            foreach (char c in data) xor ^= (byte)c;
            return xor.ToString("X2");
        }

        private static short ParseSwappedShort(string hex4)
        {
            byte lo = Convert.ToByte(hex4.Substring(0, 2), 16);
            byte hi = Convert.ToByte(hex4.Substring(2, 2), 16);
            return (short)(lo | (hi << 8));
        }
    }
}
