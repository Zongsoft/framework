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
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Expressions;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Lua;

[Service<IExpressionEvaluator>(NAME)]
public sealed class LuaExpressionEvaluator : ExpressionEvaluatorBase
{
	#region 常量定义
	internal const string NAME = "Lua";
	#endregion

	#region 构造函数
	public LuaExpressionEvaluator() : base(NAME)
	{
		//初始化全局变量集
		this.Global = new Dictionary<string, object>();

		//设置全局默认选项
		this.Options = new ExpressionEvaluatorOptions();
	}
	#endregion

	#region 公共方法
	public override object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null)
	{
		if(string.IsNullOrEmpty(expression))
			return null;

		using var engine = this.Create(options);

		if(variables != null)
		{
			foreach(var variable in variables)
				SetVariable(engine, variable);
		}

		var result = engine.DoString(expression);
		return result != null && result.Length == 1 ? Utility.Convert(result[0]) : Utility.Convert(result);
	}
	#endregion

	#region 私有方法
	private NLua.Lua Create(IExpressionEvaluatorOptions options)
	{
		var engine = new NLua.Lua();

		//设置引擎的文本编码方式
		engine.State.Encoding = Encoding.UTF8;

		//加载 .NET CLR 程序集
		engine.LoadCLRPackage();

		//设置默认的 Json 解析器
		engine[nameof(Json)] = new Json();

		//创建辅助对象
		var assistant = new Assistant(options ?? this.Options);

		//注册辅助方法
		assistant.Register(engine);

		//设置全局变量
		if(this.Global != null && this.Global.Count > 0)
		{
			foreach(var variable in this.Global)
				SetVariable(engine, variable);
		}

		return engine;
	}

	private static void SetVariable(NLua.Lua engine, KeyValuePair<string, object> variable) => SetVariable(engine, variable.Key, variable.Value);
	private static void SetVariable(NLua.Lua engine, string name, object value)
	{
		if(value is Delegate @delegate)
			engine.RegisterFunction(name, @delegate.Target, @delegate.Method);
		else
			engine[name] = value;
	}
	#endregion

	#region 重写方法
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if(disposing)
			this.Global?.Clear();
	}
	#endregion

	#region 嵌套子类
	private sealed class Assistant(IExpressionEvaluatorOptions options)
	{
		private static readonly MethodInfo ListMethod = typeof(Assistant).GetMethod(nameof(List), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly MethodInfo ArrayMethod = typeof(Assistant).GetMethod(nameof(Array), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly MethodInfo DictionaryMethod = typeof(Assistant).GetMethod(nameof(Dictionary), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly MethodInfo ErrorMethod = typeof(Assistant).GetMethod(nameof(Error), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly MethodInfo PrintMethod = typeof(Assistant).GetMethod(nameof(Print), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private readonly IExpressionEvaluatorOptions _options = options;

		public void Register(NLua.Lua lua)
		{
			if(lua == null)
				return;

			lua.RegisterFunction("list", null, ListMethod);
			lua.RegisterFunction("array", null, ArrayMethod);
			lua.RegisterFunction("error", this, ErrorMethod);
			lua.RegisterFunction("print", this, PrintMethod);
			lua.RegisterFunction("dictionary", null, DictionaryMethod);
		}

		private static List<object> List(int capacity = 0) => capacity > 0 ? new(capacity) : new();
		private static Array Array(int length = 0) => System.Array.CreateInstance(typeof(object), Math.Max(length, 0));
		private static Dictionary<string, object> Dictionary(int capacity = 0) => capacity > 0 ? new(capacity, StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase);

		private void Error(params object[] args)
		{
			if(args == null || args.Length == 0)
				return;

			var error = _options?.Error;
			if(error == null)
				return;

			for(int i = 0; i < args.Length; i++)
				error.Write(args[i]);

			error.Flush();
		}

		private void Print(params object[] args)
		{
			if(args == null || args.Length == 0)
				return;

			var output = _options?.Output;
			if(output == null)
				return;

			for(int i = 0; i < args.Length; i++)
				output.Write(args[i]);

			output.Flush();
		}
	}

	private sealed class Json
	{
		public string Serialize(object obj, bool typed = false)
		{
			var target = obj is NLua.LuaTable table ? Utility.ToDictionary(table) : obj;
			return Serializer.Json.Serialize(target, typed ? Serializer.Json.Options.Typified() : null);
		}

		public object Deserialize(string json, bool typed = false) => Serializer.Json.Deserialize<Dictionary<string, object>>(json, typed ? Serializer.Json.Options.Typified() : null);
	}
	#endregion
}
