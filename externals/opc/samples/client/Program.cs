using System;
using System.IO;
using System.Linq;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal class Program
{
	private const string CONNECTION_SETTINGS = "Server=opc.tcp://localhost:4840;UserName=Program;Password=;Certificate=zfs.local:./certificates/certificate.pfx;CertificateSecret=;";

	private static OpcClient _client;

	static void Main(string[] args)
	{
		_client = new OpcClient();

		var executor = Terminal.Console.Executor;

		executor.Command("info", Commands.Info, _client);

		executor.Command("reset", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				foreach(var subscriber in _client.Subscribers)
					subscriber.Statistics.Reset();

				return;
			}

			foreach(var key in context.Expression.Arguments)
			{
				if(!uint.TryParse(key, out var id))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{key}' subscription identifier is an illegal integer.");
					continue;
				}

				if(!_client.Subscribers.TryGetValue(id, out var subscriber))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{key}' subscription does not exist.");
					continue;
				}

				subscriber.Statistics.Reset();
			}
		});

		executor.Command("connect", (context, cancellation) =>
		{
			var settings = context.Expression.Arguments.IsEmpty ? CONNECTION_SETTINGS : context.Expression.Arguments[0];
			return _client.ConnectAsync(settings.Contains('=') ? settings : $"Server={settings}", cancellation);
		});

		executor.Command("disconnect", (context, cancellation) => _client.DisconnectAsync(cancellation));

		executor.Command("subscribe", async (context, cancellation) =>
		{
			Subscriber subscriber = null;

			if(context.Expression.Options.TryGetValue<uint>("s", out var id))
			{
				if(!_client.Subscribers.TryGetValue(id, out subscriber))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{id}' subscription does not exist.");
					return null;
				}

				if(context.Expression.Arguments.IsEmpty)
					throw new CommandException($"Missing required arguments of the subscribe command.");

				foreach(var identifier in context.Expression.Arguments)
					subscriber.Entries.Add(identifier);

				return subscriber;
			}
			else if(context.Expression.Options.TryGetValue<string>("directory", out var directory))
			{
				if(string.IsNullOrEmpty(directory))
					directory = Path.Combine(AppContext.BaseDirectory, "subscription");
				else
					directory = Path.Combine(AppContext.BaseDirectory, directory);

				var paths = context.Expression.Arguments.IsEmpty ?
					Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly) :
					context.Expression.Arguments.Select(argument => Path.Combine(directory, argument));

				var stopwatch = System.Diagnostics.Stopwatch.StartNew();

				foreach(var path in paths)
				{
					stopwatch.Restart();

					using var reader = File.OpenText(path);
					subscriber = await _client.SubscribeAsync(reader.ReadLines(), cancellation);

					stopwatch.Stop();

					if(subscriber == null)
					{
						context.Output.Write(CommandOutletColor.DarkMagenta, $"The subscription failed, possibly because the specified entries is already subscribed.");
						context.Output.Write(CommandOutletColor.DarkGray, " (");
						context.Output.Write(CommandOutletColor.DarkYellow, $"{stopwatch.Elapsed}");
						context.Output.WriteLine(CommandOutletColor.DarkGray, ")");
					}
					else
					{
						context.Output.Write(CommandOutletColor.DarkGreen, $"The '#{subscriber.Identifier}' subscription was successful.");
						context.Output.Write(CommandOutletColor.DarkGray, " (");
						context.Output.Write(CommandOutletColor.DarkYellow, $"{stopwatch.Elapsed}");
						context.Output.WriteLine(CommandOutletColor.DarkGray, ")");
					}
				}
			}
			else
			{
				if(context.Expression.Arguments.IsEmpty)
					throw new CommandException($"Missing required arguments of the subscribe command.");

				subscriber = await _client.SubscribeAsync(context.Expression.Arguments, cancellation);

				if(subscriber == null)
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The subscription failed, possibly because the specified entries is already subscribed.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The '#{subscriber.Identifier}' subscription was successful.");
			}

			return subscriber;
		});

		executor.Command("unsubscribe", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				await _client.Subscribers.UnsubscribeAsync(cancellation);
				return;
			}

			foreach(var key in context.Expression.Arguments)
			{
				if(!uint.TryParse(key, out var id))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{key}' subscription identifier is an illegal integer.");
					continue;
				}

				if(!_client.Subscribers.TryGetValue(id, out var subscriber))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{key}' subscription does not exist.");
					continue;
				}

				await subscriber.DisposeAsync();
				context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The specified '{key}' unsubscribed successfully.");
			}
		});

		executor.Command("exist", async (context, cancellation) =>
		{
			for(int i = 0; i < context.Expression.Arguments.Count; i++)
			{
				var existed = await _client.ExistsAsync(context.Expression.Arguments[i], cancellation);

				if(existed)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The '{context.Expression.Arguments[i]}' node exists.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, $"The '{context.Expression.Arguments[i]}' node was not found.");
			}

			return true;
		});

		executor.Command("type", async (context, cancellation) =>
		{
			for(int i = 0; i < context.Expression.Arguments.Count; i++)
			{
				var type = await _client.GetDataTypeAsync(context.Expression.Arguments[i], cancellation);

				if(type != null)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, TypeAlias.GetAlias(type));
				else
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The '{context.Expression.Arguments[i]}' node not found.");
			}
		});

		executor.Command("get", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.IsEmpty)
				return;

			if(context.Expression.Arguments.Count == 1)
			{
				var value = await _client.GetValueAsync(context.Expression.Arguments[0], cancellation);
				var content = CommandOutletContent.Create(string.Empty).AppendValue(value);
				context.Output.Write(content);
			}
			else
			{
				var result = _client.GetValuesAsync(context.Expression.Arguments, cancellation);

				await foreach(var entry in result)
				{
					var content = CommandOutletContent
						.Create(CommandOutletColor.DarkYellow, entry.Key)
						.Append(CommandOutletColor.DarkGray, " : ")
						.AppendValue(entry.Value);

					context.Output.Write(content);
				}
			}
		});

		executor.Command("set", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.Count < 2)
				throw new CommandException($"Missing required argument of the command.");

			//获取指定键对应的数据类型
			var type = await _client.GetDataTypeAsync(context.Expression.Arguments[0], cancellation) ??
				throw new CommandException($"The specified '{context.Expression.Arguments[0]}' does not exist, or its data type is not available.");

			object value = null;

			if(type.IsArray)
			{
				type = type.GetElementType();
				value = Array.CreateInstance(type, context.Expression.Arguments.Count - 1);

				for(int i = 1; i < context.Expression.Arguments.Count; i++)
				{
					((Array)value).SetValue(Common.Convert.ConvertValue(context.Expression.Arguments[i], type), i - 1);
				}
			}
			else
			{
				if(context.Expression.Arguments.Count > 2)
					throw new CommandException($"Too many command arguments.");

				value = Common.Convert.ConvertValue(context.Expression.Arguments[1], type);
			}

			var succeed = await _client.SetValueAsync(context.Expression.Arguments[0], value, cancellation);

			if(succeed)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The set operation was successful.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The set operation failed.");
		});

		executor.Command("folder", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.IsEmpty)
				return;

			var created = await _client.CreateFolderAsync(context.Expression.Arguments[0], context.Expression.Arguments.Count > 1 ? context.Expression.Arguments[1] : null, cancellation);

			if(created)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The folder was created successfully.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The folder creation failed.");
		});

		executor.Command("variable", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.IsEmpty)
				return;

			var name = context.Expression.Arguments[0];
			var type = TypeAlias.Parse(context.Expression.Options.GetValue<string>("type"));
			var label = context.Expression.Options.GetValue("label", name);

			var created = await _client.CreateVariableAsync(name, type, label, context.Expression.Arguments.Count > 1 ? context.Expression.Arguments[1] : null, cancellation);

			if(created)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The variable was created successfully.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The variable creation failed.");
		});

		//添加订阅事件监听命令
		executor.Root.Children.Add(new Commands.ListenCommand(_client));

		//设置相关命令的别名
		executor.Aliaser.Set("subscribe", "sub");
		executor.Aliaser.Set("unsubscribe", "unsub");

		//运行终端命令执行器
		executor.Run($"Welcome to the OPC-UA Client.{Environment.NewLine}{new string('-', 50)}");
	}
}
