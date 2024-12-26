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

	#region 成员字段
	private volatile NLua.Lua _engine;
	private readonly Assistant _assistant;
	#endregion

	#region 构造函数
	public LuaExpressionEvaluator() : base(NAME)
	{
		_engine = new NLua.Lua();

		//加载 .NET CLR 程序集
		_engine.LoadCLRPackage();

		//设置默认的 Json 解析器
		_engine[nameof(Json)] = new Json();

		//初始化全局变量集
		this.Global = new Variables(_engine);

		//设置全局默认选项
		this.Options = new ExpressionEvaluatorOptions();

		//注册辅助方法
		_assistant = new Assistant(this.Options);
		_assistant.Register(_engine);
	}
	#endregion

	#region 公共方法
	public override object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null)
	{
		var engine = _engine ?? throw new ObjectDisposedException(nameof(LuaExpressionEvaluator));

		if(string.IsNullOrEmpty(expression))
			return null;

		if(variables != null)
		{
			foreach(var variable in variables)
				SetVariable(engine, variable);
		}

		if(options != null)
			_assistant.Switch(options);

		var result = engine.DoString(expression);
		return result != null && result.Length == 1 ? Utility.Convert(result[0]) : Utility.Convert(result);
	}
	#endregion

	#region 私有方法
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
		Interlocked.Exchange(ref _engine, null)?.Dispose();
	}
	#endregion

	#region 嵌套子类
	private sealed class Variables(NLua.Lua lua) : IDictionary<string, object>
	{
		private readonly NLua.Lua _lua = lua;

		public ICollection<string> Keys => _lua.Globals.ToArray();
		public ICollection<object> Values => _lua.Globals.Select(_lua.GetObjectFromPath).ToArray();
		public object this[string name]
		{
			get => _lua[name];
			set => _lua[name] = value;
		}

		public int Count => _lua.Globals.Count();
		bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

		public void Add(string name, object value) => SetVariable(_lua, name, value);
		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> variable) => SetVariable(_lua, variable);
		public void Clear() { }
		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> variable) => this.ContainsKey(variable.Key);
		public bool ContainsKey(string name) => _lua.Globals.Contains(name);
		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotSupportedException();
		public bool Remove(string name){ _lua.SetObjectToPath(name, null); return name != null; }
		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> variable) => this.Remove(variable.Key);
		public bool TryGetValue(string name, out object value) { value = _lua.GetObjectFromPath(name); return value != null; }

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			foreach(var name in _lua.Globals)
				yield return new(name, _lua.GetObjectFromPath(name));
		}
	}

	private class Assistant(IExpressionEvaluatorOptions options)
	{
		private static MethodInfo ListMethod = typeof(Assistant).GetMethod(nameof(List), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		private static MethodInfo ArrayMethod = typeof(Assistant).GetMethod(nameof(Array), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		private static MethodInfo ErrorMethod = typeof(Assistant).GetMethod(nameof(Error), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static MethodInfo PrintMethod = typeof(Assistant).GetMethod(nameof(Print), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private IExpressionEvaluatorOptions _options = options;

		public void Switch(IExpressionEvaluatorOptions options) => _options = options;
		public void Register(NLua.Lua lua)
		{
			if(lua == null)
				return;

			lua.RegisterFunction("list", null, ListMethod);
			lua.RegisterFunction("array", null, ArrayMethod);
			lua.RegisterFunction("error", this, ErrorMethod);
			lua.RegisterFunction("print", this, PrintMethod);
		}

		private static List<object> List(int capacity = 0) => capacity > 0 ? new(capacity) : new();
		private static Array Array(int length = 0) => System.Array.CreateInstance(typeof(object), Math.Max(length, 0));

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
		public string Serialize(object obj)
		{
			var target = obj is NLua.LuaTable table ? Utility.ToDictionary(table) : obj;
			return Serializer.Json.Serialize(target);
		}

		public object Deserialize(string json) => Serializer.Json.Deserialize<Dictionary<string, object>>(json);
	}
	#endregion
}
