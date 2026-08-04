using System;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClosedXML.Excel;

using Zongsoft.Common;
using Zongsoft.Data;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml.Samples;

internal class Program
{
	private static readonly ModelDescriptor Model = Zongsoft.Data.Model.GetDescriptor<User>();
	private static readonly SpreadsheetGenerator Generator = new();
	private static readonly SpreadsheetExtractor Extractor = new();

	static async Task Main(string[] args)
	{
		var executor = Terminal.Console.Executor;

		executor.Command("export", async (context, cancellation) =>
		{
			var culture = GetCulture(context);
			var path = GetPath(context);

			using var scope = new CultureScope(culture);
			await ExportAsync(path, cancellation);
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"Exported: {path}");
			DisplayWorkbook(context.Output, path);
		});

		executor.Command("import", async (context, cancellation) =>
		{
			var path = GetPath(context);
			var users = await ImportAsync(path, cancellation);
			DisplayWorkbook(context.Output, path);
			DisplayData(context.Output, users);
		});

		executor.Command("verify", async (context, cancellation) =>
		{
			var path = GetPath(context);
			await ExportAsync(path, cancellation);
			var users = await ImportAsync(path, cancellation);
			DisplayWorkbook(context.Output, path);
			DisplayData(context.Output, users);
		});

		executor.Aliaser.Set("export", "out");
		executor.Aliaser.Set("import", "in");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64))
			.AppendLine(CommandOutletColor.Cyan, "ClosedXML Import and Export Sample".Justify(64))
			.AppendLine(CommandOutletColor.DarkGray, "export [--culture:<name>|-c:<name>] [file]".Justify(64))
			.AppendLine(CommandOutletColor.DarkGray, "import [file]  verify [file]".Justify(64))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64));

		await executor.RunAsync(splash);
	}

	private static async ValueTask ExportAsync(string path, CancellationToken cancellation)
	{
		await using var stream = File.Create(path);
		await Generator.GenerateAsync(stream, Model, User.Data, cancellation);
	}

	private static async ValueTask<IReadOnlyList<User>> ImportAsync(string path, CancellationToken cancellation)
	{
		var users = new List<User>();
		await using var stream = File.OpenRead(path);

		await foreach(var user in Extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Model), cancellation))
			users.Add(user);

		return users;
	}

	private static void DisplayWorkbook(ICommandOutlet output, string path)
	{
		using var workbook = new XLWorkbook(path);
		var table = workbook.Table(Model.Name);

		output.WriteLine(CommandOutletColor.DarkCyan, $"Workbook: {Path.GetFileName(path)}");
		output.WriteLine($"Worksheet: {table.Worksheet.Name}");
		output.WriteLine($"Table: {table.Name}");
		output.WriteLine($"Range: {table.RangeAddress}");
		output.WriteLine($"Columns: {table.ColumnCount()}, Declared rows: {table.DataRange?.RowCount() ?? 0}");
	}

	private static void DisplayData(ICommandOutlet output, IReadOnlyList<User> users)
	{
		output.WriteLine(CommandOutletColor.Magenta, $"Imported records: {users.Count}");

		for(int index = 0; index < users.Count; index++)
		{
			var user = users[index];
			output.WriteLine($"[{index + 1}] Id={user.UserId}, Name={user.Name}, Gender={user.Gender}, Birthday={user.Birthday:yyyy-MM-dd}, Email={user.Email}, Active={user.IsActive?.ToString() ?? "<null>"}, Status={user.Status?.ToString() ?? "<null>"}, Balance={user.Balance?.ToString("0.00") ?? "<null>"}");
		}
	}

	private static CultureInfo GetCulture(CommandContext context)
	{
		var specified = context.Options.TryGetValue<string>("culture", out var cultureName);

		if(!specified)
			specified = context.Options.TryGetValue<string>("c", out cultureName);

		if(!specified)
			return null;

		try
		{
			return CultureInfo.GetCultureInfo(cultureName);
		}
		catch(CultureNotFoundException)
		{
			throw new CommandOptionValueException("culture", cultureName);
		}
	}

	private static string GetPath(CommandContext context)
	{
		var path = context.Arguments.IsEmpty ? "users.xlsx" : context.Arguments[0];
		return Path.GetFullPath(path);
	}

	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
		private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

		public CultureScope(CultureInfo culture)
		{
			if(culture == null)
				return;

			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		public void Dispose()
		{
			if(_culture == null)
				return;

			CultureInfo.CurrentCulture = _culture;
			CultureInfo.CurrentUICulture = _uiCulture;
		}
	}

	private sealed class User
	{
		public static readonly User[] Data =
		[
			new() { UserId = 101, Name = "Popeye", Gender = Gender.Male, Email = "zongsoft@qq.com", IsActive = true, Status = UserStatus.Active, Balance = 128.50m },
			new() { UserId = 102, Name = "Ada", Gender = Gender.Female, Birthday = new DateTime(1815, 12, 10), Email = "ada@example.com", IsActive = false, Status = UserStatus.Inactive, Balance = -12.25m },
			new() { UserId = 103, Name = "Grace", Gender = Gender.Female, Birthday = new DateTime(1906, 12, 9), Email = "grace@example.com" },
		];

		[ModelProperty(IsPrimaryKey = true)]
		public int UserId { get; set; }
		public string Name { get; set; }
		public Gender Gender { get; set; }
		public DateTime? Birthday { get; set; }
		public string Email { get; set; }
		public bool? IsActive { get; set; }
		public UserStatus? Status { get; set; }
		[ModelProperty(Role = nameof(ModelPropertyRole.Currency))]
		public decimal? Balance { get; set; }
	}

	private enum Gender : byte
	{
		Female,
		Male,
	}

	private enum UserStatus : byte
	{
		Inactive,
		Active,
		Suspended,
	}
}
