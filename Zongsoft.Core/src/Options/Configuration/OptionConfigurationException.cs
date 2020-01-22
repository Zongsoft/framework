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

namespace Zongsoft.Options.Configuration
{
	[Serializable]
	public class OptionConfigurationException : Exception
	{
		#region 成员字段
		private string _fileName;
		#endregion

		#region 构造函数
		internal OptionConfigurationException()
		{
		}

		public OptionConfigurationException(string message) : base(message)
		{
		}

		public OptionConfigurationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected OptionConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			_fileName = info.GetString("FileName");
		}
		#endregion

		#region 公共属性
		public string FileName
		{
			get
			{
				return _fileName;
			}
			internal set
			{
				_fileName = value;
			}
		}
		#endregion

		#region 重写方法
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("FileName", _fileName);
		}
		#endregion
	}
}
