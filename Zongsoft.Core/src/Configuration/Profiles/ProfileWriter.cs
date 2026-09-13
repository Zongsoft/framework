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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

internal sealed class ProfileWriter
{
	#region 成员字段
	private readonly ProfileOptions _options;
	#endregion

	#region 构造函数
	public ProfileWriter(ProfileOptions options)
	{
		_options = options?.Clone() ?? new ProfileOptions();
	}
	#endregion

	#region 写入方法
	public void Write(Profile profile)
	{
		List<Profile> profiles = [];
		//沿已登记的导入关系按后序收集；被覆盖的来源文件也可能仍有本地修改。
		Collect(profile, profiles, new HashSet<Profile>());
		List<Output> outputs = [];
		Dictionary<string, Output> targets = new(ProfileUtility.PathComparer);

		foreach(var source in profiles)
		{
			if(!source.IsModified)
				continue;

			if(string.IsNullOrWhiteSpace(source.FilePath))
				throw new ProfileException(Properties.Resources.Profiles_SourceRequired);

			var path = ProfileUtility.GetIdentity(source.FilePath);
			var snapshot = source.Snapshot();

			if(targets.TryGetValue(path, out var output))
			{
				if(!snapshot.SequenceEqual(output.Snapshot))
					throw new ProfileException(string.Format(Properties.Resources.Profiles_SaveConflict, path));

				output.Sources.Add(source);
			}
			else
			{
				output = new Output(source, path, null, snapshot);
				targets.Add(path, output);
				outputs.Add(output);
			}
		}

		this.WriteFiles(profiles, outputs);
	}

	public void Write(Profile profile, string path, Encoding encoding)
	{
		path = ProfileUtility.GetIdentity(path);
		var output = new Output(profile, path, encoding, profile.Snapshot());

		if(string.IsNullOrEmpty(profile.FilePath) || !ProfileUtility.PathComparer.Equals(path, ProfileUtility.GetIdentity(profile.FilePath)))
			output.Sources.Clear();

		this.WriteFiles([profile], [output]);
	}

