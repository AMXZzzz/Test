using PLC.IBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PLC.Services {
    /// <summary>插件元数据,扫描期一次性读出,避免重复实例化</summary>
    public class PluginInfo {
        public string ProtocolName { get; set; }
        public Type ImplType { get; set; }
        public bool IsBuiltIn { get; set; }
    }

    /// <summary>
    /// 基于 DLL 扫描的插件加载器,实现 IPlcFactory。
    /// 不再是 static —— 唯一性由组装根负责(只创建一个实例),
    /// 这样它能实现接口、能在测试中被替换。
    /// </summary>
    public class PluginLoader : IPlcFactory {

        //! 内置协议所在的程序集(由外部传入,因为内置协议在主程序里)
        private readonly Assembly _builtInAssembly;

        //! 协议名 → 元数据
        private readonly Dictionary<string, PluginInfo> _plugins =
            new Dictionary<string, PluginInfo>(StringComparer.OrdinalIgnoreCase);

        /// <param name="builtInAssembly">内置协议所在程序集;null 表示无内置</param>
        public PluginLoader (Assembly builtInAssembly = null) {
            _builtInAssembly = builtInAssembly;
        }

        public IReadOnlyList<string> AvailableProtocols
            => new List<string>(_plugins.Keys);

        public void LoadPlugins () {
            _plugins.Clear();

            //! 1. 内置协议(如果提供了程序集)
            if (_builtInAssembly != null) {
                ScanAssembly(_builtInAssembly, "(内置)", isBuiltIn: true);
                LogHelper.Log("[插件] 已注册内置协议");
            }

            //! 2. 外部插件目录
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

        public IPlcBase Create (string protocolName) {
            if (!_plugins.TryGetValue(protocolName, out PluginInfo info))
                throw new NotSupportedException($"未找到协议插件: {protocolName}");

            var ctor = info.ImplType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
                return (IPlcBase)ctor.Invoke(null);

            throw new NotSupportedException($"协议 {protocolName} 缺少无参构造函数");
        }

        public IReadOnlyDictionary<string, string> GetRecommendedExtraParams (string protocolName) {
            if (!_plugins.TryGetValue(protocolName, out PluginInfo info))
                return new Dictionary<string, string>();
            try {
                if (Activator.CreateInstance(info.ImplType) is IPlcBase instance) {
                    return new Dictionary<string, string>(instance.GetRecommendedExtraParams());
                }
            } catch { }
            return new Dictionary<string, string>();
        }

        //! ============ 私有辅助 ============
        private void ScanAssembly (Assembly assembly, string source, bool isBuiltIn) {
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
                if (attr == null) continue;

                if (_plugins.TryGetValue(attr.Name, out PluginInfo existing)
                    && existing.IsBuiltIn && !isBuiltIn) {
                    LogHelper.Log($"[插件] 忽略外部插件 {type.Name}：协议 \"{attr.Name}\" 已由内置提供");
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