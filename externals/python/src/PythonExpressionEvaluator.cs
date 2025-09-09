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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Python library.
 *
 * The Zongsoft.Externals.Python is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Python is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Python library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using Zongsoft.Services;
using Zongsoft.Expressions;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Python;

[Service<IExpressionEvaluator>(NAME)]
public class PythonExpressionEvaluator : ExpressionEvaluatorBase
{
	#region 常量定义
	internal const string NAME = "Python";
	#endregion

	#region 静态字段
	private static readonly Lazy<ScriptEngine> _engine = new(() => IronPython.Hosting.Python.CreateEngine());
	#endregion

	#region 构造函数
	public PythonExpressionEvaluator() : base(NAME)
	{
		this.Global = new Variables(_engine.Value.Runtime.Globals);
		_engine.Value.Runtime.Globals.SetVariable("error", (Delegate)Error);
	}
	#endregion

	#region 公共方法
	public override object Evaluate(string expression, IExpressionEvaluatorOptions options, IDictionary<string, object> variables = null)
	{
		if(string.IsNullOrEmpty(expression))
			return null;

		var engine = _engine.Value;

		SetOptions(engine, this.Options);
		SetOptions(engine, options);

		var paths = engine.GetSearchPaths();
		paths.Add(@"C:\Users\95558\.nuget\packages\ironpython.stdlib\3.4.2\content\lib");
		engine.SetSearchPaths(paths);

		if(variables == null)
			return engine.Execute(expression, engine.Runtime.Globals) ?? engine.GetResult();

		foreach(var variable in this.Global)
			variables.TryAdd(variable.Key, variable.Value);

		var scope = engine.CreateScope(variables);
		return engine.Execute(expression, scope) ?? scope.GetResult();

		static void SetOptions(ScriptEngine engine, IExpressionEvaluatorOptions options)
		{
			if(options == null)
				return;

			var encoding = System.Text.Encoding.UTF8;

			if(options.Input != null)
				engine.Runtime.IO.SetInput(new TextStream(options.Input, encoding), options.Input, encoding);
			if(options.Output != null)
				engine.Runtime.IO.SetOutput(new TextStream(options.Output), options.Output);
			if(options.Error != null)
				engine.Runtime.IO.SetErrorOutput(new TextStream(options.Error), options.Error);
		}
	}
	#endregion

	#region 私有方法
	private static void Error(params object[] args)
	{
		if(args == null || args.Length == 0)
			return;

		var error = _engine.Value.Runtime.IO.ErrorWriter;
		if(error == null)
			return;

		for(int i = 0; i < args.Length; i++)
			error.Write(args[i]);

		error.Flush();
	}
	#endregion

	#region 嵌套子类
	private sealed class Variables : IDictionary<string, object>
	{
		private readonly ScriptScope _scope;

		public Variables(ScriptScope scope)
		{
			_scope = scope;
			_scope.SetVariable(nameof(Json), new Json());
		}

		public object this[string name]
		{
			get => _scope.GetVariable(name);
			set => _scope.SetVariable(name, value);
		}

		public ICollection<string> Keys => _scope.GetVariableNames().ToArray();
		public ICollection<object> Values => _scope.GetVariableNames().Select(name => (object)_scope.GetVariable(name)).ToArray();
		public int Count => _scope.GetVariableNames().Count();
		bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

		public void Add(string key, object value) => _scope.SetVariable(key, value);
		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => _scope.SetVariable(item.Key, item.Value);
		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _scope.ContainsVariable(item.Key);
		public bool ContainsKey(string key) => _scope.ContainsVariable(key);
		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotImplementedException();
		public bool Remove(string key) => _scope.RemoveVariable(key);
		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => _scope.RemoveVariable(item.Key);
		public bool TryGetValue(string key, out object value) => _scope.TryGetVariable(key, out value);

		public void Clear()
		{
			foreach(var name in _scope.GetVariableNames())
				_scope.RemoveVariable(name);
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			var items = _scope.GetItems();

			foreach(var item in items)
				yield return new(item.Key, item.Value);
		}
	}

	public sealed class Json
	{
		public string Serialize(object obj, bool typed = false)
		{
			return Serializer.Json.Serialize(obj, typed ? Serializer.Json.Options.Typified() : null);
		}

		public object Deserialize(string json, bool typed = false) => Serializer.Json.Deserialize<Dictionary<string, object>>(json, typed ? Serializer.Json.Options.Typified() : null);
	}
	#endregion
}
