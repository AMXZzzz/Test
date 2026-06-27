using PLC.IBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PLC {
    /// <summary>
    /// 插件元数据,扫描期一次性读出,避免重复实例化
    /// </summary>
    public class PluginInfo {
        public string ProtocolName { get; set; }
        public Type ImplType { get; set; }
        public bool IsBuiltIn { get; set; }
    }

    /// <summary>
    /// 插件加载器(全应用唯一)。内置协议保证始终可用,外部协议从 Plugins 目录扫描。
    /// </summary>
    public static class PluginLoader {
        //! 协议名 → 元数据。大小写不敏感。
        private static readonly Dictionary<string, PluginInfo> _plugins =
            new Dictionary<string, PluginInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 加载所有协议:先注册内置,再扫描外部插件目录。内置协议不会被外部同名插件覆盖。
        /// </summary>
        public static void LoadPlugins () {
            _plugins.Clear();

            //! ========== 1. 先注册内置协议 ==========
            RegisterBuiltInProtocols();

            //! ========== 2. 再扫描外部插件目录 ==========
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginDir)) {
                Directory.CreateDirectory(pluginDir);
                LogHelper.Log("✅ 已创建 Plugins 文件夹，请把协议DLL放入其中");
                LogHelper.Log($"[插件] 共加载 {_plugins.Count} 个协议");
                return;
            }

            LogHelper.Log($"[插件] 开始扫描: {pluginDir}");
            foreach (string dllPath in Directory.GetFiles(pluginDir, "*.dll")) {
                try {
                    var assembly = Assembly.LoadFrom(dllPath);
                    ScanAssembly(assembly, Path.GetFileName(dllPath), isBuiltIn: false);
                } catch (Exception ex) {
                    LogHelper.Log($"[插件加载失败] {Path.GetFileName(dllPath)}: {ex.Message}");
                }
            }

            LogHelper.Log($"[插件] 共加载 {_plugins.Count} 个协议");
        }

        /// <summary>
        /// 获取所有已加载的协议名称
        /// </summary>
        public static List<string> GetAvailableProtocolNames ()
            => new List<string>(_plugins.Keys);

        /// <summary>
        /// 获取指定协议的推荐扩展参数(仅选协议时调用一次,可接受实例化)
        /// </summary>
        public static Dictionary<string, string> GetRecommendedExtraParams (string protocolName) {
            if (!_plugins.TryGetValue(protocolName, out PluginInfo info))
                return new Dictionary<string, string>();
            try {
                if (Activator.CreateInstance(info.ImplType) is IPlcBase instance)
                    return instance.GetRecommendedExtraParams();
            } catch { }
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 按协议名创建一个全新的实例
        /// </summary>
        public static IPlcBase CreateClient (string protocol) {
            if (!_plugins.TryGetValue(protocol, out PluginInfo info))
                throw new NotSupportedException($"未找到协议插件: {protocol}");

            var ctorEmpty = info.ImplType.GetConstructor(Type.EmptyTypes);
            if (ctorEmpty != null)
                return (IPlcBase)ctorEmpty.Invoke(null);

            throw new NotSupportedException($"协议 {protocol} 缺少无参构造函数");
        }

        //! ************************************ 辅助方法 ************************************

        /// <summary>
        /// 注册内置协议:扫描主程序自身的程序集
        /// </summary>
        private static void RegisterBuiltInProtocols () {
            var selfAssembly = typeof(ModbusTcp).Assembly;
            ScanAssembly(selfAssembly, "(内置)", isBuiltIn: true);
            LogHelper.Log("[插件] 已注册内置协议");
        }

        /// <summary>
        /// 扫描程序集,通过 [Protocol] 特性识别协议插件(不实例化)
        /// </summary>
        private static void ScanAssembly (Assembly assembly, string source, bool isBuiltIn) {
            Type[] types;
            try {
                types = assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                types = Array.FindAll(ex.Types, t => t != null);
                LogHelper.Log($"[插件] {source} 部分类型加载失败,已跳过缺失项");
            }

            foreach (var type in types) {
                if (!typeof(IPlcBase).IsAssignableFrom(type)
                    || type.IsInterface || type.IsAbstract) continue;

                var attr = type.GetCustomAttribute<ProtocolAttribute>();
                if (attr == null) continue;   // 没打特性就跳过,正常

                //! 内置协议不可被外部同名插件覆盖
                if (_plugins.TryGetValue(attr.Name, out PluginInfo existing)
                    && existing.IsBuiltIn && !isBuiltIn) {
                    LogHelper.Log($"[插件] 忽略外部插件 {type.Name}：协议 \"{attr.Name}\" 已由内置提供,不可覆盖");
                    continue;
                }

                _plugins[attr.Name] = new PluginInfo {
                    ProtocolName = attr.Name,
                    ImplType = type,
                    IsBuiltIn = isBuiltIn
                };
                LogHelper.Log($"[插件] 加载: {attr.Name} → {type.Name} {source}");
            }
        }
    }
}