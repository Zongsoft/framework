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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Common;

/// <summary>表示操作失败的异常类。</summary>
public class OperationException : ApplicationException
{
	#region 构造函数
	public OperationException() : base(Properties.Resources.OperationException_Unknown_Message) => this.Reason = nameof(Unknown);
	public OperationException(string reason, string message) : base(message, null) => this.Reason = string.IsNullOrEmpty(reason) ? throw new ArgumentNullException(nameof(reason)) : reason;
	public OperationException(string reason, string message, Exception innerException) : base(message, innerException) => this.Reason = string.IsNullOrEmpty(reason) ? throw new ArgumentNullException(nameof(reason)) : reason;
	#endregion

	#region 公共属性
	/// <summary>获取操作异常原因的短语。</summary>
	public string Reason { get; }

	/// <summary>获取一个值，指示当前异常是否为“无效参数”的操作异常。</summary>
	public bool IsArgument => this.Reason == nameof(Argument);
	/// <summary>获取一个值，指示当前异常是否为“未知”的操作异常。</summary>
	public bool IsUnknown => this.Reason == nameof(Unknown);
	/// <summary>获取一个值，指示当前异常是否为“未找到操作对象”的操作异常。</summary>
	public bool IsUnfound => this.Reason == nameof(Unfound);
	/// <summary>获取一个值，指示当前异常是否为“未满足先决条件”的操作异常。</summary>
	public bool IsUnsatisfied => this.Reason == nameof(Unsatisfied);
	/// <summary>获取一个值，指示当前异常是否为“无法处理”的操作异常。</summary>
	public bool IsUnprocessed => this.Reason == nameof(Unprocessed);
	/// <summary>获取一个值，指示当前异常是否为“不支持该操作”的操作异常。</summary>
	public bool IsUnsupported => this.Reason == nameof(Unsupported);
	#endregion

	#region 静态方法
	/// <summary>构建一个原因“无效参数”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Argument(string message = null, Exception innerException = null) => new(nameof(Argument), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Argument_Message : message, innerException);

	/// <summary>构建一个原因“未知”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Unknown(string message = null, Exception innerException = null) => new(nameof(Unknown), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Unknown_Message : message, innerException);

	/// <summary>构建一个原因“未找到操作对象”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Unfound(string message = null, Exception innerException = null) => new(nameof(Unfound), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Unfound_Message : message, innerException);

	/// <summary>构建一个原因“未满足先决条件”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Unsatisfied(string message = null, Exception innerException = null) => new(nameof(Unsatisfied), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Unsatisfied_Message : message, innerException);

	/// <summary>构建一个原因“无法处理”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Unprocessed(string message = null, Exception innerException = null) => new(nameof(Unprocessed), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Unprocessed_Message : message, innerException);

	/// <summary>构建一个原因“不支持该操作”的操作异常。</summary>
	/// <param name="message">指定的异常消息。</param>
	/// <param name="innerException">指定的导致当前异常的内部异常。</param>
	/// <returns>返回构建的操作异常实例。</returns>
	public static OperationException Unsupported(string message = null, Exception innerException = null) => new(nameof(Unsupported), string.IsNullOrEmpty(message) ? Properties.Resources.OperationException_Unsupported_Message : message, innerException);
	#endregion
}