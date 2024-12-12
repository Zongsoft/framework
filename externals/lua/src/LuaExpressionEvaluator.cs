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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Lua library.
 *
 * The Zongsoft.Externals.Lua is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Lua is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Lua library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Expressions;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Lua;

[Service<IExpressionEvaluator>(NAME)]
public sealed class LuaExpressionEvaluator : IExpressionEvaluator, IMatchable, IMatchable<string>
{
	#region 常量定义
	private const string NAME = "Lua";
	#endregion

	#region 静态字段
	private static readonly Lazy<NLua.Lua> _engine = new(() => new NLua.Lua());
	#endregion

	#region 构造函数
	public LuaExpressionEvaluator() => this.Global = new Dictionary<string, object>()
	{
		{ "json", new Json() },
	};
	#endregion

	#region 公共属性
	public IDictionary<string, object> Global { get; }
	#endregion

	#region 公共方法
	public object Evaluate(string expression, IDictionary<string, object> variables = null)
	{
		static void SetVariable(NLua.Lua engine, KeyValuePair<string, object> variable)
		{
			if(variable.Value is Delegate @delegate)
				engine.RegisterFunction(variable.Key, @delegate.Target, @delegate.Method);
			else
				engine[variable.Key] = variable.Value;
		}

		if(string.IsNullOrEmpty(expression))
			return null;

		var engine = _engine.Value;

		foreach(var variable in this.Global)
			SetVariable(engine, variable);

		if(variables != null)
		{
			foreach(var variable in variables)
				SetVariable(engine, variable);
		}

		var result = engine.DoString(expression);
		return result != null && result.Length == 1 ? Utility.Convert(result[0]) : Utility.Convert(result);
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is string name && this.Match(name);
	public bool Match(string name) => string.Equals(name, NAME, StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 嵌套子类
	private sealed class Json
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006")]
		public string serialize(object obj)
		{
			var target = obj is NLua.LuaTable table ? Utility.ToDictionary(table) : obj;
			return Serializer.Json.Serialize(target);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006")]
		public object deserialize(string json) => Serializer.Json.Deserialize<Dictionary<string, object>>(json);
	}
	#endregion
}
