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
using System.Collections.Generic;

namespace Zongsoft.Services;

[Serializable]
public class CommandExecutedEventArgs : EventArgs
{
	#region 成员变量
	private CommandContext _context;
	private object _parameter;
	private object _result;
	private IDictionary<string, object> _extendedProperties;
	private bool _exceptionHandled;
	private Exception _exception;
	#endregion

	#region 构造函数
	public CommandExecutedEventArgs(CommandContext context, object result)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		_context = context;
		_result = result;
	}

	public CommandExecutedEventArgs(CommandContext context, Exception exception)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		_context = context;
		_exception = exception;
	}

	/// <summary>构造一个命令执行成功的事件参数对象。</summary>
	/// <param name="parameter">命令执行参数对象。</param>
	/// <param name="result">命令执行的结果。</param>
	/// <param name="extendedProperties">指定的扩展属性集。</param>
	public CommandExecutedEventArgs(object parameter, object result, IDictionary<string, object> extendedProperties = null)
	{
		var context = parameter as CommandContext;

		if(context != null)
		{
			_context = context;

			if(extendedProperties != null && extendedProperties.Count > 0)
			{
				foreach(var pair in extendedProperties)
					context.States[pair.Key] = pair.Value;
			}
		}
		else
		{
			_parameter = parameter;
			_extendedProperties = extendedProperties;
		}

		_result = result;
	}

	/// <summary>构造一个命令执行失败的事件参数对象。</summary>
	/// <param name="parameter">命令执行参数对象。</param>
	/// <param name="exception">命令执行失败的异常对象。</param>
	/// <param name="extendedProperties">指定的扩展属性集。</param>
	public CommandExecutedEventArgs(object parameter, Exception exception, IDictionary<string, object> extendedProperties = null)
	{
		var context = parameter as CommandContext;

		if(context != null)
		{
			_context = context;

			if(extendedProperties != null && extendedProperties.Count > 0)
			{
				foreach(var pair in extendedProperties)
					context.States[pair.Key] = pair.Value;
			}
		}
		else
		{
			_parameter = parameter;
			_extendedProperties = extendedProperties;
		}

		_exception = exception;
	}
	#endregion

	#region 公共属性
	/// <summary>获取命令执行过程中的异常，如果返回空则表示为发生异常。</summary>
	public Exception Exception => _exception;

	/// <summary>获取或设置异常是否处理完成，如果返回假(<c>False</c>)则异常信息将被抛出。</summary>
	public bool ExceptionHandled
	{
		get => _exceptionHandled;
		set => _exceptionHandled = value;
	}

	/// <summary>获取命令的执行上下文对象。</summary>
	public CommandContext Context => _context;

	/// <summary>
	/// 获取命令的执行参数对象。
	/// </summary>
	public object Parameter => _parameter;

	public object Result
	{
		get => _result;
		set => _result = value;
	}

	public bool HasExtendedProperties => _extendedProperties != null && _extendedProperties.Count > 0;

	/// <summary>
	/// 获取可用于在命令执行过程中在各处理模块之间组织和共享数据的键/值集合。
	/// </summary>
	public IDictionary<string, object> ExtendedProperties
	{
		get
		{
			if(_extendedProperties == null)
				System.Threading.Interlocked.CompareExchange(ref _extendedProperties, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

			return _extendedProperties;
		}
	}
	#endregion
}
