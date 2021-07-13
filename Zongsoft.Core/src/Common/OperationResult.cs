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
	/// 表示操作结果的结构。
	/// </summary>
	public struct OperationResult
	{
		#region 私有构造
		private OperationResult(string reason, string message = null)
		{
			this.Reason = reason;
			this.Message = message;
		}
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示结果是否成功。</summary>
		public bool Succeed { get => string.IsNullOrEmpty(this.Reason); }

		/// <summary>获取一个值，指示结果是否失败。</summary>
		public bool Failed { get => !string.IsNullOrEmpty(this.Reason); }

		/// <summary>获取操作失败的原因短语。</summary>
		public string Reason { get; }

		/// <summary>获取操作失败的消息描述。</summary>
		public string Message { get; set; }
		#endregion

		#region 静态方法
		public static OperationResult Success() => new OperationResult();
		public static OperationResult<T> Success<T>(T value) => new OperationResult<T>(value);
		public static OperationResult Fail(string reason, string message = null) => new OperationResult(string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason, message);
		#endregion
	}

	/// <summary>
	/// 表示操作结果的结构。
	/// </summary>
	/// <typeparam name="T">结果值的类型。</typeparam>
	public struct OperationResult<T>
	{
		#region 私有构造
		internal OperationResult(T value)
		{
			this.Value = value;
			this.Reason = null;
			this.Message = null;
		}

		private OperationResult(string reason, string message)
		{
			this.Value = default;
			this.Reason = string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason;
			this.Message = message;
		}
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示结果是否成功。</summary>
		public bool Succeed { get => string.IsNullOrEmpty(this.Reason); }

		/// <summary>获取一个值，指示结果是否失败。</summary>
		public bool Failed { get => !string.IsNullOrEmpty(this.Reason); }

		/// <summary>获取操作成功的结果值。</summary>
		public T Value { get; }

		/// <summary>获取操作失败的原因短语。</summary>
		public string Reason { get; }

		/// <summary>获取操作失败的消息描述。</summary>
		public string Message { get; set; }
		#endregion

		#region 类型转换
		public static implicit operator OperationResult (OperationResult<T> result) => result.Succeed ? OperationResult.Success() : OperationResult.Fail(result.Reason, result.Message);
		public static implicit operator OperationResult<T> (OperationResult result) => result.Succeed ? OperationResult.Success() : new OperationResult<T>(result.Reason, result.Message);
		#endregion
	}
}
