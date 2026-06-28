using Newtonsoft.Json;
using PLC.Models;
using PLC.Presenters;
using PLC.Services;
using S7.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PLC {
    public partial class Form1 : Form {
        //! ====================== 变量 ======================
        //! PLC列表
        private List<TcpPlcConfig> _plcList = new List<TcpPlcConfig>();
        //! 协议列表
        private List<string> _protocols = new List<string> { ModbusTcp.ProtocolName };
        //! 轮询定时器
        private Timer _pollTimer;
        //! 是否正在轮询
        private bool _polling = false;
        //! 当前IP
        public static string SelectedLocalIP { get; private set; } = "";
        //! 配置文件路径
        private static readonly string SavePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "plc_config.json");

        // 字段:工厂用接口类型声明(依赖抽象,不依赖具体)
        private readonly IPlcFactory _plcFactory;
        private readonly PlcStore _plcStore = new PlcStore();
        private PlcListPresenter _plcListPresenter;

        /// <summary>
        /// 窗体构造函数
        /// </summary>
        public Form1 () {
            //! 初始化窗体设计器
            InitializeComponent();

            // 创建唯一的工厂实例。内置协议在主程序自己的程序集里,所以传 typeof(Form1).Assembly
            _plcFactory = new PluginLoader(typeof(Form1).Assembly);
            _plcFactory.LoadPlugins();

            //! 事件绑定
            HookPlcListEvents();

            // 把工厂注入 Presenter(下一步 Presenter 会加这个参数)
            _plcListPresenter = new PlcListPresenter(this, _plcStore, _plcFactory);

            // 协议列表推给界面
            IShowProtocolList(_plcFactory.AvailableProtocols);

            _plcListPresenter.Initialize();
            //! 初始化点读取Task
            InitializeComm();
            //! 加载配置存储文件
            LoadFromFile();
            //! 扫描网络适配器
            LoadNetworkAdapters();
        }

        //! ===================== 事件 ======================
        /// <summary>
        /// 当前光标选中的列表格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentCellDirtyStateChanged (object sender, EventArgs e) {
            if (dgvPoints.IsCurrentCellDirty)
                dgvPoints.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        /// <summary>
        /// 网络适配器应用按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnApplyAdapter_Click (object sender, EventArgs e) {
            //! 获取选择适配器的字符串
            string selected = cmbAdapter.SelectedItem?.ToString() ?? "";

            //! 默认适配器
            if (cmbAdapter.SelectedIndex == 0) {
                SelectedLocalIP = "";
                ILog("已切换为默认网络适配器（自动选择）");
            } else {
                //! 解析IP地址
                int start = selected.LastIndexOf('(');
                int end = selected.LastIndexOf(')');
                //! 提取IP地址
                if (start > 0 && end > start) {
                    SelectedLocalIP = selected.Substring(start + 1, end - start - 1);
                    ILog($"已应用网络适配器: {selected}");
                }
            }
        }

        /// <summary>
        /// 关闭窗体时保存配置
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing (FormClosingEventArgs e) {
            //! 触发关闭事件
            //BtnDisconnect_Click(null, null);
            //! 保存配置文件
            SaveToFile();
            //! 关闭窗体
            base.OnFormClosing(e);
        }

        //! ====================== 辅助函数 ======================
        /// <summary>
        /// 追加日志到 RichTextBox
        /// </summary>
        private void AppendLog (string logText) {
            rtbLog.AppendText(logText);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadFromFile () {
            if (File.Exists(SavePath)) {
                try {
                    string json = File.ReadAllText(SavePath);
                    var list = JsonConvert.DeserializeObject<List<TcpPlcConfig>>(json);
                    if (list != null && list.Count > 0) {
                        _plcList = list;
                        //RefreshPLCList();
                        ILog($"📂 已加载 {_plcList.Count} 台PLC配置");
                        return;
                    }
                } catch { }
            }
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        private void SaveToFile () {
            try {
                foreach (var p in _plcList) p.Link.Client = null;
                string json = JsonConvert.SerializeObject(_plcList, Formatting.Indented);
                File.WriteAllText(SavePath, json);
            } catch { }
        }

        //! 加载扫描网络适配器
        private void LoadNetworkAdapters () {
            cmbAdapter.Items.Clear();
            cmbAdapter.Items.Add("默认（自动选择）");

            try {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                    //! 所有有 IP 的网卡
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses) {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                            string adapterName = !string.IsNullOrEmpty(ni.Description) ? ni.Description : ni.Name;
                            cmbAdapter.Items.Add($"{adapterName} ({addr.Address})");
                        }
                    }
                }
            } catch { }

            //! 默认选择第一个
            if (cmbAdapter.Items.Count != 0) cmbAdapter.SelectedIndex = 0;
        }


        //! ====================== 日志系统 ======================
        //private void Log (string msg) => LogHelper.Log(msg);
    }
}
