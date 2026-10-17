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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Runtime.ExceptionServices;

namespace Zongsoft.Plugins.Builders;

public class BuilderManager
{
	#region 单例字段
	public static readonly BuilderManager Current = new();
	#endregion

	#region 私有字段
	private readonly object _syncRoot = new();
	private readonly Stack<BuilderContext> _stack = new();
	private readonly HashSet<Builtin> _types = new(ReferenceEqualityComparer.Instance);
	private readonly List<(BuilderContext Context, bool Append)> _pending = new();
	#endregion

	#region 私有构造
	private BuilderManager() { }
	#endregion

	#region 公共方法
	/// <summary>重新创建并装配指定构件。</summary>
	/// <param name="builtin">待构建的构件。</param>
	/// <param name="settings">构建结果的类型提示。</param>
	/// <returns>构件的目标对象。</returns>
	public object Build(Builtin builtin, BuilderSettings settings)
	{
		lock(_syncRoot)
			return this.Execute(BuilderContext.CreateContext(GetBuilder(builtin), builtin, settings), false, true);
	}
	#endregion

	#region 内部方法
	internal Type GetValueType(Builtin builtin)
	{
		lock(_syncRoot)
		{
			if(builtin.HasValue)
				return builtin.Value.GetType();

			if(builtin.BuiltinType != null)
				return builtin.BuiltinType.Type;

			if(!_types.Add(builtin))
				throw CreateCircularBuildException(builtin);

			try
			{
				return GetBuilder(builtin).GetValueType(builtin);
			}
			finally
			{
				_types.Remove(builtin);
			}
		}
	}

	internal object GetValue(Builtin builtin, ObtainMode mode, BuilderSettings settings)
	{
		lock(_syncRoot)
		{
			if(mode == ObtainMode.Never)
				return builtin.Value;

			if(this.TryGetContext(builtin, out var context))
				return GetValue(context);

			if(mode == ObtainMode.Alway)
				return this.Execute(BuilderContext.CreateContext(GetBuilder(builtin), builtin, settings), false, false);

			builtin.BuildFailure?.Throw();

			return builtin.IsBuilded ? builtin.Value :
				this.Execute(BuilderContext.CreateContext(GetBuilder(builtin), builtin, settings), true, true);
		}
	}

	//仅用于定位所属模块：取得实例而不遍历子节点，OnBuilded 在正常装配完成时调用。
	internal object GetInstance(Builtin builtin)
	{
		lock(_syncRoot)
		{
			if(this.TryGetContext(builtin, out var activeContext))
				return activeContext.Result;

			builtin.BuildFailure?.Throw();

			if(builtin.HasValue)
				return builtin.Value;

			var context = BuilderContext.CreateContext(GetBuilder(builtin), builtin);
			_stack.Push(context);

			try
			{
				this.CreateInstance(context);
				return context.Result;
			}
			finally
			{
				_stack.Pop();
			}
		}
	}
	#endregion

	#region 私有方法
	private object Execute(BuilderContext context, bool reuse, bool append)
	{
		try
		{
			var result = this.BuildCore(context, reuse, append);

			//属性引用可能先创建父对象；待当前引用链返回后再完成父节点，避免重入子节点。
			while(_stack.Count == 0 && _pending.Count > 0)
			{
				var pending = _pending.ToArray();
				_pending.Clear();
				var completed = false;

				foreach(var item in pending)
				{
					this.BuildCore(item.Context, true, item.Append);
					completed |= item.Context.Builtin.IsBuilded;
				}

				if(!completed && _pending.Count > 0)
					throw CreateCircularBuildException(_pending[0].Context.Builtin);
			}

			return result;
		}
		catch(Exception exception)
		{
			if(_stack.Count == 0)
			{
				foreach(var item in _pending)
					item.Context.Builtin.BuildFailure ??= ExceptionDispatchInfo.Capture(exception);

				_pending.Clear();
			}

			throw;
		}
	}

	private object BuildCore(BuilderContext context, bool reuse, bool append)
	{
		if(this.TryGetContext(context.Builtin, out var activeContext))
			return GetValue(activeContext);

		var builtin = context.Builtin;
		_stack.Push(context);

