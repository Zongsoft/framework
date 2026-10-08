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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Expressions;

/// <summary>评估包含变量引用、成员导航及格式化的文本模板。</summary>
/// <remarks>求值期间须保持提供器集合、选项及事件订阅稳定；提供器负责自身数据及并发访问。</remarks>
public partial class TemplateEvaluator
{
	#region 构造函数
	public TemplateEvaluator(TemplateEvaluatorOptions options = null)
	{
		this.Options = options ?? new TemplateEvaluatorOptions();
		this.Providers = new List<IVariableProvider>();
	}
	#endregion

	#region 事件定义
	public event EventHandler<VariableEvaluationContext> Resolving;
	public event EventHandler<VariableEvaluationContext> Resolved;
	public event EventHandler<VariableFormattingContext> Formatting;
	public event EventHandler<VariableFormattingContext> Formatted;
	#endregion

	#region 公共属性
	public IList<IVariableProvider> Providers { get; }
	public TemplateEvaluatorOptions Options { get; }
	#endregion

	#region 公共方法
	public string Evaluate(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		for(int i = 0; i < this.Providers.Count; i++)
		{
			if(this.Providers[i] == null)
				throw new ArgumentException(Properties.Resources.Template_NullProvider_Message, nameof(this.Providers));
		}

		return this.Evaluate(text, 1);
	}

	public bool TryEvaluate(string text, out string result, out TemplateEvaluationException error)
	{
		try
		{
			result = this.Evaluate(text);
			error = null;
			return true;
		}
		catch(TemplateEvaluationException exception)
		{
			result = null;
			error = exception;
			return false;
		}
	}
	#endregion

	#region 求值处理
	private string Evaluate(string template, int depth)
	{
		var parts = new Parser(template, depth).Parse();
		var text = new StringBuilder();

		foreach(var part in parts)
		{
			if(part.Reference == null)
			{
				text.Append(part.Text);
				continue;
			}

			var value = this.Resolve(template, part.Reference, depth, false, out var context);
			var formatting = new VariableFormattingContext(context, value, part.Format, this.Options.Culture);

			try
			{
				this.Formatting?.Invoke(this, formatting);
			}
			catch(Exception exception)
			{
				throw Error("CallbackFailed", TemplateEvaluationStage.Formatting, template, part.Reference, depth, exception);
			}

			if(!formatting.Handled)
			{
				try
				{
					//使用基础库的单值格式化，格式中的大括号不进入复合格式语法。
					var handler = new DefaultInterpolatedStringHandler(0, 1, formatting.Culture);

					try
					{
						handler.AppendFormatted(formatting.Value, formatting.Format);
					}
					finally
					{
						formatting.Text = handler.ToStringAndClear();
					}
				}
				catch(Exception exception)
				{
					throw Error("FormattingFailed", TemplateEvaluationStage.Format, template, part.Reference, depth, exception);
				}
			}

			try
			{
				this.Formatted?.Invoke(this, formatting);
			}
			catch(Exception exception)
			{
				throw Error("CallbackFailed", TemplateEvaluationStage.Formatted, template, part.Reference, depth, exception);
			}

			text.Append(formatting.Text);
		}

		return text.ToString();
	}

	private object Resolve(string template, Reference reference, int depth, bool isIndex, out VariableEvaluationContext context)
	{
		context = new VariableEvaluationContext(template, reference.Text, reference.Namespace, reference.Name, reference.Position, reference.Length, depth, isIndex);

		try
		{
			this.Resolving?.Invoke(this, context);
		}
		catch(Exception exception)
		{
			throw Error("CallbackFailed", TemplateEvaluationStage.Resolving, template, reference, depth, exception);
		}

		if(!context.Handled)
		{
			var found = false;
			object value = null;

			try
			{
				for(int i = 0; i < this.Providers.Count; i++)
				{
					if(this.Providers[i].TryGetValue(reference.Name, reference.Namespace, out value))
					{
						found = true;
						break;
					}
				}
			}
			catch(Exception exception)
			{
				throw Error("ProviderFailed", TemplateEvaluationStage.Resolution, template, reference, depth, exception);
			}

			if(!found)
				throw Error("MissingVariable", TemplateEvaluationStage.Resolution, template, reference, depth);

			foreach(var accessor in reference.Accessors)
			{
				if(value == null)
					throw Error("NullTarget", TemplateEvaluationStage.Resolution, template, reference, depth);

				object[] arguments = null;

				if(accessor.Arguments != null)
				{
					arguments = new object[accessor.Arguments.Count];

					for(int i = 0; i < arguments.Length; i++)
					{
						var argument = accessor.Arguments[i];
						arguments[i] = argument.Reference == null ? argument.Value :
							this.Resolve(template, argument.Reference, depth, true, out _);
					}
				}

				try
				{
					value = arguments == null ?
						Reflection.MemberAccess.GetValue(value, accessor.Name) :
						Reflection.MemberAccess.GetValue(value, arguments);
				}
				catch(Exception exception)
				{
					throw Error("NavigationFailed", TemplateEvaluationStage.Resolution, template, reference, depth, exception);
				}
			}

			context.Value = value;
		}

		try
		{
			this.Resolved?.Invoke(this, context);
		}
		catch(Exception exception)
		{
			throw Error("CallbackFailed", TemplateEvaluationStage.Resolved, template, reference, depth, exception);
		}

		if(this.Options.Recursive && context.Value is string content)
		{
			if(depth >= this.Options.MaximumDepth)
				throw Error("DepthExceeded", TemplateEvaluationStage.Recursion, template, reference, depth);

			return this.Evaluate(content, depth + 1);
		}

		return context.Value;
	}
	#endregion

	#region 私有方法
	private static TemplateEvaluationException Error(string code, TemplateEvaluationStage stage, string template, Reference reference, int depth, Exception innerException = null) =>
		new(code, stage, template, reference.Text, reference.Position, reference.Length, depth, innerException);
	#endregion
}
