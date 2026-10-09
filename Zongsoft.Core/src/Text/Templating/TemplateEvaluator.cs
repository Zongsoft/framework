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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Zongsoft.Expressions;

namespace Zongsoft.Text.Templating;

/// <summary>评估包含变量引用、成员导航及格式化的文本模板。</summary>
/// <remarks>求值期间须保持提供器集合、选项及事件订阅稳定；提供器负责自身数据及并发访问。</remarks>
public partial class TemplateEvaluator
{
	#region 构造函数
	public TemplateEvaluator(TemplateEvaluatorOptions options = null)
	{
		this.Options = options ?? new TemplateEvaluatorOptions();
		this.Providers = new List<IVariables>();
	}
	#endregion

	#region 事件定义
	public event EventHandler<ResolutionContext> Resolving;
	public event EventHandler<ResolutionContext> Resolved;
	public event EventHandler<FormattingContext> Formatting;
	public event EventHandler<FormattingContext> Formatted;
	#endregion

	#region 公共属性
	public IList<IVariables> Providers { get; }
	public TemplateEvaluatorOptions Options { get; }
	#endregion

	#region 公共方法
	/// <summary>评估指定的表达式。</summary>
	/// <param name="text">要评估的模板表达式内容。</param>
	/// <returns>返回模板表达式求值后的文本，空跨度返回空字符串。</returns>
	public string Evaluate(ReadOnlySpan<char> text)
	{
		for(int i = 0; i < this.Providers.Count; i++)
		{
			if(this.Providers[i] == null)
				throw new ArgumentException(Properties.Resources.Template_NullProvider_Message, nameof(this.Providers));
		}

		return this.Evaluate(text, 1);
	}

	/// <summary>尝试评估指定的表达式。</summary>
	/// <param name="text">要评估的模板表达式内容。</param>
	/// <param name="result">返回模板表达式求值后的文本，空跨度返回空字符串。</param>
	/// <param name="error">返回评估过程中发生的异常，如果没有异常则为空(<c>null</c>)。</param>
	/// <returns>如果评估成功则返回真(<c>true</c>)，否则返回假(<c>false</c>)。</returns>
	public bool TryEvaluate(ReadOnlySpan<char> text, out string result, out TemplateEvaluationException error)
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
	private string Evaluate(ReadOnlySpan<char> template, int depth)
	{
		string source = null;
		var parts = new Parser(template, depth).Parse();
		var text = new StringBuilder();

		foreach(var part in parts)
		{
			if(part.Reference == null)
			{
				text.Append(part.Text);
				continue;
			}

			//事件上下文及异常可在调用结束后保留，每层只在首次求值时固化源码
			source ??= template.ToString();
			var value = this.Resolve(source, part.Reference, depth, false, out var context);
			var formatting = new FormattingContext(context, value, part.Format, this.Options.Culture);

			try
			{
				this.Formatting?.Invoke(this, formatting);
			}
			catch(Exception exception)
			{
				throw Error("CallbackFailed", TemplateEvaluationStage.Formatting, source, part.Reference, depth, exception);
			}

			if(!formatting.Handled)
			{
				try
				{
					//使用基础库的单值格式化，格式中的大括号不进入复合格式语法
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
					throw Error("FormattingFailed", TemplateEvaluationStage.Format, source, part.Reference, depth, exception);
				}
			}

			try
			{
				this.Formatted?.Invoke(this, formatting);
			}
			catch(Exception exception)
			{
				throw Error("CallbackFailed", TemplateEvaluationStage.Formatted, source, part.Reference, depth, exception);
			}

			text.Append(formatting.Text);
		}

		return text.ToString();
	}

	private object Resolve(string template, Reference reference, int depth, bool isIndex, out ResolutionContext context)
	{
		context = new ResolutionContext(template, reference.Text, reference.Namespace, reference.Name, reference.Position, reference.Length, depth, isIndex);

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
					if(this.Providers[i].TryGetValue(reference.Namespace, reference.Name, out value))
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

			for(int index = 0; index < (reference.Accessors?.Count ?? 0); index++)
			{
				var accessor = reference.Accessors[index];

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
					value = Reflection.Reflector.GetValue(ref value, accessor.Name, arguments);
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
	private static TemplateEvaluationException Error(string code, TemplateEvaluationStage stage, string template, Reference reference, int depth, Exception innerException = null) => new(code, stage, template, reference.Text, reference.Position, reference.Length, depth, innerException);
	#endregion
}
