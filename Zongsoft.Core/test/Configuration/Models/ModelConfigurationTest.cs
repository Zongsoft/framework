using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration.Models
{
	public class ModelConfigurationTest
	{
		#region 常量定义
		private const int TENANT_ID = 1;
		private const int BRANCH_ID = 0x01020300;
		private const string MODULE = "*";
		#endregion

		public static IConfigurationRoot GetConfiguration(Action<IConfigurationEntity> persistent = null)
		{
			//初始化数据（模拟数据库中的配置表中的记录）
			var dictionary = new Dictionary<string, string>
			{
				{ "general:name", "general.name" },
				{ "general:intranet", "true" },

				{ "general:certificates:default", "main" },

				{ "general:certificates:main:name", "main" },
				{ "general:certificates:main:code", "C001" },
				{ "general:certificates:main:secret", "xxxx" },

				{ "general:certificates:test:name", "test" },
				{ "general:certificates:test:code", "C002" },
				{ "general:certificates:test:secret", "zzzz" },

				{ "mobile:region", "Shenzhen" },
				{ "mobile.certificate", "" },

				{ "mobile:messages:alarm:name", "Alarm" },
				{ "mobile:messages:alarm:code", "SMS_01" },
				{ "mobile:messages:alarm:scheme", "Zongsoft" },

				{ "mobile:voices:numbers", "400123456, 400666888" },
				{ "mobile:voices:alarm:name", "Alarm" },
				{ "mobile:voices:alarm:code", "TTS_01" },
				{ "mobile:voices:password.forget:name", "Password.Forget" },
				{ "mobile:voices:password.forget:code", "TTS_02" },

				{ "mobile:pushing:wechat:key", "wechat" },
				{ "mobile:pushing:wechat:code", "A123" },
				{ "mobile:pushing:wechat:secret", "****" },
			};

			//获取配置数据（模拟数据库访问操作）
			IEnumerable<IConfigurationEntity> GetModels(IDictionary<string, string> data)
			{
				foreach(var entry in data)
				{
					yield return Data.Model.Build<IConfigurationEntity>(model =>
					{
						model.TenantId = TENANT_ID;
						model.BranchId = BRANCH_ID;
						model.Module = MODULE;
						model.Key = entry.Key;
						model.Value = entry.Value;
					});
				}
			}

			var models = GetModels(dictionary);

			return new ConfigurationBuilder()
				.AddModels(models, source =>
				{
					source.OnChange(model => System.Diagnostics.Debug.WriteLine(model.GetInfo()));

					if(persistent != null)
						source.OnChange(persistent);
				})
				.Build();
		}

		[Fact]
		public void TestLoad()
		{
			var configuration = GetConfiguration();

			Assert.NotEmpty(configuration.Providers);

			Assert.Equal("general.name", configuration.GetSection("general:name").Value);
			Assert.Equal("true", configuration.GetSection("general:intranet").Value);
			Assert.Equal("main", configuration.GetSection("general:certificates:default").Value);

			Assert.Equal("main", configuration.GetSection("general:certificates:main:name").Value);
			Assert.Equal("C001", configuration.GetSection("general:certificates:main:code").Value);
			Assert.Equal("xxxx", configuration.GetSection("general:certificates:main:secret").Value);

			Assert.Equal("test", configuration.GetSection("general:certificates:test:name").Value);
			Assert.Equal("C002", configuration.GetSection("general:certificates:test:code").Value);
			Assert.Equal("zzzz", configuration.GetSection("general:certificates:test:secret").Value);

			Assert.Equal("Shenzhen", configuration.GetSection("mobile:region").Value);
			Assert.Empty(configuration.GetSection("mobile.certificate").Value);

			Assert.Equal("Alarm", configuration.GetSection("mobile:messages:alarm:name").Value);
			Assert.Equal("SMS_01", configuration.GetSection("mobile:messages:alarm:code").Value);
			Assert.Equal("Zongsoft", configuration.GetSection("mobile:messages:alarm:scheme").Value);

			Assert.Equal("400123456, 400666888", configuration.GetSection("mobile:voices:numbers").Value);
			Assert.Equal("Alarm", configuration.GetSection("mobile:voices:alarm:name").Value);
			Assert.Equal("TTS_01", configuration.GetSection("mobile:voices:alarm:code").Value);
			Assert.Equal("Password.Forget", configuration.GetSection("mobile:voices:password.forget:name").Value);
			Assert.Equal("TTS_02", configuration.GetSection("mobile:voices:password.forget:code").Value);

			Assert.Equal("wechat", configuration.GetSection("mobile:pushing:wechat:key").Value);
			Assert.Equal("A123", configuration.GetSection("mobile:pushing:wechat:code").Value);
			Assert.Equal("****", configuration.GetSection("mobile:pushing:wechat:secret").Value);
		}

		[Fact]
		public void TestChange()
		{
			var configuration = GetConfiguration(OnChanged);

			Assert.NotEmpty(configuration.Providers);

			configuration.GetSection("general:name").Value = "abc";
			configuration.GetSection("general:intranet").Value = "false";
			configuration.GetSection("general:certificates:default").Value = "test";
			configuration.GetSection("general:newkey").Value = "new-value";

			static void OnChanged(IConfigurationEntity entity)
			{
				switch(entity.Key)
				{
					case "general:name":
						Assert.Equal("abc", entity.Value);
						break;
					case "general:intranet":
						Assert.Equal("false", entity.Value);
						break;
					case "general:certificates:default":
						Assert.Equal("test", entity.Value);
						break;
				}

				if(entity.TenantId != TENANT_ID)
					entity.TenantId = TENANT_ID;
				if(entity.BranchId != BRANCH_ID)
					entity.BranchId = BRANCH_ID;
				if(string.IsNullOrEmpty(entity.Module))
					entity.Module = MODULE;

				/*
				 * 这里进行持久化操作，譬如调用数据层的Upsert方法：
				 * this.DataAccess.Upsert(entity);
				 */
			}
		}
	}

	public interface IConfigurationEntity : Data.IModel
	{
		int TenantId { get; set; }

		int BranchId { get; set; }

		string Module { get; set; }

		string Key { get; set; }

		string Value { get; set; }
	}

	public static class ConfigurationEntityExtension
	{
		public static string GetInfo(this Models.IConfigurationEntity entity)
		{
			if(entity == null)
				return string.Empty;

			return $"[{entity.TenantId}.{entity.BranchId}:{entity.Module}]" + Environment.NewLine +
				   $"{entity.Key}={entity.Value}";
		}
	}
}