	public void Write(Profile profile, Stream stream, Encoding encoding)
	{
		using(var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8))
			this.Write(profile, writer);
	}

	public void Write(Profile profile, TextWriter writer)
	{
		profile.BeginWrite();

		try
		{
			Validate(profile);
			var snapshot = profile.Snapshot();
			this.Render(profile, writer);
			VerifyUnchanged(profile, snapshot);
		}
		finally
		{
			profile.EndWrite();
		}
	}
	#endregion

	#region 私有方法
	private void WriteFiles(List<Profile> profiles, List<Output> outputs)
	{
		List<Profile> active = [];
		List<string> completed = [];
		Exception failure = null;

		try
		{
			var snapshots = profiles.Select(profile => profile.Snapshot()).ToArray();

			foreach(var profile in profiles)
			{
				profile.BeginWrite();
				active.Add(profile);
			}

			foreach(var output in outputs)
			{
				Validate(output.Profile);
				VerifyTarget(output.Path);
			}

			//全部临时输出完成后才替换原文件，准备阶段失败不会修改任何目标。
			foreach(var output in outputs)
			{
				output.TemporaryPath = Path.Combine(Path.GetDirectoryName(output.Path), ".profile-" + Guid.NewGuid().ToString("N") + ".tmp");

				using(var stream = new FileStream(output.TemporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				using(var writer = new StreamWriter(stream, output.Encoding ?? Encoding.UTF8))
					this.Render(output.Profile, writer);
			}

			for(int i = 0; i < profiles.Count; i++)
				VerifyUnchanged(profiles[i], snapshots[i]);

			foreach(var output in outputs)
			{
				try
				{
					VerifyTarget(output.Path);

					if(File.Exists(output.Path))
						File.Replace(output.TemporaryPath, output.Path, null);
					else
						File.Move(output.TemporaryPath, output.Path);
				}
				catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
				{
					var error = new ProfileException(string.Format(Properties.Resources.Profiles_SaveFailed, output.Path, string.Join(", ", completed)), exception);
					error.Data["FailedPath"] = output.Path;
					error.Data["CompletedPaths"] = completed.ToArray();
					throw error;
				}

				//逐个提交不构成跨文件事务，仅更新成功目标的基线，未完成项留待重试。
				completed.Add(output.Path);
				output.TemporaryPath = null;

				foreach(var source in output.Sources)
					source.AcceptChanges(output.Snapshot);
			}
		}
		catch(Exception exception)
		{
			failure = exception;
			throw;
		}
		finally
		{
			foreach(var profile in active)
				profile.EndWrite();

			foreach(var output in outputs)
			{
				if(output.TemporaryPath == null)
					continue;

				try
				{
					File.Delete(output.TemporaryPath);
				}
				catch(Exception exception) when(failure != null && exception is IOException or UnauthorizedAccessException)
				{
					failure.Data[output.TemporaryPath] = exception;
				}
			}
		}
	}

	private void Render(Profile profile, TextWriter writer)
	{
		ProfileSection section = null;
		var blanks = _options.PreserveBlanks ? profile.Blanks ?? [] : [];
		int blankIndex = 0;

		foreach(var statement in profile.Statements)
		{
			while(blankIndex < blanks.Length && (statement.LineNumber < 0 || blanks[blankIndex] <= statement.LineNumber))
			{
				writer.WriteLine();
				blankIndex++;
			}

			if(statement.IsSection)
			{
				section = statement.Section;
				WriteSection(writer, section);
				continue;
			}

			if(section != statement.Section)
			{
				section = statement.Section;
				WriteSection(writer, section);
			}

			if(statement.Item is ProfileEntry entry)
				writer.WriteLine(entry.Value == null ? entry.Name : entry.Name + "=" + entry.Value);
			else if(statement.Item is ProfileComment comment)
			{
				if(comment.Lines.Length == 0)
					writer.WriteLine("#");
				else
				{
					foreach(var line in comment.Lines)
						writer.WriteLine("#" + line);
				}
			}
		}

		while(blankIndex < blanks.Length)
		{
			writer.WriteLine();
			blankIndex++;
		}
	}

	private static void VerifyUnchanged(Profile profile, string[] snapshot)
	{
		if(!profile.Snapshot().SequenceEqual(snapshot))
			throw new ProfileException(Properties.Resources.Profiles_SaveMutation);
	}

	private static void WriteSection(TextWriter writer, ProfileSection section) => writer.WriteLine("[" + section?.FullName + "]");

	private static void Collect(Profile profile, List<Profile> profiles, HashSet<Profile> visited)
	{
		if(!visited.Add(profile))
			return;

		foreach(var imported in profile.Imports)
			Collect(imported, profiles, visited);

		profiles.Add(profile);
	}

	private static void VerifyTarget(string path)
	{
		if(File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
			throw new UnauthorizedAccessException(string.Format(Properties.Resources.Profiles_ReadOnly, path));
	}

	private static void Validate(Profile profile)
	{
		foreach(var statement in profile.Statements)
		{
			for(var section = statement.Section; section != null; section = section.Section)
			{
				if(section.Name.Any(char.IsWhiteSpace))
					throw new ProfileException(string.Format(Properties.Resources.Profiles_SectionInvalid, section.Name));
			}

			if(statement.Item is ProfileEntry entry)
			{
				var line = entry.Value == null ? entry.Name : entry.Name + "=" + entry.Value;

				if(line.Contains('\r') || line.Contains('\n') || entry.Name.Contains('=') ||
					entry.Value != null && entry.Value != entry.Value.Trim() ||
					ProfileUtility.ParseLine(line, out _) != ProfileUtility.LineType.Entry)
					throw new ProfileException(string.Format(Properties.Resources.Profiles_EntryInvalid, entry.Name));
			}
			else if(statement.Item is ProfileComment comment)
			{
				foreach(var line in comment.Lines)
				{
					if(line == null || line.Contains('\r') || line.Contains('\n'))
						throw new ProfileException(Properties.Resources.Profiles_CommentInvalid);
				}
			}
		}

		if(profile.Blanks != null)
		{
			int previous = -1;

			foreach(var blank in profile.Blanks)
			{
				if(blank <= previous)
					throw new ProfileException(Properties.Resources.Profiles_BlanksInvalid);

				previous = blank;
			}
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class Output(Profile profile, string path, Encoding encoding, string[] snapshot)
	{
		public Profile Profile { get; } = profile;
		public string Path { get; } = path;
		public Encoding Encoding { get; } = encoding;
		public string[] Snapshot { get; } = snapshot;
		public List<Profile> Sources { get; } = [profile];
		public string TemporaryPath { get; set; }
	}
	#endregion
}
