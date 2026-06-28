using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PLC.Models;
using PLC.Presenters.Views;

namespace PLC {
    public partial class Form1 : IPlcListView {

        // ============ 属性:把控件状态读出来给 Presenter ============

        //! 选择的 PLC 索引, 来自 ListBox.SelectedIndex
        public int SelectedPlcIndex => dgvPLCList.CurrentRow?.Index ?? -1;

        //!  当前 PLC 的 IP, 来自 TextBox.Text
        public string IpText => txtIp.Text;

        //! 当前 PLC 的端口, 来自 TextBox.Text
        public string PortText => txtPort.Text;

        //! 当前 PLC 的协议, 来自 ComboBox.SelectedItem
        public string SelectedProtocol => cmbProtocol.SelectedItem?.ToString() ?? "";

        // ============ 方法:Presenter 让界面显示什么 ============
        /// <summary>
        /// 显示PLC列表
        /// </summary>
        /// <param name="plcs"></param>
        public void ShowPlcList (IReadOnlyList<TcpPlcConfig> plcs) {
            // 刷新时摘掉选择事件,防止误触发
            dgvPLCList.SelectionChanged -= OnPlcSelectionChanged;

            //! 刷新
            dgvPLCList.Rows.Clear();
            foreach (var p in plcs) {
                string status = p.Link.Client?.IsConnected == true ? "已连接" : "断开";
                dgvPLCList.Rows.Add(p.Link.PlcName, p.Link.Ip, p.Link.Port, p.Link.Protocol, status);
            }
            //! 重新绑定事件
            dgvPLCList.SelectionChanged += OnPlcSelectionChanged;
        }


        /// <summary>
        /// 显示PLC详情
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        public void ShowPlcDetail (string ip, string port, string protocol) {
            //! 回填到UI编辑区
            txtIp.Text = ip;
            txtPort.Text = port;

            // 改协议下拉时别触发协议变更事件
            cmbProtocol.SelectedIndexChanged -= OnProtocolChanged;
            //! 回填协议
            int i = cmbProtocol.Items.IndexOf(protocol);
            cmbProtocol.SelectedIndex = i >= 0 ? i : 0;
            //! 重新绑定事件
            cmbProtocol.SelectedIndexChanged += OnProtocolChanged;
        }

        /// <summary>
        /// 显示刷新协议列表
        /// </summary>
        /// <param name="protocols"></param>
        public void ShowProtocolList (IReadOnlyList<string> protocols) {
            //! 刷新协议下拉列表
            cmbProtocol.Items.Clear();
            foreach (var p in protocols)
                cmbProtocol.Items.Add(p);
        }


        /// <summary>
        /// 选择PLC
        /// </summary>
        /// <param name="index"></param>
        public void SelectPlc (int index) {
            //! 索引校验
            if (index < 0 || index >= dgvPLCList.Rows.Count) return;
            //! 选中指定行
            dgvPLCList.Rows[index].Selected = true;
            dgvPLCList.CurrentCell = dgvPLCList.Rows[index].Cells[0];
        }

        /// <summary>
        /// 弹框确认事件
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        public bool Confirm (string message, string caption)
            => MessageBox.Show(message, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;

        /// <summary>
        /// 显示日志信息
        /// </summary>
        /// <param name="message"></param>
        public void Log (string message) => LogHelper.Log(message);

        // ============ 事件:接口要求的事件 + 控件转发 ============

        public event EventHandler AddPlcEvent;
        public event EventHandler DeletePlcEvent;
        public event EventHandler ConnectRequested;
        public event EventHandler ConnectAllRequested;
        public event EventHandler DisconnectRequested;
        public event EventHandler DisconnectAllRequested;
        public event EventHandler MoveUpRequested;
        public event EventHandler MoveDownRequested;
        public event EventHandler ApplyIpRequested;
        public event EventHandler ApplyPortRequested;
        public event EventHandler PlcSelectionChanged;
        public event EventHandler ProtocolChangeRequested;

        /// <summary>
        /// 初始化控件事件 → 接口事件(纯转发,在初始化时挂这些)
        /// </summary>
        private void HookPlcListEvents () {
            btnAddPLC.Click += (s, e) => AddPlcEvent?.Invoke(this, e);
            btnDeletePLC.Click += (s, e) => DeletePlcEvent?.Invoke(this, e);
            btnConnect.Click += (s, e) => ConnectRequested?.Invoke(this, e);
            btnConnectAll.Click += (s, e) => ConnectAllRequested?.Invoke(this, e);
            btnDisconnect.Click += (s, e) => DisconnectRequested?.Invoke(this, e);
            btnMovePLCUp.Click += (s, e) => MoveUpRequested?.Invoke(this, e);
            btnMovePLCDown.Click += (s, e) => MoveDownRequested?.Invoke(this, e);
            btnApplyIp.Click += (s, e) => ApplyIpRequested?.Invoke(this, e);
            btnApplyPort.Click += (s, e) => ApplyPortRequested?.Invoke(this, e);
                
            dgvPLCList.SelectionChanged += OnPlcSelectionChanged;
            cmbProtocol.SelectedIndexChanged += OnProtocolChanged;
        }

        /// <summary>
        /// PLC 列表选择变更事件
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnPlcSelectionChanged (object s, EventArgs e)
            => PlcSelectionChanged?.Invoke(this, e);

        /// <summary>
        /// 协议下拉变更事件
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnProtocolChanged (object s, EventArgs e)
            => ProtocolChangeRequested?.Invoke(this, e);
    }
}