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
using System.Threading.Tasks;
using System.Xml;

namespace Zongsoft.Externals.Aliyun
{
	public static class Utility
	{
		#region 常量定义
		private static readonly DateTime EPOCH = new DateTime(1970, 1, 1);
		#endregion

		/// <summary>
		/// 将本地时间转换成GMT格式的时间文本。
		/// </summary>
		/// <param name="datetime">本地时间。</param>
		/// <returns>返回被转换后的GMT格式的时间文本。</returns>
		/// <remarks>
		///		<para>如果北京时间为：2017-12-23 17:40:00，则该方法的返回结果为：Sat, 23 Dec 2017 09:40:00 GMT</para>
		/// </remarks>
		public static string GetGmtTime(DateTime? datetime = null)
		{
			return (datetime.HasValue ? datetime.Value : DateTime.Now).ToUniversalTime().ToString("r");
		}

		public static string GetTimestamp(DateTime? datetime = null)
		{
			return (datetime.HasValue ? datetime.Value : DateTime.Now).ToUniversalTime().ToString("s") + "Z";
		}

		public static DateTime GetDateTimeFromEpoch(int milliseconds)
		{
			return EPOCH.AddMilliseconds(milliseconds);
		}

		public static DateTime GetDateTimeFromEpoch(string milliseconds)
		{
			double number;

			if(Zongsoft.Common.Convert.TryConvertValue(milliseconds, out number))
				return EPOCH.AddMilliseconds(number);
			else
				throw new ArgumentException(string.Format("Invalid '{0}' value of 'totalMilliseconds' argument.", milliseconds));
		}

		public static TimeSpan? GetDuration(object parameter, DateTime baseTime)
		{
			if(parameter == null)
				return null;

			if(parameter.GetType() == typeof(TimeSpan))
				return (TimeSpan)parameter;

			TimeSpan? duration = null;

			switch(Type.GetTypeCode(parameter.GetType()))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					duration = TimeSpan.FromSeconds(Zongsoft.Common.Convert.ConvertValue<int>(parameter));
					break;
				case TypeCode.DateTime:
					duration = ((DateTime)parameter) - baseTime;
					break;
			}

			if(duration.HasValue)
				return duration.Value;

			throw new ArgumentException(string.Format("The '{0}' value of parameter is not supported.", parameter));
		}

		public static string GetQueryString(IDictionary<string, string> parameters)
		{
			if(parameters == null || parameters.Count == 0)
				return string.Empty;

			var text = new System.Text.StringBuilder();

			foreach(var parameter in parameters)
			{
				if(text.Length > 0)
					text.Append("&");

				text.Append(parameter.Key + "=" + parameter.Value);
			}

			return text.ToString();
		}

		/// <summary>
		/// 异步包装方法：确保在Web程序中不会被异步操作的并发线程乱入。
		/// </summary>
		/// <typeparam name="T">返回值的类型。</typeparam>
		/// <param name="thunk">异步任务的委托。</param>
		/// <returns>返回以同步方式返回异步任务的执行结果。</returns>
		public static T ExecuteTask<T>(Func<Task<T>> thunk)
		{
			return Task.Run(() => ExecuteTaskDelegate(() => thunk())).Result;
		}

		private static async Task<T> ExecuteTaskDelegate<T>(Func<Task<T>> thunk)
		{
			return await thunk();
		}

		public static IDictionary<string, string> GetDictionary(string text)
		{
			if(string.IsNullOrEmpty(text))
				return null;

			var parts = text.Split(',', '|');
			var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach(var part in parts)
			{
				if(string.IsNullOrEmpty(part))
					continue;

				var index = part.IndexOf('=');

				if(index < 0)
					index = part.IndexOf(':');

				if(index <= 0)
					continue;

				var key = part.Substring(0, index);
				var value = index < part.Length - 1 ? part.Substring(index + 1) : string.Empty;

				dictionary[key] = value;
			}

			return dictionary;
		}

		public static class Xml
		{
			public static void MoveToEndElement(XmlReader reader)
			{
				if(reader == null || reader.ReadState != ReadState.Interactive || reader.IsEmptyElement)
					return;

				if(reader.NodeType == XmlNodeType.Element)
				{
					int depth = reader.Depth;

					while(reader.Read() && reader.Depth > depth)
						;
				}
			}

			public static string ReadContentAsString(XmlReader reader)
			{
				if(reader.NodeType != XmlNodeType.Element)
					return null;

				if(reader.IsEmptyElement)
					return string.Empty;

				var depth = reader.Depth;
				string text = null;

				while(reader.Read() && reader.Depth > depth)
				{
					if(text == null && reader.NodeType == XmlNodeType.Text)
						text = reader.Value;
				}

				return text;
			}
		}
	}
}
