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

namespace Zongsoft.Common
{
	/// <summary>
	/// 表示操作失败的结构。
	/// </summary>
	public struct OperationResultFailure
	{
		#region 构造函数
		public OperationResultFailure(int code, string message = null) : this(code, null, message) { }
		public OperationResultFailure(string reason, string message = null) : this(0, reason, message) { }
		public OperationResultFailure(int code, string reason, string message = null)
		{
			this.Code = code;
			this.Reason = reason;
			this.Message = message;
			this.Exception = null;
		}

		public OperationResultFailure(Exception exception)
		{
			if(exception == null)
				throw new ArgumentNullException(nameof(exception));

			this.Code = 0;
			this.Reason = GetReason(exception);
			this.Message = exception.Message;
			this.Exception = exception;
		}
		public OperationResultFailure(int code, Exception exception) : this(code, null, exception) { }
		public OperationResultFailure(string reason, Exception exception) : this(0, reason, exception) { }
		public OperationResultFailure(int code, string reason, Exception exception)
		{
			if(exception == null)
			{
				this.Code = code;
				this.Reason = reason;
				this.Message = null;
				this.Exception = null;
			}
			else
			{
				this.Code = code;
				this.Reason = string.IsNullOrEmpty(reason) ? GetReason(exception) : reason;
				this.Message = exception.Message;
				this.Exception = exception;
			}
		}
		#endregion

		#region 公共属性
		/// <summary>获取失败的代号。</summary>
		public int Code { get; }

		/// <summary>获取失败的原因短语。</summary>
		public string Reason { get; }

		/// <summary>获取或设置失败的消息内容。</summary>
		public string Message { get; set; }

		/// <summary>获取或设置失败的异常对象。</summary>
		public Exception Exception { get; set; }
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(this.Code == 0)
				return string.IsNullOrEmpty(this.Reason) ? this.Message : $"[{this.Reason}]{this.Message}";
			else
				return string.IsNullOrEmpty(this.Reason) ? $"[{this.Code}]{this.Message}" : $"[{this.Reason}#{this.Code}]{this.Message}";
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetReason(Exception exception) => (exception == null || exception.GetType() == typeof(Exception)) ? "Unknown" : exception.GetType().Name.TrimEnd(nameof(Exception));
		#endregion

		#region 重写符号
		public static implicit operator OperationResult(OperationResultFailure failure) => OperationResult.Fail(failure);
		#endregion
	}

	/// <summary>
	/// 表示操作结果的结构。
	/// </summary>
	public readonly struct OperationResult
	{
		#region 成员字段
		private readonly object _value;
		private readonly OperationResultFailure? _failure;
		#endregion

		#region 私有构造
		private OperationResult(object value)
		{
			_value = value;
			_failure = null;
		}

		private OperationResult(OperationResultFailure failure)
		{
			_value = null;
			_failure = failure;
		}
		#endregion

		#region 公共属性
		/// <summary>获取操作成功的结果值。</summary>
		public object Value { get => _value; }

		/// <summary>获取一个值，指示结果是否成功。</summary>
		public bool Succeed { get => _failure == null; }

		/// <summary>获取一个值，指示结果是否失败。</summary>
		public bool Failed { get => _failure.HasValue; }

		/// <summary>获取操作失败的结构。</summary>
		public OperationResultFailure Failure { get => _failure.HasValue ? _failure.Value : default; }
		#endregion

		#region 静态方法
		public static OperationResult Success(object value = null) => new OperationResult(value);
		public static OperationResult<T> Success<T>(T value) => new OperationResult<T>(value);
		public static OperationResult Fail(Exception exception = null) => new OperationResult(new OperationResultFailure(exception));
		public static OperationResult Fail(OperationResultFailure failure) => new OperationResult(failure);
		public static OperationResult Fail(int code, string message = null) => new OperationResult(new OperationResultFailure(code, message));
		public static OperationResult Fail(string reason, string message = null) => new OperationResult(new OperationResultFailure(reason, message));
		public static OperationResult Fail(int code, string reason, string message = null) => new OperationResult(new OperationResultFailure(code, reason, message));
		public static OperationResult Fail(int code, Exception exception) => new OperationResult(new OperationResultFailure(code, exception));
		public static OperationResult Fail(string reason, Exception exception) => new OperationResult(new OperationResultFailure(reason, exception));
		public static OperationResult Fail(int code, string reason, Exception exception) => new OperationResult(new OperationResultFailure(code, reason, exception));
		#endregion
	}

	/// <summary>
	/// 表示操作结果的结构。
	/// </summary>
	/// <typeparam name="T">结果值的类型。</typeparam>
	public readonly struct OperationResult<T>
	{
		#region 成员字段
		private readonly OperationResultFailure? _failure;
		#endregion

		#region 私有构造
		internal OperationResult(T value)
		{
			this.Value = value;
			_failure = null;
		}

		private OperationResult(OperationResultFailure failure)
		{
			this.Value = default;
			_failure = failure;
		}
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示结果是否成功。</summary>
		public bool Succeed { get => _failure == null; }

		/// <summary>获取一个值，指示结果是否失败。</summary>
		public bool Failed { get => _failure.HasValue; }

		/// <summary>获取操作成功的结果值。</summary>
		public T Value { get; }

		/// <summary>获取操作失败的结构。</summary>
		public OperationResultFailure Failure { get => _failure.HasValue ? _failure.Value : default; }
		#endregion

		#region 类型转换
		public static implicit operator OperationResult (OperationResult<T> result) => result.Succeed ? OperationResult.Success(result.Value) : OperationResult.Fail(result.Failure);
		public static implicit operator OperationResult<T> (OperationResult result) => result.Succeed ? new OperationResult<T>(default(T)) : new OperationResult<T>(result.Failure);
		#endregion
	}
}
