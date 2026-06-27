using System;
using System.Net.Sockets;
using System.Threading; 
using System.Threading.Tasks;

namespace PLC {
    /// <summary>
    /// 自封装的 ModbusTcp 库
    /// </summary>
    public class ModbusTcpLib : IDisposable {
        //! Tcp对象
        private TcpClient _tcp = new TcpClient();
        //! Sockets对象
        private NetworkStream _stream;
        //! 链接状态
        private bool _isConnected = false;

        //! ================== 对外API ==================
        public bool IsConnected => _isConnected && _tcp?.Connected == true;
        /// <summary>
        /// 链接
        /// </summary>
        /// <param name="ip">链接的IP地址</param>
        /// <param name="port">链接的端口</param>
        /// <param name="timeoutMs">链接超时时间（毫秒）</param>
        /// <param name="localIP">本地网卡IP地址（可选）</param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task<bool> ConnectAsync (string ip, int port, int timeoutMs = 8000, string localIP = null) {
            //! 如果已经链接，则先断开
            Disconnect();

            //! ip不为空
            if (!string.IsNullOrEmpty(localIP)) {
                //! 绑定本地网卡
                try {
                    var localEndPoint = new System.Net.IPEndPoint(
                        System.Net.IPAddress.Parse(localIP), 0);
                    //! 绑定本地网卡
                    _tcp.Client.Bind(localEndPoint);
                    LogHelper.Log($"[网络] 已绑定本地网卡: {localIP}");
                } catch (Exception ex) {
                    LogHelper.Log($"[警告] 绑定本地网卡失败: {ex.Message}，将使用默认网卡");
                }
            }

            //! Tcp链接
            var connectTask = _tcp.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(timeoutMs);

            //! 等待链接或超时
            if (await Task.WhenAny(connectTask, timeoutTask) != connectTask) {
                Disconnect();
                throw new TimeoutException($"连接 {ip}:{port} 超时");
            }

            //! 链接成功
            _stream = _tcp.GetStream();
            _stream.ReadTimeout = timeoutMs;
            _stream.WriteTimeout = timeoutMs;
            _isConnected = true;
            return true;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect () {
            _isConnected = false;
            try { _stream?.Dispose(); _tcp?.Dispose(); } catch { }
            _stream = null;
            _tcp = null;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose () => Disconnect();

        // ==================== 读方法 ====================
        /// <summary>
        /// 读取寄存器
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="count">要读取的寄存器数量</param>
        /// <returns>返回读取到的寄存器值数组</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<ushort[]> ReadHoldingRegistersAsync (string address, int count) {

            if (!IsConnected) throw new InvalidOperationException("未连接");

            byte[] req = BuildRequest(0x03, address, count);

            byte[] resp = await SendAndReceiveAsync(req);

            return ParseRegisters(resp, count);
        }

        /// <summary>
        /// 写入寄存器
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="value">要写入的寄存器值</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteSingleRegisterAsync (string address, short value) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            byte[] req = BuildRequest(0x06, address, value);
            await SendAndReceiveAsync(req);
        }
        //! ushort 重载
        public async Task WriteSingleRegisterAsync(string address, ushort value)
        {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            byte[] req = BuildRequest(0x06, address, value);
            await SendAndReceiveAsync(req);
        }

