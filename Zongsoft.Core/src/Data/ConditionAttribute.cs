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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示条件组合成员的描述特性。
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ConditionAttribute : Attribute
	{
		#region 成员字段
		private Type _converterType;
		#endregion

		#region 构造函数
		public ConditionAttribute(bool ignored)
		{
			this.Ignored = ignored;
			this.Behaviors = ConditionBehaviors.IgnoreNullOrEmpty;
		}

		public ConditionAttribute(params string[] names)
		{
			this.Names = names;
			this.Behaviors = ConditionBehaviors.IgnoreNullOrEmpty;
		}

		public ConditionAttribute(ConditionOperator @operator, params string[] names) : this(ConditionBehaviors.IgnoreNullOrEmpty, @operator, names)
		{
		}

		public ConditionAttribute(ConditionBehaviors behaviors, params string[] names)
		{
			this.Behaviors = behaviors;
			this.Names = names;
		}

		public ConditionAttribute(ConditionBehaviors behaviors, ConditionOperator @operator, params string[] names)
		{
			this.Behaviors = behaviors;
			this.Operator = @operator;
			this.Names = names;
		}

		public ConditionAttribute(Type converterType, params string[] names) : this(converterType, ConditionBehaviors.IgnoreNullOrEmpty, names)
		{
		}

		public ConditionAttribute(Type converterType, ConditionOperator @operator, params string[] names) : this(converterType, ConditionBehaviors.IgnoreNullOrEmpty, @operator, names)
		{
		}

		public ConditionAttribute(Type converterType, ConditionBehaviors behaviors, params string[] names)
		{
			this.ConverterType = converterType;
			this.Behaviors = behaviors;
			this.Names = names;
		}

		public ConditionAttribute(Type converterType, ConditionBehaviors behaviors, ConditionOperator @operator, params string[] names)
		{
			this.ConverterType = converterType;
			this.Behaviors = behaviors;
			this.Operator = @operator;
			this.Names = names;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置条件成员对应的条件名数组。</summary>
		public string[] Names { get; set; }

		/// <summary>获取或设置一个值，指示是否忽略当前条件成员。</summary>
		public bool Ignored { get; set; }

		/// <summary>获取或设置查询条件的行为。</summary>
		public ConditionBehaviors Behaviors { get; set; }

		/// <summary>获取或设置条件成员的运算符，如果为空则表示自动匹配一个合适的运算符。</summary>
		public ConditionOperator? Operator { get; set; }

		/// <summary>获取或设置条件转换器类型。</summary>
		public Type ConverterType
		{
			get => _converterType;
			set => _converterType = value == null || typeof(IConditionConverter).IsAssignableFrom(value) ? value : throw new ArgumentException($"The specified '{value}' type is not an unimplemented '{nameof(IConditionConverter)}' interface.");
		}
		#endregion
	}
}
