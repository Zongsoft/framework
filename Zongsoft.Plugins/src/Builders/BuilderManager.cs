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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Plugins.Builders
{
	public class BuilderManager
	{
		#region 事件定义
		public event EventHandler<BuilderEventArgs> BuildCompleted;
		#endregion

		#region 单例字段
		public static readonly BuilderManager Current = new BuilderManager();
		#endregion

		#region 私有变量
		private readonly object _syncRoot;
		private readonly ConcurrentStack<BuildToken> _stack;
		#endregion

		#region 私有构造
		private BuilderManager()
		{
			_syncRoot = new object();
			_stack = new ConcurrentStack<BuildToken>();
		}
		#endregion

		#region 公共方法
		public object Build(Builtin builtin, BuilderSettings settings)
		{
			//获取当前构件的构建器对象
			IBuilder builder = this.GetBuilder(builtin);

			lock(_syncRoot)
			{
				return this.BuildCore(BuilderContext.CreateContext(builder, builtin, settings));
			}
		}
		#endregion

		#region 保护方法
		protected virtual void OnBuildCompleted(BuilderContext context)
		{
			this.BuildCompleted?.Invoke(this, new BuilderEventArgs(context));
		}
		#endregion

		#region 私有方法
		private object BuildCore(BuilderContext context)
		{
			//判断当前待创建的构件是否正在当前构建序列中，如果是则立即返回其目标值
			if(this.TryGetBuilding(context.Builtin, out var value))
				return value;

			try
			{
				//将创建成功的目标对象保存到当前调用栈中，以便在子构件的构建过程中进行循环创建调用作检测之用
				//注意：将当前创建的目标对象保存到调用栈中的操作必须在构建子构件集合之前完成！
				_stack.Push(new BuildToken(context.Builtin, context.Result));

				//调用构建器进行构建目标对象
				var target = context.Builder.Build(context);

				if(context.Result == null)
					context.Result = target;

				//如果构建的目标对象保存在对应的构件(Builtin)的Value属性中
				context.Builtin.Value = context.Result;

				//如果构建结果不为空并且不取消后续构建操作的话，则继续构建子集
				if((context.Settings == null || !context.Settings.HasFlags(BuilderSettingsFlags.IgnoreChildren)) && context.Result != null && !context.Cancel)
				{
					BuildChildren(context.Node, context.Depth, context.Result, context.Node);
				}

				//设置当前构件为构建已完成标志
				context.Builtin.IsBuilded = (context.Result != null);
			}
			finally
			{
				_stack.TryPop(out _);
			}

			//回调构建完成通知方法
			context?.Settings?.Builded?.Invoke(context);

			//通知构建器本次构建工作已完成
			context.Builder.OnBuildComplete(context);

			//激发构建完成事件
			this.OnBuildCompleted(context);

			//返回生成的目标对象
			return context.Result;
		}

		private void BuildChildren(PluginTreeNode node, int depth, object owner, PluginTreeNode ownerNode)
		{
			if(node == null)
				return;

			foreach(var child in node.Children)
			{
				switch(child.NodeType)
				{
					case PluginTreeNodeType.Builtin:
						this.BuildChild((Builtin)child.Value, depth + 1, owner, ownerNode);
						break;
					case PluginTreeNodeType.Empty:
						this.BuildChildren(child, depth + 1, owner, ownerNode);
						break;
					case PluginTreeNodeType.Custom:
						this.BuildChildren(child, depth + 1, child.Value, child);
						break;
				}
			}
		}

		private void BuildChild(Builtin builtin, int depth, object owner, PluginTreeNode ownerNode)
		{
			//获取当前构件的构建器对象
			IBuilder builder = this.GetBuilder(builtin);

			if(builtin.IsBuilded)
			{
				if(owner != null && builder is IAppender appender)
					appender.Append(new AppenderContext(builtin.Value, builtin.Node, owner, ownerNode, AppenderBehavior.Appending));
			}
			else
			{
				this.BuildCore(BuilderContext.CreateContext(builder, builtin, null, depth, owner, ownerNode));
			}
		}

		private IBuilder GetBuilder(Builtin builtin)
		{
			if(builtin == null)
				throw new ArgumentNullException("builtin");

			//获取当前构件的构建器对象
			IBuilder builder = builtin.Plugin.GetBuilder(builtin.Scheme);

			if(builder == null)
				throw new PluginException(string.Format(@"The name is '{0}' of Builder not found in '{1}' plugin." + Environment.NewLine + "{2}", builtin.Scheme, builtin.Plugin.Name, builtin.Plugin.FilePath));

			return builder;
		}

		private bool TryGetBuilding(Builtin builtin, out object value)
		{
			value = null;

			foreach(var item in _stack)
			{
				if(object.ReferenceEquals(item.Builtin, builtin))
				{
					value = item.Target;
					return true;
				}
			}

			return false;
		}
		#endregion

		#region 嵌套子类
		private class BuildToken
		{
			#region 公共字段
			internal Builtin Builtin;
			internal object Target;
			#endregion

			#region 构造函数
			internal BuildToken(Builtin builtin, object target)
			{
				this.Builtin = builtin ?? throw new ArgumentNullException(nameof(builtin));
				this.Target = target;
			}
			#endregion

			#region 重写方法
			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != typeof(BuildToken))
					return false;

				return object.Equals(this.Builtin, ((BuildToken)obj).Builtin);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Builtin);
			}

			public override string ToString()
			{
				return this.Builtin.ToString();
			}
			#endregion
		}
		#endregion
	}
}
