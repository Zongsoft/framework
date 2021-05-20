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

namespace Zongsoft.Plugins
{
	public class PluginTreeNode : PluginElement
	{
		#region 成员字段
		private object _value;
		private string _path;
		private string _fullPath;
		private PluginTree _tree;
		private PluginTreeNodeType _nodeType;
		private PluginTreeNode _parent;
		private PluginTreeNodeCollection _children;
		private PluginExtendedPropertyCollection _properties;
		#endregion

		#region 构造函数
		public PluginTreeNode(PluginTree tree, string name) : this(tree, name, null)
		{
		}

		public PluginTreeNode(PluginTree tree, string name, object value) : base(name, null)
		{
			_tree = tree ?? throw new ArgumentNullException(nameof(tree));
			_children = new PluginTreeNodeCollection(this);
			this.Value = value;
		}

		//该构造函数用来构造根节点专用
		internal PluginTreeNode(PluginTree tree) : base("/", true)
		{
			_tree = tree ?? throw new ArgumentNullException(nameof(tree));
			_children = new PluginTreeNodeCollection(this);
			this.Value = null;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取插件树节点的类型。
		/// </summary>
		public PluginTreeNodeType NodeType
		{
			get
			{
				return _nodeType;
			}
		}

		/// <summary>
		/// 获取当前插件树节点挂载的对象。
		/// </summary>
		/// <remarks>
		///		<list type="number">
		///			<item>
		///				<term>如果当前<see cref="NodeType"/>属性为Empty，则返回空(null)。</term>
		///			</item>
		///			<item>
		///				<term>如果当前<see cref="NodeType"/>属性为Builtin，则返回对应的<seealso cref="Builtin"/>对象。</term>
		///			</item>
		///			<item>
		///				<term>如果当前<see cref="NodeType"/>属性为Custom，则返回对应的自定义对象。</term>
		///			</item>
		///		</list>
		/// </remarks>
		public object Value
		{
			get
			{
				return _value;
			}
			internal set
			{
				if(object.ReferenceEquals(_value, value))
					return;

				this.SetValue(value);
			}
		}

		/// <summary>
		/// 获取节点中挂载的目标对象类型，如果节点类型为<see cref="PluginTreeNodeType.Empty"/>则返回空(null)，该属性始终不会引发目标对象的创建动作。
		/// </summary>
		/// <remarks>
		///		<para>注意：该方法不会激发节点类型为<see cref="PluginTreeNodeType.Builtin"/>的创建动作，因此适合在不需要获取目标值的场景中使用该方法来获取其类型。</para>
		///		<para>当节点类型为<see cref="PluginTreeNodeType.Builtin"/>时的更详细行为请参考<seealso cref="Builtin.GetValueType()"/>方法的描述信息。</para>
		/// </remarks>
		public Type ValueType
		{
			get
			{
				switch(_nodeType)
				{
					case PluginTreeNodeType.Empty:
						return null;
					case PluginTreeNodeType.Custom:
						return _value.GetType();
					case PluginTreeNodeType.Builtin:
						return ((Builtin)_value).GetValueType();
					default:
						throw new NotSupportedException();
				}
			}
		}

		/// <summary>
		/// 获取插件树节点所在的插件树对象。
		/// </summary>
		public PluginTree Tree
		{
			get
			{
				return _tree;
			}
		}

		/// <summary>
		/// 获取插件树节点的父级节点。
		/// </summary>
		public PluginTreeNode Parent
		{
			get
			{
				return _parent;
			}
			internal set
			{
				if(object.ReferenceEquals(_parent, value))
					return;

				_parent = value;

				//重置路径变量，这将导致Path、FullPath属性重新计算
				_path = null;
				_fullPath = null;
			}
		}

		/// <summary>
		/// 获取插件树节点的子级节点集。
		/// </summary>
		public PluginTreeNodeCollection Children
		{
			get
			{
				return _children;
			}
		}

		/// <summary>
		/// 获取插件树节点的路径，该路径不含当前节点名称，如果是根节点则返回空字符串("")。
		/// </summary>
		public string Path
		{
			get
			{
				if(_path == null)
				{
					if(_parent == null)
						_path = string.Empty;
					else
						_path = _parent.FullPath;
				}

				return _path;
			}
		}

		/// <summary>
		/// 获取插件树节点的完整路径，该路径包含当前节点的名称。
		/// </summary>
		public string FullPath
		{
			get
			{
				if(_fullPath == null)
				{
					if(_parent == null)
						_fullPath = this.Name;
					else
						_fullPath = _parent.FullPath.TrimEnd('/') + "/" + this.Name;
				}

				return _fullPath;
			}
		}

		/// <summary>
		/// 获取当前插件节点是否具有扩展属性。
		/// </summary>
		public bool HasProperties
		{
			get
			{
				var properties = _properties;
				return properties != null && properties.Count > 0;
			}
		}

		/// <summary>
		/// 获取插件节点的扩展属性集。
		/// </summary>
		public PluginExtendedPropertyCollection Properties
		{
			get
			{
				if(_properties == null)
					System.Threading.Interlocked.CompareExchange(ref _properties, new PluginExtendedPropertyCollection(this), null);

				return _properties;
			}
		}
		#endregion

		#region 公共方法
		public PluginTreeNode Find(string path)
		{
			return this.Find(new string[] { path });
		}

		public PluginTreeNode Find(params string[] paths)
		{
			if(paths == null || paths.Length == 0)
				return null;

			var node = this;

			foreach(var path in paths)
			{
				var start = -1;
				var part = string.Empty;

				//如果路径为空则忽略
				if(string.IsNullOrWhiteSpace(path))
					continue;

				do
				{
					var index = path.IndexOf('/', start + 1);

					if(index == 0)
						part = string.Empty;
					else if(index < 0)
						part = start < 0 ? path : path.Substring(start + 1);
					else if(index > 0)
						part = path.Substring(start + 1, index - (start + 1)); //注意：(start+1) 这个小括号不能去掉

					start = index;

					switch(part)
					{
						case "":
							node = _tree.Root;
							break;
						case ".":
							break;
						case "..":
							node = _parent ?? _tree.Root;
							break;
						default:
							node = node._children[part];
							break;
					}

					//如果查找失败则返回
					if(node == null)
						return null;
				} while(start >= 0);
			}

			return node;
		}

		public object UnwrapValue(ObtainMode obtainMode, Builders.BuilderSettings settings = null)
		{
			if(_nodeType == PluginTreeNodeType.Builtin)
				return ((Builtin)_value).GetValue(obtainMode, settings);

			return _value;
		}
		#endregion

		#region 内部方法
		internal void Remove()
		{
			var parent = this.Parent;

			if(parent != null)
				parent.Children.Remove(this);
		}

		internal object Resolve(string pathText = null, ObtainMode obtainMode = ObtainMode.Auto, Type targetType = null)
		{
			var node = this;
			Reflection.Expressions.IMemberExpression accessor = null;

			if(!string.IsNullOrWhiteSpace(pathText))
			{
				var expression = Collections.HierarchicalExpression.Parse(pathText);

				node = this.Find(expression.Path) ??
					throw new PluginException($"Not found the PluginTreeNode with '{expression.Path}' path.");

				accessor = expression.Accessor;
			}

			//获取指定路径的目标对象
			object target = node.UnwrapValue(obtainMode, targetType == null ? null : new Builders.BuilderSettings(targetType));

			if(target != null && accessor != null)
				return Reflection.Expressions.MemberExpressionEvaluator.Default.GetValue(accessor, target);

			return target;
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(this.Plugin == null)
				return this.FullPath;
			else
				return string.Format("[{0}]{1}@{2}",this.NodeType, this.FullPath, (this.Plugin == null ? string.Empty : this.Plugin.Name));
		}
		#endregion

		#region 私有方法
		private void SetValue(object value)
		{
			if(value == null)
			{
				_value = null;
				_nodeType = PluginTreeNodeType.Empty;
				this.Plugin = null;
				return;
			}

			if(value is Builtin builtin)
			{
				if(builtin.Node != null)
					throw new InvalidOperationException();

				//将构建的所属节点指向本节点
				builtin.Node = this;

				//更新当前节点的属性值
				_value = builtin;
				_nodeType = PluginTreeNodeType.Builtin;
				this.Plugin = builtin.Plugin;
			}
			else
			{
				_value = value;
				_nodeType = PluginTreeNodeType.Custom;
				this.Plugin = null;

				var properties = _properties;

				if(properties != null && properties.Count > 0)
				{
					foreach(PluginExtendedProperty property in properties)
					{
						//Reflection.MemberAccess.TryGetMemberType(_value, property.Name, out var propertyType, out _);
						//Reflection.MemberAccess.TrySetMemberValue(_value, property.Name, property.GetValue(propertyType));

						Reflection.Reflector.SetValue(ref _value, property.Name, property.GetValue(null));
					}
				}
			}
		}
		#endregion
	}
}
