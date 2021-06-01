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

using Zongsoft.Common;

namespace Zongsoft.Security
{
	public abstract class IdentityChallengerBase<TEntity> : IIdentityChallenger
	{
		#region 构造函数
		protected IdentityChallengerBase() { }
		#endregion

		#region 公共方法
		public virtual bool CanChallenge(ClaimsPrincipal principal) => true;

		public OperationResult Challenge(ClaimsPrincipal principal)
		{
			//通知质询开始
			var result = this.OnChallenging(principal);

			if(result.Failed)
				return result;

			//获取当前主体的场景
			var scenario = principal is CredentialPrincipal credential ? credential.Scenario : null;

			//依次验证主体身份
			foreach(var identity in principal.Identities)
			{
				var validation = this.Validate(identity, scenario);

				if(validation.Failed)
					return validation;

				//更新当前用户的声明属性
				if(validation.Value != null)
					this.OnClaims(identity, validation.Value);
			}

			//通知质询完成
			return this.OnChallenged(principal);
		}
		#endregion

		#region 虚拟方法
		protected virtual OperationResult OnChallenging(ClaimsPrincipal principal) => OperationResult.Success();
		protected virtual OperationResult OnChallenged(ClaimsPrincipal principal) => OperationResult.Success();
		#endregion

		#region 抽象方法
		protected abstract void OnClaims(ClaimsIdentity identity, TEntity user);
		protected abstract OperationResult<TEntity> Validate(ClaimsIdentity identity, string scenario);
		#endregion
	}
}
