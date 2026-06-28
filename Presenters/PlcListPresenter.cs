using PLC.Presenters.Views;
using PLC.IBase;
using PLC.Models;
using System;

namespace PLC.Presenters {
    /// <summary>
    /// PLC 列表的表现逻辑:订阅视图动作,操作 PlcStore,再驱动视图更新。
    /// 不持有任何 UI 控件,可独立测试。
    /// </summary>
    public class PlcListPresenter {

        //! ============== 变量 :界面和数据   =============
        private readonly IPlcListView _view;            //! 间接与UI交互
        private readonly PlcStore _store;               //! 底层数据模块PlcStore


        //! ============== 初始化 ==============
        public PlcListPresenter (IPlcListView view, PlcStore store) {
            _view = view;
            _store = store;

            // 绑定View动作
            _view.AddPlcEvent += OnAddPlc;
            _view.DeletePlcEvent += OnDeletePlc;
            _view.MoveUpRequested += OnMoveUp;
            _view.MoveDownRequested += OnMoveDown;
            _view.ApplyIpRequested += OnApplyIp;
            _view.ApplyPortRequested += OnApplyPort;
            _view.PlcSelectionChanged += OnSelectionChanged;
            _view.ProtocolChangeRequested += OnProtocolChange;

            // 数据变了就刷新列表显示
            _store.Changed += (s, e) => _view.ShowPlcList(_store.Plcs);
        }

        /// <summary>
        /// 初始化: 首次把数据推给视图
        /// </summary>
        public void Initialize () {
            _view.ShowPlcList(_store.Plcs);
        }

        // ==================== 动作处理 ====================
        /// <summary>
        /// 删除PLC配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeletePlc (object sender, EventArgs e) {
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) { _view.Log("请先选中要删除的PLC"); return; }

            var config = _store.GetAt(idx);
            if (!_view.Confirm($"确定删除 [{config.Link.PlcName}]？\n此操作不可恢复。", "删除确认"))
                return;

            // 断开后再移除
            config.Link.Client?.Disconnect();
            config.Link.Client = null;
            _store.RemoveAt(idx);   // 这会触发 Changed → 视图自动刷新

            // 删除后重新定位选中行
            if (_store.Count > 0)
                _view.SelectPlc(Math.Min(idx, _store.Count - 1));

            _view.Log($"🗑️ 已删除 PLC: {config.Link.PlcName}");
        }

        /// <summary>
        /// PLC位置移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMoveUp (object sender, EventArgs e) => Move(-1);
        private void OnMoveDown (object sender, EventArgs e) => Move(1);

        /// <summary>
        /// 应用设置IP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplyIp (object sender, EventArgs e) {
            //! 选择校验
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) { _view.Log("请先选中PLC"); return; }
            var config = _store.GetAt(idx);

            //! IP校验
            string newIp = _view.IpText.Trim();
            if (!IsValidIp(newIp)) {
                _view.Log($"IP格式无效: {newIp}"); return;
            }

            //! 连接校验
            if (config.Link.Client != null) {
                _view.Log("请先断开连接后再修改IP"); return;
            }
            //! 应用修改
            config.Link.Ip = newIp;
            _store.NotifyChanged();             //! 属性数据, 会自动触发UI刷新
            _view.Log($"✅ [{config.Link.PlcName}] IP已更新为 {newIp}");
        }

        /// <summary>
        /// 设置端口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplyPort (object sender, EventArgs e) {
            //! 选择校验
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) { _view.Log("请先选中PLC"); return; }
            var config = _store.GetAt(idx);

            //! 端口校验
            if (!int.TryParse(_view.PortText.Trim(), out int port) || port <= 0 || port > 65535) {
                _view.Log("端口必须是 1~65535 的整数"); return;
            }

            //! 连接校验
            if (config.Link.Client != null) {
                _view.Log("请先断开连接后再修改端口"); return;
            }

            //! 应用修改
            config.Link.Port = port;
            _store.NotifyChanged();             //! 属性数据, 会自动触发UI刷新
            _view.Log($"✅ [{config.Link.PlcName}] 端口已更新为 {port}");
        }

        /// <summary>
        /// PLC协议切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProtocolChange (object sender, EventArgs e) {
            //! 选择校验
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) return;
            var config = _store.GetAt(idx);

            //! 连接校验
            string newProto = _view.SelectedProtocol;
            if (string.IsNullOrEmpty(newProto) || newProto == config.Link.Protocol) return;
            if (config.Link.Client != null) {
                _view.Log("请先断开连接后再修改协议");
                return;
            }

            //! 应用修改
            config.Link.Protocol = newProto;
            _store.NotifyChanged();             //! 属性数据, 会自动触发UI刷新
            _view.Log($"✅ [{config.Link.PlcName}] 协议已更新为 {newProto}");
        }

        /// <summary>
        /// PLC选择变更:把选中 PLC 的详情回填到编辑区
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectionChanged (object sender, EventArgs e) {
            //! 选择校验
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) return;
            var config = _store.GetAt(idx);

            // 把选中 PLC 的详情回填到编辑区
            _view.ShowPlcDetail(config.Link.Ip, config.Link.Port.ToString(), config.Link.Protocol);
        }

        // OnAddPlc 涉及弹对话框拿新配置,放下一步和连接逻辑一起处理
        private void OnAddPlc (object sender, EventArgs e) {
            // 占位:下一步实现(需要 View 提供"显示添加对话框"的能力)
            _view.Log("(添加 PLC 待接入对话框)");
        }

        //! ========================== 辅助函数 ==========================
        /// <summary>
        /// 移动PLC位置
        /// </summary>
        /// <param name="direction"></param>
        private void Move (int direction) {
            int idx = _view.SelectedPlcIndex;
            if (idx < 0) return;
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= _store.Count) return;

            _store.Swap(idx, newIdx);   // 触发 Changed → 刷新
            _view.SelectPlc(newIdx); // 让选中跟着移动
        }

        /// <summary>
        /// 校验 IP 字符串是否合法
        /// </summary>
        private static bool IsValidIp (string ip)
            => System.Net.IPAddress.TryParse(ip, out _);
    }
}