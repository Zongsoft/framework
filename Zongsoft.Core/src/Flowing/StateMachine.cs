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
		private static readonly ConcurrentDictionary<Type, MethodInfo> _getHandlers = new ConcurrentDictionary<Type, MethodInfo>();
		#endregion

		#region 成员字段
		private int _disposed;
		private readonly Stack<object> _stack;
		private IStateHandlerProvider _handlers;
		private volatile Dictionary<object, object> _parameters;
		#endregion

		#region 构造函数
		public StateMachine()
		{
			_stack = new Stack<object>();
			_handlers = StateHandlerProvider.Default;
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
		public void Run<T>(State<T> state, IEnumerable<KeyValuePair<object, object>> parameters = null) where T : struct, IEquatable<T>
		{
			if(state == null)
				throw new ArgumentNullException(nameof(state));

			if(parameters != null)
			{
				foreach(var parameter in parameters)
					_parameters.TryAdd(parameter.Key, parameter.Value);
			}

			var context = GetContext(state);

			if(context == null)
				return;

			var handlers = this.GetHandlers<T>();

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

			foreach(var frame in frames)
			{
				this.OnStop(frame);
			}
		}

		private void OnStop(object context)
		{
			var contextType = context.GetType().GetTypeInfo();
			var argumentType = contextType.GenericTypeArguments[0];
			var contextInterfaceType = typeof(IStateContext<>).MakeGenericType(argumentType);

			var method = _getHandlers.GetOrAdd(argumentType, type => __GetHandlersMethodTemplate__.MakeGenericMethod(type));
			var invoker = Delegate.CreateDelegate(typeof(IEnumerable<>).MakeGenericType(typeof(IStateHandler<>).MakeGenericType(argumentType)), method);
			var hs = invoker.DynamicInvoke();
			var handlers = (System.Collections.IEnumerable)method.Invoke(this, null);

			foreach(var handler in handlers)
			{
				var contract = handler.GetType().GetInterfaceMap(typeof(IStateHandler<>).MakeGenericType(argumentType));

				for(int i = 0; i < contract.InterfaceMethods.Length; i++)
				{
					if(contract.InterfaceMethods[i].Name == "Finish")
					{
						var finish = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(contextInterfaceType), contract.TargetMethods[i]);
						contract.TargetMethods[i].Invoke(handler, new object[] { context });
					}
				}
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual IEnumerable<IStateHandler<T>> GetHandlers<T>() where T : struct, IEquatable<T>
		{
			return _handlers.GetHandlers<T>() ?? Array.Empty<IStateHandler<T>>();
		}
		#endregion

		#region 私有方法
		private bool IsTransferred<T>(State<T> state) where T : struct, IEquatable<T>
		{
			return state != null && _stack.Any(frame => frame is IStateContext<T> context && (context.Origin.Equals(state) || context.Destination.Equals(state)));
		}

		private IStateContext<T> GetContext<T>(State<T> destination) where T : struct, IEquatable<T>
		{
			//如果指定状态实例已经被处理过
			if(IsTransferred(destination))
				return null;

			//获取指定状态实例的当前状态
			var origin = destination.Diagram.GetState(destination) ?? throw new InvalidOperationException($"Unable to obtain the current state of the '{destination}' state instance.");

			//如果流程图定义了当前的流转向量则返回新建的上下文对象
			if(destination.Diagram.CanTransfer(origin, destination))
				return this.CreateContext(origin, destination);

			//返回空对象
			return null;
		}

		private StateContext<T> CreateContext<T>(State<T> origin, State<T> destination) where T : struct, IEquatable<T>
		{
			var contextType = typeof(StateContext<>).MakeGenericType(typeof(T));
			return (StateContext<T>)Activator.CreateInstance(contextType, new object[] { this, origin, destination });
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
	}
}
