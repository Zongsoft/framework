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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Zongsoft.Components;

public abstract class CommandBase : ICommand, Services.IMatchable, INotifyPropertyChanged
{
	#region 事件定义
	public event EventHandler EnabledChanged;
	public event PropertyChangedEventHandler PropertyChanged;
	public event EventHandler<CommandExecutedEventArgs> Executed;
	public event EventHandler<CommandExecutingEventArgs> Executing;
	#endregion

	#region 成员字段
	private string _name;
	private bool _enabled;
	private Common.IPredication _predication;
	#endregion

	#region 构造函数
	protected CommandBase() : this(null, true) { }
	protected CommandBase(string name) : this(name, true) { }
	protected CommandBase(string name, bool enabled)
	{
		_enabled = enabled;
		_predication = null;
		this.Name = string.IsNullOrWhiteSpace(name) ? Common.StringExtension.TrimEnd(this.GetType().Name, "Command", StringComparison.OrdinalIgnoreCase) : name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置命令的名称。</summary>
	public string Name
	{
		get => _name;
		protected set
		{
			if(string.IsNullOrWhiteSpace(value))
				throw new ArgumentNullException(nameof(value));

			if(value.Length > 100)
				throw new ArgumentOutOfRangeException(nameof(value));

			if(value.Contains('.') || value.Contains('/') || value.Contains('\\'))
				throw new ArgumentException($"The specified command name contains illegal characters.", nameof(value));

			if(string.Equals(_name, value, StringComparison.Ordinal))
				return;

			_name = value.Trim();

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Name));
		}
	}

	/// <summary>获取或设置当前命令的断言对象，该断言决定当前命令是否可用。</summary>
	public Common.IPredication Predication
	{
		get => _predication;
		set
		{
			if(_predication == value)
				return;

			_predication = value;

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Predication));
		}
	}

	/// <summary>获取或设置当前命令是否可用。</summary>
	/// <remarks>该属性作为当前命令是否可被执行的备选方案，命令是否可被执行由<see cref="CanExecuteAsync"/>方法决定，该方法的不同实现方式可能导致不同的判断逻辑。有关默认的判断逻辑请参考<seealso cref="CanExecuteAsync"/>方法的帮助。</remarks>
	public bool Enabled
	{
		get => _enabled;
		set
		{
			if(_enabled == value)
				return;

			_enabled = value;

			//激发“EnabledChanged”事件
			this.OnEnabledChanged(EventArgs.Empty);

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Enabled));
		}
	}
	#endregion

	#region 虚拟方法
	/// <summary>判断命令是否可被执行。</summary>
	/// <param name="argument">判断命令是否可被执行的参数对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果返回真(true)则表示命令可被执行，否则表示不可执行。</returns>
	/// <remarks>
	///		<para>本方法为虚拟方法，可由子类更改基类的默认实现方式。</para>
	///		<para>如果<seealso cref="Predication"/>属性为空(null)，则返回<see cref="Enabled"/>属性值；否则返回由<see cref="Predication"/>属性指定的断言对象的断言方法的值。</para>
	/// </remarks>
	protected virtual ValueTask<bool> CanExecuteAsync(object argument, CancellationToken cancellation)
	{
		var predication = this.Predication;

		//如果断言对象是空则返回是否可用变量的值
		if(predication == null)
			return ValueTask.FromResult(this.Enabled);

		//返回断言对象的断言测试的值
		return ValueTask.FromResult(this.Enabled && predication.Predicate(argument));
	}

	/// <summary>判断命令是否为指定要匹配的名称。</summary>
	/// <param name="argument">要匹配的参数，如果参数为空(null)则返回真；如果参数为字符串则返回其当前命令名进行不区分大小写匹对值；否则返回假(false)。</param>
	/// <returns>如果匹配成功则返回真(true)，否则返回假(false)。</returns>
	protected virtual bool IsMatch(object argument) => argument != null && string.Equals(_name, argument.ToString(), StringComparison.OrdinalIgnoreCase);

	/// <summary>执行命令。</summary>
	/// <param name="argument">执行命令的参数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回执行的返回结果。</returns>
	/// <remarks>
	///		<para>本方法的实现中首先调用<see cref="CanExecuteAsync"/>方法，以确保阻止非法的调用。</para>
	/// </remarks>
	protected virtual async ValueTask<object> ExecuteAsync(object argument, CancellationToken cancellation)
	{
		//在执行之前首先判断是否可以执行
		if(!await this.CanExecuteAsync(argument, cancellation))
			return null;

		//创建事件参数对象
		var executingArgs = CreateExecutingEventArgs(argument);
		//激发“Executing”事件
		this.OnExecuting(executingArgs);

		//如果事件处理程序要取消后续操作，则返回退出
		if(executingArgs.Cancel)
			return executingArgs.Result;

		object result;
		CommandExecutedEventArgs executedArgs;

		try
		{
			//执行具体的工作
			result = await this.OnExecuteAsync(argument, cancellation);
		}
		catch(AggregateException ex)
		{
			//创建事件参数对象
			executedArgs = CreateExecutedEventArgs(argument, ex.InnerException);

			//激发“Executed”事件
			this.OnExecuted(executedArgs);

			if(!executedArgs.ExceptionHandled)
				throw ex.InnerException;

			return executedArgs.Result;
		}
		catch(Exception ex)
		{
			//创建事件参数对象
			executedArgs = CreateExecutedEventArgs(argument, ex);

			//激发“Executed”事件
			this.OnExecuted(executedArgs);

			if(!executedArgs.ExceptionHandled)
				throw;

			return executedArgs.Result;
		}

		//创建事件参数对象
		executedArgs = CreateExecutedEventArgs(argument, result);

		//激发“Executed”事件
		this.OnExecuted(executedArgs);

		//返回执行成功的结果
		return executedArgs.Result;
	}

	protected virtual void OnEnabledChanged(EventArgs e) => this.EnabledChanged?.Invoke(this, e);
	protected virtual void OnExecuted(CommandExecutedEventArgs e) => this.Executed?.Invoke(this, e);
	protected virtual void OnExecuting(CommandExecutingEventArgs e) => this.Executing?.Invoke(this, e);
	protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	#endregion

	#region 抽象方法
	protected abstract ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation);
	#endregion

	#region 事件参数
	private static CommandExecutingEventArgs CreateExecutingEventArgs(object argument) =>
		argument is CommandContext context ?
		new CommandExecutingEventArgs(context) :
		new CommandExecutingEventArgs(argument);

	private static CommandExecutedEventArgs CreateExecutedEventArgs(object argument, object result) =>
		argument is CommandContext context ?
		new CommandExecutedEventArgs(context, result) :
		new CommandExecutedEventArgs(argument, result);

	private static CommandExecutedEventArgs CreateExecutedEventArgs(object argument, Exception exception) =>
		argument is CommandContext context ?
		new CommandExecutedEventArgs(context, exception) :
		new CommandExecutedEventArgs(argument, exception);
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}";
	#endregion

	#region 显式实现
	ValueTask<bool> ICommand.CanExecuteAsync(object argument, CancellationToken cancellation) => this.CanExecuteAsync(argument, cancellation);
	ValueTask<object> ICommand.ExecuteAsync(object argument, CancellationToken cancellation) => this.ExecuteAsync(argument, cancellation);

	/// <summary>判断命令是否为指定要匹配的名称。</summary>
	/// <param name="argument">要匹配的参数，如果参数为空(null)则返回真；如果参数为字符串则返回其当前命令名进行不区分大小写匹对值；否则返回假(false)。</param>
	/// <returns>如果匹配成功则返回真(true)，否则返回假(false)。</returns>
	/// <remarks>该显式实现为调用<see cref="IsMatch"/>虚拟方法。</remarks>
	bool Services.IMatchable.Match(object argument) => this.IsMatch(argument);
	#endregion
}
