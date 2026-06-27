using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLC { 
    public partial class Form1 {
        //! ====================== 变量 ======================

        /// <summary>
        /// 初始化读取Task
        /// </summary>
        private void InitializeComm () {
            //! 50ms定时器
            _pollTimer = new Timer { Interval = 50 };

            //! 定时器回调, 用于轮询读取数据
            _pollTimer.Tick += async (s, e) => {
                if (_polling) return;
                _polling = true;
                try { await TimerPollAsync(); }
                finally { _polling = false; }
            };
        }

        /// <summary>
        /// 轮询读取Task
        /// </summary>
        /// <returns></returns>
        private async Task TimerPollAsync () {
            //! PLC列表为空, 跳过
            if (dgvPLCList.CurrentRow == null) return;

            //! 当前PLC在PLC列表中. 跳过
            int plcIdx = dgvPLCList.CurrentRow.Index;
            if (plcIdx < 0 || plcIdx >= _plcList.Count) return;

            //! 当前PLC未连接, 跳过
            var _plc = _plcList[plcIdx];
            if (_plc.Link.Client == null) return;

            // ==================== 获取所有需要轮询的寄存器 ====================
            var registersToRead = GetRegistersToPoll(_plc);

            // ==================== 执行读取并更新界面 ====================
            foreach (var (reg, gridRow) in registersToRead) {
                if (reg.Address == null) continue;

                try {
                    int val = await ReadRegisterAsync(_plc, reg);
                    //! 更新寄存器的当前值
                    reg.CurrentValue = val;

                    //! 更新到界面
                    UpdateGridValue(gridRow, val);

                } catch (Exception ex) {
                    bool shouldLog = _plc.LastError != ex.Message ||
                                    (DateTime.Now - _plc.LastErrorTime).TotalSeconds > 5;

                    if (shouldLog) {
                        Log($"[{_plc.Link.PlcName}] 读取失败: {ex.Message}");
                        _plc.LastError = ex.Message;
                        _plc.LastErrorTime = DateTime.Now;
                    }
                }
            }
        }

        // ==================== 事件 ====================
        /// <summary>
        /// 一键调整所有宽度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnAdjustWidth_Click (object sender, EventArgs e) {

            //! 读取宽度
            if (!decimal.TryParse(txtWidth1.Text, out decimal w1) || w1 <= 0) { Log("❌ 轨道1宽度无效"); return; }
            if (!decimal.TryParse(txtWidth2.Text, out decimal w2) || w2 <= 0) { Log("❌ 轨道2宽度无效"); return; }

            //! 计算宽度值, 单位: 0.1mm
            int w1Int = (int)(w1 * 10);
            int w2Int = (int)(w2 * 10);

            //! PLC链接测试
            var connected = _plcList.FindAll(p => p.Link.Client != null);
            if (connected.Count == 0) { Log("没有已连接的PLC，请先连接"); return; }

            //! 写入宽度值
            btnAdjustWidth.Enabled = false;     //! 禁用按钮，防止重复点击
            Log($"══ 开始一键调宽  轨道1:{w1}mm  轨道2:{w2}mm  共{connected.Count}台 ══");

            //! 并行写入所有已连接的PLC
            foreach (var p in connected) {
                p.Track.OneWidthAddr.TargetValue = w1Int;
                p.Track.TwoWidthAddr.TargetValue = w2Int;
                await AdjustWidthSingleAsync(p);
            }

            //! 写入完成, 启用按钮
            Log("══ 一键调宽全部完成 ══");
            btnAdjustWidth.Enabled = true;
        }


        // ==================== 辅助函数 ====================

        /// <summary>
        /// 根据PLC配置，生成需要轮询的寄存器列表（包含网格行索引）
        /// </summary>
        private List<(PLC.IBase.IRegister register, int gridRowIndex)> GetRegistersToPoll (TcpPlcConfig config) {
            var list = new List<(PLC.IBase.IRegister register, int gridRowIndex)>();
            int trackRowCount = GetTrackRowCount(config);
            int currentRow = 0;

            // ==================== 轨道配置行 ====================
            // 轨道1（必有）
            list.Add((config.Track.OneWidthAddr, currentRow++));
            list.Add((config.Track.OneTriggerAddr, currentRow++));

            // 轨道2
            if (config.Track.TrackCount >= 2) {
                list.Add((config.Track.TwoWidthAddr, currentRow++));
                list.Add((config.Track.TwoTriggerAddr, currentRow++));
            }

            // 轨道1完成状态
            if (config.Track.OneHasStatus)
                list.Add((config.Track.OneStatusAddr, currentRow++));

            // 轨道2完成状态
            if (config.Track.TrackCount >= 2 && config.Track.TwoHasStatus)
                list.Add((config.Track.TwoStatusAddr, currentRow++));

            // ==================== 自定义读写点 ====================
            for (int i = 0; i < config.RegisterSheet.Count; i++) {
                int gridRow = trackRowCount + i;
                list.Add((config.RegisterSheet[i], gridRow));
            }

            return list;
        }

        /// <summary>
        /// 一键调整宽度单台PLC
        /// </summary>
        /// <param name="config">PLC 配置对象</param>
        /// <param name="width1">轨道1宽度</param>
        /// <param name="width2">轨道2宽度</param>
        /// <returns></returns>
        private async Task AdjustWidthSingleAsync (TcpPlcConfig config) {
            try {
                //! 写入轨道1宽度
                await WriteRegisterAsync(config, config.Track.OneWidthAddr);
                Log($"[{config.Track.TrackCount} 轨道1] 写入宽度: {config.Track.OneWidthAddr.TargetValue}");
                await Task.Delay(100);

                //! 写入轨道1触发地址
                await WriteRegisterAsync(config, config.Track.OneTriggerAddr);
                Log($"[{config.Track.TrackCount} 轨道1] 触发调宽");

                //! 判断是否有第二轨道
                if (config.Track.TrackCount >= 2) {
                    //! 写入轨道2宽度
                    await WriteRegisterAsync(config, config.Track.TwoWidthAddr);
                    Log($"[{config.Track.TrackCount} 轨道2] 写入宽度: {config.Track.TwoWidthAddr.TargetValue}");
                    await Task.Delay(100);

                    //! 写入轨道2触发地址
                    await WriteRegisterAsync(config, config.Track.TwoTriggerAddr);
                    Log($"[{config.Track.TrackCount} 轨道2] 触发调宽");
                }

                //! 判断第二轨道是否有完成地址
                if (config.Track.TwoHasStatus) {
                    //! 等待第二轨道完成
                    bool done = await WaitForStatusAsync(config, config.Track.TwoStatusAddr,
                        config.Track.TwoStatusDoneValue, 15000);
                    Log(done ? $"✅ [{config.Track.TrackCount} 轨道2] 完成" : $"⚠️ [{config.Track.TrackCount} 轨道2] 超时");
                }
                //! 判断轨道1是否需要等待
                if (config.Track.OneHasStatus) {
                    //! 等待轨道1调宽完成, 超时10秒
                    bool done = await WaitForStatusAsync(config, config.Track.OneStatusAddr, config.Track.OneStatusDoneValue, 10000);
                    Log(done ? $"✅ [{config.Track.TrackCount} 轨道1] 完成" : $"⚠️ [{config.Track.TrackCount} 轨道1] 超时");
                }

            } catch (Exception ex) { Log($"❌ [{config.Track.TrackCount}] 调宽异常: {ex.Message}"); }
        }

        /// <summary>
        /// 动态计算当前PLC有多少轨道配置行
        /// </summary>
        private int GetTrackRowCount (TcpPlcConfig config) {
            int count = 2; // 宽度 + 触发
            if (config.Track.TrackCount >= 2) count += 2;
            if (config.Track.OneHasStatus) count += 1;
            if (config.Track.TrackCount >= 2 && config.Track.TwoHasStatus) count += 1;
            return count;
        }

        /// <summary>
        /// 安全更新 DataGridView 中的值（自动处理跨线程）
        /// </summary>
        private void UpdateGridValue (int rowIndex, int value) {
            if (rowIndex < 0 || rowIndex >= dgvPoints.Rows.Count) return;

            Action action = () =>
            {
                dgvPoints.Rows[rowIndex].Cells[colCurVal.Name].Value = value;
            };

            if (dgvPoints.InvokeRequired)
                dgvPoints.BeginInvoke(action);
            else
                action();
        }
        /// <summary>
        /// 等待状态寄存器达到指定值
        /// </summary>
        /// <param name="config">PLC 配置对象</param>
        /// <param name="statusAddr">状态寄存器地址</param>
        /// <param name="doneValue">完成值</param>
        /// <param name="dt">数据类型</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns></returns>
        private async Task<bool> WaitForStatusAsync (TcpPlcConfig config, PLC.IBase.IRegister register, int doneValue,int timeoutMs) {
            int elapsed = 0;
            const int interval = 500;

            while (elapsed < timeoutMs) {
                await Task.Delay(interval);
                elapsed += interval;

                try {

                    int cur = await ReadRegisterAsync( config, register);
                    if (cur == doneValue) return true;
                } catch { }
            }
            return false;
        }

        /// <summary>
        /// 读取寄存器
        /// </summary>
        /// <param name="config">PLC 配置对象</param>
        /// <param name="address">寄存器地址</param>
        /// <param name="type">数据类型</param>
        /// <param name="description">描述信息</param>
        /// <returns>读取到的值</returns>

        private async Task<int> ReadRegisterAsync (TcpPlcConfig config, PLC.IBase.IRegister register) {
            //! 如果没有选择协议，则返回
            if (config.Link.Client is PLC.IBase.IPlcBase client) {
                try {
                    //! 读取寄存器
                    return Convert.ToInt32((await client.ReadAsync(register)).CurrentValue);

                } catch (Exception ex) {
                    LogHelper.Log($"读取失败: {ex.Message}");
                    return 0;
                }
            }

            Log("PLC 未连接，无法读取");
            return 0;
        }

        /// <summary>
        /// 写入寄存器
        /// </summary>
        /// <param name="config">PLC 配置对象</param>
        /// <param name="addr">寄存器地址</param>
        /// <param name="value">写入的值</param>
        /// <param name="type">数据类型</param>
        /// <param name="description">描述信息</param>
        /// <returns></returns>
        private async Task WriteRegisterAsync (TcpPlcConfig config, PLC.IBase.IRegister register) {
            //! 如果没有选择协议，则返回
            if (config.Link.Client is PLC.IBase.IPlcBase client) {
                await client.WriteAsync(register);
            } else {
                Log("PLC 未连接，无法写入");
            }
        }

        /// <summary>
        /// 测试网络连通性
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <returns>是否连通</returns>
        private async Task<bool> PingHostAsync (string host) {
            try {
                using (var ping = new System.Net.NetworkInformation.Ping()) {
                    var reply = await ping.SendPingAsync(host, 1500);
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            } catch { return false; }
        }
    }
}