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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Zongsoft.Externals.Wechat;

public static class AuthorityUtility
{
	#region 获取商户
	public static async ValueTask<IAuthority> GetAuthorityAsync(string name = null, CancellationToken cancellation = default) =>
		await FindAuthorityAsync(name, cancellation) ?? throw new WechatException(Properties.Resources.Wechat_DefaultAuthorityRequired_Message);

	public static ValueTask<IAuthority> FindAuthorityAsync(string name = null, CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		var options = string.IsNullOrEmpty(name) ?
			Utility.GetOptions<Options.AuthorityOptionsCollection>("/Externals/Wechat/Authorities")?.GetDefault() :
			Utility.GetOptions<Options.AuthorityOptions>($"/Externals/Wechat/Authorities/{name}");

		return options == null ? ValueTask.FromResult<IAuthority>(null) : CreateAsync(options, cancellation);
	}
	#endregion

	#region 加载快照
	public static async ValueTask<IAuthority> CreateAsync(Options.AuthorityOptions options, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(options);
		cancellation.ThrowIfCancellationRequested();

		//在异步读取前取得本次配置值；发布后不保留可变配置对象或打开的流。
		var name = options.Name;
		var code = options.Code;
		var apiV2Key = string.IsNullOrEmpty(options.ApiV2Key) ? null : options.ApiV2Key;
		var apiV3Key = string.IsNullOrEmpty(options.ApiV3Key) ? null : options.ApiV3Key;
		var serial = options.Signing?.SerialNumber;
		var privateKeyFile = options.Signing?.PrivateKeyFile;
		var certificateFile = options.Signing?.CertificateFile;
		var mode = options.Verification?.Mode;
		var encryptionKeyId = options.Verification?.EncryptionKeyId;
		var files = options.Verification?.Keys.Select(key => (key.Name, key.File)).ToArray();
		var accounts = new AccountCollection(options.Apps);
		Authority.SigningCredential signing = null;
		Authority.Verifier verification = null;

		if(options.Signing != null)
		{
			var pem = await ReadAsync(privateKeyFile, cancellation);
			var certificate = string.IsNullOrEmpty(certificateFile) ? null : await ReadAsync(certificateFile, cancellation);
			signing = new(code, serial, pem, certificate: certificate);
		}

		if(mode.HasValue)
		{
			var keys = new Authority.VerificationKey[files.Length];
			for(int i = 0; i < files.Length; i++)
			{
				var pem = await ReadAsync(files[i].File, cancellation);
				keys[i] = mode == VerificationMode.PublicKey ?
					Authority.VerificationKey.FromPublicKey(files[i].Name, pem) : Authority.VerificationKey.FromCertificate(pem);

				if(!string.Equals(files[i].Name, keys[i].Identifier, StringComparison.Ordinal))
					throw new CryptographicException();
			}

			verification = new(mode.Value, keys, encryptionKeyId);
		}

		cancellation.ThrowIfCancellationRequested();
		return new Authority(name, code, apiV2Key, apiV3Key, signing, verification, accounts);
	}
	#endregion

	#region 私有方法
	private static async ValueTask<string> ReadAsync(string path, CancellationToken cancellation)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		await using var stream = await Zongsoft.IO.FileSystem.File.OpenAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read, cancellation);
		using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
		return await reader.ReadToEndAsync(cancellation);
	}
	#endregion
}
