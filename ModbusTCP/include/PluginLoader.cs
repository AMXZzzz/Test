using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PLC {
    /// <summary>
    /// 插件加载器
    /// </summary>
    public static class PluginLoader {
        //! ====================== 变量 ======================
        //! 插件目录
        private static readonly Dictionary<string, Type> _pluginTypes =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 加载插件
        /// </summary>
        public static void LoadPlugins () {
            _pluginTypes.Clear();

            // ===== 扫描外部插件 =====
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginDir)) {
                Directory.CreateDirectory(pluginDir);
                LogHelper.Log("✅ 已创建 Plugins 文件夹，请把协议DLL放入其中");
                LogHelper.Log($"[插件] 共加载 {_pluginTypes.Count} 个协议");
                return;
            }

            LogHelper.Log($"[插件] 开始扫描: {pluginDir}");
            //! 扫描所有 DLL 文件
            foreach (string dllPath in Directory.GetFiles(pluginDir, "*.dll")) {
                try {
                    //! 加载DLL
                    var assembly = Assembly.LoadFrom(dllPath);
                    //! 获取DLL内所有的公开对象
                    foreach (var type in assembly.GetTypes()) {
                        //! 检查类型是否实现了 IProtocol 接口
                        if (!typeof(PLC.IBase.IPlcBase).IsAssignableFrom(type)
                            || type.IsInterface || type.IsAbstract) continue;

                        //! 临时实例取协议名
                        if (Activator.CreateInstance(type) is PLC.IBase.IPlcBase instance) {
                            string name = instance.GetProtocolName();
                            _pluginTypes[name] = type;
                            LogHelper.Log($"[插件] 加载: {name} → {type.Name}");
                        }
                    }
                } catch (Exception ex) {
                    LogHelper.Log($"[插件加载失败] {Path.GetFileName(dllPath)}: {ex.Message}");
                }
            }

            LogHelper.Log($"[插件] 共加载 {_pluginTypes.Count} 个协议");
        }

        /// <summary>
        /// 获取指定协议的推荐扩展参数
        /// </summary>
        /// <param name="protocolName">协议名称</param>
        /// <returns>返回推荐的扩展参数字典</returns>
        public static Dictionary<string, string> GetRecommendedExtraParams (string protocolName) {
            //! 检查协议是否已加载
            if (!_pluginTypes.TryGetValue(protocolName, out Type type))
                return new Dictionary<string, string>();
            //! 创建实例并获取推荐参数
            try {
                if (Activator.CreateInstance(type) is PLC.IBase.IPlcBase instance) {
                    return instance.GetRecommendedExtraParams();
                }
            } catch { }

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取所有已加载的协议名称
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailableProtocolNames ()
            => new List<string>(_pluginTypes.Keys);

        /// <summary>
        /// 获取指定协议的对象
        /// </summary>
        /// <param name="protocol">协议名称</param>
        /// <returns>返回协议实例</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static PLC.IBase.IPlcBase CreateClient (string protocol) {
            //! 检查协议是否已加载
            if (!_pluginTypes.TryGetValue(protocol, out Type type))
                throw new NotSupportedException($"未找到协议插件: {protocol}");

            //! 创建实例
            var ctorEmpty = type.GetConstructor(Type.EmptyTypes);

            //! 创建成功
            if (ctorEmpty != null)
                return (PLC.IBase.IPlcBase)ctorEmpty.Invoke(null);

            //! 创建实例失败
            throw new NotSupportedException($"协议 {protocol} 缺少无参构造函数");
        }
    }
}