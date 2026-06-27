using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLC {
    public partial class Form1 {


        // ========================== 事件 ==========================
        /// <summary>
        /// PLC选择改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgvPLCList_SelectionChanged (object sender, EventArgs e) {
            //! plc表格行不为空
            if (dgvPLCList.CurrentRow == null) return;
            //! 获取当前选中PLC的idx
            int idx = dgvPLCList.CurrentRow.Index;
            //! idx合法
            if (idx < 0 || idx >= _plcList.Count) return;

            //! 获取选中PLC的配置
            var config = _plcList[idx];

            //! 更新UI显示
            txtIp.Text = config.Link.Ip;
            txtPort.Text = config.Link.Port.ToString();

            // 暂时摘掉协议事件，避免修改时切换PLC时误触发
            cmbProtocol.SelectedIndexChanged -= CmbProtocol_SelectedIndexChanged;
            //! 协议类型
            int protoIdx = cmbProtocol.Items.IndexOf(config.Link.Protocol);
            //! 协议类型idx合法
            cmbProtocol.SelectedIndex = protoIdx >= 0 ? protoIdx : 0;
            //! 重新绑定协议事件
            cmbProtocol.SelectedIndexChanged += CmbProtocol_SelectedIndexChanged;

            //! 显示连接状态
            dgvPoints.ReadOnly = false;
            //! 加载点表格
            LoadPointsToGrid(config);
        }

        /// <summary>
        /// 协议改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbProtocol_SelectedIndexChanged (object sender, EventArgs e) {
            //! plc表格行不为空
            if (dgvPLCList.CurrentRow == null) return;
            //! 获取当前选中PLC的idx
            var config = _plcList[dgvPLCList.CurrentRow.Index];
            //! 协议类型正确
            string newProto = cmbProtocol.SelectedItem?.ToString() ?? "";
            //! 协议未改变
            if (newProto == config.Link.Protocol) return;
            //! 提示更新协议
            if (config.Link.Client != null) { Log("请先断开连接后再修改协议"); cmbProtocol.SelectedItem = config.Link.Protocol; return; }
            //! 记录更新协议
            config.Link.Protocol = newProto;
            //! 刷新PLC列表显示
            RefreshPLCList();
            Log($"✅ [{config.Link.PlcName}] 协议已更新为 {newProto}");
        }

        /// <summary>
        /// 添加PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnAddPLC_Click (object sender, EventArgs e) {
            //! 显示添加PLC对话框,等待用户输入PLC信息完成
            var config = AddPlcDialog.ShowDialog();
            //! 输入完成, 判空
            if (config == null) return;
            //! 添加到PLC列表
            _plcList.Add(config);
            //! 刷新PLC列表显示
            RefreshPLCList();
            Log($"✅ 已新增 PLC: {config.Link.PlcName}");
        }

        /// <summary>
        /// 链接单个PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnConnect_Click (object sender, EventArgs e) {
            //! 未选择PLC
            if (dgvPLCList.CurrentRow == null) { Log("请选中一行PLC"); return; }
            //! 链接选中的PLC
            await ConnectPlcAsync(_plcList[dgvPLCList.CurrentRow.Index]);
        }

        /// <summary>
        /// 链接全部PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnConnectAll_Click (object sender, EventArgs e) {
            if (_plcList.Count == 0) { Log("没有可连接的PLC"); return; }
            Log("开始一键连接全部PLC...");
            //! 依次链接每台PLC
            int success = 0, fail = 0;
            foreach (var config in _plcList) {
                if (config.Link.Client != null) { Log($"⏭ {config.Link.PlcName} 已连接，跳过"); continue; }
                await ConnectPlcAsync(config);
                if (config.Link.Client != null) success++; else fail++;
            }
            Log($"一键连接完成：成功 {success} 台，失败 {fail} 台");
        }

        /// <summary>
        /// 断开PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDisconnect_Click (object sender, EventArgs e) {
            //! 停止所有点的刷新
            _pollTimer.Stop();
            //! 遍历PLC列表，断开每台PLC连接
            foreach (var p in _plcList) {
                //! 断开连接
                if (p.Link.Client is PLC.IBase.IPlcBase client)
                    try { client.Disconnect(); } catch { }
                p.Link.Client = null;
            }
            //! 刷新PLC列表显示
            RefreshPLCList();
            Log("全部断开");
        }

        /// <summary>
        /// 删除PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeletePLC_Click (object sender, EventArgs e) {
            //! 未选择PLC
            if (dgvPLCList.CurrentRow == null) { Log("请先选中要删除的PLC"); return; }
            int index = dgvPLCList.CurrentRow.Index;
            var config = _plcList[index];

            //! 删除前确认
            if (MessageBox.Show($"确定删除 [{config.Link.PlcName}]？\n此操作不可恢复。", "删除确认",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;

            //! 删除PLC配置
            if (config.Link.Client is PLC.IBase.IPlcBase c)
                try { c.Disconnect(); } catch { }
            config.Link.Client = null;

            //! 移除绑定事件, 避免误触发
            dgvPLCList.SelectionChanged -= DgvPLCList_SelectionChanged;

            //! 从列表中移除
            _plcList.RemoveAt(index);
            RefreshPLCList();

            //! 如果删除后还有PLC，选中第一行
            if (dgvPLCList.Rows.Count > 0) {
                int newIdx = Math.Min(index, dgvPLCList.Rows.Count - 1);
                dgvPLCList.Rows[newIdx].Selected = true;
                dgvPLCList.CurrentCell = dgvPLCList.Rows[newIdx].Cells[0];
            } else {
                dgvPoints.Rows.Clear();
            }
            //! 重新绑定事件
            dgvPLCList.SelectionChanged += DgvPLCList_SelectionChanged;

            Log($"🗑️ 已删除 PLC: {config.Link.PlcName}");
        }

        /// <summary>
        /// 移动PLC按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMovePLCUp_Click (object sender, EventArgs e) => MovePlc(-1);
        private void BtnMovePLCDown_Click (object sender, EventArgs e) => MovePlc(1);

        /// <summary>
        /// 应用Ip修改按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnApplyIp_Click (object sender, EventArgs e) {
            if (dgvPLCList.CurrentRow == null) { Log("请先选中PLC"); return; }
            var config = _plcList[dgvPLCList.CurrentRow.Index];
            string newIp = txtIp.Text.Trim();
            if (!System.Net.IPAddress.TryParse(newIp, out _)) { Log($"IP格式无效: {newIp}"); return; }
            if (config.Link.Client != null) { Log("请先断开连接后再修改IP"); return; }
            config.Link.Ip = newIp;
            RefreshPLCList();
            Log($"✅ [{config.Link.PlcName}] IP已更新为 {newIp}");
        }

        /// <summary>
        /// 应用Port修改按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnApplyPort_Click (object sender, EventArgs e) {
            if (dgvPLCList.CurrentRow == null) { Log("请先选中PLC"); return; }
            var config = _plcList[dgvPLCList.CurrentRow.Index];
            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port <= 0 || port > 65535) { Log("端口必须是 1~65535 的整数"); return; }
            if (config.Link.Client != null) { Log("请先断开连接后再修改端口"); return; }
            config.Link.Port = port;
            RefreshPLCList();
            Log($"✅ [{config.Link.PlcName}] 端口已更新为 {port}");
        }

        //! =================================== 辅助方法 ===================================
        /// <summary>
        /// 刷新PLC列表
        /// </summary>
        private void RefreshPLCList () {
            dgvPLCList.Rows.Clear();
            foreach (var p in _plcList) {
                string status = (p.Link.Client as PLC.IBase.IPlcBase)?.IsConnected == true ? "已连接" : "断开";
                dgvPLCList.Rows.Add(p.Link.PlcName, p.Link.Ip, p.Link.Port, p.Link.Protocol, status);
            }
        }

        /// <summary>
        /// 连接PLC
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task ConnectPlcAsync (TcpPlcConfig config) {
            if (config.Link.Client != null) return;

            //! 测试连接
            if (!await PingHostAsync(config.Link.Ip)) {
                Log($"❌ {config.Link.PlcName} Ping 失败");
                return;
            }

            //! 获取网络适配器
            string localIP = Form1.SelectedLocalIP;
            string adapterInfo = string.IsNullOrEmpty(localIP) ? "默认网卡" : localIP;
            Log($"[连接] 开始连接 {config.Link.PlcName} ({config.Link.Protocol})，使用网卡: {adapterInfo}");

            try {
                PLC.IBase.IPlcBase client = null;

                //! 内置协议处理
                if (config.Link.Protocol == ModbusTcp.ProtocolName) {
                    var modbus = new ModbusTcp();
                    bool ok = await modbus.ConnectAsync(config.Link.Ip, config.Link.Port, localIP, config.ExtraParams);
                    if (ok) client = modbus;
                } else {
                    //! 根据协议创建PLC客户端实例
                    client = PluginLoader.CreateClient(config.Link.Protocol);
                    if (client != null) {
                        bool ok = await client.ConnectAsync(config.Link.Ip, config.Link.Port, localIP, config.ExtraParams);
                        if (!ok) client = null;
                    }
                }
                //! 连接结果处理
                if (client != null) {
                    config.Link.Client = client;
                    Log($"✅ {config.Link.PlcName} 连接成功");
                    RefreshPLCList();
                    _pollTimer.Start();
                } else {
                    Log($"❌ {config.Link.PlcName} 连接失败");
                }
            } catch (Exception ex) {
                Log($"❌ {config.Link.PlcName} 连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 移动PLC位置
        /// </summary>
        /// <param name="direction">移动方向，正数表示向下移动，负数表示向上移动</param>
        private void MovePlc (int direction) {
            //! 当前没有选中PLC
            if (dgvPLCList.CurrentRow == null) return;

            //! 获取当前选中PLC的索引和目标索引
            int idx = dgvPLCList.CurrentRow.Index;
            int newIdx = idx + direction;
            //! 目标索引越界检查
            if (newIdx < 0 || newIdx >= _plcList.Count) return;
            //! 暂时移除事件绑定，避免刷新列表时触发SelectionChanged事件
            dgvPLCList.SelectionChanged -= DgvPLCList_SelectionChanged;
            //! 移动新的索引位置
            (_plcList[newIdx], _plcList[idx]) = (_plcList[idx], _plcList[newIdx]);
            //! 刷新PLC列表显示
            RefreshPLCList();
            //! 重新选中移动后的PLC行
            dgvPLCList.Rows[newIdx].Selected = true;
            //! 设置当前单元格，确保SelectionChanged事件能正确识别选中行
            dgvPLCList.CurrentCell = dgvPLCList.Rows[newIdx].Cells[0];
            //! 重新绑定事件
            dgvPLCList.SelectionChanged += DgvPLCList_SelectionChanged;
        }
    }
}