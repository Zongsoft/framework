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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Zongsoft.Options
{
	internal static class OptionHelper
	{
		#region 私有变量
		private static readonly Dictionary<string, Dictionary<string, object>> _options = new Dictionary<string,Dictionary<string,object>>(StringComparer.OrdinalIgnoreCase);
		#endregion

		public static void UpdateOptionObject(string path, object optionObject)
		{
			if(string.IsNullOrWhiteSpace(path) || optionObject == null || optionObject.GetType().IsValueType)
				return;

			Dictionary<string, object> optionData;

			if(_options.TryGetValue(path, out optionData))
			{
				string[] keys = new string[optionData.Count];
				optionData.Keys.CopyTo(keys, 0);

				foreach(var key in keys)
				{
					var property = optionObject.GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public);

					if(property != null)
						optionData[key] = property.GetValue(optionObject, null);
				}
			}
			else
			{
				var properties = optionObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
				optionData = new Dictionary<string, object>(properties.Length);

				foreach(var property in properties)
				{
					if(property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
					{
						optionData[property.Name] = property.GetValue(optionObject, null);
					}
				}

				lock(((ICollection)_options).SyncRoot)
				{
					_options[path] = optionData;
				}
			}
		}

		public static void RejectOptionObject(string path, object optionObject)
		{
			if(string.IsNullOrWhiteSpace(path) || optionObject == null || optionObject.GetType().IsValueType)
				return;

			Dictionary<string, object> optionData;

			if(_options.TryGetValue(path, out optionData))
			{
				foreach(var entry in optionData)
				{
					var property = optionObject.GetType().GetProperty(entry.Key, BindingFlags.Instance | BindingFlags.Public);

					if(property != null && property.CanWrite)
						property.SetValue(optionObject, entry.Value, null);
				}
			}
		}
	}
}
