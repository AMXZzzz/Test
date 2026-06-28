using System.Collections.Generic;
using PLC.IBase;

namespace PLC.Services {
    /// <summary>
    /// PLC 协议工厂:按协议名创建协议实例,并提供可用协议列表。
    /// 上层(Presenter)依赖此抽象,不依赖具体的插件加载实现。
    /// </summary>
    public interface IPlcFactory {
        /// <summary>所有可用的协议名称</summary>
        IReadOnlyList<string> AvailableProtocols { get; }

        /// <summary>
        /// 按协议名创建一个全新的协议实例
        /// </summary>
        IPlcBase Create (string protocolName);

        /// <summary>
        /// 获取指定协议的推荐扩展参数
        /// </summary>
        IReadOnlyDictionary<string, string> GetRecommendedExtraParams (string protocolName);

        /// <summary>
        /// 加载/重新加载所有插件
        /// </summary>
        void LoadPlugins ();
    }
}