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
    [Serializable]
    public class OperationException : ApplicationException
    {
        #region 构造函数
        public OperationException() { }
        public OperationException(string message) : base(message, null) { }
        public OperationException(string message, Exception innerException) : base(message, innerException) { }
        public OperationException(string reason, string message) : base(message, null) => Reason = reason;
        public OperationException(string reason, string message, Exception innerException) : base(message, innerException) => Reason = reason;
        protected OperationException(SerializationInfo info, StreamingContext context) : base(info, context) => Reason = info.GetString(nameof(Reason));
        #endregion

        #region 公共方法
        /// <summary>获取或设置异常理由的短语。</summary>
        public string Reason { get; set; }
        #endregion

        #region 重写方法
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Reason), Reason);
        }
        #endregion
    }
}