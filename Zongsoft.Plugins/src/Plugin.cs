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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 关于插件的描述信息。
	/// </summary>
	public class Plugin
	{
		#region 成员字段
		private PluginTree _pluginTree;
		private string _name;
		private string _filePath;
		private bool _isHidden;
		private PluginStatus _status;
		private PluginManifest _manifest;
		private Plugin _parent;
		private PluginCollection _children;
		private List<Builtin> _builtins;

		private BuilderElementCollection _builders;
		private FixedElementCollection<IParser> _parsers;
		#endregion

		#region 构造函数
		/// <summary>
		/// 创建插件对象。
		/// </summary>
		/// <param name="pluginTree">依附的插件树对象。</param>
		/// <param name="name">插件名称，该名称必须在同级插件中唯一。</param>
		/// <param name="filePath">插件文件路径(完整路径)。</param>
		/// <param name="parent">所属的父插件。</param>
		/// <remarks>创建的插件对象，并没有被加入到<paramref name="parent"/>参数指定的父插件的子集中(<seealso cref="Zongsoft.Plugins.Plugin.Children"/>)。</remarks>
		internal Plugin(PluginTree pluginTree, string name, string filePath, Plugin parent)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException("filePath");

			_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
			_name = name.Trim();
			_filePath = filePath;
			_parent = parent;
			_isHidden = string.Equals(Path.GetFileName(filePath), ".plugin", StringComparison.OrdinalIgnoreCase);
			_status = PluginStatus.None;
			_manifest = new PluginManifest(this);
			_children = new PluginCollection(this);
			_builtins = new List<Builtin>();

			_parsers = new FixedElementCollection<IParser>();
			_builders = new BuilderElementCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取插件树对象。
		/// </summary>
		public PluginTree PluginTree
		{
			get => _pluginTree;
		}

		/// <summary>
		/// 获取插件名。
		/// </summary>
		public string Name
		{
			get => _name;
		}

		/// <summary>
		/// 获取插件的文件路径，该属性值为完全限定路径格式，即包含完整路径和文件名。
		/// </summary>
		public string FilePath
		{
			get => _filePath;
		}

		/// <summary>
		/// 获取插件清单描述对象。
		/// </summary>
		public PluginManifest Manifest
		{
			get => _manifest;
		}

		/// <summary>
		/// 获取当前插件是否为隐藏式插件文件。
		/// </summary>
		/// <remarks>
		///		<para>注意：隐藏式插件不可成为主插件，即对应的<see cref="IsMaster"/>属性始终为假(False)。</para>
		///		<para>注意：隐藏式插件文件中不能定义依赖项。</para>
		/// </remarks>
		public bool IsHidden
		{
			get => _isHidden;
		}

		/// <summary>
		/// 获取当前插件是否为主插件，即没有依赖项的插件。
		/// </summary>
		public bool IsMaster
		{
			get => (!_isHidden) && (this.Manifest.Dependencies == null || this.Manifest.Dependencies.Count < 1);
		}

		/// <summary>
		/// 获取当前插件是否为从插件，即含有依赖项的插件。
		/// </summary>
		public bool IsSlave
		{
			get => _isHidden || (this.Manifest.Dependencies != null && this.Manifest.Dependencies.Count > 0);
		}

		/// <summary>
		/// 获取当前插件的状态。
		/// </summary>
		public PluginStatus Status
		{
			get => _status;
			internal set => _status = value;
		}

		/// <summary>
		/// 获取当前插件的父插件对象。
		/// </summary>
		/// <remarks>关于父插件定义和父插件的搜索策略，请参考<seealso cref="Zongsoft.Plugins.PluginLoader"/>类的帮助。</remarks>
		public Plugin Parent
		{
			get => _parent;
		}

		/// <summary>
		/// 获取当前插件的子插件集合。
		/// </summary>
		/// <remarks>关于父插件定义和父插件的搜索策略，请参考<seealso cref="Zongsoft.Plugins.PluginLoader"/>类的帮助。</remarks>
		public PluginCollection Children
		{
			get => _children;
		}

		/// <summary>
		/// 获取当前插件中的所有构件对象。
		/// </summary>
		public Builtin[] Builtins
		{
			get => _builtins.ToArray();
		}

		/// <summary>
		/// 获取当前插件的所有构建器集合。
		/// </summary>
		public BuilderElementCollection Builders
		{
			get => _builders;
		}

		/// <summary>
		/// 获取当前插件的所有解析器元素集合。
		/// </summary>
		public FixedElementCollection<IParser> Parsers
		{
			get => _parsers;
		}
		#endregion

		#region 内部属性
		internal IList<Builtin> InnerBuiltins
		{
			get => _builtins;
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 获取指定构件的构建器。
		/// </summary>
		/// <param name="builderName">指定的构建器名称。</param>
		/// <returns>如果找到对应的构建器则返回构建器对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="builderName"/>参数为空(null)或空白字符。</exception>
		/// <remarks>
		/// <para>查找构建器的流程如下：</para>
		/// <list type="number">
		///		<item>
		///			<term>在当前插件的构建器集合中查找指定名称的构建器，如果找到则返回，否则继续；</term>
		///			<term>依次在当前插件的依赖插件的构建器集合中查找指定名称的构建器，如果找到则返回，否则递归重复该项查找；</term>
		///			<term>在当前插件的父插件的构建器集合中查找，如果找到则返回；</term>
		///			<term>查找失败，返回空(null)。</term>
		///		</item>
		/// </list>
		/// </remarks>
		public IBuilder GetBuilder(string builderName)
		{
			var element = this.GetFixedElement(builderName, this, (plugin) => plugin._builders);

			if(element != null)
				return (IBuilder)element.GetValue();
			else
				return null;
		}

		/// <summary>
		/// 获取指定构件的解析器。
		/// </summary>
		/// <param name="parserName">指定的解析器名称。</param>
		/// <returns>如果找到对应的解析器则返回解析器对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="parserName"/>参数为空(null)或空白字符。</exception>
		/// <remarks>
		/// <para>查找解析器的流程如下：</para>
		/// <list type="number">
		///		<item>
		///			<term>在当前插件的解析器集合中查找指定名称的解析器，如果找到则返回，否则继续；</term>
		///			<term>依次在当前插件的依赖插件的解析器集合中查找指定名称的解析器，如果找到则返回，否则递归重复该项查找；</term>
		///			<term>在当前插件的父插件的解析器集合中查找，如果找到则返回；</term>
		///			<term>查找失败，返回空(null)。</term>
		///		</item>
		/// </list>
		/// </remarks>
		public IParser GetParser(string parserName)
		{
			var element = this.GetFixedElement(parserName, this, (plugin) => plugin._parsers);

			if(element != null)
				return (IParser)element.GetValue();
			else
				return null;
		}

		private FixedElement GetFixedElement(string name, Plugin plugin, Func<Plugin, FixedElementCollection> getElements)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			//从当前指定的插件的固定元素集合中查找指定名称的固定元素
			var element = getElements(plugin).Get(name);

			if(element != null)
				return element;

			//从当前指定插件的依赖插件中查找指定名称的固定元素
			foreach(var dependency in plugin.Manifest.Dependencies)
			{
				if(dependency != null && dependency.Plugin != null)
				{
					element = getElements(dependency.Plugin).Get(name);

					if(element != null)
						return element;
				}
			}

			//从当前指定插件的附属插件集中查找指定名称的固定元素
			foreach(var slave in plugin.GetSlaves())
			{
				element = getElements(slave).Get(name);

				if(element != null)
					return element;
			}

			//如果当前指定的插件有父插件则从其父插件中获取指定名称的固定元素，否则返回空
			if(plugin.Parent != null)
				return this.GetFixedElement(name, plugin.Parent, getElements);
			else
				return null;
		}

		/// <summary>
		/// 获取当前插件的所有同级兄弟插件。
		/// </summary>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings()
		{
			return this.GetSiblings(true, true);
		}

		/// <summary>
		/// 获取当前插件的同级兄弟插件。
		/// </summary>
		/// <param name="containsMasters">指示返回的插件集是否包含类型为主插件的兄弟插件。</param>
		/// <param name="containsSlaves">指示返回的插件集是否包含类型为从插件的兄弟插件。</param>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings(bool containsMasters, bool containsSlaves)
		{
			return this.GetSiblings(sibling =>
			{
				if(containsMasters && containsSlaves)
					return true;

				if(containsMasters)
					return sibling.IsMaster;
				if(containsSlaves)
					return sibling.IsSlave;

				return false;
			});
		}

		/// <summary>
		/// 根据指定的判断依据获取当前插件的同级兄弟插件。
		/// </summary>
		/// <param name="match">指定要搜索的同级插件的条件，该委托返回真(True)表示传入同级插件对象符合过滤条件，否则将其排除在外。</param>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings(Predicate<Plugin> match)
		{
			IEnumerable<Plugin> source = (_parent == null ? _pluginTree.Plugins : _parent.Children);
			List<Plugin> siblings = new List<Plugin>();

			foreach(var item in source)
			{
				if(item == null || object.ReferenceEquals(this, item))
					continue;

				if(match != null && match(item))
					siblings.Add(item);
			}

			return siblings.ToArray();
		}

		/// <summary>
		/// 搜索当前插件的主插件集。
		/// </summary>
		/// <returns>包含当前插件依赖项中的主插件集合，如果当前插件为主插件则返回空集。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public ICollection<Plugin> GetMasters()
		{
			if(_pluginTree.Status == PluginTreeStatus.None)
				throw new InvalidOperationException();

			if(this.IsMaster)
				return new Plugin[0];

			List<Plugin> masters = new List<Plugin>();

			this.AddMasters(this, masters);

			return masters.ToArray();
		}

		private void AddMasters(Plugin current, IList<Plugin> masters)
		{
			if(masters.Contains(current))
				return;

			foreach(PluginDependency depend in current.Manifest.Dependencies)
			{
				if(depend.Plugin.IsMaster)
					masters.Add(depend.Plugin);
				else
					AddMasters(depend.Plugin, masters);
			}
		}

		/// <summary>
		/// 搜索当前插件的直隶附属插件集。
		/// </summary>
		/// <returns>只包含当前插件的直隶附属插件集合。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public Plugin[] GetSlaves()
		{
			return this.GetSlaves(false);
		}

		/// <summary>
		/// 搜索当前插件的附属插件集。
		/// </summary>
		/// <param name="containsAll">指示返回的结果集中是否要包含所有的间接被依赖项。</param>
		/// <returns>包含满足搜索选项的附属插件集合。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public Plugin[] GetSlaves(bool containsAll)
		{
			if(_pluginTree.Status == PluginTreeStatus.None)
				throw new InvalidOperationException();

			List<Plugin> slaves = new List<Plugin>();
			IEnumerable<Plugin> siblings = this.GetSiblings(false, true);
			this.AddSlaves(this, siblings, slaves);

			if(containsAll && slaves.Count > 0)
			{
				for(int i = 0; i < slaves.Count; i++)
					this.AddSlaves(slaves[i], siblings, slaves);
			}

			return slaves.ToArray();
		}

		private void AddSlaves(Plugin master, IEnumerable<Plugin> siblings, IList<Plugin> slaves)
		{
			if(siblings == null || siblings.Count() < 1)
				return;

			foreach(Plugin sibling in siblings)
			{
				if(sibling == master || sibling.IsMaster)
					continue;

				if(!slaves.Contains(sibling))
				{
					if(sibling.Manifest.Dependencies.Any(depend =>
					{
						if(depend.Plugin != null)
							return depend.Plugin == master;
						else
							return string.Equals(depend.Name, master.Name, StringComparison.OrdinalIgnoreCase);
					}))
						slaves.Add(sibling);
				}
			}
		}
		#endregion

		#region 设置构件
		internal void RegisterBuiltin(Builtin builtin)
		{
			if(builtin == null)
				throw new ArgumentNullException("builtin");

			if(builtin.Plugin != null && (!object.ReferenceEquals(builtin.Plugin, this)))
				throw new ArgumentException();

			_builtins.Add(builtin);
		}

		internal void UnregisterBuiltin(Builtin builtin)
		{
			if(builtin == null)
				throw new ArgumentNullException("builtin");

			if(builtin.Plugin != null && (!object.ReferenceEquals(builtin.Plugin, this)))
				throw new ArgumentException();

			_builtins.Remove(builtin);
		}
		#endregion

		#region 创建构件
		internal Builtin CreateBuiltin(string builderName, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				name = "$" + builderName + "-" + PluginUtility.GetAnonymousId().ToString();

			return new Builtin(builderName, name, this);
		}
		#endregion

		#region 重写方法
		/// <summary>
		/// 重写了<seealso cref="System.Object"/>类的同名方法。
		/// </summary>
		/// <returns>返回当前插件的名称和文件名。</returns>
		public override string ToString()
		{
			return string.Format("{0} [{1}]", _name, _filePath);
		}

		public override int GetHashCode()
		{
			if(_filePath == null)
				return base.GetHashCode();

			switch(Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
				case PlatformID.MacOSX:
					return _filePath.GetHashCode();
				default:
					return _filePath.ToLowerInvariant().GetHashCode();
			}
		}

		/// <summary>
		/// 确定与指定的插件对象是否相等。
		/// </summary>
		/// <param name="obj">与当前插件相比较的插件对象。</param>
		/// <returns>如果指定的<seealso cref="Zongsoft.Plugins.Plugin"/>等于当前的<seealso cref="Zongsoft.Plugins.Plugin"/>，则为 true；否则为 false。</returns>
		/// <remarks>该相等性是按照插件的<see cref="Zongsoft.Plugins.Plugin.FilePath"/>文件名进行不区分大小写比较。</remarks>
		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			var other = (Plugin)obj;

			switch(Environment.OSVersion.Platform)
			{
				case PlatformID.Unix:
				case PlatformID.MacOSX:
					return string.Equals(_filePath, other.FilePath, StringComparison.Ordinal);
				default:
					return string.Equals(_filePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
			}
		}
		#endregion

		#region 嵌套子类
		public sealed class PluginManifest
		{
			#region 成员变量
			private readonly Plugin _plugin;
			private readonly List<Assembly> _assemblies;
			private PluginDependencyCollection _dependencies;
			#endregion

			#region 构造函数
			internal PluginManifest(Plugin owner)
			{
				_plugin = owner;
				_assemblies = new List<Assembly>();
			}
			#endregion

			#region 公共属性
			/// <summary>
			/// 获取插件的标题描述文本。
			/// </summary>
			public string Title
			{
				get;
				internal set;
			}

			/// <summary>
			/// 获取插件的作者信息。
			/// </summary>
			public string Author
			{
				get;
				internal set;
			}

			/// <summary>
			/// 获取插件的版本信息。
			/// </summary>
			public Version Version
			{
				get;
				internal set;
			}

			/// <summary>
			/// 获取插件的版权声明。
			/// </summary>
			public string Copyright
			{
				get;
				internal set;
			}

			/// <summary>
			/// 获取插件的描述信息。
			/// </summary>
			public string Description
			{
				get;
				internal set;
			}

			/// <summary>
			/// 获取插件的宿主程序集数组。
			/// </summary>
			public Assembly[] Assemblies
			{
				get => _assemblies.ToArray();
			}

			public bool HasDependencies
			{
				get => _dependencies != null && _dependencies.Count > 0;
			}

			/// <summary>
			/// 获取插件的依赖项集合。
			/// </summary>
			/// <remarks>
			/// 依赖项通过插件定义文件(*.plugin)中的&lt;dependencies&gt;节点进行声明。依赖项只表明插件的加载顺序，并无类型依赖的暗喻。
			/// </remarks>
			public PluginDependencyCollection Dependencies
			{
				get
				{
					if(_dependencies == null)
						System.Threading.Interlocked.CompareExchange(ref _dependencies, new PluginDependencyCollection(), null);

					return _dependencies;
				}
			}
			#endregion

			#region 内部方法
			internal void SetAssembly(string assemblyName)
			{
				if(string.IsNullOrWhiteSpace(assemblyName))
					return;

				//首先从当前应用域中查看是否有已加载的程序集名称与指定的程序集名相同，如果将当前插件的程序集引用指向它即可
				foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if(string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
					{
						_assemblies.Add(assembly);
						return;
					}
				}

				string filePath = assemblyName;

				if(!Path.IsPathRooted(assemblyName))
				{
					string directoryName = Path.GetDirectoryName(_plugin.FilePath);
					filePath = Path.Combine(directoryName, assemblyName);
				}

				if((!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) && (!filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
				{
					filePath = filePath + ".dll";

					if(!File.Exists(filePath))
						filePath = filePath + ".exe";
				}

				if(!File.Exists(filePath))
					throw new PluginException(string.Format("The '{0}' assembly file is not exists. in '{1}' plugin file.", assemblyName, _plugin.FilePath));

				Assembly result = Assembly.LoadFrom(filePath);

				if(result != null)
					_assemblies.Add(result);
			}
			#endregion
		}
		#endregion
	}
}
