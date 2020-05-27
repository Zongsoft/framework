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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Externals.Aliyun
{
	[Serializable]
	public class AliyunException : ApplicationException
	{
		#region 常量定义
		private static readonly Regex _error_regex = new Regex(@"\<(?'tag'(Code|Message|RequestId|HostId))\>\s*(?<value>[^<>]+)\s*\<\/\k'tag'\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 成员字段
		private IDictionary<string, string> _properties;
		#endregion

		#region 构造函数
		private AliyunException(IDictionary<string, string> properties)
		{
			if(properties == null)
				throw new ArgumentNullException("properties");

			_properties = properties;
		}
		#endregion

		#region 重写属性
		public string Code
		{
			get
			{
				return this.GetPropertyValue("Code");
			}
		}

		public override string Message
		{
			get
			{
				return this.GetPropertyValue("Message");
			}
		}

		public string RequestId
		{
			get
			{
				return this.GetPropertyValue("RequestId");
			}
		}

		public override System.Collections.IDictionary Data
		{
			get
			{
				return _properties as System.Collections.IDictionary;
			}
		}
		#endregion

		#region 私有方法
		private string GetPropertyValue(string name)
		{
			string result;

			if(_properties.TryGetValue(name, out result))
				return result;

			return null;
		}
		#endregion

		#region 静态方法
		public static AliyunException Parse(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return null;

			var matches = _error_regex.Matches(text);

			if(matches == null || matches.Count < 1)
				return null;

			IDictionary<string, string> properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach(Match match in matches)
			{
				properties[match.Groups["tag"].Value.Trim()] = match.Groups["value"].Value.Trim();
			}

			return new AliyunException(properties);
		}
		#endregion
	}
}
