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
using System.Reflection;
using System.Runtime.Loader;
using System.Collections.Concurrent;

namespace Zongsoft.Plugins
{
	public class PluginTree
	{
		#region 事件定义
		public event EventHandler<PluginMountEventArgs> Mounted;
		public event EventHandler<PluginMountEventArgs> Mounting;
		#endregion

		#region 私有函数
		private PluginTree(PluginOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
			this.Root = new PluginTreeNode(this);

			//创建插件加载器
			this.Loader = new PluginLoader(new PluginResolver(this));

			//侦听插件加载器的相关事件
			this.Loader.Loaded += delegate (object sender, PluginLoadEventArgs args)
			{
				//设置插件树当前状态为“已初始化”
				this.Status = PluginTreeStatus.Loaded;
			};

			this.Loader.Loading += delegate (object sender, PluginLoadEventArgs args)
			{
				//设置插件树当前状态为“已初始化”
				this.Status = PluginTreeStatus.Loading;

				//清空所有子节点
				this.Root.Children.Clear();
			};

			AssemblyLoadContext.Default.Resolving += Default_Resolving;
		}
		#endregion

		#region 静态构建
		private static readonly ConcurrentDictionary<PluginOptions, PluginTree> _instances = new ConcurrentDictionary<PluginOptions, PluginTree>();

		public static PluginTree Get(PluginOptions options)
		{
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			return _instances.GetOrAdd(options, key => new PluginTree(key));
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前插件树的选项。
		/// </summary>
		public PluginOptions Options { get; }

		/// <summary>
		/// 获取当前插件加载器对象。
		/// </summary>
		public PluginLoader Loader { get; }

		/// <summary>
		/// 获取插件树中的根节点。
		/// </summary>
		public PluginTreeNode Root { get; }

		/// <summary>
		/// 获取加载的根插件集，如果插件树还没加载则返回空。
		/// </summary>
		public PluginCollection Plugins { get => this.Loader.Plugins; }

		/// <summary>
		/// 获取插件树的状态。
		/// </summary>
		public PluginTreeStatus Status { get; private set; }
		#endregion

		#region 加载方法
		public PluginTree Load()
		{
			if(this.Status == PluginTreeStatus.None)
				this.Loader.Load(this.Options);

			return this;
		}
		#endregion

		#region 查找方法
		/// <summary>
		/// 查找指定路径的插件树节点。
		/// </summary>
		/// <param name="path">指定的路径。</param>
		/// <returns>如果查找成功则返回对应的插件树节点对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="path"/>参数为空或全空格字符串。</exception>
		public PluginTreeNode Find(string path)
		{
			return Root.Find(path);
		}

		public PluginTreeNode Find(params string[] paths)
		{
			return Root.Find(paths);
		}
		#endregion

		#region 创建节点
		/// <summary>
		/// 获取或创建指定路径的插件树节点。
		/// </summary>
		/// <param name="path">要获取或创建的插件路径。</param>
		/// <returns>返回存在的或者新建的节点对象，如果指定的<paramref name="path"/>路径参数是已存在的，则返回其对应的节点对象否则新建该节点。</returns>
		public PluginTreeNode EnsurePath(string path)
		{
			return this.EnsurePath(path, null, out _);
		}

		/// <summary>
		/// 获取或创建指定路径的插件树节点。
		/// </summary>
		/// <param name="path">要获取或创建的插件路径。</param>
		/// <param name="position">当创建指定路径对应的叶子节点时，由该参数确认其插入的位置，如果该参数为空(null)或空字符串则默认追加到同级节点的最后。</param>
		/// <returns>返回存在的或者新建的节点对象，如果指定的<paramref name="path"/>路径参数是已存在的，则返回其对应的节点对象否则新建该节点。</returns>
		public PluginTreeNode EnsurePath(string path, string position)
		{
			return this.EnsurePath(path, position, out _);
		}

		/// <summary>
		/// 获取或创建指定路径的插件树节点。
		/// </summary>
		/// <param name="path">要获取或创建的插件路径。</param>
		/// <param name="existed">输出参数，如果指定的路径已存在则返回真(true)，否则返回假(false)。</param>
		/// <returns>返回存在的或者新建的节点对象，如果指定的<paramref name="path"/>路径参数是已存在的，则返回其对应的节点对象否则新建该节点。</returns>
		public PluginTreeNode EnsurePath(string path, out bool existed)
		{
			return this.EnsurePath(path, null, out existed);
		}

		/// <summary>
		/// 获取或创建指定路径的插件树节点。
		/// </summary>
		/// <param name="path">要获取或创建的插件路径。</param>
		/// <param name="position">当创建指定路径对应的叶子节点时，由该参数确认其插入的位置，如果该参数为空(null)或空字符串则默认追加到同级节点的最后。</param>
		/// <param name="existed">输出参数，如果指定的路径已存在则返回真(true)，否则返回假(false)。</param>
		/// <returns>返回存在的或者新建的节点对象，如果指定的<paramref name="path"/>路径参数是已存在的，则返回其对应的节点对象否则新建该节点。</returns>
		public PluginTreeNode EnsurePath(string path, string position, out bool existed)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException("path");

			existed = true;
			PluginTreeNode node = Root;
			string[] parts = path.Split('/');

			for(int i = 0; i < parts.Length; i++)
			{
				string part = parts[i];

				if(string.IsNullOrEmpty(part))
				{
					if(node == Root)
						continue;
					else
						throw new PluginException("Invlaid '" + path + "' path.");
				}

				PluginTreeNode child = node.Children[part];

				if(child == null)
				{
					child = new PluginTreeNode(this, part);
					node.Children.Insert(child, position);
					existed = false;
				}

				node = child;
			}

			return node;
		}
		#endregion

