/*
 *     ___         __                            
 *    /   | __  __/ /_____  ____ ___  ____  ____ 
 *   / /| |/ / / / __/ __ \/ __ ` _ \/ __ \/ __ \
 *  / ___ / /_/ / /_/ /_/ / / / / / / /_/ / /_/ /
 * /_/  |_\__/\/\__/\____/_/ /_/ /_/\__/\/\____/ 
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.cn>
 *
 * Copyright (c) 2015-2020 Shenzhen Automao Network Co., Ltd. All rights reserved.
 */

using System;
using System.Security.Claims;

namespace Zongsoft.Security
{
	public abstract class ChallengerBase<TEntity> : IChallenger
	{
		#region 构造函数
		protected ChallengerBase() { }
		#endregion

		#region 公共方法
		public object Challenge(ClaimsPrincipal principal, string scenario)
		{
			//通知质询开始
			var succeed = this.OnChallenging(principal);

			if(!succeed)
				return succeed;

			//依次验证主体身份
			foreach(var identity in principal.Identities)
			{
				var entity = this.Validate(identity, scenario);

				//更新当前用户的声明属性
				if(entity != null)
					this.OnClaims(identity, entity);
			}

			//通知质询完成
			return this.OnChallenged(principal);
		}
		#endregion

		#region 虚拟方法
		protected virtual bool OnChallenging(ClaimsPrincipal principal) => true;
		protected virtual object OnChallenged(ClaimsPrincipal principal) => null;
		#endregion

		#region 抽象方法
		protected abstract void OnClaims(ClaimsIdentity identity, TEntity user);
		protected abstract TEntity Validate(ClaimsIdentity identity, string scenario);
		#endregion
	}
}
