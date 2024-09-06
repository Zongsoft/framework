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

namespace Zongsoft.Externals.Python;

[Service<IExpressionEvaluator>(NAME)]
public class PythonExpressionEvaluator : IExpressionEvaluator, IMatchable, IMatchable<string>
{
	#region 常量定义
	private const string NAME = "Scriban";
	#endregion

	#region 静态字段
	private static readonly Lazy<ScriptEngine> _engine = new(() => IronPython.Hosting.Python.CreateEngine());
	#endregion

	#region 公共属性
	public IDictionary<string, object> Global => new Variables(_engine.Value.Runtime.Globals);
	#endregion

	#region 公共方法
	public object Evaluate(string expression, IDictionary<string, object> variables = null)
	{
		if(string.IsNullOrEmpty(expression))
			return null;

		var engine = _engine.Value;
		var scope = engine.CreateScope(variables);
		return engine.Execute(expression, scope);
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is string name && this.Match(name);
	public bool Match(string name) => string.Equals(name, NAME, StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 嵌套子类
	private sealed class Variables(ScriptScope scope) : IDictionary<string, object>
	{
		private readonly ScriptScope _scope = scope;

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

		IEnumerator IEnumerable.GetEnumerator() => _scope.GetItems().GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => this.GetEnumerator();
	}
	#endregion
}