        /// <summary>
        /// 读取线圈
        /// </summary>
        /// <param name="address">线圈起始地址</param>
        /// <param name="count">要读取的线圈数量</param>
        /// <returns>返回读取到的线圈状态数组</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool[]> ReadCoilsAsync (string address, int count) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            byte[] req = BuildRequest(0x01, address, count);
            byte[] resp = await SendAndReceiveAsync(req);
            return ParseCoils(resp, count);
        }

        /// <summary>
        /// 写入线圈
        /// </summary>
        /// <param name="address">线圈起始地址</param>
        /// <param name="value">要写入的线圈状态</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteSingleCoilAsync (string address, bool value) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            ushort coilValue = value ? (ushort)0xFF00 : (ushort)0x0000;
            byte[] req = BuildRequest(0x05, address, coilValue);
            await SendAndReceiveAsync(req);
        }



        /// <summary>
        /// 读取浮点数
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回读取到的浮点数值</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<float> ReadFloatAsync (string address, bool highWordFirst = true) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            //! 读取两个寄存器
            ushort[] regs = await ReadHoldingRegistersAsync(address, 2);
            //! 根据高低字节顺序转换为浮点数
            return RegistersToFloat(regs[0], regs[1], highWordFirst);
        }

        /// <summary>
        /// 读取浮点数数组, double类型
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="count">浮点数数量</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回读取到的浮点数数组</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<float[]> ReadFloatsAsync (string address, int count, bool highWordFirst = true) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            //! 读取2 * count个寄存器
            ushort[] regs = await ReadHoldingRegistersAsync(address, count * 2);
            float[] result = new float[count];
            //! 根据高低字节顺序转换为浮点数
            for (int i = 0; i < count; i++) {
                result[i] = RegistersToFloat(regs[i * 2], regs[i * 2 + 1], highWordFirst);
            }
            return result;
        }

        /// <summary>
        /// 将两个寄存器值转换为浮点数
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的浮点数值</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteFloatAsync (string address, float value, bool highWordFirst = true) {
            if (!IsConnected) throw new InvalidOperationException("未连接");

            ushort[] regs = FloatToRegisters(value, highWordFirst);
            await WriteMultipleRegistersAsync(address, regs);
        }

        /// <summary>
        /// 将浮点数数组写入寄存器
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="values">要写入的浮点数数组</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<double> ReadDoubleAsync (string address, bool highWordFirst = true) {
            if (!IsConnected) throw new InvalidOperationException("未连接");

            ushort[] regs = await ReadHoldingRegistersAsync(address, 4);
            return RegistersToDouble(regs, highWordFirst);
        }

        /// <summary>
        /// 将浮点数数组写入寄存器
        /// </summary>
        /// <param name="address">寄存器起始地址</param>
        /// <param name="value">要写入的浮点数值</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteDoubleAsync (string address, double value, bool highWordFirst = true) {
            if (!IsConnected) throw new InvalidOperationException("未连接");
            ushort[] regs = DoubleToRegisters(value, highWordFirst);
            await WriteMultipleRegistersAsync(address, regs);
        }

        /// <summary>
        /// 将寄存器转为字符串
        /// </summary>
        /// <param name="registers">要转换的寄存器数组</param>
        /// <returns>返回转换后的字符串</returns>
        public string RegistersToString (ushort[] registers) {
            byte[] bytes = new byte[registers.Length * 2];
            for (int i = 0; i < registers.Length; i++) {
                bytes[i * 2] = (byte)(registers[i] >> 8);
                bytes[i * 2 + 1] = (byte)(registers[i] & 0xFF);
            }
            return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }


        // ==================== 内部辅助函数 ====================
        //! 字符串转int类型
        private int ConvertStringToInt (string str) {
            if (int.TryParse(str, out int result)) {
                return result;
            } else {
                throw new FormatException($"无法将字符串 '{str}' 转换为整数。");
            }
        }

        /// <summary>
        /// 构建ModbusTcp请求报文
        /// </summary>
        /// <param name="func">功能码</param>
        /// <param name="addr">寄存器地址</param>
        /// <param name="val">寄存器值</param>
        /// <returns>返回构建好的请求报文</returns>
        private byte[] BuildRequest (byte func, string addr, int val) {
            int address = ConvertStringToInt(addr);
            byte[] frame = new byte[12];
            frame[0] = 0x00; frame[1] = 0x01; // Transaction ID
            frame[2] = 0x00; frame[3] = 0x00;
            frame[4] = 0x00; frame[5] = 0x06;
            frame[6] = 0x01;
            frame[7] = func;
            frame[8] = (byte)(address >> 8);
            frame[9] = (byte)address;
            frame[10] = (byte)(val >> 8);
            frame[11] = (byte)val;

            PLC.LogHelper.DebugLog($"[Modbus TX] {BitConverter.ToString(frame).Replace("-", " ")}");

            return frame;
        }

        /// <summary>
        /// 发送请求并接收响应
        /// </summary>
        /// <param name="request">要发送的请求报文</param>
        /// <returns>返回接收到的响应报文</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TimeoutException"></exception>
        private async Task<byte[]> SendAndReceiveAsync (byte[] request) {
            //! 判空
            if (_stream == null || !_isConnected)
                throw new InvalidOperationException("连接已断开");

            //! 创建超时令牌
            var cts = new CancellationTokenSource(3000); // 3秒超时


            try {
                //! 发送请求
                await _stream.WriteAsync(request, 0, request.Length, cts.Token);

                //! 接收响应
                byte[] header = new byte[7];
                await _stream.ReadAsync(header, 0, 7, cts.Token);

                //! 解析响应长度
                int pduLen = ((header[4] << 8) | header[5]) - 1;
                byte[] response = new byte[7 + pduLen];
                Buffer.BlockCopy(header, 0, response, 0, 7);

                //! 读取剩余的PDU数据
                await _stream.ReadAsync(response, 7, pduLen, cts.Token);

                PLC.LogHelper.DebugLog($"[Modbus RX] {BitConverter.ToString(response).Replace("-", " ")}");

                //! 检验
                if ((response[7] & 0x80) != 0)
                    throw new Exception($"Modbus异常码: 0x{response[8]:X2}");

                return response;

            } catch (OperationCanceledException) {
                _isConnected = false;
                PLC.LogHelper.Log("[Modbus] 超时：设备没有回复读取请求");
                throw new TimeoutException("Modbus 响应超时");
            } catch {
                _isConnected = false;
                throw;
            }
        }

        /// <summary>
        /// 解析寄存器响应
        /// </summary>
        /// <param name="resp">响应报文</param>
        /// <param name="count">寄存器数量</param>
        /// <returns>返回解析后的寄存器值数组</returns>
        private ushort[] ParseRegisters (byte[] resp, int count) {
            ushort[] r = new ushort[count];
            //! 解析寄存器值
            for (int i = 0; i < count; i++)
                //! 寄存器值是两个字节，高字节在前
                //! 注意：Modbus寄存器地址从0开始，而功能码返回的数据是从1开始的，所以需要减1
                //! 例如：读取寄存器地址0x0000，返回的数据是从第一个寄存器开始的，所以需要减1
                r[i] = (ushort)((resp[9 + i * 2] << 8) | resp[10 + i * 2]);
            return r;
        }

        /// <summary>
        /// 解析线圈响应
        /// </summary>
        /// <param name="resp">响应报文</param>
        /// <param name="count">线圈数量</param>
        /// <returns>返回解析后的线圈状态数组</returns>
        private bool[] ParseCoils (byte[] resp, int count) {
            bool[] r = new bool[count];
            //! 公式: 线圈状态按位存储，每个字节包含8个线圈状态，低位在前
            for (int i = 0; i < count; i++) {
                int b = 9 + (i / 8);
                r[i] = (resp[b] & (1 << (i % 8))) != 0;
            }
            return r;
        }

        /// <summary>
        /// 将寄存器转换为浮点数
        /// </summary>
        /// <param name="highReg">高位寄存器</param>
        /// <param name="lowReg">低位寄存器</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回转换后的浮点数</returns>
        private float RegistersToFloat (ushort highReg, ushort lowReg, bool highWordFirst) {
            byte[] bytes = new byte[4];

            if (highWordFirst) {
                // 高字在前
                bytes[0] = (byte)(highReg >> 8);
                bytes[1] = (byte)(highReg & 0xFF);
                bytes[2] = (byte)(lowReg >> 8);
                bytes[3] = (byte)(lowReg & 0xFF);
            } else {
                // 低字在前
                bytes[0] = (byte)(lowReg >> 8);
                bytes[1] = (byte)(lowReg & 0xFF);
                bytes[2] = (byte)(highReg >> 8);
                bytes[3] = (byte)(highReg & 0xFF);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// 将浮点数转换为寄存器
        /// </summary>
        /// <param name="value">要转换的浮点数</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回转换后的寄存器数组</returns>
        private ushort[] FloatToRegisters (float value, bool highWordFirst) {
            byte[] bytes = BitConverter.GetBytes(value);
            ushort high, low;

            if (highWordFirst) {
                high = (ushort)((bytes[0] << 8) | bytes[1]);
                low = (ushort)((bytes[2] << 8) | bytes[3]);
            } else {
                high = (ushort)((bytes[2] << 8) | bytes[3]);
                low = (ushort)((bytes[0] << 8) | bytes[1]);
            }

            return new ushort[] { high, low };
        }

        /// <summary>
        /// 将寄存器转换为双精度浮点数
        /// </summary>
        /// <param name="regs">要转换的寄存器数组</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回转换后的双精度浮点数</returns>
        private double RegistersToDouble (ushort[] regs, bool highWordFirst) {
            byte[] bytes = new byte[8];

            if (highWordFirst) {
                for (int i = 0; i < 4; i++) {
                    bytes[i * 2] = (byte)(regs[i] >> 8);
                    bytes[i * 2 + 1] = (byte)(regs[i] & 0xFF);
                }
            } else {
                for (int i = 0; i < 4; i++) {
                    bytes[i * 2] = (byte)(regs[3 - i] >> 8);
                    bytes[i * 2 + 1] = (byte)(regs[3 - i] & 0xFF);
                }
            }

            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// 将双精度浮点数转换为寄存器
        /// </summary>
        /// <param name="value">要转换的双精度浮点数</param>
        /// <param name="highWordFirst">是否高字在前</param>
        /// <returns>返回转换后的寄存器数组</returns>
        private ushort[] DoubleToRegisters (double value, bool highWordFirst) {
            byte[] bytes = BitConverter.GetBytes(value);
            ushort[] regs = new ushort[4];

            if (highWordFirst) {
                for (int i = 0; i < 4; i++) {
                    regs[i] = (ushort)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
                }
            } else {
                for (int i = 0; i < 4; i++) {
                    regs[i] = (ushort)((bytes[(3 - i) * 2] << 8) | bytes[(3 - i) * 2 + 1]);
                }
            }

            return regs;
        }

       /// <summary>
       /// 多批量写入
       /// </summary>
       /// <param name="addr">起始地址</param>
       /// <param name="values">要写入的寄存器值数组</param>
       /// <returns></returns>
       /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteMultipleRegistersAsync (string addr, ushort[] values) {
            if (!IsConnected) throw new InvalidOperationException("未连接");

            int address = ConvertStringToInt(addr);

            //! 构建请求报文
            byte[] frame = new byte[13 + values.Length * 2];
            frame[0] = 0x00; frame[1] = 0x01;
            frame[2] = 0x00; frame[3] = 0x00;
            frame[4] = 0x00; frame[5] = (byte)(7 + values.Length * 2);
            frame[6] = 0x01;
            frame[7] = 0x10;                    // 功能码 0x10
            frame[8] = (byte)(address >> 8);
            frame[9] = (byte)address;
            frame[10] = (byte)(values.Length >> 8);
            frame[11] = (byte)values.Length;
            frame[12] = (byte)(values.Length * 2);

            //! 填充寄存器值
            for (int i = 0; i < values.Length; i++) {
                frame[13 + i * 2] = (byte)(values[i] >> 8);
                frame[14 + i * 2] = (byte)(values[i] & 0xFF);
            }

            await SendAndReceiveAsync(frame);
        }
    }
}