		#region 挂载方法
		/// <summary>
		/// 挂载对象到插件树中。
		/// </summary>
		/// <param name="path">要挂载的路径。</param>
		/// <param name="value">要挂载的对象。</param>
		/// <returns>挂载成功则返回真(True)否则返回假(False)。</returns>
		/// <remarks>
		///		<para>注意：如果<paramref name="path"/>参数指定的路径对应的插件树节点已经存在，并且节点类型为<seealso cref="Zongsoft.Plugins.PluginTreeNodeType.Builtin"/>并且已经构建完成则返回假(False)。</para>
		///		<para>如果符合上面的条件，则激发<seealso cref="Zongsoft.Plugins.PluginTree.Mounting"/>事件，挂入成功后激发<seealso cref="Zongsoft.Plugins.PluginTree.Mounted"/>事件。</para>
		///		<para>如果<paramref name="path"/>参数指定的路径不存在，则创建它并挂载由<paramref name="value"/>参数指定的对象。</para>
		///	</remarks>
		public bool Mount(string path, object value)
		{
			return this.Mount(path, value, null);
		}

		/// <summary>
		/// 挂载对象到插件树中。
		/// </summary>
		/// <param name="path">要挂载的路径。</param>
		/// <param name="value">要挂载的对象。</param>
		/// <param name="position">当挂载路径对应的叶子节点不存在时，由该参数确认其插入的位置，如果该参数为空(null)或空字符串则默认追加到同级节点的最后。</param>
		/// <returns>挂载成功则返回真(True)否则返回假(False)。</returns>
		/// <remarks>
		///		<para>注意：如果<paramref name="path"/>参数指定的路径对应的插件树节点已经存在，并且节点类型为<seealso cref="Zongsoft.Plugins.PluginTreeNodeType.Builtin"/>并且已经构建完成则返回假(False)。</para>
		///		<para>如果符合上面的条件，则激发<seealso cref="Zongsoft.Plugins.PluginTree.Mounting"/>事件，挂入成功后激发<seealso cref="Zongsoft.Plugins.PluginTree.Mounted"/>事件。</para>
		///		<para>如果<paramref name="path"/>参数指定的路径不存在，则创建它并挂载由<paramref name="value"/>参数指定的对象。</para>
		///	</remarks>
		public bool Mount(string path, object value, string position)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));

			//确认要挂载的路径对应的节点
			PluginTreeNode node = this.EnsurePath(path, position);

			return this.Mount(node, value);
		}

		public bool Mount(PluginTreeNode node, object value)
		{
			if(node == null)
				throw new ArgumentNullException(nameof(node));

			if(value is Builtin || value is PluginElement || value is PluginTree)
				return false;

			//激发“Mounting”事件
			this.OnMounting(new PluginMountEventArgs(node.FullPath, value));

			//更新节点值
			node.Value = value;

			//激发“Mounted”事件
			this.OnMounted(new PluginMountEventArgs(node.FullPath, value));

			return true;
		}

		internal void MountBuiltin(string path, Builtin builtin)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException("path");

			if(builtin == null)
				throw new ArgumentNullException("builtin");

			var fullPath = PluginPath.Combine(path, builtin.Name);

			//激发“Mounting”事件
			this.OnMounting(new PluginMountEventArgs(fullPath, builtin));

			//创建对应的构件节点
			this.MountItem(fullPath, builtin, builtin.Position);

			//激发“Mounted”事件
			this.OnMounted(new PluginMountEventArgs(fullPath, builtin));
		}

		private void MountItem(string path, object item, string position)
		{
			var node = this.EnsurePath(path, position);

			if(node != null)
				node.Value = item;
		}
		#endregion

		#region 卸载方法
		/// <summary>
		/// 卸载指定路径的自定义对象。
		/// </summary>
		/// <param name="path">指定要卸载的路径。</param>
		/// <returns>如果成功卸载则返回被卸载的对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="path"/>参数为空或全空字符串。</exception>
		/// <exception cref="System.ArgumentException">当<paramref name="path"/>参数对应的节点对象的<see cref="Zongsoft.Plugins.PluginTreeNode.Tree"/>属性与当前插件树对象不是同一个引用。</exception>
		/// <remarks>
		///		<para>如果<paramref name="path"/>参数指定的路径不存在，则返回失败。</para>
		///		<para>如果<paramref name="path"/>参数对应的<seealso cref="Zongsoft.Plugins.PluginTreeNode"/>对象的节点类型(即<seealso cref="Zongsoft.Plugins.PluginTreeNode.NodeType"/>属性)不是自定义对象，则返回失败。</para>
		///		<para>有关本方法的卸载逻辑请参考<see cref="Unmount(PluginTreeNode)"/>重载方法。</para>
		/// </remarks>
		public object Unmount(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException("path");

			PluginTreeNode node = this.Find(path);

			if(node == null)
				return null;

			return this.Unmount(node);
		}

		/// <summary>
		/// 卸载指定插件树节点对应的自定义对象。
		/// </summary>
		/// <param name="node">指定要卸载对象的挂靠节点。</param>
		/// <returns>如果成功卸载则返回被卸载的对象，否则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="node"/>参数为空(null)。</exception>
		/// <exception cref="System.ArgumentException">当<paramref name="node"/>参数的<see cref="Zongsoft.Plugins.PluginTreeNode.Tree"/>属性与当前插件树对象不是同一个引用。</exception>
		/// <remarks>
		///		<para>注意：当前该方法的实现是不完备的，请谨慎使用！</para>
		///		<para>将<paramref name="node"/>参数指定的节点对象的Value属性置空，导致该节点类型成为路径节点(即空节点)。</para>
		///		<para>如果<paramref name="node"/>参数指定的节点对象没有子节点了，则将该节点从插件树中删除，否则保留。</para>
		/// </remarks>
		public object Unmount(PluginTreeNode node)
		{
			if(node == null)
				return null;

			if(!object.ReferenceEquals(node.Tree, this))
				throw new ArgumentException();

			//以永不构建的方式获取当前节点的目标对象
			var value = node.UnwrapValue(ObtainMode.Never);

			//将当前节点置空
			node.Value = null;

			//判断当前节点是否为叶子节点，如果是则将其删除
			if(node.Children.Count == 0)
				node.Remove();

			//返回被卸载的目标对象
			return value;
		}

		internal void Unmount(Builtin builtin)
		{
			if(builtin == null)
				return;

			this.UnmountItem(builtin.Plugin, builtin.FullPath);
		}

		private void UnmountItem(Plugin plugin, string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException("path");

			PluginTreeNode node = this.Find(path);

			if(node != null)
				this.UnmountItem(plugin, node);
		}

		private void UnmountItem(Plugin plugin, PluginTreeNode node)
		{
			if(node == null)
				return;

			if(plugin != null)
			{
				foreach(PluginTreeNode child in node.Children)
				{
					this.UnmountItem(plugin, child);
				}
			}

			if(node.NodeType == PluginTreeNodeType.Custom)
			{
				this.Unmount(node);
				return;
			}

			if(node.NodeType == PluginTreeNodeType.Builtin)
			{
				Builtin builtin = (Builtin)node.Value;

				if(string.ReferenceEquals(builtin.Plugin, plugin))
				{
					IBuilder builder = node.Plugin.GetBuilder(builtin.Scheme);
					if(builder != null)
						builder.Destroy(Builders.BuilderContext.CreateContext(builder, builtin));

					plugin.UnregisterBuiltin(builtin);
					node.Value = null;
				}
			}

			if(node.Children.Count < 1 && node.Parent != null)
				node.Parent.Children.Remove(node);
		}
		#endregion

		#region 事件方法
		internal void OnMounted(PluginMountEventArgs args)
		{
			if(Mounted != null)
				this.Mounted(this, args);
		}

		internal void OnMounting(PluginMountEventArgs args)
		{
			if(Mounting != null)
				this.Mounting(this, args);
		}
		#endregion

		#region 类库加载
		private Assembly Default_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
		{
			foreach(var plugin in this.Plugins)
			{
				var assembly = LoadAssembly(plugin, assemblyName);

				if(assembly != null)
					return assembly;
			}

			return null;
		}

		private static Assembly LoadAssembly(Plugin plugin, AssemblyName assemblyName)
		{
			var resolver = plugin.Manifest.Resolver;

			if(resolver != null)
			{
				var filePath = resolver.ResolveAssemblyToPath(assemblyName);

				if(!string.IsNullOrEmpty(filePath))
					return AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath);
			}

			if(plugin.HasChildren)
			{
				foreach(var child in plugin.Children)
				{
					var assembly = LoadAssembly(child, assemblyName);

					if(assembly != null)
						return assembly;
				}
			}

			return null;
		}
		#endregion

		#region 内部方法
		internal object GetOwner(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return null;

			return this.GetOwner(this.Find(path));
		}

		/// <summary>
		/// 获取指定节点的所有者对象。
		/// </summary>
		/// <param name="node">要获取的所有者对象的节点。</param>
		/// <returns>返回指定节点的所有者对象，如果没有则返回空(null)。</returns>
		/// <remarks>
		///		<para>注意：该方法不会引起上级节点的创建动作，可确保在<see cref="Zongsoft.Plugins.IBuilder"/>构建器中使用而不会导致循环创建的问题。</para>
		/// </remarks>
		internal object GetOwner(PluginTreeNode node)
		{
			var ownerNode = this.GetOwnerNode(node);

			if(ownerNode != null)
				return ownerNode.Value;

			return null;
		}

		public PluginTreeNode GetOwnerNode(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return null;

			return this.GetOwnerNode(this.Find(path));
		}

		public PluginTreeNode GetOwnerNode(PluginTreeNode node)
		{
			if(node == null)
				return null;

			var parent = node.Parent;

			if(parent == null)
				return null;

			if(parent.NodeType == PluginTreeNodeType.Empty)
				return this.GetOwnerNode(parent);

			return parent;
		}
		#endregion
	}
}
