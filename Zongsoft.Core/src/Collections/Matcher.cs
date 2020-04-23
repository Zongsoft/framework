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
using System.Reflection;

namespace Zongsoft.Collections
{
	public class Matcher : IMatcher
	{
		#region 单例字段
		public static readonly Matcher Default = new Matcher();
		#endregion

		#region 匹配方法
		public virtual bool Match(object target, object parameter)
		{
			if(target == null)
				return false;

            if(target is IMatchable matchable)
                return matchable.Match(parameter);

            var matcher = GetMatcher(target.GetType());

            if(matcher != null)
                return matcher.Match(target, parameter);

			//注意：默认返回必须是真
			return true;
		}
        #endregion

        #region 静态方法
        public static IMatcher GetMatcher(MemberInfo member)
        {
            if(member == null)
                return null;

            var attribute = member.GetCustomAttribute<MatcherAttribute>(true);

            if(attribute != null && attribute.MatcherType != null)
            {
                var members = attribute.MatcherType.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.Static | BindingFlags.Public, FindMember, null);

                if(members != null && members.Length > 0)
                {
                    if(members[0] is FieldInfo field)
                        return (IMatcher)field.GetValue(null);
                    if(members[0] is PropertyInfo property)
                        return (IMatcher)property.GetValue(null);
                }

                return (IMatcher)Activator.CreateInstance(attribute.MatcherType);
            }

            return null;
        }

        private static bool FindMember(MemberInfo member, object state)
        {
            if(member.MemberType == MemberTypes.Property)
                return typeof(IMatcher).IsAssignableFrom(((PropertyInfo)member).PropertyType);
            if(member.MemberType == MemberTypes.Field)
                return typeof(IMatcher).IsAssignableFrom(((FieldInfo)member).FieldType);

            return false;
        }
		#endregion
	}
}
