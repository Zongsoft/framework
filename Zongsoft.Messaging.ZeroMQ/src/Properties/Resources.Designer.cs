﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Zongsoft.Messaging.ZeroMQ.Properties {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Zongsoft.Messaging.ZeroMQ.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性，对
        ///   使用此强类型资源类的所有资源查找执行重写。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 Client 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Client {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Client", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Group 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Group {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Group", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 A message queue group identifier is a unit of message isolation. 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Group_Description {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Group.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Heartbeat 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Heartbeat {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Heartbeat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Heartbeat interval for maintaining network connection. The default value is 10 seconds. 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Heartbeat_Description {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Heartbeat.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Port 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Port {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Port", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Port number of the ZeroMQ message queue server. The default value is 7969. 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Port_Description {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Port.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Server 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Server {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Server", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Timeout 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Timeout {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Timeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Topic 的本地化字符串。
        /// </summary>
        internal static string ZeroMQ_Settings_Topic {
            get {
                return ResourceManager.GetString("ZeroMQ.Settings.Topic", resourceCulture);
            }
        }
    }
}
