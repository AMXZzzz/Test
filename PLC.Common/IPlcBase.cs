using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PLC.IBase {

    /// <summary>
    /// 枚举, 支持的基础类型
    /// </summary>
    public enum RegisterDataType {
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float,
        Double,
        String32,
        Coil
    }

    /// <summary>
    /// 数据寄存器封装类
    /// </summary>
    public class IRegister {
        //! 地址
        public string Address { get; set; }

        //! 数据基本类型
        public PLC.IBase.RegisterDataType DataType { get; set; } = PLC.IBase.RegisterDataType.Int16;

        //! 实时值
        public object CurrentValue { get; set; } = 0;

        //! 目标值
        public object TargetValue { get; set; } = 0;

        //! 寄存器描述
        public string Description { get; set; } = "";

        /// <summary>
        /// 深拷贝（推荐使用）
        /// </summary>
        public IRegister Clone () {
            return new IRegister {
                Address = this.Address,
                DataType = this.DataType,
                CurrentValue = this.CurrentValue,   // object 直接赋值即可（值类型会拷贝，引用类型这里是浅拷贝，但 object 通常存值类型）
                TargetValue = this.TargetValue,
                Description = this.Description
            };
        }
    }

    /// <summary>
    /// 标注一个 PLC 协议插件。扫描时直接读特性拿协议名,无需实例化。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ProtocolAttribute : Attribute {
        public string Name { get; }

        public ProtocolAttribute (string name) {
            Name = name;
        }
    }

    /// <summary>
    /// PLC父类接口
    /// </summary>
    public abstract class IPlcBase {
        //! 连接标志位
        public abstract bool IsConnected { get; }

        //! 协议名称
        public abstract string GetProtocolName ();

        /// <summary>
        /// 异步连接
        /// </summary>
        /// <param name="ip">通讯IP</param>
        /// <param name="port">通讯端口</param>
        /// <param name="localIP">可选, 通讯适配器</param>
        /// <param name="extraParams">可选, 传入扩展参数</param>
        /// <returns>链接标志位</returns>
        public abstract Task<bool> ConnectAsync (string ip, int port, string localIP = null, Dictionary<string, string> extraParams=null);

        //! 返回该协议需要的扩展参数（键值对）
        public abstract Dictionary<string, string> GetRecommendedExtraParams ();

        //! 断开链接
        public abstract void Disconnect ();

        /// <summary>
        /// 异步读取
        /// </summary>
        /// <param name="dt">寄存器对象,无法修改</param>
        /// <returns>返回读取到的值</returns>
        public abstract Task<IRegister> ReadAsync (IRegister dt);

        /// <summary>
        /// 异步写入
        /// </summary>
        /// <param name="dt">寄存器对象, 无法修改</param>
        /// <returns>返回写入完成的值</returns>
        public abstract Task<IRegister> WriteAsync (IRegister dt);

    }

}