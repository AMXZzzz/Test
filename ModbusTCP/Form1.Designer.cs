using System.Windows.Forms;

namespace PLC {
    partial class Form1 {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose (bool disposing) {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent () {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnAddPLC = new System.Windows.Forms.Button();
            this.btnDeletePLC = new System.Windows.Forms.Button();
            this.btnConnectAll = new System.Windows.Forms.Button();
            this.btnMovePLCUp = new System.Windows.Forms.Button();
            this.btnMovePLCDown = new System.Windows.Forms.Button();
            this.btnAdjustWidth = new System.Windows.Forms.Button();
            this.dgvPLCList = new System.Windows.Forms.DataGridView();
            this.colPlcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlcIp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlcPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlcProtocol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlcStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.btnApplyIp = new System.Windows.Forms.Button();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnApplyPort = new System.Windows.Forms.Button();
            this.lblProtocol = new System.Windows.Forms.Label();
            this.cmbProtocol = new System.Windows.Forms.ComboBox();
            this.lblWidth1 = new System.Windows.Forms.Label();
            this.txtWidth1 = new System.Windows.Forms.TextBox();
            this.lblWidth2 = new System.Windows.Forms.Label();
            this.txtWidth2 = new System.Windows.Forms.TextBox();
            this.lblPointsTitle = new System.Windows.Forms.Label();
            this.lblTrackInfo = new System.Windows.Forms.Label();
            this.dgvPoints = new System.Windows.Forms.DataGridView();
            this.btnAddPoint = new System.Windows.Forms.Button();
            this.btnDeletePoint = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.lblAdapter = new System.Windows.Forms.Label();
            this.cmbAdapter = new System.Windows.Forms.ComboBox();
            this.btnApplyAdapter = new System.Windows.Forms.Button();
            this.colAddr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurVal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTgt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDataType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.colDesc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWrite = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPLCList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPoints)).BeginInit();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(20, 20);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(110, 34);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "连接选中";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(140, 20);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(110, 34);
            this.btnDisconnect.TabIndex = 1;
            this.btnDisconnect.Text = "断开全部";
            // 
            // btnAddPLC
            // 
            this.btnAddPLC.Location = new System.Drawing.Point(20, 62);
            this.btnAddPLC.Name = "btnAddPLC";
            this.btnAddPLC.Size = new System.Drawing.Size(110, 34);
            this.btnAddPLC.TabIndex = 2;
            this.btnAddPLC.Text = "新增PLC";
            // 
            // btnDeletePLC
            // 
            this.btnDeletePLC.ForeColor = System.Drawing.Color.Red;
            this.btnDeletePLC.Location = new System.Drawing.Point(140, 62);
            this.btnDeletePLC.Name = "btnDeletePLC";
            this.btnDeletePLC.Size = new System.Drawing.Size(110, 34);
            this.btnDeletePLC.TabIndex = 3;
            this.btnDeletePLC.Text = "删除选中PLC";
            // 
            // btnConnectAll
            // 
            this.btnConnectAll.Location = new System.Drawing.Point(260, 20);
            this.btnConnectAll.Name = "btnConnectAll";
            this.btnConnectAll.Size = new System.Drawing.Size(120, 34);
            this.btnConnectAll.TabIndex = 4;
            this.btnConnectAll.Text = "一键连接全部";
            // 
            // btnMovePLCUp
            // 
            this.btnMovePLCUp.Location = new System.Drawing.Point(730, 108);
            this.btnMovePLCUp.Name = "btnMovePLCUp";
            this.btnMovePLCUp.Size = new System.Drawing.Size(50, 62);
            this.btnMovePLCUp.TabIndex = 5;
            this.btnMovePLCUp.Text = "↑\n上移";
            // 
            // btnMovePLCDown
            // 
            this.btnMovePLCDown.Location = new System.Drawing.Point(730, 176);
            this.btnMovePLCDown.Name = "btnMovePLCDown";
            this.btnMovePLCDown.Size = new System.Drawing.Size(50, 62);
            this.btnMovePLCDown.TabIndex = 6;
            this.btnMovePLCDown.Text = "↓\n下移";
            // 
            // btnAdjustWidth
            // 
            this.btnAdjustWidth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(200)))), ((int)(((byte)(60)))));
            this.btnAdjustWidth.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.btnAdjustWidth.Location = new System.Drawing.Point(600, 20);
            this.btnAdjustWidth.Name = "btnAdjustWidth";
            this.btnAdjustWidth.Size = new System.Drawing.Size(110, 76);
            this.btnAdjustWidth.TabIndex = 11;
            this.btnAdjustWidth.Text = "一键调宽";
            this.btnAdjustWidth.UseVisualStyleBackColor = false;
            this.btnAdjustWidth.Click += new System.EventHandler(this.BtnAdjustWidth_Click);
            // 
            // dgvPLCList
            // 
            this.dgvPLCList.AllowUserToAddRows = false;
            this.dgvPLCList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPLCList.ColumnHeadersHeight = 32;
            this.dgvPLCList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colPlcName,
            this.colPlcIp,
            this.colPlcPort,
            this.colPlcProtocol,
            this.colPlcStatus});
            this.dgvPLCList.Location = new System.Drawing.Point(20, 108);
            this.dgvPLCList.Name = "dgvPLCList";
            this.dgvPLCList.ReadOnly = true;
            this.dgvPLCList.RowHeadersWidth = 62;
            this.dgvPLCList.Size = new System.Drawing.Size(700, 130);
            this.dgvPLCList.TabIndex = 12;
            // 
            // colPlcName
            // 
            this.colPlcName.HeaderText = "PLC名称";
            this.colPlcName.MinimumWidth = 8;
            this.colPlcName.Name = "colPlcName";
            this.colPlcName.ReadOnly = true;
            // 
            // colPlcIp
            // 
            this.colPlcIp.HeaderText = "IP地址";
            this.colPlcIp.MinimumWidth = 8;
            this.colPlcIp.Name = "colPlcIp";
            this.colPlcIp.ReadOnly = true;
            // 
            // colPlcPort
            // 
            this.colPlcPort.HeaderText = "端口";
            this.colPlcPort.MinimumWidth = 8;
            this.colPlcPort.Name = "colPlcPort";
            this.colPlcPort.ReadOnly = true;
            // 
            // colPlcProtocol
            // 
            this.colPlcProtocol.HeaderText = "协议";
            this.colPlcProtocol.MinimumWidth = 8;
            this.colPlcProtocol.Name = "colPlcProtocol";
            this.colPlcProtocol.ReadOnly = true;
            // 
            // colPlcStatus
            // 
            this.colPlcStatus.HeaderText = "状态";
            this.colPlcStatus.MinimumWidth = 8;
            this.colPlcStatus.Name = "colPlcStatus";
            this.colPlcStatus.ReadOnly = true;
            // 
            // lblIp
            // 
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(20, 255);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(71, 18);
            this.lblIp.TabIndex = 13;
            this.lblIp.Text = "PLC IP:";
            // 
            // txtIp
            // 
            this.txtIp.Location = new System.Drawing.Point(80, 251);
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(150, 28);
            this.txtIp.TabIndex = 14;
            // 
            // btnApplyIp
            // 
            this.btnApplyIp.Location = new System.Drawing.Point(238, 249);
            this.btnApplyIp.Name = "btnApplyIp";
            this.btnApplyIp.Size = new System.Drawing.Size(55, 28);
            this.btnApplyIp.TabIndex = 15;
            this.btnApplyIp.Text = "应用";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(310, 255);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(53, 18);
            this.lblPort.TabIndex = 16;
            this.lblPort.Text = "端口:";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(358, 251);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(75, 28);
            this.txtPort.TabIndex = 17;
            // 
            // btnApplyPort
            // 
            this.btnApplyPort.Location = new System.Drawing.Point(441, 249);
            this.btnApplyPort.Name = "btnApplyPort";
            this.btnApplyPort.Size = new System.Drawing.Size(55, 28);
            this.btnApplyPort.TabIndex = 18;
            this.btnApplyPort.Text = "应用";
            // 
            // lblProtocol
            // 
            this.lblProtocol.AutoSize = true;
            this.lblProtocol.Location = new System.Drawing.Point(515, 255);
            this.lblProtocol.Name = "lblProtocol";
            this.lblProtocol.Size = new System.Drawing.Size(53, 18);
            this.lblProtocol.TabIndex = 19;
            this.lblProtocol.Text = "协议:";
            // 
            // cmbProtocol
            // 
            this.cmbProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProtocol.Location = new System.Drawing.Point(563, 251);
            this.cmbProtocol.Name = "cmbProtocol";
            this.cmbProtocol.Size = new System.Drawing.Size(140, 26);
            this.cmbProtocol.TabIndex = 20;
            // 
            // lblWidth1
            // 
            this.lblWidth1.AutoSize = true;
            this.lblWidth1.Location = new System.Drawing.Point(430, 28);
            this.lblWidth1.Name = "lblWidth1";
            this.lblWidth1.Size = new System.Drawing.Size(98, 18);
            this.lblWidth1.TabIndex = 7;
            this.lblWidth1.Text = "轨道1宽度:";
            // 
            // txtWidth1
            // 
            this.txtWidth1.Location = new System.Drawing.Point(520, 24);
            this.txtWidth1.Name = "txtWidth1";
            this.txtWidth1.Size = new System.Drawing.Size(65, 28);
            this.txtWidth1.TabIndex = 8;
            this.txtWidth1.Text = "300.0";
            // 
            // lblWidth2
            // 
            this.lblWidth2.AutoSize = true;
            this.lblWidth2.Location = new System.Drawing.Point(430, 70);
            this.lblWidth2.Name = "lblWidth2";
            this.lblWidth2.Size = new System.Drawing.Size(98, 18);
            this.lblWidth2.TabIndex = 9;
            this.lblWidth2.Text = "轨道2宽度:";
            // 
            // txtWidth2
            // 
            this.txtWidth2.Location = new System.Drawing.Point(520, 66);
            this.txtWidth2.Name = "txtWidth2";
            this.txtWidth2.Size = new System.Drawing.Size(65, 28);
            this.txtWidth2.TabIndex = 10;
            this.txtWidth2.Text = "300.0";
            // 
            // lblPointsTitle
            // 
            this.lblPointsTitle.AutoSize = true;
            this.lblPointsTitle.Location = new System.Drawing.Point(20, 320);
            this.lblPointsTitle.Name = "lblPointsTitle";
            this.lblPointsTitle.Size = new System.Drawing.Size(188, 18);
            this.lblPointsTitle.TabIndex = 24;
            this.lblPointsTitle.Text = "当前PLC 读写点列表：";
            // 
            // lblTrackInfo
            // 
            this.lblTrackInfo.Location = new System.Drawing.Point(0, 0);
            this.lblTrackInfo.Name = "lblTrackInfo";
            this.lblTrackInfo.Size = new System.Drawing.Size(100, 23);
            this.lblTrackInfo.TabIndex = 0;
            // 
            // dgvPoints
            // 
            this.dgvPoints.AllowUserToAddRows = false;
            this.dgvPoints.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPoints.ColumnHeadersHeight = 32;
            this.dgvPoints.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAddr,
            this.colCurVal,
            this.colTgt,
            this.colDataType,
            this.colDesc,
            this.colWrite});
            this.dgvPoints.Location = new System.Drawing.Point(20, 340);
            this.dgvPoints.Name = "dgvPoints";
            this.dgvPoints.RowHeadersWidth = 62;
            this.dgvPoints.Size = new System.Drawing.Size(740, 200);
            this.dgvPoints.TabIndex = 25;
            // 
            // btnAddPoint
            // 
            this.btnAddPoint.Location = new System.Drawing.Point(20, 550);
            this.btnAddPoint.Name = "btnAddPoint";
            this.btnAddPoint.Size = new System.Drawing.Size(100, 34);
            this.btnAddPoint.TabIndex = 26;
            this.btnAddPoint.Text = "添加点";
            this.btnAddPoint.Click += new System.EventHandler(this.BtnAddPoint_Click);
            // 
            // btnDeletePoint
            // 
            this.btnDeletePoint.Location = new System.Drawing.Point(130, 550);
            this.btnDeletePoint.Name = "btnDeletePoint";
            this.btnDeletePoint.Size = new System.Drawing.Size(110, 34);
            this.btnDeletePoint.TabIndex = 27;
            this.btnDeletePoint.Text = "删除选中点";
            this.btnDeletePoint.Click += new System.EventHandler(this.BtnDeletePoint_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(20, 595);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(740, 120);
            this.rtbLog.TabIndex = 28;
            this.rtbLog.Text = "";
            // 
            // lblAdapter
            // 
            this.lblAdapter.AutoSize = true;
            this.lblAdapter.Location = new System.Drawing.Point(20, 286);
            this.lblAdapter.Name = "lblAdapter";
            this.lblAdapter.Size = new System.Drawing.Size(107, 18);
            this.lblAdapter.TabIndex = 21;
            this.lblAdapter.Text = "网络适配器:";
            // 
            // cmbAdapter
            // 
            this.cmbAdapter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAdapter.DropDownWidth = 480;
            this.cmbAdapter.Location = new System.Drawing.Point(110, 282);
            this.cmbAdapter.Name = "cmbAdapter";
            this.cmbAdapter.Size = new System.Drawing.Size(320, 26);
            this.cmbAdapter.TabIndex = 22;
            // 
            // btnApplyAdapter
            // 
            this.btnApplyAdapter.Location = new System.Drawing.Point(440, 281);
            this.btnApplyAdapter.Name = "btnApplyAdapter";
            this.btnApplyAdapter.Size = new System.Drawing.Size(55, 28);
            this.btnApplyAdapter.TabIndex = 23;
            this.btnApplyAdapter.Text = "应用";
            this.btnApplyAdapter.Click += new System.EventHandler(this.BtnApplyAdapter_Click);
            // 
            // colAddr
            // 
            this.colAddr.FillWeight = 80F;
            this.colAddr.HeaderText = "地址";
            this.colAddr.MinimumWidth = 8;
            this.colAddr.Name = "colAddr";
            // 
            // colCurVal
            // 
            this.colCurVal.FillWeight = 80F;
            this.colCurVal.HeaderText = "当前值";
            this.colCurVal.MinimumWidth = 8;
            this.colCurVal.Name = "colCurVal";
            // 
            // colTgt
            // 
            this.colTgt.FillWeight = 80F;
            this.colTgt.HeaderText = "目标值";
            this.colTgt.MinimumWidth = 8;
            this.colTgt.Name = "colTgt";
            // 
            // colDataType
            // 
            dataGridViewCellStyle1.NullValue = "Int16";
            this.colDataType.DefaultCellStyle = dataGridViewCellStyle1;
            this.colDataType.FillWeight = 70F;
            this.colDataType.HeaderText = "类型";
            this.colDataType.Items.AddRange(new object[] {
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Coil"});
            this.colDataType.MinimumWidth = 8;
            this.colDataType.Name = "colDataType";
            // 
            // colDesc
            // 
            this.colDesc.FillWeight = 200F;
            this.colDesc.HeaderText = "描述";
            this.colDesc.MinimumWidth = 8;
            this.colDesc.Name = "colDesc";
            // 
            // colWrite
            // 
            this.colWrite.HeaderText = "写入";
            this.colWrite.MinimumWidth = 8;
            this.colWrite.Name = "colWrite";
            this.colWrite.Text = "写";
            this.colWrite.UseColumnTextForButtonValue = true;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(800, 740);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnAddPLC);
            this.Controls.Add(this.btnDeletePLC);
            this.Controls.Add(this.btnConnectAll);
            this.Controls.Add(this.btnMovePLCUp);
            this.Controls.Add(this.btnMovePLCDown);
            this.Controls.Add(this.lblWidth1);
            this.Controls.Add(this.txtWidth1);
            this.Controls.Add(this.lblWidth2);
            this.Controls.Add(this.txtWidth2);
            this.Controls.Add(this.btnAdjustWidth);
            this.Controls.Add(this.dgvPLCList);
            this.Controls.Add(this.lblIp);
            this.Controls.Add(this.txtIp);
            this.Controls.Add(this.btnApplyIp);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.btnApplyPort);
            this.Controls.Add(this.lblProtocol);
            this.Controls.Add(this.cmbProtocol);
            this.Controls.Add(this.lblAdapter);
            this.Controls.Add(this.cmbAdapter);
            this.Controls.Add(this.btnApplyAdapter);
            this.Controls.Add(this.lblPointsTitle);
            this.Controls.Add(this.dgvPoints);
            this.Controls.Add(this.btnAddPoint);
            this.Controls.Add(this.btnDeletePoint);
            this.Controls.Add(this.rtbLog);
            this.Name = "Form1";
            this.Text = "多PLC上位机";
            ((System.ComponentModel.ISupportInitialize)(this.dgvPLCList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPoints)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // ==================== 控件声明 ====================
        private Button btnConnect;
        private Button btnDisconnect;
        private Button btnAddPLC;
        private Button btnDeletePLC;
        private Button btnConnectAll;
        private Button btnMovePLCUp;
        private Button btnMovePLCDown;
        private Button btnAdjustWidth;
        private Button btnAddPoint;
        private Button btnDeletePoint;
        private Button btnApplyIp;
        private Button btnApplyPort;
        private Button btnApplyAdapter;

        private Label lblIp;
        private Label lblPort;
        private Label lblProtocol;
        private Label lblWidth1;
        private Label lblWidth2;
        private Label lblPointsTitle;
        private Label lblTrackInfo;
        private Label lblAdapter;

        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtWidth1;
        private TextBox txtWidth2;

        private ComboBox cmbProtocol;
        private ComboBox cmbAdapter;

        private DataGridView dgvPLCList;
        private DataGridView dgvPoints;
        private RichTextBox rtbLog;

        private DataGridViewTextBoxColumn colPlcName;
        private DataGridViewTextBoxColumn colPlcIp;
        private DataGridViewTextBoxColumn colPlcPort;
        private DataGridViewTextBoxColumn colPlcProtocol;
        private DataGridViewTextBoxColumn colPlcStatus;
        private DataGridViewTextBoxColumn colAddr;
        private DataGridViewTextBoxColumn colCurVal;
        private DataGridViewTextBoxColumn colTgt;
        private DataGridViewComboBoxColumn colDataType;
        private DataGridViewTextBoxColumn colDesc;
        private DataGridViewButtonColumn colWrite;
    }
}