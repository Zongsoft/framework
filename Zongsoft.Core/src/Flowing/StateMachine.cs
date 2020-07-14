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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Flowing
{
	public class StateMachine : IStateMachine
	{
		#region 静态变量
		private static readonly MethodInfo __GetHandlersMethodTemplate__ = typeof(StateMachine).GetMethod(nameof(GetHandlers), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly ConcurrentDictionary<ValueTuple<Type, Type>, MethodInfo> _getHandlers = new ConcurrentDictionary<ValueTuple<Type, Type>, MethodInfo>();
		#endregion

		#region 成员字段
		private int _disposed;
		private readonly Stack<object> _stack;
		private IStateHandlerProvider _handlers;
		private volatile Dictionary<object, object> _parameters;
		#endregion

		#region 构造函数
		public StateMachine(IServiceProvider serviceProvider)
		{
			_stack = new Stack<object>();
			_handlers = new StateHandlerProvider(serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)));
		}
		#endregion

		#region 公共属性
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		public IDictionary<object, object> Parameters
		{
			get
			{
				if(_parameters == null)
				{
					lock(_stack)
					{
						if(_parameters == null)
							_parameters = new Dictionary<object, object>();
					}
				}

				return _parameters;
			}
		}

		public IStateHandlerProvider Handlers
		{
			get => _handlers;
			set => _handlers = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 运行方法
		public void Run<TKey, TValue>(State<TKey, TValue> state, string description, IEnumerable<KeyValuePair<object, object>> parameters = null) where TKey : struct, IEquatable<TKey> where TValue : struct
		{
			if(state == null)
				throw new ArgumentNullException(nameof(state));

			var context = GetContext(state, description);

			if(context == null)
				return;

			if(parameters != null)
			{
				foreach(var parameter in parameters)
					context.Parameters.TryAdd(parameter.Key, parameter.Value);
			}

			var handlers = this.GetHandlers<TKey, TValue>();

			foreach(var handler in handlers)
				state.Diagram.Transfer(context, handler);

			_stack.Push(context);
		}
		#endregion

		#region  停止方法
		private void Stop()
		{
			if(_stack.Count == 0)
				return;

			var frames = _stack.Reverse().ToArray();

			using(var transaction = new Transactions.Transaction())
			{
				for(int i = 0; i < frames.Length; i++)
				{
					this.OnStop(frames[i]);
				}

				//提交事务
				transaction.Commit();
			}
		}

		private void OnStop(object context)
		{
			var contextType = context.GetType();
			var method = _getHandlers.GetOrAdd((contextType.GenericTypeArguments[0], contextType.GenericTypeArguments[1]), key => __GetHandlersMethodTemplate__.MakeGenericMethod(key.Item1, key.Item2));
			var handlers = (System.Collections.IEnumerable)method.Invoke(this, null);

			foreach(var handler in handlers)
			{
				var contract = handler.GetType().GetInterfaceMap(typeof(IStateHandler<,>).MakeGenericType(contextType.GenericTypeArguments));

				for(int i = 0; i < contract.InterfaceMethods.Length; i++)
				{
					if(contract.InterfaceMethods[i].Name == "Finish")
					{
						contract.TargetMethods[i].Invoke(handler, new object[] { context });
						break;
					}
				}
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual IEnumerable<IStateHandler<TKey, TValue>> GetHandlers<TKey, TValue>() where TKey : struct, IEquatable<TKey> where TValue : struct
		{
			return _handlers.GetHandlers<TKey, TValue>() ?? Array.Empty<IStateHandler<TKey, TValue>>();
		}
		#endregion

		#region 私有方法
		private bool IsTransferred<TKey, TValue>(State<TKey, TValue> state, out TValue? origin) where TKey : struct, IEquatable<TKey> where TValue : struct
		{
			origin = null;

			if(state == null)
				return false;

			foreach(var frame in _stack)
			{
				if(frame is IStateContext<TKey, TValue> context)
				{
					if(context.Key.Equals(state.Key))
					{
						if(origin == null)
							origin = context.State.Destination;

						if(context.State.Contains(state.Value))
							return true;
					}
				}
			}

			return false;
		}

		private IStateContext<TKey, TValue> GetContext<TKey, TValue>(State<TKey, TValue> destination, string description) where TKey : struct, IEquatable<TKey> where TValue : struct
		{
			//如果指定状态实例已经被处理过
			if(IsTransferred(destination, out var origin))
				return null;

			//获取指定状态实例的当前状态
			if(origin == null)
				origin = destination.Diagram.GetState(destination.Key)?.Value;

			//如果源状态与目的状态相同则返回空
			if(origin == null || origin.Value.Equals(destination.Value))
				return null;

			//如果流程图定义了当前的流转向量则返回新建的上下文对象
			if(destination.Diagram.CanTransfer(origin.Value, destination.Value))
			{
				return this.CreateContext(destination.Diagram, destination.Key, origin.Value, destination.Value, description);
			}

			//返回空对象
			return null;
		}

		private StateContext<TKey, TValue> CreateContext<TKey, TValue>(IStateDiagram<TKey, TValue> diagram, TKey key, TValue origin, TValue destination, string description) where TKey : struct, IEquatable<TKey> where TValue : struct
		{
			var contextType = typeof(StateContext<,>).MakeGenericType(typeof(TKey), typeof(TValue));
			return (StateContext<TKey, TValue>)Activator.CreateInstance(contextType, new object[] { this, diagram, key, origin, destination, description });
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(System.Threading.Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			if(disposing)
			{
				this.Stop();

				if(_stack != null)
					_stack.Clear();
			}
		}
		#endregion

		#region 嵌套子类
		private class StateHandlerProvider : IStateHandlerProvider
		{
			#region 成员字段
			private readonly IServiceProvider _serviceProvider;
			#endregion

			#region 构造函数
			internal StateHandlerProvider(IServiceProvider serviceProvider)
			{
				_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			}
			#endregion

			#region 公共方法
			public IEnumerable<IStateHandler<TKey, TValue>> GetHandlers<TKey, TValue>() where TKey : struct, IEquatable<TKey> where TValue : struct
			{
				return (IEnumerable<IStateHandler<TKey, TValue>>)_serviceProvider.GetService(typeof(IEnumerable<IStateHandler<TKey, TValue>>));
			}
			#endregion
		}
		#endregion
	}
}
