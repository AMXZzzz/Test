using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PLC
{
    /// <summary>
    /// Modbus RTU 帧封装在 TCP 流里的底层客户端
    /// （注意：与标准 Modbus-TCP 不同，没有 MBAP 头，帧尾是 CRC16）
    /// </summary>
    public class ModbusRtuOverTcpClient : IDisposable
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private bool _isConnected = false;

        //! 从站地址（如需可改为构造参数）
        private const byte StationId = 0x01;
        //! 收发超时
        private const int TimeoutMs = 3000;
        //! 串行化收发，避免并发帧交错
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);

        public bool IsConnected => _isConnected && _tcp?.Connected == true;

        public async Task ConnectAsync(string ip, int port, string localIP = null)
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
                    //! 绑定失败则用默认网卡
                }
            }

            await _tcp.ConnectAsync(ip, port);
            _stream = _tcp.GetStream();
            _stream.ReadTimeout = TimeoutMs;
            _stream.WriteTimeout = TimeoutMs;
            _isConnected = true;
        }

        public void Disconnect()
        {
            _isConnected = false;
            try { _stream?.Close(); _tcp?.Close(); } catch { }
            _stream = null;
            _tcp = null;
        }

        public void Dispose() => Disconnect();

        // ==================== 读写方法 ====================

        public async Task<ushort[]> ReadHoldingRegistersAsync(int address, int count)
        {
            byte[] frame = BuildSingleFrame(0x03, address, count);
            byte[] response = await SendAndReceiveAsync(frame);
            return ParseRegisters(response);
        }

        public async Task<bool[]> ReadCoilsAsync(int address, int count)
        {
            byte[] frame = BuildSingleFrame(0x01, address, count);
            byte[] response = await SendAndReceiveAsync(frame);
            return ParseCoils(response, count);
        }

        public async Task WriteSingleRegisterAsync(int address, ushort value)
        {
            byte[] frame = BuildSingleFrame(0x06, address, value);
            await SendAndReceiveAsync(frame);
        }

        public async Task WriteSingleCoilAsync(int address, bool value)
        {
            ushort coilValue = value ? (ushort)0xFF00 : (ushort)0x0000;
            byte[] frame = BuildSingleFrame(0x05, address, coilValue);
            await SendAndReceiveAsync(frame);
        }

        /// <summary>
        /// 写多个寄存器（功能码 0x10），用于 32 位写入
        /// </summary>
        public async Task WriteMultipleRegistersAsync(int address, ushort[] values)
        {
            byte[] frame = BuildWriteMultipleFrame(address, values);
            await SendAndReceiveAsync(frame);
        }

        // ==================== 帧构建 ====================

        private byte[] BuildSingleFrame(byte func, int addr, int val)
        {
            byte[] frame = new byte[8];
            frame[0] = StationId;
            frame[1] = func;
            frame[2] = (byte)(addr >> 8);
            frame[3] = (byte)addr;
            frame[4] = (byte)(val >> 8);
            frame[5] = (byte)val;

            ushort crc = CalculateCrc(frame, 6);
            frame[6] = (byte)crc;
            frame[7] = (byte)(crc >> 8);
            return frame;
        }

        private byte[] BuildWriteMultipleFrame(int addr, ushort[] values)
        {
            int n = values.Length;
            byte[] frame = new byte[9 + n * 2];
            frame[0] = StationId;
            frame[1] = 0x10;
            frame[2] = (byte)(addr >> 8);
            frame[3] = (byte)addr;
            frame[4] = (byte)(n >> 8);
            frame[5] = (byte)n;
            frame[6] = (byte)(n * 2);
            for (int i = 0; i < n; i++)
            {
                frame[7 + i * 2] = (byte)(values[i] >> 8);
                frame[8 + i * 2] = (byte)values[i];
            }
            ushort crc = CalculateCrc(frame, 7 + n * 2);
            frame[7 + n * 2] = (byte)crc;
            frame[8 + n * 2] = (byte)(crc >> 8);
            return frame;
        }

        // ==================== 收发（按长度读满 + CRC 校验 + 串行锁） ====================

        private async Task<byte[]> SendAndReceiveAsync(byte[] request)
        {
            await _ioLock.WaitAsync();
            try
            {
                if (_stream == null || !IsConnected)
                    throw new InvalidOperationException("Modbus RTU 未连接");

                using (var cts = new CancellationTokenSource(TimeoutMs))
                {
                    await _stream.WriteAsync(request, 0, request.Length, cts.Token);

                    //! 先读 从站地址 + 功能码
                    byte[] head = await ReadExactAsync(2, cts.Token);
                    byte func = head[1];
                    byte[] resp;

                    if ((func & 0x80) != 0)
                    {
                        //! 异常响应：异常码(1) + CRC(2)
                        byte[] tail = await ReadExactAsync(3, cts.Token);
                        resp = Combine(head, tail);
                        VerifyCrc(resp);
                        throw new Exception($"Modbus RTU 异常码: 0x{resp[2]:X2}");
                    }

                    if (func == 0x01 || func == 0x02 || func == 0x03 || func == 0x04)
                    {
                        //! 读类响应：字节数(1) + 数据(n) + CRC(2)
                        byte[] bc = await ReadExactAsync(1, cts.Token);
                        byte[] data = await ReadExactAsync(bc[0] + 2, cts.Token);
                        resp = Combine(head, Combine(bc, data));
                    }
                    else
                    {
                        //! 写类回显(0x05/0x06/0x0F/0x10)：地址(2)+值或数量(2)+CRC(2)
                        byte[] tail = await ReadExactAsync(6, cts.Token);
                        resp = Combine(head, tail);
                    }

                    VerifyCrc(resp);
                    return resp;
                }
            }
            catch (OperationCanceledException)
            {
                _isConnected = false;
                throw new TimeoutException("Modbus RTU 响应超时");
            }
            catch
            {
                _isConnected = false;
                throw;
            }
            finally
            {
                _ioLock.Release();
            }
        }

        /// <summary>
        /// 读满指定字节数（TCP 可能分包）
        /// </summary>
        private async Task<byte[]> ReadExactAsync(int count, CancellationToken token)
        {
            byte[] buf = new byte[count];
            int off = 0;
            while (off < count)
            {
                int n = await _stream.ReadAsync(buf, off, count - off, token);
                if (n <= 0) throw new IOException("连接已关闭");
                off += n;
            }
            return buf;
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private void VerifyCrc(byte[] frame)
        {
            if (frame.Length < 4) throw new Exception("Modbus RTU 响应过短");
            ushort calc = CalculateCrc(frame, frame.Length - 2);
            ushort recv = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
            if (calc != recv) throw new Exception("Modbus RTU CRC 校验失败");
        }

        private ushort CalculateCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        private ushort[] ParseRegisters(byte[] resp)
        {
            int count = resp[2] / 2;
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
                result[i] = (ushort)((resp[3 + i * 2] << 8) | resp[4 + i * 2]);
            return result;
        }

        private bool[] ParseCoils(byte[] resp, int count)
        {
            bool[] result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                int byteIndex = 3 + (i / 8);
                result[i] = (resp[byteIndex] & (1 << (i % 8))) != 0;
            }
            return result;
        }
    }
}
