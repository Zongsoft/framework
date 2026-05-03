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
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

using Zongsoft.Serialization;

namespace Zongsoft.Upgrading;

internal static class AppSettingsUtility
{
	public static void Load(IDictionary<string, string> variables, string directory)
	{
		if(variables == null)
			return;

		if(string.IsNullOrEmpty(directory))
			directory = Environment.CurrentDirectory;

		var filePath = Path.Combine(directory, "appsettings.json");
		if(!File.Exists(filePath))
			return;

		var settings = Serializer.Json.Deserialize<IDictionary<string, object>>(File.OpenRead(filePath));
		if(settings == null || settings.Count == 0)
			return;

		//为“应用程序名称”添加一个别名变量
		if(settings.TryGetValue("ApplicationName", out var applicationName) && applicationName != null)
			variables["Application"] = applicationName.ToString();

		foreach(var setting in settings)
		{
			if(setting.Value == null)
				continue;

			if(setting.Value is JsonElement element)
				Populate(variables, setting.Key, element);
		}
	}

	private static void Populate(IDictionary<string, string> variables, string path, JsonElement element)
	{
		switch(element.ValueKind)
		{
			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				break;
			case JsonValueKind.True:
			case JsonValueKind.False:
			case JsonValueKind.Number:
				variables[path] = element.ToString();
				break;
			case JsonValueKind.String:
				variables[path] = element.GetString();
				break;
			case JsonValueKind.Object:
				Populate(variables, path, element.EnumerateObject());
				break;
			case JsonValueKind.Array:
				var index = 0;
				var array = element.EnumerateArray();
				while(array.MoveNext())
				{
					Populate(variables, $"{path}[{index++}]", array.Current);
				}
				break;
		}
	}

	private static void Populate(IDictionary<string, string> variables, string path, JsonElement.ObjectEnumerator iterator)
	{
		while(iterator.MoveNext())
		{
			var property = iterator.Current;

			switch(property.Value.ValueKind)
			{
				case JsonValueKind.Null:
				case JsonValueKind.Undefined:
					break;
				case JsonValueKind.True:
				case JsonValueKind.False:
				case JsonValueKind.Number:
					variables[$"{path}.{property.Name}"] = property.Value.ToString();
					break;
				case JsonValueKind.String:
					variables[$"{path}.{property.Name}"] = property.Value.GetString();
					break;
				case JsonValueKind.Object:
					Populate(variables, $"{path}.{property.Name}", property.Value.EnumerateObject());
					break;
				case JsonValueKind.Array:
					var index = 0;
					var array = property.Value.EnumerateArray();
					while(array.MoveNext())
					{
						Populate(variables, $"{path}[{index++}]", array.Current);
					}
					break;
			}
		}
	}
}