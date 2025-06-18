using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal static class Program
{
	static async Task Main(string[] args)
	{
		using var server = new OpcServer();
		server.Options.Storages.Define("http://zongsoft.com/opc/ua").Initialize();
		await server.StartAsync(args);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync(args, cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync(args, cancellation));

		executor.Command("info", context =>
		{
			int index = 0;

			context.Output.Write(CommandOutletColor.Cyan, "State: ");
			context.Output.WriteLine(server.State == WorkerState.Running ? CommandOutletColor.Green : CommandOutletColor.Magenta, server.State);
			context.Output.Write(CommandOutletColor.Cyan, "Elapsed: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Elapsed.ToString());
			context.Output.Write(CommandOutletColor.Cyan, "Certificate: ");
			if(server.Certificate == null)
				context.Output.WriteLine(CommandOutletColor.White, "NULL");
			else
			{
				var content = CommandOutletContent.Create(Environment.NewLine)
					.AppendLine(CommandOutletColor.DarkGray, '{')
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.Issuer)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.Issuer)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.Subject)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.Subject)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.FriendlyName)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.FriendlyName)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.SerialNumber)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.SerialNumber)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.Thumbprint)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.Thumbprint)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.Version)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.Version)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.NotBefore)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.NotBefore)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.NotAfter)}: ")
					.AppendLine(CommandOutletColor.DarkGreen, server.Certificate.NotAfter)
					.Append(CommandOutletColor.DarkYellow, $"    {nameof(server.Certificate.PublicKey)}: ")
					.Append(CommandOutletColor.DarkMagenta, $" {server.Certificate.PublicKey.Oid.FriendlyName} ")
					.AppendLine(CommandOutletColor.Yellow, $"{System.Convert.ToBase64String(server.Certificate.PublicKey.EncodedKeyValue.RawData)}")
					.AppendLine(CommandOutletColor.DarkGray, '}');

				context.Output.WriteLine(content);
			}

			foreach(var channel in server.Channels)
			{
				var content = CommandOutletContent.Create(CommandOutletColor.DarkYellow, nameof(OpcServer.Channel))
					.Append(CommandOutletColor.DarkGray, " #")
					.Append(CommandOutletColor.Magenta, $"{index++}");

				content.Dump(channel);
				context.Output.WriteLine(content);
			}
		});

		executor.Command("get", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
				return;

			if(context.Expression.Arguments.Count == 1)
			{
				var succeed = server.TryGetValue(context.Expression.Arguments[0], out var value);
				var content = CommandOutletContent.Create(string.Empty);

				if(succeed)
					content.AppendValue(value);
				else
					content.AppendLine(CommandOutletColor.DarkRed, $"The value of '{context.Expression.Arguments[0]}' was not found.");

				context.Output.Write(content);
			}
			else
			{
				var result = server.GetValues(context.Expression.Arguments);

				foreach(var entry in result)
				{
					var content = CommandOutletContent
						.Create(CommandOutletColor.DarkYellow, entry.Key)
						.Append(CommandOutletColor.DarkGray, " : ")
						.AppendValue(entry.Value);

					context.Output.Write(content);
				}
			}
		});

		executor.Command("set", context =>
		{
			if(context.Expression.Arguments.Count < 2)
				throw new CommandException($"Missing required argument of the command.");

			//获取指定键对应的数据类型
			var type = server.GetDataType(context.Expression.Arguments[0]) ??
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

			var round = context.Expression.Options.GetValue("round", 0);

			if(round > 0)
			{
				for(int i = 0; i < round; i++)
				{
					var succeed = server.SetValue(context.Expression.Arguments[0], Add(value, i));

					Thread.Sleep(100);

					var content = CommandOutletContent.Create()
						.Append(CommandOutletColor.DarkGray, "[")
						.Append(CommandOutletColor.Cyan, $"{i + 1}")
						.Append(CommandOutletColor.DarkGray, "] ");

					if(succeed)
						content.Last.AppendLine(CommandOutletColor.DarkGreen, "The set operation was successful.");
					else
						content.Last.AppendLine(CommandOutletColor.DarkRed, "The set operation failed.");

					context.Output.Write(content);
				}
			}
			else
			{
				var succeed = server.SetValue(context.Expression.Arguments[0], value);

				if(succeed)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, "The set operation was successful.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, "The set operation failed.");
			}
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Blue, "Welcome to the OPC-UA Server.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		//运行终端命令执行器
		await executor.RunAsync(splash);
	}

	private static void Initialize(this OpcServerOptions.StorageOptions storage) => Initialize(storage.Prefabs);
	private static void Initialize(this PrefabCollection prefabs)
	{
		if(prefabs == null)
			return;

		prefabs.Folder("F0", "Folder #0");
		prefabs.Folder("F1", "Folder #1")
			.Variable<float>("x");
		prefabs.Folder("F2", "Folder #2")
			.Folder("F2-A", "Folder A")
			.Variable<ulong>("y", ulong.MaxValue);
		prefabs.Folder("F3", "Folder #3")
			.Folder("F3-A", "Folder A")
			.Folder("F3-A-1")
			.Variable<byte[]>("z");

		prefabs.Object("Person", typeof(Person), new Person()
		{
			Name = "Popeye",
			Gender = true,
			Birthday = new DateTime(2000, 1, 10),
		});

		var variables = prefabs.Folder("variables");
		variables.Variable("double", typeof(double));
		variables.Variable("float", typeof(float), 1.23);
		variables.Variable("integer", typeof(int), 100);
		variables.Variable<string>("text");
		variables.Variable("boolean", true);
		variables.Variable("guid", Guid.NewGuid());
		variables.Variable("date", DateTime.Now);
		variables.Variable<byte[]>("binary", [1, 2, 3, 4, 5]);

		//特殊标识符
		variables.Variable("#", "A");
		variables.Variable($"#{int.MaxValue}", int.MaxValue);
		variables.Variable($"#{uint.MaxValue}", uint.MaxValue);
		variables.Variable($"#{short.MaxValue}", short.MaxValue);
		variables.Variable($"#{ushort.MaxValue}", ushort.MaxValue);
		variables.Variable("#01234567-1234-ABCD-5678-A1B2C3D4E5F6", Guid.NewGuid());
		variables.Variable("guid()", Guid.NewGuid());
		variables.Variable("uuid()", Guid.NewGuid());
	}

	private static object Add(object value, int interval) => value switch
	{
		double number => number + interval,
		float number => number + interval,
		int number => number + interval,
		uint number => number + interval,
		short number => number + interval,
		ushort number => number + interval,
		long number => number + interval,
		ulong number => number + (ulong)interval,
		byte number => number + interval,
		sbyte number => number + interval,
		string => $"{value}#{interval}",
		DateOnly date => date.AddDays(interval),
		TimeOnly time => time.AddMinutes(interval),
		DateTime datetime => datetime.AddSeconds(interval),
		DateTimeOffset datetime => datetime.AddSeconds(interval),
		_ => value,
	};

	public class Person
	{
		public string Name { get; set; }
		public bool? Gender { get; set; }
		public DateTime? Birthday { get; set; }
		public Address? HomeAddress { get; set; }
		public Address? OfficeAddress { get; set; }
	}

	public struct Address
	{
		public int Country { get; set; }
		public string Province { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public string Detail { get; set; }
		public string PostalCode { get; set; }
	}
}
