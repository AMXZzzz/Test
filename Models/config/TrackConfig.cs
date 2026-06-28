using PLC.IBase;

namespace PLC.Models {
    /// <summary>
    /// 轨道配置。描述一台 PLC 的轨道相关寄存器与状态。
    /// </summary>
    public class TrackConfig {
        /// <summary>轨道数量</summary>
        public int TrackCount { get; set; } = 1;

        // ===== 轨道一 =====
        public IRegister OneReady { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public IRegister OneWidthAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public IRegister OneTriggerAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public bool OneHasStatus { get; set; } = false;
        public IRegister OneStatusAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public int OneStatusDoneValue { get; set; } = 1;

        // ===== 轨道二 =====
        public IRegister TwoReady { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public IRegister TwoWidthAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public IRegister TwoTriggerAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public bool TwoHasStatus { get; set; } = false;
        public IRegister TwoStatusAddr { get; set; } = new IRegister { DataType = RegisterDataType.Int32 };
        public int TwoStatusDoneValue { get; set; } = 1;
    }
}