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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Plugins.Builders
{
	public abstract class BuilderBase : IBuilder
	{
		#region 成员变量
		private IEnumerable<string> _ignoredProperties;
		#endregion

		#region 构造函数
		protected BuilderBase()
		{
		}

		protected BuilderBase(IEnumerable<string> ignoredProperties)
		{
			_ignoredProperties = ignoredProperties;
		}
		#endregion

		#region 保护属性
		/// <summary>
		/// 获取在创建目标对象时要忽略设置的扩展属性名。
		/// </summary>
		/// <remarks>
		///		<para>对重写<see cref="Build"/>方法的实现者的说明：在构建目标对象后应排除本属性所指定的在Builtin.Properties中的属性项。</para>
		/// </remarks>
		protected virtual IEnumerable<string> IgnoredProperties
		{
			get
			{
				return _ignoredProperties;
			}
		}
		#endregion

		#region 获取类型
		public virtual Type GetValueType(Builtin builtin)
		{
			if(builtin.HasValue)
				return builtin.Value.GetType();

			if(builtin.BuiltinType != null)
				return builtin.BuiltinType.Type;

			var attribute = (BuilderBehaviorAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(BuilderBehaviorAttribute), true);

			if(attribute != null)
				return attribute.ValueType;

			return null;
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 创建指定构件对应的目标对象。
		/// </summary>
		/// <param name="context">调用本方法进行构建的上下文对象，可通过该参数获取构建过程的相关设置或状态。</param>
		/// <returns>创建成功后的目标对象。</returns>
		public virtual object Build(BuilderContext context)
		{
			return PluginUtility.BuildBuiltin(context.Builtin, context.Settings, this.IgnoredProperties);
		}

		public virtual void Destroy(BuilderContext context)
		{
			if(context == null || context.Builtin == null)
				return;

			Builtin builtin = context.Builtin;

			if(builtin.HasValue)
			{
				IDisposable value = builtin.Value as IDisposable;

				if(value != null)
				{
					value.Dispose();
				}
				else
				{
					System.Collections.IEnumerable collection = builtin.Value as System.Collections.IEnumerable;

					if(collection != null)
					{
						foreach(object item in collection)
						{
							IDisposable disposable = item as IDisposable;

							if(disposable != null)
								disposable.Dispose();
						}
					}
				}

				builtin.Value = null;
			}
		}

		/// <summary>
		/// 当构建器所属的插件被卸载，该方法被调用。
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region 虚拟方法
		protected virtual void ApplyBehaviors(BuilderContext context)
		{
			BuiltinBehavior behavior = null;

			if(context.Builtin.HasBehaviors && context.Builtin.Behaviors.TryGet("property", out behavior))
			{
				//var path = behavior.GetPropertyValue<string>("name");
				//var target = behavior.GetPropertyValue<object>("target");
				//Reflection.MemberAccess.SetMemberValue(target, path, () => behavior.GetPropertyValue<object>("value"));

				var expression = Reflection.Expressions.MemberExpression.Parse(behavior.GetPropertyValue<string>("name"));
				var target = behavior.GetPropertyValue<object>("target");
				Reflection.Expressions.MemberExpressionEvaluator.Default.SetValue(expression, target, behavior.GetPropertyValue<object>("value"));
			}
		}

		protected virtual void OnBuildComplete(BuilderContext context)
		{
			if(context.OwnerNode == null || context.OwnerNode.NodeType != PluginTreeNodeType.Builtin)
				return;

			//获取构建上下文所关联的追加器
			var appender = context.Appender;

			//注意：如果追加器为空表示忽略后续的追加操作
			if(appender != null && (context.Settings == null || !context.Settings.HasFlags(BuilderSettingsFlags.IgnoreAppending)))
				appender.Append(new AppenderContext(context.PluginContext, context.Result, context.Node, context.Owner, context.OwnerNode, AppenderBehavior.Appending));
		}

		protected virtual void Dispose(bool disposing)
		{
		}
		#endregion

		#region 显式实现
		void IBuilder.OnBuildComplete(BuilderContext context)
		{
			this.ApplyBehaviors(context);
			this.OnBuildComplete(context);
		}
		#endregion
	}
}
