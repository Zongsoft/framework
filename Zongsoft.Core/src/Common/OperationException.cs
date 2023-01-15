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
using System.Runtime.Serialization;

namespace Zongsoft.Common
{
    /// <summary>
    /// 表示操作失败的异常类。
    /// </summary>
    [Serializable]
    public class OperationException : ApplicationException
    {
        #region 构造函数
        public OperationException() : base(Properties.Resources.OperationException_Unknown_Message) => this.Reason = nameof(Unknown);
        public OperationException(string reason, string message) : base(message, null) => this.Reason = string.IsNullOrEmpty(reason) ? throw new ArgumentNullException(nameof(reason)) : reason;
        public OperationException(string reason, string message, Exception innerException) : base(message, innerException) => this.Reason = string.IsNullOrEmpty(reason) ? throw new ArgumentNullException(nameof(reason)) : reason;
        protected OperationException(SerializationInfo info, StreamingContext context) : base(info, context) => this.Reason = info.GetString(nameof(Reason));
        #endregion

        #region 公共属性
        /// <summary>获取操作异常原因的短语。</summary>
        public string Reason { get; }
		#endregion

		#region 重写方法
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Reason), this.Reason);
        }
		#endregion

		#region 静态方法
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
}