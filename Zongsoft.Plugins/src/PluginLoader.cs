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
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 关于插件加载的功能。
	/// </summary>
	/// <remarks>
	///		<para>插件加载器根据一系列策略进行插件加载，可以通过<seealso cref="Zongsoft.Plugins.PluginLoader.Topmosts"/>或<seealso cref="Zongsoft.Plugins.PluginTree.Plugins"/>属性获取加载成功的所有根插件集。</para>
	///		<para>关于插件加载中的相关定义如下：</para>
	///		<list type="table">
	///			<item>
	///				<term>插件根目录</term>
	///				<description>由<seealso cref="Zongsoft.Plugins.PluginOptions.PluginsPath"/>指定的完全限定路径，默认为当前应用程序目录下的plugins文件夹。</description>
	///			</item>
	///			<item>
	///				<term>插件目录</term>
	///				<description>
	///					<para>位于插件根目录下的子目录或者插件根目录均称为插件目录。不是所有插件根目录下的子目录都是插件子目录，必须包含插件定义文件(*.plugin)的子目录才是插件子目录。</para>
	///				</description>
	///			</item>
	///			<item>
	///				<term>父子插件</term>
	///				<description>插件的父子关系只依赖于插件目录的层次关系，处于上级插件目录中的插件为其下级插件目录中各插件的父插件，一个插件可以有零或多个子插件但是只能有零或一个父插件。有关父子插件的加载策略与关系确定条件请参考后面的说明。</description>
	///			</item>
	///			<item>
	///				<term>插件依赖项</term>
	///				<description>插件依赖项在插件定义文件(*.plugin)中通过&lt;dependencies&gt;节点进行声明。它表示在同级插件中的加载次序，父插件总是在子插件之前加载完成，如果父插件加载失败，系统不会去尝试加载它的子插件。</description>
	///			</item>
	///			<item>
	///				<term>主插件</term>
	///				<description>主插件是相对于插件目录而言的。在插件目录中如果只有一个插件定义文件(*.plugin)，那么这个插件定义文件对应的插件即为该插件目录的主插件；如果目录中有多个插件定义文件，则其中没有任何依赖项的即为主插件，因此目录中主插件可能有多个。</description>
	///			</item>
	///		</list>
	///		
	///		<para>插件的父子关系确定涉及下列步骤：</para>
	///		<list type="number">
	///			<item>
	///				<description>在当前插件目录下如果有子文件夹，则启动子插件的搜索。</description>
	///			</item>
	///			<item>
	///				<description>如果插件目录下有子插件，则这些子插件的父插件为上级插件目录的主插件；当上级插件目录有多个主插件，则他们之间没有从属关系，即为平级关系，并以此类推。</description>
	///			</item>
	///		</list>
	///		
	///		<list type="number">
	///			<item>
	///				<description>从插件根目录中以插件文件名排序依次预加载插件，预加载成功的根插件进入根插件集合中。</description>
	///			</item>
	///			<item>
	///				<description>如果预加载插件成功的插件是主插件，则完整的加载它。</description>
	///			</item>
	///			<item>
	///				<description>依次递归预加载子插件目录中的各主插件文件。</description>
	///			</item>
	///			<item>
	///				<description>在系统中所有主插件加载完毕后，则从上向下按级加载从插件。</description>
	///			</item>
	///			<item>
	///				<description>如果从插件集中不能有加载的依赖项或者有循环引用的情况，则这些从插件的状态被置为失败，并从上级插件树列表移除。</description>
	///			</item>
	///			<item>
	///				<description>依次递归预加载子插件目录中的各从插件。</description>
	///			</item>
	///		</list>
	/// </remarks>
	public class PluginLoader
	{
		#region 事件定义
		/// <summary>表示所有插件加载完成。</summary>
		/// <remarks>该事件由Load方法激发。只要Load被执行，该事件总会被激发，无论加载过程是否异常。</remarks>
		public event EventHandler<PluginLoadEventArgs> Loaded;

		/// <summary>表示开始进行整体插件加载。</summary>
		/// <remarks>该事件由Load方法激发，只要Load被执行，该事件总是第一个被激发。</remarks>
		public event EventHandler<PluginLoadEventArgs> Loading;

		/// <summary>表示单个插件加载完成。</summary>
		/// <remarks>该事件由Load方法激发，在对应的<seealso cref="Zongsoft.Plugins.PluginLoader.PluginLoading"/>事件被激发后，该事件不一定总会得到激发，因为加载过程可能出现异常。在<seealso cref="Zongsoft.Plugins.PluginLoadedEventArgs"/>事件参数中通过Plugin属性获取到成功加载的插件对象。</remarks>
		public event EventHandler<PluginLoadedEventArgs> PluginLoaded;

		/// <summary>表示单个插件开始加载。</summary>
		/// <remarks>该事件由Load方法激发，在每次加载到相应的插件会得到激发。</remarks>
		public event EventHandler<PluginLoadingEventArgs> PluginLoading;

		/// <summary>表示单个插件卸载完成。</summary>
		/// <remarks>该事件由Unload方法激发，在对应的<seealso cref="Zongsoft.Plugins.PluginLoader.PluginUnloading"/>事件被激发后，该事件不一定总会得到激发，因为卸载过程可能出现异常。在<seealso cref="Zongsoft.Plugins.PluginUnloadedEventArgs"/>事件参数中通过Plugin属性获取到成功卸载的插件对象。</remarks>
		public event EventHandler<PluginUnloadedEventArgs> PluginUnloaded;

		/// <summary>表示单个插件开始卸载。</summary>
		/// <remarks>该事件由Unload方法激发，在每次卸载指定的插件会得到激发。</remarks>
		public event EventHandler<PluginUnloadingEventArgs> PluginUnloading;
		#endregion

		#region 成员字段
		private readonly PluginResolver _resolver;
		private readonly PluginCollection _topmosts;
		#endregion

		#region 构造函数
		internal PluginLoader(PluginResolver resolver)
		{
			_resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
			_topmosts = new PluginCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>获取加载的顶级插件对象集。</summary>
		public PluginCollection Topmosts => _topmosts;
		#endregion

		#region 加载方法
		/// <summary>应用指定的加载配置进行插件加载。</summary>
		/// <param name="options">指定的加载配置对象。</param>
		/// <remarks>
		///		<para>使用不同的<see cref="Zongsoft.Plugins.PluginOptions"/>设置项多次加载，会导致最后一次加载覆盖上次加载的插件结构，这有可能会影响您的插件应用对构件或服务的获取路径，从而导致不可预知的结果。</para>
		///		<para>如果要重用上次加载的配置，请调用无参的Load方法。</para>
		///	</remarks>
		/// <exception cref="System.ArgumentNullException">参数<paramref name="options"/>为空(null)。</exception>
		internal void Load(PluginOptions options)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			//如果指定的目录路径不存在则激发“Failure”事件，并退出
			if(!Directory.Exists(options.PluginsPath))
				throw new DirectoryNotFoundException($"The '{options.PluginsPath}' plugins directory is not exists.");

			try
			{
				//激发“Loading”事件
				this.OnLoading(new PluginLoadEventArgs(options));

				//清空插件列表
				_topmosts.Clear();

				//预加载插件目录下的所有插件文件
				this.PreloadPluginFiles(options.PluginsPath, null, options);

				//正式加载所有插件
				this.LoadPlugins(_topmosts, options);

				//激发“Loaded”事件
				this.OnLoaded(new PluginLoadEventArgs(options));
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.GetLogger(this).Error(ex);
				throw;
			}
		}

		/// <summary>卸载所有插件。</summary>
		internal void Unload()
		{
			if(_topmosts == null || _topmosts.Count < 1)
				return;

			var plugins = new Plugin[_topmosts.Count];
			_topmosts.CopyTo(plugins, 0);

			foreach(var plugin in plugins)
			{
				this.Unload(plugin);
			}
		}

		/// <summary>卸载指定的插件。</summary>
		/// <param name="plugin">指定要卸载的插件。</param>
		/// <remarks>
		///		<para>如果指定的插件状态不是已经加载的（即插件对象的Status属性值不等于<seealso cref="Zongsoft.Plugins.PluginStatus.Loaded"/>），则不能对其进行卸载。</para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">当<paramref name="plugin"/>参数为空(null)。</exception>
		internal void Unload(Plugin plugin)
		{
			if(plugin == null || plugin.Status != PluginStatus.Loaded)
				return;

			//激发“PluginUnloading”事件，如果事件处理程序取消后续执行则返回
			if(this.OnPluginUnloading(plugin))
				return;

			//设置插件状态
			plugin.Status = PluginStatus.Unloading;

			//递推卸载子插件
			foreach(Plugin child in plugin.Children)
			{
				this.Unload(child);
				plugin.Children.Remove(child.Name);
			}

			//获取指定插件的直隶从属插件集
			var slaves = plugin.GetSlaves(false);

			//递归卸载附属插件
			foreach(Plugin slave in slaves)
			{
				this.Unload(slave);
				slave.Manifest.Dependencies.Remove(plugin);
			}

			//将指定插件中的所有构件依次从插件树中卸载掉，因为UnmountBuiltin方法会改变PluginTree对象的内部构件列表，所以不能使用foreach而必须使用while进行遍历
			//注意：对构件的卸载必须在卸载构建器之前，因为卸载构件需要使用到对应构建器的Destroy方法。
			while(plugin.InnerBuiltins.Count > 0)
				_resolver.PluginTree.Unmount(plugin.InnerBuiltins[0]);

			//卸载当前插件下的所有固定元素(构建器、解析器、模块)
			this.UnloadFixedElements(plugin);

			//将指定卸载的插件从当前根插件列表中删除
			if(_topmosts != null && plugin.Parent == null)
				_topmosts.Remove(plugin.Name);

			//设置插件状态
			plugin.Status = PluginStatus.Unloaded;

			//激发“PluginUnloaded”事件
			this.OnPluginUnloaded(new PluginUnloadedEventArgs(plugin));
		}
		#endregion

		#region 私有方法
		private void UnloadFixedElements(Plugin plugin)
		{
			if(plugin == null)
				return;

			foreach(BuilderElement builderElement in plugin.Builders)
			{
				this.UnloadBuilder(builderElement);
			}

			foreach(FixedElement<IParser> element in plugin.Parsers)
			{
				if(element.HasValue && element.Value is IDisposable disposable)
					disposable.Dispose();
			}

			plugin.Builders.Clear();
			plugin.Parsers.Clear();
		}

		private void UnloadBuilder(BuilderElement builderElement)
		{
			if(builderElement == null)
				return;

			if(builderElement.HasValue)
				builderElement.Value.Dispose();
		}

		private void PreloadPluginFiles(string directoryPath, Plugin parent, PluginOptions options)
		{
			//获取指定目录下的插件文件
			var filePaths = Directory.GetFiles(directoryPath, "*.plugin", SearchOption.TopDirectoryOnly);

			//如果当前目录下没有插件文件则查找子目录
			if(filePaths == null || filePaths.Length < 1)
			{
				this.PreloadPluginChildrenFiles(directoryPath, parent, options);
				return;
			}

			//已经成功加载的主插件列表
			var masters = new List<Plugin>();

			//依次加载所有插件文件
			foreach(string filePath in filePaths)
			{
				//首先加载插件文件的清单信息(根据清单信息中的依赖插件来决定是否需要完整加载)
				var plugin = this.LoadPluginManifest(filePath, parent, options);

				//如果预加载失败，则跳过以进行下一个插件文件的处理
				if(plugin == null)
					continue;

				//判断当前预加载的插件是否为主插件
				if(plugin.IsMaster)
				{
					//将当前已完整加载的插件加入主插件列表中
					masters.Add(plugin);
				}
			}

			//定义子插件的父插件，默认为当前插件目录的父插件
			var ownerPlugin = parent;

			//如果当前插件目录下有主插件则所有子插件的父为第一个主插件
			if(masters.Count > 0)
				ownerPlugin = masters[0];

			//预加载子插件
			this.PreloadPluginChildrenFiles(directoryPath, ownerPlugin, options);
		}

		private void PreloadPluginChildrenFiles(string directoryPath, Plugin parent, PluginOptions options)
		{
			//获取当前插件目录的下级子目录
			string[] childDirectoriePaths = Directory.GetDirectories(directoryPath);

			//依次加载子目录下的所有插件
			foreach(string childDirectoryPath in childDirectoriePaths)
			{
				this.PreloadPluginFiles(childDirectoryPath, parent, options);
			}
		}

		private Plugin LoadPluginManifest(string filePath, Plugin parent, PluginOptions options)
		{
			//激发“PluginLoading”事件
			this.OnPluginLoading(new PluginLoadingEventArgs(filePath, options));

			//解析插件清单
			var plugin = _resolver.ResolvePluginManifestOnly(filePath, parent);

			if(plugin == null)
				return null;

			//设置插件状态
			plugin.Status = PluginStatus.Loading;

			if(parent == null)
			{
				if(_topmosts.Any(p => string.Equals(p.Name, plugin.Name, StringComparison.OrdinalIgnoreCase)))
					throw new PluginFileException(plugin.FilePath, $"The name is '{plugin.Name}' of plugin was exists. it's path is: '{plugin.FilePath}'");

				//将预加载的插件对象加入到根插件的集合中
				_topmosts.Add(plugin);
			}
			else
			{
				//将预加载的插件对象加入到父插件的子集中，如果返回假则表示加载失败
				if(!parent.Children.TryAdd(plugin))
					throw new PluginFileException(plugin.FilePath, $"The name is '{plugin.Name}' of plugin was exists. it's path is: '{plugin.FilePath}'");
			}

			return plugin;
		}

		private void LoadPluginContent(Plugin plugin, PluginOptions options)
		{
			if(plugin == null)
				throw new ArgumentNullException(nameof(plugin));

			if(plugin.Status != PluginStatus.Loading)
				return;

			try
			{
				//解析插件对象
				_resolver.ResolvePluginWithoutManifest(plugin);

				//设置插件状态
				plugin.Status = PluginStatus.Loaded;

				//激发“PluginLoaded”事件
				this.OnPluginLoaded(new PluginLoadedEventArgs(plugin, options));
			}
			catch(Exception ex)
			{
				if(plugin.Parent == null)
					_topmosts.Remove(plugin.Name);
				else
					plugin.Parent.Children.Remove(plugin.Name);

				throw new PluginFileException(plugin.FilePath, $"The '{plugin.FilePath}' plugin file resolve failed.", ex);
			}
		}

		private void LoadPlugins(PluginCollection plugins, PluginOptions options)
		{
			if(plugins == null || plugins.Count < 1)
				return;

			Plugin hidden = null;
			var stack = new Stack<Plugin>();

			//注意：①. 先加载同级插件（忽略隐藏式插件）
			foreach(var plugin in plugins)
			{
				//确保同级插件栈内的所有插件一定都是未加载的插件
				if(plugin.Status == PluginStatus.Loading)
				{
					//如果是隐藏式插件，则先忽略它，待最后再来加载
					if(plugin.IsHidden)
					{
						hidden = plugin;
					}
					else
					{
						TryPushToStack(plugin, stack);
						this.LoadPlugin(stack, options);
					}
				}
			}

			//注意：②. 再依次加载各个子插件
			foreach(var plugin in plugins)
			{
				if(plugin.Status == PluginStatus.Loaded)
					this.LoadPlugins(plugin.Children, options);
			}

			//注意：③. 如果当前层级中含有隐藏式插件，则留待最后再来加载它
			if(hidden != null)
			{
				TryPushToStack(hidden, stack);
				this.LoadPlugin(stack, options);
			}
		}

		private void LoadPlugin(Stack<Plugin> stack, PluginOptions options)
		{
			if(stack == null || stack.Count < 1)
				return;

			var plugin = stack.Peek();

			if(this.CanLoad(plugin))
			{
				this.LoadPluginContent(stack.Pop(), options);
			}
			else
			{
				foreach(var dependency in plugin.Manifest.Dependencies)
				{
					if(this.CanLoad(dependency.Plugin))
						this.LoadPluginContent(dependency.Plugin, options);
					else
						TryPushToStack(dependency.Plugin, stack);
				}
			}

			if(stack.Count > 0)
				this.LoadPlugin(stack, options);
		}

		private bool CanLoad(Plugin plugin)
		{
			//如果当前插件状态不是未加载状态则返回假，即表示该插件现在还不能立即加载
			if(plugin == null || plugin.Status != PluginStatus.Loading)
				return false;

			if(plugin.Manifest.HasDependencies)
			{
				foreach(var dependency in plugin.Manifest.Dependencies)
				{
					if(dependency.Plugin == null)
					{
						dependency.Plugin = this.FindDependency(dependency.Name) ??
							throw new PluginException($"The '{plugin.Name}' plugin load failed. it's '{dependency.Name}' dependent plugin is not exists.");
					}

					//只要有一个依赖插件未加载完成则表示该插件不能立即加载(即返回假)
					if(dependency.Plugin.Status != PluginStatus.Loaded)
						return false;
				}
			}

			//表示当前插件的所有依赖插件都已加载完成则返回真(表示可以立即加载了)
			return true;
		}

		private Plugin FindDependency(string name)
		{
			foreach(var topmost in _topmosts)
			{
				var found = Find(topmost, name);
				if(found != null) return found;
			}

			return null;

			static Plugin Find(Plugin plugin, string name)
			{
				if(plugin != null && string.Equals(plugin.Name, name, StringComparison.OrdinalIgnoreCase))
					return plugin;

				if(plugin.HasChildren)
				{
					foreach(var child in plugin.Children)
					{
						var found = Find(child, name);

						if(found != null)
							return found;
					}
				}

				return null;
			}
		}

		private static bool TryPushToStack(Plugin plugin, Stack<Plugin> stack)
		{
			if(plugin == null || plugin.Status != PluginStatus.Loading || stack.Contains(plugin))
				return false;

			stack.Push(plugin);

			return true;
		}
		#endregion

		#region 激发事件
		private void OnLoaded(PluginLoadEventArgs args) => this.Loaded?.Invoke(this, args);
		private void OnLoading(PluginLoadEventArgs args) => this.Loading?.Invoke(this, args);
		private void OnPluginLoaded(PluginLoadedEventArgs args) => this.PluginLoaded?.Invoke(this, args);
		private void OnPluginLoading(PluginLoadingEventArgs args) => this.PluginLoading?.Invoke(this, args);
		private void OnPluginUnloaded(PluginUnloadedEventArgs args) => this.PluginUnloaded?.Invoke(this, args);
		private void OnPluginUnloading(PluginUnloadingEventArgs args) => this.PluginUnloading?.Invoke(this, args);
		private bool OnPluginUnloading(Plugin plugin)
		{
			PluginUnloadingEventArgs args = new PluginUnloadingEventArgs(plugin);
			this.OnPluginUnloading(args);
			return args.Cancel;
		}
		#endregion
	}
}
