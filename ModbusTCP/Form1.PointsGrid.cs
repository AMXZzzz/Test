using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLC {
    public partial class Form1 {
        // ==================== 变量 ====================
        private readonly Color _widthAddrColo = Color.FromArgb(230, 240, 255);
        private readonly Color _triggerAddrColo = Color.FromArgb(230, 255, 230);

        // ==================== 事件 ====================
        private void DgvPoints_CellValueChanged (object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0 || dgvPLCList.CurrentRow == null) return;

            var config = _plcList[dgvPLCList.CurrentRow.Index];
            var cell = dgvPoints.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string raw = cell.Value?.ToString() ?? "";

            //! 改为解析结构体
            string desc = dgvPoints.Rows[e.RowIndex].Cells[colDesc.Name].Value?.ToString() ?? "";
            bool isTrackRow = desc.StartsWith("[轨道");

            PLC.IBase.IRegister targetReg = null;

            if (isTrackRow) {
                targetReg = GetTrackRegister(config, e.RowIndex);
            } else {
                int trackCount = GetCurrentTrackRowCount(config);
                int pi = e.RowIndex - trackCount;
                if (pi >= 0 && pi < config.RegisterSheet.Count)
                    targetReg = config.RegisterSheet[pi];
            }

            if (targetReg == null) return;

            switch (e.ColumnIndex) {
                case 0: // 地址
                    targetReg.Address = raw;
                    break;

                case 1: // 当前值
                    if (int.TryParse(raw, out int cur)) targetReg.CurrentValue = cur;
                    else { cell.Value = targetReg.CurrentValue; Log("当前值格式错误，已还原"); }
                    break;

                case 2: // 目标值
                    if (int.TryParse(raw, out int tgt)) targetReg.TargetValue = tgt;
                    else { cell.Value = targetReg.TargetValue; Log("目标值格式错误，已还原"); }
                    break;

                case 3: // 类型
                    if (Enum.TryParse(raw, out PLC.IBase.RegisterDataType dt)) {
                        targetReg.DataType = dt;
                    }
                    break;

                case 4: // 描述
                    targetReg.Description = raw;
                    break;
            }
        }

        // ==================== 读/写按钮 ====================
        private async void DgvPoints_CellClick (object sender, DataGridViewCellEventArgs e) {
            if (e.RowIndex < 0 || dgvPLCList.CurrentRow == null) return;

            var config = _plcList[dgvPLCList.CurrentRow.Index];
            if (config.Link.Client == null) { Log("PLC未连接"); return; }

            // 只处理写入列
            if (e.ColumnIndex != dgvPoints.Columns[colWrite.Name].Index) return;

            string desc = dgvPoints.Rows[e.RowIndex].Cells[colDesc.Name].Value?.ToString() ?? "";
            bool isTrackRow = desc.StartsWith("[轨道");

            PLC.IBase.IRegister reg = null;

            if (isTrackRow) {
                reg = GetTrackRegister(config, e.RowIndex);
            } else {
                int trackCount = GetTrackRowCount(config);
                int customIndex = e.RowIndex - trackCount;
                if (customIndex >= 0 && customIndex < config.RegisterSheet.Count)
                    reg = config.RegisterSheet[customIndex];
            }

            if (reg == null) return;

            try {
                await WriteRegisterAsync(config, reg);
                Log($"写入成功: {AddrToDisplay(reg)} = {reg.TargetValue}");

                // 写入后自动刷新当前值
                int newVal = await ReadRegisterAsync(config, reg);
                UpdateGridValue(e.RowIndex, newVal);
            } catch (Exception ex) {
                Log($"写入失败: {ex.Message}");
            }
        }

        // ==================== 添加/删除点 ====================
        private void BtnAddPoint_Click (object sender, EventArgs e) {
            if (dgvPLCList.CurrentRow == null) { Log("请先选中PLC"); return; }

            var config = _plcList[dgvPLCList.CurrentRow.Index];
            var point = AddPointDialog.ShowDialog();
            if (point != null) {
                config.RegisterSheet.Add(point);
                LoadPointsToGrid(config);
                Log($"已添加点: {point.Address}");
            }
        }

        private void BtnDeletePoint_Click (object sender, EventArgs e) {
            if (dgvPLCList.CurrentRow == null || dgvPoints.CurrentRow == null) return;

            string desc = dgvPoints.Rows[dgvPoints.CurrentRow.Index].Cells[colDesc.Name].Value?.ToString() ?? "";
            if (desc.StartsWith("[轨道")) {
                Log("轨道配置行不能删除");
                return;
            }

            var config = _plcList[dgvPLCList.CurrentRow.Index];
            int trackCount = GetCurrentTrackRowCount(config);
            int pi = dgvPoints.CurrentRow.Index - trackCount;

            if (pi < 0 || pi >= config.RegisterSheet.Count) return;

            config.RegisterSheet.RemoveAt(pi);
            LoadPointsToGrid(config);
            Log("已删除选中点");
        }

        //! ===================== 辅助函数 =====================
        private void LoadPointsToGrid (TcpPlcConfig config) {
            dgvPoints.Rows.Clear();

            void AddTrack (PLC.IBase.IRegister register, string desc, Color color) {
                int i = dgvPoints.Rows.Add();
                dgvPoints.Rows[i].Cells[colAddr.Name].Value = AddrToDisplay(register);
                dgvPoints.Rows[i].Cells[colDataType.Name].Value = register.DataType.ToString();
                dgvPoints.Rows[i].Cells[colDesc.Name].Value = desc;
                SetTrackRow(i, color);
            }

            // 添加轨道行
            AddTrack(config.Track.OneWidthAddr, "[轨道1] 宽度地址", _widthAddrColo);
            AddTrack(config.Track.OneTriggerAddr, "[轨道1] 触发地址", _triggerAddrColo);

            if (config.Track.TrackCount >= 2) {
                AddTrack(config.Track.TwoWidthAddr, "[轨道2] 宽度地址", _widthAddrColo);
                AddTrack(config.Track.TwoTriggerAddr, "[轨道2] 触发地址", _triggerAddrColo);
            }

            if (config.Track.OneHasStatus)
                AddTrack(config.Track.OneStatusAddr, $"[轨道1] 状态地址 (完成={config.Track.OneStatusDoneValue})", _widthAddrColo);

            if (config.Track.TrackCount >= 2 && config.Track.TwoHasStatus)
                AddTrack(config.Track.TwoStatusAddr, $"[轨道2] 状态地址 (完成={config.Track.TwoStatusDoneValue})", _triggerAddrColo);

            // 添加自定义读写点
            foreach (var point in config.RegisterSheet) {
                int ri = dgvPoints.Rows.Add();
                dgvPoints.Rows[ri].Cells[colAddr.Name].Value = AddrToDisplay(point);
                dgvPoints.Rows[ri].Cells[colCurVal.Name].Value = point.CurrentValue;
                dgvPoints.Rows[ri].Cells[colTgt.Name].Value = point.TargetValue;
                dgvPoints.Rows[ri].Cells[colDataType.Name].Value = point.DataType.ToString();
                dgvPoints.Rows[ri].Cells[colDesc.Name].Value = point.Description;
            }
        }

        /// <summary>
        /// 获取当前配置的轨道行数量（动态计算）
        /// </summary>
        private int GetCurrentTrackRowCount (TcpPlcConfig config) {
            int count = 2;
            if (config.Track.TrackCount >= 2) count += 2;
            if (config.Track.OneHasStatus) count += 1;
            if (config.Track.TrackCount >= 2 && config.Track.TwoHasStatus) count += 1;
            return count;
        }

        /// <summary>
        /// 根据行索引获取对应的轨道寄存器
        /// </summary>
        private PLC.IBase.IRegister GetTrackRegister (TcpPlcConfig config, int rowIndex) {
            int idx = 0;
            if (rowIndex == idx++) return config.Track.OneWidthAddr;
            if (rowIndex == idx++) return config.Track.OneTriggerAddr;

            if (config.Track.TrackCount >= 2) {
                if (rowIndex == idx++) return config.Track.TwoWidthAddr;
                if (rowIndex == idx++) return config.Track.TwoTriggerAddr;
            }

            if (config.Track.OneHasStatus) {
                if (rowIndex == idx++) return config.Track.OneStatusAddr;
            }

            if (config.Track.TrackCount >= 2 && config.Track.TwoHasStatus) {
                if (rowIndex == idx++) return config.Track.TwoStatusAddr;
            }
            return null;
        }

        /// <summary>
        /// 地址显示（已移除 AddressText）
        /// </summary>
        private string AddrToDisplay (PLC.IBase.IRegister reg) {
            return reg?.Address ?? "0";
        }

        private string AddrToDisplay (string address, PLC.IBase.RegisterDataType dt = PLC.IBase.RegisterDataType.Int16) {
            return address ?? "0";
        }

        private static bool TryParseAddr (string s, PLC.IBase.RegisterDataType dt, out int addr) {
            s = s?.Trim() ?? "";
            if (dt == PLC.IBase.RegisterDataType.Coil) {
                s = s.TrimStart('0');
                if (s == "") s = "0";
                return int.TryParse(s, NumberStyles.HexNumber, null, out addr);
            }
            return int.TryParse(s, out addr);
        }

        private void SetTrackRow (int rowIdx, Color backColor) {
            var row = dgvPoints.Rows[rowIdx];
            row.DefaultCellStyle.BackColor = backColor;
            row.DefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 120);
            row.Cells[0].ReadOnly = true;
            row.Cells[1].ReadOnly = true;
            row.Cells[4].ReadOnly = true;
        }
    }
}