		try
		{
			if(reuse)
			{
				builtin.BuildFailure?.Throw();
				context.Result = builtin.Value;

				if(builtin.HasValue)
				{
					context.AppendToOwner = builtin.AppendToOwner;
					context.BuildChildren = builtin.BuildChildren;
				}
			}
			else
			{
				builtin.BuildFailure = null;
				builtin.IsBuilded = false;
			}

			if(!reuse || context.Result == null)
				this.CreateInstance(context);

			if(context.Result == null)
				return null;

			if(builtin.IsBuilded)
			{
				if(append)
					AppendToOwner(context);

				return context.Result;
			}

			if(builtin.BuildChildren && !this.BuildChildren(context.Node, context.Depth, context.Result, context.Node))
			{
				if(!_pending.Exists(item => ReferenceEquals(item.Context.Builtin, builtin)))
					_pending.Add((context, append));

				return context.Result;
			}

			//完成初始化与追加分别由构建器和管理器承担，缓存路径不重放完成钩子。
			if(context.Builder is IBuilderLifecycle lifecycle)
				lifecycle.OnBuilded(context);

			if(append)
				AppendToOwner(context);

			builtin.IsBuilded = true;
			return context.Result;
		}
		catch(Exception exception)
		{
			builtin.IsBuilded = false;

			//存在实例时可能已有装配副作用，后续读取不能将它当作成功结果或自动重放。
			if(builtin.HasValue)
				builtin.BuildFailure ??= ExceptionDispatchInfo.Capture(exception);

			throw;
		}
		finally
		{
			_stack.Pop();
		}
	}

	private void CreateInstance(BuilderContext context)
	{
		if(context.Builder is IBuilderLifecycle lifecycle)
			lifecycle.OnBuilding(context);

		context.Result = context.Builder.Build(context);
		context.Builtin.AppendToOwner = context.AppendToOwner;
		context.Builtin.BuildChildren = context.BuildChildren;
		context.Builtin.Value = context.Result;
	}

	private bool BuildChildren(PluginTreeNode node, int depth, object owner, PluginTreeNode ownerNode)
	{
		if(node == null)
			return true;

		var completed = true;

		foreach(var child in node.Children)
		{
			switch(child.NodeType)
			{
				case PluginTreeNodeType.Builtin:
					var builtin = (Builtin)child.Value;

					if(this.TryGetContext(builtin, out _))
					{
						completed = false;
						break;
					}

					this.BuildCore(BuilderContext.CreateContext(GetBuilder(builtin), builtin, null, depth + 1, owner, ownerNode), true, true);
					completed &= builtin.IsBuilded || !builtin.HasValue;
					break;
				case PluginTreeNodeType.Empty:
					completed &= this.BuildChildren(child, depth + 1, owner, ownerNode);
					break;
				case PluginTreeNodeType.Custom:
					completed &= this.BuildChildren(child, depth + 1, child.Value, child);
					break;
			}
		}

		return completed;
	}

	private static void AppendToOwner(BuilderContext context)
	{
		var builtin = context.Builtin;
		var ownerNode = context.OwnerNode;
		var owner = context.Owner;

		if(owner == null || ownerNode?.NodeType != PluginTreeNodeType.Builtin || !builtin.AppendToOwner)
			return;

		//同一节点向同一所有者追加同一实例至多一次，引用求值与树遍历共用此规则。
		if(ReferenceEquals(builtin.AppendedOwner, owner) && ReferenceEquals(builtin.AppendedValue, context.Result))
			return;

		if(GetBuilder((Builtin)ownerNode.Value) is IAppender appender &&
			appender.Append(new AppenderContext(context.Result, context.Node, owner, ownerNode, AppenderBehavior.Appending)))
		{
			builtin.AppendedOwner = owner;
			builtin.AppendedValue = context.Result;
		}
	}

	private static IBuilder GetBuilder(Builtin builtin)
	{
		ArgumentNullException.ThrowIfNull(builtin);

		return builtin.Plugin.GetBuilder(builtin.Scheme) ?? throw new PluginException(string.Format(
			Properties.Resources.Plugin_BuilderNotFound_Message, builtin.Scheme, builtin.Plugin.Name, builtin.Plugin.FilePath, Environment.NewLine));
	}

	private bool TryGetContext(Builtin builtin, out BuilderContext context)
	{
		foreach(var item in _stack)
		{
			if(ReferenceEquals(item.Builtin, builtin))
			{
				context = item;
				return true;
			}
		}

		context = null;
		return false;
	}

	private static object GetValue(BuilderContext context) => context.Result ?? throw CreateCircularBuildException(context.Builtin);
	private static PluginException CreateCircularBuildException(Builtin builtin) => new(string.Format(Properties.Resources.Builtin_CircularConstruction_Message, builtin.FullPath));
	#endregion
}
