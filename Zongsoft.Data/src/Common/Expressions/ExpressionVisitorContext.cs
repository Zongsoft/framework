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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Collections;

namespace Zongsoft.Data.Common.Expressions;

public class ExpressionVisitorContext
{
	#region 成员字段
	private ExpressionVisitorCounter _counter;
	private readonly ExpressionVisitorBase _visitor;
	private readonly StringBuilder _output;
	private readonly ExpressionStack _stack;
	#endregion

	#region 构造函数
	public ExpressionVisitorContext(ExpressionVisitorBase visitor, int capacity = 256)
	{
		_visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
		_stack = new ExpressionStack();
		_output = new StringBuilder(Math.Max(capacity, 64));
		_counter = new ExpressionVisitorCounter();
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前访问深度。</summary>
	public int Depth => _stack.Depth;

	/// <summary>获取当前访问的计数器。</summary>
	public ExpressionVisitorCounter Counter => _counter;

	/// <summary>获取当前访问输出缓存。</summary>
	public StringBuilder Output => _output;

	/// <summary>获取表达式方言。</summary>
	public IExpressionDialect Dialect => _visitor.Dialect;

	/// <summary>获取当前访问表达式。</summary>
	public IExpression Expression => _stack.TryPeek(out var value) ? value : null;

	/// <summary>获取访问调用栈。</summary>
	public IStack<IExpression> Stack => _stack;
	#endregion

	#region 公共方法
	public T Find<T>(int level = 0) where T : IExpression
	{
		var index = 0;
		var found = default(T);

		foreach(var frame in this.Stack)
		{
			if(frame is T matched)
			{
				found = matched;

				if(level >= 0 && index++ == level)
					return found;
			}
		}

		return found;
	}

	public void Visit(IExpression expression)
	{
		_visitor.OnVisit(this, expression);
	}

	public ExpressionVisitorContext Write(char value)
	{
		_output.Append(value);
		return this;
	}

	public ExpressionVisitorContext Write(string text)
	{
		_output.Append(text);
		return this;
	}

	public ExpressionVisitorContext WriteLine()
	{
		_output.AppendLine();
		return this;
	}

	public ExpressionVisitorContext WriteLine(string text)
	{
		_output.AppendLine(text);
		return this;
	}
	#endregion

	#region 嵌套子类
	public class ExpressionVisitorCounter
	{
		#region 成员字段
		private int _value;
		private int[] _values;
		#endregion

		#region 构造函数
		public ExpressionVisitorCounter() { }
		#endregion

		#region 公共方法
		public int GetValue(int index = 0)
		{
			return index <= 0 && _values == null ? _value : _values[index];
		}

		public bool Indent(int index = 0)
		{
			if(index <= 0 && _values == null)
				return _value++ > 0;

			this.Resize(index + 1);
			return _values[index]++ > 0;
		}

		public bool Dedent(int index = 0)
		{
			if(index <= 0 && _values == null)
				return --_value > 0;

			this.Resize(index + 1);
			return --_values[index] > 0;
		}
		#endregion

		#region 私有方法
		private void Resize(int count)
		{
			if(count < 2)
				return;

			if(count > 64)
				throw new ArgumentOutOfRangeException(nameof(count));

			if(_values == null)
			{
				_values = new int[count];
				_values[0] = _value;
			}
			else
			{
				Array.Resize(ref _values, count);
			}
		}
		#endregion
	}

	private class ExpressionStack : IStack<IExpression>
	{
		#region 私有遍历
		private int _depth;
		private readonly Stack<IExpression> _stack;
		#endregion

		#region 构造函数
		public ExpressionStack()
		{
			_depth = -1;
			_stack = new Stack<IExpression>();
		}
		#endregion

		#region 公共属性
		public int Count => _stack.Count;
		public int Depth => _depth;
		#endregion

		#region 公共方法
		public void Clear()
		{
			_stack.Clear();
			_depth = -1;
		}

		public IExpression Pop()
		{
			_depth--;
			return _stack.Pop();
		}

		public void Push(IExpression value)
		{
			_stack.Push(value);
			_depth++;
		}

		public IExpression Peek() => _stack.Peek();
		public IExpression Take(int index) => this.TryTake(index, out var value) ? value : null;
		public IExpression[] Take(int index, int count) => this.TryTake(index, count, out var values) ? values : null;
		public bool TryPeek(out IExpression value) => _stack.TryPeek(out value);
		public bool TryPop(out IExpression value) => _stack.TryPop(out value);

		public bool TryTake(int index, out IExpression value)
		{
			if(_stack.Count > 0)
			{
				if(index < 0 || index > _stack.Count - 1)
					throw new ArgumentOutOfRangeException(nameof(index));

				int offset = 0;

				foreach(var frame in _stack)
				{
					if(offset++ == index)
					{
						value = frame;
						return true;
					}
				}
			}

			value = null;
			return false;
		}

		public bool TryTake(int index, int count, out IExpression[] values)
		{
			if(_stack.Count == 0)
			{
				values = null;
				return false;
			}

			if(index < 0 || index > _stack.Count - 1)
				throw new ArgumentOutOfRangeException(nameof(index));

			if(count < 1)
				count = _stack.Count - index;
			else
				count = Math.Min(count, _stack.Count - index);

			int offset = 0;
			values = new IExpression[count];

			foreach(var frame in _stack)
			{
				if(offset++ >= index)
				{
					if(offset < values.Length)
						values[offset] = frame;
					else
						break;
				}
			}

			return true;
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<IExpression> GetEnumerator() => _stack.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _stack.GetEnumerator();
		#endregion
	}
	#endregion
}
