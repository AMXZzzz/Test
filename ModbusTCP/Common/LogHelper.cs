using System;
using System.Collections.Generic;

namespace PLC {
    /// <summary>
    /// 日志帮助类（已与UI解耦）
    /// </summary>
    public static class LogHelper {
        private static readonly List<string> _logList = new List<string>();
        private static bool _isDebug = false;

        /// <summary>
        /// 日志新增事件（推荐使用此方式订阅）
        /// </summary>
        public static event Action<string> LogAdded;

        /// <summary>
        /// 获取所有日志（只读）
        /// </summary>
        public static IReadOnlyList<string> Logs => _logList.AsReadOnly();

        /// <summary>
        /// 初始化日志系统（可选）
        /// </summary>
        public static void Initialize (bool debug = false) {
            _isDebug = debug;
        }

        /// <summary>
        /// 普通日志
        /// </summary>
        public static void Log (string msg) {
            string logText = $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
            _logList.Add(logText);

            // 触发事件（解耦核心）
            LogAdded?.Invoke(logText);
        }

        /// <summary>
        /// 调试日志
        /// </summary>
        public static void DebugLog (string msg) {
            if (!_isDebug) return;

            string logText = $"[{DateTime.Now:HH:mm:ss}] [调试] {msg}\n";
            _logList.Add(logText);

            LogAdded?.Invoke(logText);
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public static void Clear () {
            _logList.Clear();
        }
    }
}