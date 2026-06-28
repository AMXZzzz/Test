using System;
using System.Collections.Generic;

namespace PLC.Models {
    /// <summary>
    /// PLC 配置仓库:全应用唯一的 PLC 列表数据源。
    /// 所有对 PLC 列表的增删改查都经过这里,保证数据只有一个真相。
    /// </summary>
    public class PlcStore {
        private readonly List<TcpPlcConfig> _plcs = new List<TcpPlcConfig>();

        /// <summary>
        /// 只读访问列表(外部不能直接改集合,只能通过本类方法)
        /// </summary>
        public IReadOnlyList<TcpPlcConfig> Plcs => _plcs;

        public void Add (TcpPlcConfig plc) {
            _plcs.Add(plc);
            OnChanged();
        }
        /// <summary>
        /// 删除指定索引的 PLC 配置
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt (int index) {
            if (index < 0 || index >= _plcs.Count) return;
            _plcs.RemoveAt(index);
            //! 刷新
            OnChanged();
        }

        /// <summary>
        /// 获取指定索引的 PLC 配置
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TcpPlcConfig GetAt (int index)
            => (index >= 0 && index < _plcs.Count) ? _plcs[index] : null;

        /// <summary>
        /// 当前 PLC 配置列表数量
        /// </summary>
        public int Count => _plcs.Count;

        /// <summary>
        /// 交换两个位置(上移/下移用)
        /// </summary>
        public void Swap (int a, int b) {
            if (a < 0 || b < 0 || a >= _plcs.Count || b >= _plcs.Count) return;
            (_plcs[a], _plcs[b]) = (_plcs[b], _plcs[a]);
            OnChanged();
        }

        /// <summary>
        /// 整体替换(从配置文件加载时用)
        /// </summary>
        /// <param name="plcs"></param>
        public void ReplaceAll (IEnumerable<TcpPlcConfig> plcs) {
            _plcs.Clear();
            _plcs.AddRange(plcs);
            OnChanged();
        }

        /// <summary>
        /// 触发 Changed 事件
        /// </summary>
        private void OnChanged () => Changed?.Invoke(this, EventArgs.Empty);


        /// <summary>
        /// 列表发生变化时触发(增、删、移动后)
        /// </summary>
        public event EventHandler Changed;
    }
}