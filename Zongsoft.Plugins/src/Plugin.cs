/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
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
using System.Runtime.Loader;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 关于插件的描述信息。
	/// </summary>
	public class Plugin : IEquatable<Plugin>
	{
		#region 成员字段
		private readonly PluginTree _pluginTree;
		private readonly string _name;
		private readonly string _filePath;
		private readonly bool _isHidden;
		private readonly PluginManifest _manifest;
		private readonly Plugin _parent;
		private readonly PluginCollection _children;
		private readonly List<Builtin> _builtins;
		private PluginStatus _status;

		private readonly BuilderElementCollection _builders;
		private readonly FixedElementCollection<IParser> _parsers;
		#endregion

		#region 构造函数
		/// <summary>创建插件对象。</summary>
		/// <param name="pluginTree">依附的插件树对象。</param>
		/// <param name="name">插件名称，该名称必须在同级插件中唯一。</param>
		/// <param name="filePath">插件文件路径(完整路径)。</param>
		/// <param name="parent">所属的父插件。</param>
		/// <remarks>创建的插件对象，并没有被加入到<paramref name="parent"/>参数指定的父插件的子集中(<seealso cref="Zongsoft.Plugins.Plugin.Children"/>)。</remarks>
		internal Plugin(PluginTree pluginTree, string name, string filePath, Plugin parent)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

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
		/// <summary>获取插件树对象。</summary>
		public PluginTree PluginTree => _pluginTree;

		/// <summary>获取插件名。</summary>
		public string Name => _name;

		/// <summary>获取插件的文件路径，该属性值为完全限定路径格式，即包含完整路径和文件名。</summary>
		public string FilePath => _filePath;

		/// <summary>获取插件清单描述对象。</summary>
		public PluginManifest Manifest => _manifest;

		/// <summary>获取当前插件是否为隐藏式插件文件。</summary>
		/// <remarks>
		///		<para>注意：隐藏式插件不可成为主插件，即对应的<see cref="IsMaster"/>属性始终为假(False)。</para>
		///		<para>注意：隐藏式插件文件中不能定义依赖项。</para>
		/// </remarks>
		public bool IsHidden => _isHidden;

		/// <summary>获取当前插件是否为主插件，即没有依赖项的插件。</summary>
		public bool IsMaster => (!_isHidden) && (this.Manifest.Dependencies == null || this.Manifest.Dependencies.Count < 1);

		/// <summary>获取当前插件是否为从插件，即含有依赖项的插件。</summary>
		public bool IsSlave => _isHidden || (this.Manifest.Dependencies != null && this.Manifest.Dependencies.Count > 0);

		/// <summary>取当前插件的状态。</summary>
		public PluginStatus Status
		{
			get => _status;
			internal set => _status = value;
		}

		/// <summary>获取当前插件的父插件对象。</summary>
		/// <remarks>关于父插件定义和父插件的搜索策略，请参考<seealso cref="Zongsoft.Plugins.PluginLoader"/>类的帮助。</remarks>
		public Plugin Parent => _parent;

		/// <summary>获取一个值，指示当前插件是否具有子插件集。</summary>
		public bool HasChildren => _children != null && _children.Count > 0;

		/// <summary>获取当前插件的子插件集合。</summary>
		/// <remarks>关于父插件定义和父插件的搜索策略，请参考<seealso cref="Zongsoft.Plugins.PluginLoader"/>类的帮助。</remarks>
		public PluginCollection Children => _children;

		/// <summary>获取当前插件中的所有构件对象。</summary>
		public IReadOnlyList<Builtin> Builtins => _builtins;

		/// <summary>获取当前插件的所有构建器集合。</summary>
		public BuilderElementCollection Builders => _builders;

		/// <summary>获取当前插件的所有解析器元素集合。</summary>
		public FixedElementCollection<IParser> Parsers => _parsers;
		#endregion

		#region 内部属性
		internal IList<Builtin> InnerBuiltins => _builtins;
		#endregion

		#region 公共方法
		/// <summary>获取指定构件的构建器。</summary>
		/// <param name="scheme">指定的构建器名称。</param>
		/// <returns>如果找到对应的构建器则返回构建器对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="scheme"/>参数为空(null)或空白字符。</exception>
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
		public IBuilder GetBuilder(string scheme)
		{
			var element = this.GetFixedElement(scheme, this, (plugin) => plugin._builders);

			if(element != null)
				return (IBuilder)element.GetValue();
			else
				return null;
		}

		/// <summary>获取指定构件的解析器。</summary>
		/// <param name="scheme">指定的解析器名称。</param>
		/// <returns>如果找到对应的解析器则返回解析器对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="scheme"/>参数为空(null)或空白字符。</exception>
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
		public IParser GetParser(string scheme)
		{
			var element = this.GetFixedElement(scheme, this, (plugin) => plugin._parsers);

			if(element != null)
				return (IParser)element.GetValue();
			else
				return null;
		}

		private FixedElement GetFixedElement(string name, Plugin plugin, Func<Plugin, FixedElementCollection> getElements)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

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

		/// <summary>获取当前插件的所有同级兄弟插件。</summary>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings() => this.GetSiblings(true, true);

		/// <summary>获取当前插件的同级兄弟插件。</summary>
		/// <param name="containsMasters">指示返回的插件集是否包含类型为主插件的兄弟插件。</param>
		/// <param name="containsSlaves">指示返回的插件集是否包含类型为从插件的兄弟插件。</param>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings(bool containsMasters, bool containsSlaves) => this.GetSiblings(sibling =>
		{
			if(containsMasters && containsSlaves)
				return true;

			if(containsMasters)
				return sibling.IsMaster;
			if(containsSlaves)
				return sibling.IsSlave;

			return false;
		});

		/// <summary>根据指定的判断依据获取当前插件的同级兄弟插件。</summary>
		/// <param name="match">指定要搜索的同级插件的条件，该委托返回真(True)表示传入同级插件对象符合过滤条件，否则将其排除在外。</param>
		/// <returns>返回的同级兄弟插件集合，该结果集中始终不包含当前插件对象。</returns>
		public IEnumerable<Plugin> GetSiblings(Predicate<Plugin> match)
		{
			var source = (_parent == null ? _pluginTree.Plugins : _parent.Children);
			var siblings = new List<Plugin>();

			foreach(var item in source)
			{
				if(item == null || object.ReferenceEquals(this, item))
					continue;

				if(match != null && match(item))
					siblings.Add(item);
			}

			return siblings;
		}

		/// <summary>搜索当前插件的主插件集。</summary>
		/// <returns>包含当前插件依赖项中的主插件集合，如果当前插件为主插件则返回空集。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public ICollection<Plugin> GetMasters()
		{
			if(_pluginTree.Status == PluginTreeStatus.None)
				throw new InvalidOperationException();

			if(this.IsMaster)
				return Array.Empty<Plugin>();

			var masters = new List<Plugin>();
			this.AddMasters(this, masters);
			return masters;
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

		/// <summary>搜索当前插件的直隶附属插件集。</summary>
		/// <returns>只包含当前插件的直隶附属插件集合。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public IReadOnlyList<Plugin> GetSlaves() => this.GetSlaves(false);

		/// <summary>搜索当前插件的附属插件集。</summary>
		/// <param name="containsAll">指示返回的结果集中是否要包含所有的间接被依赖项。</param>
		/// <returns>包含满足搜索选项的附属插件集合。</returns>
		/// <exception cref="System.InvalidOperationException">当插件树尚未初始化，即<seealso cref="Zongsoft.Plugins.PluginTree.Status"/>属性值为<seealso cref="Zongsoft.Plugins.PluginTreeStatus.None"/>时。</exception>
		public IReadOnlyList<Plugin> GetSlaves(bool containsAll)
		{
			if(_pluginTree.Status == PluginTreeStatus.None)
				throw new InvalidOperationException();

			var slaves = new List<Plugin>();
			var siblings = this.GetSiblings(false, true);
			AddSlaves(this, siblings, slaves);

			if(containsAll && slaves.Count > 0)
			{
				for(int i = 0; i < slaves.Count; i++)
					AddSlaves(slaves[i], siblings, slaves);
			}

			return slaves;
		}

		private static void AddSlaves(Plugin master, IEnumerable<Plugin> siblings, IList<Plugin> slaves)
		{
			if(siblings == null || !siblings.Any())
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
				throw new ArgumentNullException(nameof(builtin));

			if(builtin.Plugin != null && (!object.ReferenceEquals(builtin.Plugin, this)))
				throw new ArgumentException($"Invalid builtin.");

			_builtins.Add(builtin);
		}

		internal void UnregisterBuiltin(Builtin builtin)
		{
			if(builtin == null)
				throw new ArgumentNullException(nameof(builtin));

			if(builtin.Plugin != null && (!object.ReferenceEquals(builtin.Plugin, this)))
				throw new ArgumentException($"Invalid builtin.");

			_builtins.Remove(builtin);
		}
		#endregion

		#region 创建构件
		internal Builtin CreateBuiltin(string scheme, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				name = "$" + scheme + "-" + PluginUtility.GetAnonymousId().ToString();

			return new Builtin(scheme, name, this);
		}
		#endregion

		#region 重写方法
		public bool Equals(Plugin other) => other is not null && string.Equals(_filePath, other._filePath);
		public override bool Equals(object obj) => obj is Plugin other && this.Equals(other);
		public override int GetHashCode() => string.IsNullOrEmpty(_filePath) ? 0 : _filePath.GetHashCode();
		public override string ToString() => $"{_name} [{_filePath}]";
		#endregion

		#region 嵌套子类
		public sealed class PluginManifest
		{
			#region 成员变量
			private readonly Plugin _plugin;
			private readonly List<Assembly> _assemblies;
			private readonly PluginDependencyCollection _dependencies;
			private AssemblyDependencyResolver _resolver;
			private Version _version;
			#endregion

			#region 构造函数
			internal PluginManifest(Plugin plugin)
			{
				_plugin = plugin;
				_assemblies = new List<Assembly>();
				_dependencies = new PluginDependencyCollection();
				//_loader = new PluginAssemblyLoader(plugin);

				var assemblyPath = Path.Combine(Path.GetDirectoryName(plugin.FilePath), Path.GetFileNameWithoutExtension(plugin.FilePath) + ".dll");

				if(File.Exists(assemblyPath))
					_resolver = new AssemblyDependencyResolver(assemblyPath);
			}
			#endregion

			#region 公共属性
			/// <summary>获取插件的标题描述文本。</summary>
			public string Title { get; internal set; }

			/// <summary>获取插件的作者信息。</summary>
			public string Author { get; internal set; }

			/// <summary>获取插件的版本信息。</summary>
			public Version Version
			{
				get
				{
					if(_version == null)
					{
						var assemblyPath = Path.Combine(Path.GetDirectoryName(_plugin.FilePath), Path.GetFileNameWithoutExtension(_plugin.FilePath) + ".dll");

						if(File.Exists(assemblyPath))
						{
							var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyPath);
							_version = info == null ? new Version(0, 0, 0) : new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
						}
						else
							_version = new Version();
					}

					return _version;
				}
				internal set => _version = value;
			}

			/// <summary>获取插件的版权声明。</summary>
			public string Copyright { get; internal set; }

			/// <summary>获取插件的描述信息。</summary>
			public string Description { get; internal set; }

			/// <summary>获取插件的宿主程序集数组。</summary>
			public IReadOnlyList<Assembly> Assemblies => _assemblies;

			/// <summary>获取依赖程序集的路径解析器。</summary>
			public AssemblyDependencyResolver Resolver => _resolver;

			/// <summary>获取一个值，指示是否有依赖项。</summary>
			public bool HasDependencies => _dependencies != null && _dependencies.Count > 0;

			/// <summary>获取插件的依赖项集合。</summary>
			/// <remarks>依赖项通过插件定义文件(*.plugin)中的&lt;dependencies&gt;节点进行声明。依赖项只表明插件的加载顺序，并无类型依赖的暗喻。</remarks>
			public PluginDependencyCollection Dependencies => _dependencies;
			#endregion

			#region 内部方法
			internal void SetAssembly(string assemblyName, bool optional)
			{
				if(string.IsNullOrWhiteSpace(assemblyName))
					return;

				foreach(var assembly in AssemblyLoadContext.Default.Assemblies)
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

				if(!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
					filePath += ".dll";

				if(!File.Exists(filePath))
				{
					if(optional)
						return;

					throw new PluginException(string.Format("The '{0}' assembly file is not exists. in '{1}' plugin file.", assemblyName, _plugin.Name));
				}

				if(_resolver == null)
					_resolver = new AssemblyDependencyResolver(filePath);

				//var result = Assembly.LoadFrom(filePath);
				//var result = _loader.LoadFromAssemblyPath(filePath);
				var result = AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath);

				if(result != null)
					_assemblies.Add(result);
			}
			#endregion
		}

		private class PluginAssemblyLoader : AssemblyLoadContext
		{
			#region 成员字段
			private readonly Plugin _plugin;
			private AssemblyDependencyResolver _resolver;
			#endregion

			#region 构造函数
			public PluginAssemblyLoader(Plugin plugin)
			{
				_plugin = plugin;

				var assemblyPath = Path.Combine(Path.GetDirectoryName(plugin.FilePath), Path.GetFileNameWithoutExtension(plugin.FilePath) + ".dll");

				if(File.Exists(assemblyPath))
					_resolver = new AssemblyDependencyResolver(assemblyPath);

				Default.Resolving += Default_Resolving;
			}
			#endregion

			#region 重写方法
			protected override Assembly Load(AssemblyName assemblyName)
			{
				if(assemblyName.Name.StartsWith("System") || assemblyName.Name.StartsWith("Microsoft"))
					return null;

				var assembly = FindAssembly(_plugin, assemblyName);

				if(assembly == null)
				{
					if(_resolver == null && _plugin._manifest.Assemblies != null && _plugin._manifest.Assemblies.Count > 0)
						_resolver = new AssemblyDependencyResolver(_plugin._manifest.Assemblies[0].Location);

					if(_resolver != null)
					{
						var filePath = _resolver.ResolveAssemblyToPath(assemblyName);

						if(!string.IsNullOrEmpty(filePath))
							assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath);
					}
				}

				return assembly;
			}

			private Assembly Default_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
			{
				if(_resolver != null)
				{
					var filePath = _resolver.ResolveAssemblyToPath(assemblyName);

					if(!string.IsNullOrEmpty(filePath))
						return context.LoadFromAssemblyPath(filePath);
				}

				return null;
			}
			#endregion

			#region 私有方法
			private static Assembly FindAssembly(Plugin plugin, AssemblyName assemblyName)
			{
				if(plugin.Manifest.Assemblies != null)
				{
					var assembly = plugin.Manifest.Assemblies.FirstOrDefault(assembly => MatchAssembly(assembly.GetName(), assemblyName));

					if(assembly != null)
						return assembly;
				}

				var dependencies = plugin._manifest.HasDependencies ? plugin.Manifest.Dependencies : null;

				if(dependencies != null && dependencies.Count > 0)
				{
					foreach(var dependency in dependencies)
					{
						var assembly = FindAssembly(dependency.Plugin, assemblyName);

						if(assembly != null)
							return assembly;
					}
				}

				return (plugin.Parent != null) ? FindAssembly(plugin.Parent, assemblyName) : null;
			}

			private static bool MatchAssembly(AssemblyName loaded, AssemblyName unloaded) => string.Equals(loaded.FullName, unloaded.FullName) && loaded.Version >= unloaded.Version;
			#endregion
		}
		#endregion
	}
}
