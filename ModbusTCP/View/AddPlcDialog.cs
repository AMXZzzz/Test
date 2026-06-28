using System;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;
using PLC.Models;
namespace PLC {

    /// <summary>
    /// PLC表格事件处理
    /// </summary>
    public static class AddPlcDialog {
        /// <summary>
        /// 显示窗口
        /// </summary>
        /// <returns></returns>
        public static TcpPlcConfig ShowDialog () {
            //! 创建窗口
            Form form = new Form() {
                Width = 520,
                Height = 680,
                Text = "新增PLC",
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            // 标定位置
            int y = 0;

            //! 创建tab控件
            var tab = new TabControl { Left = 10, Top = 10, Width = 490, Height = 560 };
            var page1 = new TabPage("基本信息 & 读写点");
            var page2 = new TabPage("调宽配置");
            tab.TabPages.Add(page1);
            tab.TabPages.Add(page2);
            form.Controls.Add(tab);


            // ==================== 基本信息控件 ====================
            y = 15;
            AddLabelAndText(page1, "PLC名称:", 10, y, out TextBox txtName, "新PLC"); y += 40;
            AddLabelAndText(page1, "IP地址:", 10, y, out TextBox txtIp, "192.168.1.100"); y += 40;
            AddLabelAndText(page1, "端口:", 10, y, out TextBox txtPort, "502"); y += 40;

            // ==================== 协议选择控件 ====================
            page1.Controls.Add(new Label { Text = "协议:", Left = 10, Top = y + 3, AutoSize = true });
            var cmbProtocol = new ComboBox {
                Left = 110,
                Top = y,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 加载插件协议
            var protocols = PluginLoader.GetAvailableProtocolNames();
            cmbProtocol.Items.AddRange(protocols.ToArray());

            // 确保 ModbusTCP 一定存在（因为它是主程序内置的，不是插件）
            if (!cmbProtocol.Items.Contains(ModbusTcp.ProtocolName))
                cmbProtocol.Items.Insert(0, ModbusTcp.ProtocolName);

            // 安全设置 SelectedIndex
            if (cmbProtocol.Items.Count > 0)
                cmbProtocol.SelectedIndex = 0;

            page1.Controls.Add(cmbProtocol);
            y += 50;

            // ==================== 扩展参数控件 ====================
            page1.Controls.Add(new Label { Text = "扩展参数（可选）:", Left = 10, Top = y + 5, AutoSize = true });
            y += 30;

            var lvParams = new ListView {
                Left = 10,
                Top = y,
                Width = 460,
                Height = 90,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvParams.Columns.Add("参数名", 180);
            lvParams.Columns.Add("参数值", 260);
            page1.Controls.Add(lvParams);
            y += 100;

            var btnAddParam = new Button { Text = "添加", Left = 10, Top = y, Width = 80 };
            var btnEditParam = new Button { Text = "编辑", Left = 100, Top = y, Width = 80 };
            var btnDelParam = new Button { Text = "删除", Left = 190, Top = y, Width = 80 };
            page1.Controls.Add(btnAddParam);
            page1.Controls.Add(btnEditParam);
            page1.Controls.Add(btnDelParam);
            y += 45;

            // ==================== 读写地址点控件 ====================
            page1.Controls.Add(new Label { Text = "读写地址点:", Left = 10, Top = y, AutoSize = true });
            y += 25;

            var lstPoints = new ListBox { Left = 10, Top = y, Width = 460, Height = 100 };
            page1.Controls.Add(lstPoints);

            var btnAddPt = new Button { Text = "添加地址点", Left = 10, Top = y + 110, Width = 110 };
            var btnDelPt = new Button { Text = "删除选中", Left = 130, Top = y + 110, Width = 100 };
            page1.Controls.Add(btnAddPt);
            page1.Controls.Add(btnDelPt);



            // ==================== 事件绑定 ====================
            //! 协议选择事件
            cmbProtocol.SelectedIndexChanged += (s, e) => {
                string selectedProtocol = cmbProtocol.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedProtocol)) return;

                lvParams.Items.Clear();
                var recommended = PluginLoader.GetRecommendedExtraParams(selectedProtocol);
                foreach (var kv in recommended) {
                    var item = new ListViewItem(kv.Key);
                    item.SubItems.Add(kv.Value);
                    lvParams.Items.Add(item);
                }
            };

            //! 自动填充扩展参数
            if (cmbProtocol.SelectedItem != null) {
                var recommended = PluginLoader.GetRecommendedExtraParams(cmbProtocol.SelectedItem.ToString());
                foreach (var kv in recommended) {
                    var item = new ListViewItem(kv.Key);
                    item.SubItems.Add(kv.Value);
                    lvParams.Items.Add(item);
                }
            }

            //! 添加地址点事件
            btnAddPt.Click += (s, e) => {
                var point = AddPointDialog.ShowDialog();
                if (point != null)
                    lstPoints.Items.Add($"{point.Address} | 值:{point.TargetValue} | {point.Description}");
            };

            //! 删除地址点事件
            btnDelPt.Click += (s, e) => {
                if (lstPoints.SelectedIndex >= 0)
                    lstPoints.Items.RemoveAt(lstPoints.SelectedIndex);
            };

            //! 添加扩展参数事件
            btnAddParam.Click += (s, e) => AddExtraParamDialog(lvParams);
            btnEditParam.Click += (s, e) => EditExtraParamDialog(lvParams);
            btnDelParam.Click += (s, e) => {
                if (lvParams.SelectedItems.Count > 0)
                    lvParams.Items.Remove(lvParams.SelectedItems[0]);
            };

            // ==================== 第二页：调宽配置控件 ====================
            //! 第二个panel控件
            var panel2 = new Panel { AutoScroll = true, Dock = DockStyle.Fill };
            page2.Controls.Add(panel2);
            //! 轨道数量控件
            y = 15;
            panel2.Controls.Add(new Label { Text = "轨道数:", Left = 10, Top = y + 3, AutoSize = true });
            var cmbTrack = new ComboBox { Left = 110, Top = y, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTrack.Items.AddRange(new object[] { "1个轨道", "2个轨道" });
            cmbTrack.SelectedIndex = 0;
            panel2.Controls.Add(cmbTrack);
            y += 38;

            // 轨道1控件
            AddSectionLabel(panel2, "轨道1", 10, y); y += 26;
            AddLabelAndText(panel2, "宽度地址:", 10, y, out TextBox txtW1, "0"); y += 35;
            AddLabelAndText(panel2, "触发地址:", 10, y, out TextBox txtTrig1, "0"); y += 35;

            var chkS1 = new CheckBox { Text = "有完成状态反馈", Left = 10, Top = y, AutoSize = true };
            panel2.Controls.Add(chkS1); y += 26;
            AddLabelAndText(panel2, "状态地址:", 10, y, out TextBox txtSA1, "0"); y += 32;
            AddLabelAndText(panel2, "完成值:", 10, y, out TextBox txtDV1, "1"); y += 38;

            txtSA1.Enabled = false; txtDV1.Enabled = false;
            chkS1.CheckedChanged += (s, e) => {
                txtSA1.Enabled = chkS1.Checked;
                txtDV1.Enabled = chkS1.Checked;
            };

            // 轨道2控件
            AddSectionLabel(panel2, "轨道2", 10, y, "t2"); y += 26;
            AddLabelAndText(panel2, "宽度地址:", 10, y, out TextBox txtW2, "0", "t2"); y += 35;
            AddLabelAndText(panel2, "触发地址:", 10, y, out TextBox txtTrig2, "0", "t2"); y += 35;

            var chkS2 = new CheckBox {
                Text = "有完成状态反馈",
                Left = 10,
                Top = y,
                AutoSize = true,
                Tag = "t2",
                Visible = false
            };
            panel2.Controls.Add(chkS2); y += 26;

            AddLabelAndText(panel2, "状态地址:", 10, y, out TextBox txtSA2, "0", "t2"); y += 32;
            AddLabelAndText(panel2, "完成值:", 10, y, out TextBox txtDV2, "1", "t2");

            txtSA2.Enabled = false; txtDV2.Enabled = false;
            chkS2.CheckedChanged += (s, e) => {
                txtSA2.Enabled = chkS2.Checked;
                txtDV2.Enabled = chkS2.Checked;
            };

            cmbTrack.SelectedIndexChanged += (s, e) => {
                bool show = cmbTrack.SelectedIndex == 1;
                foreach (Control c in panel2.Controls)
                    if (c.Tag?.ToString() == "t2") c.Visible = show;
            };



            // ==================== 确定 / 取消 ====================
            var btnOk = new Button { Text = "确定", Left = 300, Top = 600, Width = 90, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Left = 400, Top = 600, Width = 90, DialogResult = DialogResult.Cancel };
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;


            // ==================== 直到点击确认后 ====================
            if (form.ShowDialog() != DialogResult.OK) return null;


            // ==================== 构建 TcpPlcConfig ====================
            string protoText = cmbProtocol.SelectedItem?.ToString() ?? ModbusTcp.ProtocolName;

            TcpPlcConfig config = new TcpPlcConfig();

            config.Link.PlcName = txtName.Text.Trim();
            config.Link.Ip = txtIp.Text.Trim();
            config.Link.Port = int.TryParse(txtPort.Text, out int p2) ? p2 : 502;
            config.Link.Protocol = protoText;
            config.Track.TrackCount = cmbTrack.SelectedIndex + 1;
            //config.Track.OneReady = chkS1.Checked;
            config.Track.OneWidthAddr.Address = txtW1.Text.Trim();
            config.Track.OneTriggerAddr.Address = txtTrig1.Text.Trim();
            config.Track.OneHasStatus = chkS1.Checked;
            config.Track.OneStatusAddr.Address = txtSA1.Text.Trim();
            config.Track.OneStatusDoneValue = ParseInt(txtDV1.Text, 1);
            //config.Track.TwoReady = chkS1.Checked;
            config.Track.TwoWidthAddr.Address = txtW2.Text.Trim();
            config.Track.TwoTriggerAddr.Address = txtTrig2.Text.Trim();
            config.Track.TwoHasStatus = chkS2.Checked;
            config.Track.TwoStatusAddr.Address = txtSA2.Text.Trim();
            config.Track.TwoStatusDoneValue = ParseInt(txtDV2.Text, 1);


            // 添加读写点
            foreach (var item in lstPoints.Items) {
                var parts = item.ToString().Split('|');
                if (parts.Length > 0) {
                    int val = parts.Length > 1 && int.TryParse(parts[1].Replace("值:", "").Trim(), out int v) ? v : 0;
                    string desc = parts.Length > 2 ? parts[2].Trim() : "";
                    config.RegisterSheet.Add(new PLC.IBase.IRegister {
                        Address = parts[0].Trim(),
                        TargetValue = val,
                        Description = desc
                    });
                }
            }

            // 保存扩展参数
            config.ExtraParams.Clear();
            foreach (ListViewItem item in lvParams.Items) {
                if (!string.IsNullOrWhiteSpace(item.Text))
                    config.ExtraParams[item.Text] = item.SubItems[1].Text ?? "";
            }

            return config;
        }

        // ==================== 辅助方法 ====================
        private static int ParseInt (string s, int def = 0) => int.TryParse(s, out int v) ? v : def;

        private static void AddSectionLabel (Control parent, string text, int x, int y, string tag = null) {
            parent.Controls.Add(new Label {
                Text = text,
                Left = x,
                Top = y,
                AutoSize = true,
                Tag = tag,
                Visible = tag == null,
                Font = new System.Drawing.Font("Microsoft YaHei", 9f, System.Drawing.FontStyle.Bold)
            });
        }

        private static void AddLabelAndText (Control parent, string labelText, int x, int y,
                                            out TextBox txt, string defaultText, string tag = null) {
            bool visible = tag == null;
            parent.Controls.Add(new Label {
                Text = labelText,
                Left = x,
                Top = y + 3,
                AutoSize = true,
                Tag = tag,
                Visible = visible
            });
            txt = new TextBox { Left = x + 100, Top = y, Width = 260, Text = defaultText, Tag = tag, Visible = visible };
            parent.Controls.Add(txt);
        }

        private static void AddExtraParamDialog (ListView lvParams) {
            using (var pf = new Form {
                Width = 360,
                Height = 200,
                Text = "添加扩展参数",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog
            }) {
                int py = 20;
                pf.Controls.Add(new Label { Text = "参数名:", Left = 20, Top = py + 5, AutoSize = true });
                var txtKey = new TextBox { Left = 90, Top = py, Width = 220 }; pf.Controls.Add(txtKey); py += 40;

                pf.Controls.Add(new Label { Text = "参数值:", Left = 20, Top = py + 5, AutoSize = true });
                var txtVal = new TextBox { Left = 90, Top = py, Width = 220 }; pf.Controls.Add(txtVal); py += 50;

                var btnOK = new Button { Text = "确定", Left = 140, Top = py, Width = 80, DialogResult = DialogResult.OK };
                pf.Controls.Add(btnOK);
                pf.AcceptButton = btnOK;

                if (pf.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtKey.Text)) {
                    var item = new ListViewItem(txtKey.Text.Trim());
                    item.SubItems.Add(txtVal.Text.Trim());
                    lvParams.Items.Add(item);
                }
            }
        }

        private static void EditExtraParamDialog (ListView lvParams) {
            if (lvParams.SelectedItems.Count == 0) return;
            var item = lvParams.SelectedItems[0];

            using (var pf = new Form {
                Width = 360,
                Height = 200,
                Text = "编辑扩展参数",
                StartPosition = FormStartPosition.CenterParent
            }) {
                int py = 20;
                pf.Controls.Add(new Label { Text = "参数名:", Left = 20, Top = py + 5, AutoSize = true });
                var txtKey = new TextBox { Left = 90, Top = py, Width = 220, Text = item.Text }; pf.Controls.Add(txtKey); py += 40;

                pf.Controls.Add(new Label { Text = "参数值:", Left = 20, Top = py + 5, AutoSize = true });
                var txtVal = new TextBox { Left = 90, Top = py, Width = 220, Text = item.SubItems[1].Text }; pf.Controls.Add(txtVal); py += 50;

                var btnOK = new Button { Text = "确定", Left = 140, Top = py, Width = 80, DialogResult = DialogResult.OK };
                pf.Controls.Add(btnOK);
                pf.AcceptButton = btnOK;

                if (pf.ShowDialog() == DialogResult.OK) {
                    item.Text = txtKey.Text.Trim();
                    item.SubItems[1].Text = txtVal.Text.Trim();
                }
            }
        }
    }
}