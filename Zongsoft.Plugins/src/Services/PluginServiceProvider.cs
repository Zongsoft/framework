/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Plugins;
using Zongsoft.Plugins.Builders;

namespace Zongsoft.Services
{
	public class PluginServiceProvider : ServiceProviderBase
	{
		#region 常量定义
		private const string SERVICES_PATH = "/Workspace/Services";
		#endregion

		#region 成员变量
		private string _path;
		private PluginTreeNode _node;
		private PluginTree _tree;
		#endregion

		#region 构造函数
		public PluginServiceProvider(Builtin builtin) : base(builtin.Name)
		{
			var path = builtin.Properties.GetRawValue("path");

			if(string.IsNullOrWhiteSpace(path))
			{
				if(string.Equals(builtin.FullPath, SERVICES_PATH, StringComparison.OrdinalIgnoreCase))
					_path = builtin.FullPath;
				else
					_path = PluginPath.Combine(SERVICES_PATH, builtin.Name);
			}
			else
				_path = path == "." ? builtin.FullPath : path;

			_tree = builtin.Tree;
			this.Storage = new PluginServiceStorage(this);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置当前服务供应程序的插件路径，即注册在其下各服务对象所在的插件路径。
		/// </summary>
		public string Path
		{
			get
			{
				return _path;
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value) && string.IsNullOrEmpty(_path))
					throw new ArgumentNullException();

				_path = value.Trim();

				//重置节点对象
				_node = null;
			}
		}

		/// <summary>
		/// 获取当前服务提供程序注册的插件节点。
		/// </summary>
		public PluginTreeNode Node
		{
			get
			{
				if(_node == null)
					_node = _tree.Find(_path);

				return _node;
			}
		}
		#endregion

		#region 重写方法
		/// <summary>
		/// 返回服务供应程序的描述文本，该格式为：[Path]{Type.FullName}
		/// </summary>
		public override string ToString()
		{
			return string.Format("[{0}]{1}", this.Path, this.GetType().FullName);
		}
		#endregion

		#region 嵌套子类
		private class PluginServiceStorage : ServiceStorage
		{
			#region 构造函数
			internal PluginServiceStorage(PluginServiceProvider provider) : base(provider)
			{
			}
			#endregion

			#region 公共属性
			public PluginTreeNode ProviderNode
			{
				get
				{
					return ((PluginServiceProvider)base.Provider).Node;
				}
			}
			#endregion

			#region 重写方法
			public override int Count
			{
				get
				{
					var providerNode = this.ProviderNode;
					return base.Count + (providerNode == null ? 0 : providerNode.Children.Count);
				}
			}

			public override ServiceEntry Get(string name)
			{
				if(name == null)
					return null;

				var providerNode = this.ProviderNode;

				if(providerNode != null)
				{
					//由于插件节点名不允许包含点(.)，所以必须先把点转换成横杠(-)
					var node = providerNode.Children[name.Replace('.', '-')];

					//本地查找成功则返回
					if(node != null)
					{
						var result = node.UnwrapValue(ObtainMode.Auto);

						if(result is ServiceEntry)
							return (ServiceEntry)result;

						return new ServiceEntry(name, result, null, node);
					}
				}

				//调用基类的查找
				return base.Get(name);
			}

			public override IEnumerator<ServiceEntry> GetEnumerator()
			{
				var providerNode = this.ProviderNode;

				if(providerNode != null)
				{
					foreach(var child in providerNode.Children)
					{
						yield return new PluginServiceEntry(child);
					}
				}

				var enumerator = base.GetEnumerator();

				while(enumerator.MoveNext())
				{
					yield return enumerator.Current;
				}
			}
			#endregion
		}

		private class PluginServiceEntry : ServiceEntry
		{
			#region 成员字段
			private PluginTreeNode _node;
			#endregion

			#region 构造函数
			internal PluginServiceEntry(PluginTreeNode node) : base(node.Name)
			{
				_node = node;
			}
			#endregion

			#region 重写方法
			protected override Type GetServiceType()
			{
				return _node.ValueType;
			}

			protected override object CreateService()
			{
				var serviceType = this.ServiceType;
				var settings = serviceType != null && typeof(ServiceProviderBase).IsAssignableFrom(serviceType) ?
					BuilderSettings.Create(BuilderSettingsFlags.IgnoreChildren) : null;

				return _node.UnwrapValue(ObtainMode.Auto, settings);
			}
			#endregion
		}
		#endregion
	}
}
