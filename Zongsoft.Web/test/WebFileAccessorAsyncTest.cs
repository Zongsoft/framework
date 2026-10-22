using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.IO;
using Zongsoft.Data;

using Xunit;

namespace Zongsoft.Web.Tests;

[Collection("WebFileAccessor")]
public class WebFileAccessorAsyncTest
{
	[Fact]
	public async Task Write_UsesAsyncOnlyStorageAndPreservesMultipartMetadata()
	{
		using var fixture = new UploadFixture();
		using var cancellation = new CancellationTokenSource();
		var files = new List<Zongsoft.IO.FileInfo>();

		await foreach(var file in fixture.Accessor.Write(fixture.Context.Request, null, null, cancellation.Token))
			files.Add(file);

		var result = Assert.Single(files);
		Assert.Equal("sample.txt", result.Name);
		Assert.Equal("text/plain", result.Type);
		Assert.Equal(UploadFixture.Content.Length, result.Size);
		Assert.Equal("upload", result.Properties["Name"]);
		Assert.Equal("sample.txt", result.Properties["FileName"]);
		Assert.Equal(UploadFixture.Content, Encoding.UTF8.GetString(fixture.Output.ToArray()));
		Assert.Equal(cancellation.Token, fixture.OpenToken);
		Assert.Equal(cancellation.Token, fixture.Output.WriteToken);
		Assert.Equal(1, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
		Assert.True(fixture.Context.Request.Body.CanRead);
	}

	[Fact]
	public async Task Write_WaitsForOpenCopyAndDisposeBeforeReturningFile()
	{
		using var fixture = new UploadFixture();
		using var cancellation = new CancellationTokenSource();
		fixture.OpenGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Output.WriteGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Output.DisposeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

		var configured = false;
		var returned = false;
		var operation = ReadAllAsync(fixture.Accessor.Write(fixture.Context.Request, null, options =>
		{
			Assert.False(fixture.OpenEntered.Task.IsCompleted);
			Assert.Equal("sample.txt", options.FileName);
			configured = true;
		}, cancellation.Token), _ => returned = true);

		await fixture.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.True(configured);
		Assert.False(operation.IsCompleted);
		Assert.False(fixture.Output.WriteEntered.Task.IsCompleted);
		Assert.Equal(cancellation.Token, fixture.OpenToken);
		fixture.OpenGate.SetResult();

		await fixture.Output.WriteEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(operation.IsCompleted);
		Assert.False(fixture.Output.DisposeEntered.Task.IsCompleted);
		Assert.False(returned);
		Assert.Equal(cancellation.Token, fixture.Output.WriteToken);
		fixture.Output.WriteGate.SetResult();

		await fixture.Output.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(operation.IsCompleted);
		Assert.Equal(UploadFixture.Content, Encoding.UTF8.GetString(fixture.Output.ToArray()));
		Assert.False(returned);
		fixture.Output.DisposeGate.SetResult();

		Assert.Single(await operation.WaitAsync(TimeSpan.FromSeconds(10)));
		Assert.Equal(1, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
	}

	[Fact]
	public async Task Write_OpenFailureDoesNotDisposeUnacquiredStream()
	{
		using var fixture = new UploadFixture();
		var failure = new IOException("test open failure");
		fixture.OpenFailure = failure;

		Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => ReadAllAsync(fixture.Accessor.Write(fixture.Context.Request))));
		Assert.False(fixture.Output.WriteEntered.Task.IsCompleted);
		Assert.Equal(0, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
	}

	[Fact]
	public async Task Write_CopyFailureWaitsForDisposal()
	{
		using var fixture = new UploadFixture();
		var failure = new IOException("test write failure");
		fixture.Output.WriteFailure = failure;
		fixture.Output.DisposeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		var operation = ReadAllAsync(fixture.Accessor.Write(fixture.Context.Request));

		await fixture.Output.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(operation.IsCompleted);
		fixture.Output.DisposeGate.SetResult();
		Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => operation));
		Assert.Equal(1, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
		Assert.True(fixture.Context.Request.Body.CanRead);
	}

	[Fact]
	public async Task Write_CopyCancellationWaitsForDisposal()
	{
		using var fixture = new UploadFixture();
		using var cancellation = new CancellationTokenSource();
		fixture.Output.WriteGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Output.DisposeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		var operation = ReadAllAsync(fixture.Accessor.Write(fixture.Context.Request, null, null, cancellation.Token));

		await fixture.Output.WriteEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(cancellation.Token, fixture.Output.WriteToken);
		cancellation.Cancel();
		await fixture.Output.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(operation.IsCompleted);
		fixture.Output.DisposeGate.SetResult();
		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.Equal(1, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
		Assert.True(fixture.Context.Request.Body.CanRead);
	}

	[Fact]
	public async Task Write_DisposeFailureDoesNotReturnFile()
	{
		using var fixture = new UploadFixture();
		var failure = new IOException("test disposal failure");
		fixture.Output.DisposeFailure = failure;
		var returned = 0;

		Assert.Same(failure, await Assert.ThrowsAsync<IOException>(async () =>
		{
			await foreach(var file in fixture.Accessor.Write(fixture.Context.Request))
				returned++;
		}));
		Assert.Equal(0, returned);
		Assert.Equal(UploadFixture.Content, Encoding.UTF8.GetString(fixture.Output.ToArray()));
		Assert.Equal(1, fixture.Output.AsyncDisposals);
		Assert.Equal(0, fixture.Output.SyncDisposals);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, false)]
	[InlineData(false, true)]
	public async Task UploadAsync_CallbackRunsAfterDisposalAndAwaitsRejectedFileDeletion(bool accepted, bool deleteFails)
	{
		using var fixture = new UploadFixture();
		using var cancellation = new CancellationTokenSource();
		fixture.Output.DisposeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.DeleteGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.DeleteFailure = deleteFails ? new IOException("test delete failure") : null;
		var controller = new UploadController { ControllerContext = new ControllerContext { HttpContext = fixture.Context } };
		var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbackGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var operation = ReadAllAsync(controller.Upload(fixture.Scheme + ":/files/", async (file, token) =>
		{
			Assert.Equal(cancellation.Token, token);
			Assert.Equal(1, fixture.Output.AsyncDisposals);
			Assert.False(fixture.Output.CanWrite);
			Assert.Equal(UploadFixture.Content, Encoding.UTF8.GetString(fixture.Output.ToArray()));
			callbackEntered.SetResult();
			await callbackGate.Task;
			cancellation.Cancel();
			return accepted ? file.Name : null;
		}, cancellation.Token));

		try
		{
			await fixture.Output.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.False(operation.IsCompleted);
			Assert.False(callbackEntered.Task.IsCompleted);
			fixture.Output.DisposeGate.SetResult();
			await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.False(operation.IsCompleted);
			Assert.Equal(0, fixture.DeleteCalls);
			callbackGate.SetResult();

			if(accepted)
			{
				Assert.Equal("sample.txt", Assert.Single(await operation.WaitAsync(TimeSpan.FromSeconds(10))));
				Assert.Equal(0, fixture.DeleteCalls);
			}
			else
			{
				await Task.WhenAny(fixture.DeleteEntered.Task, operation).WaitAsync(TimeSpan.FromSeconds(10));
				Assert.Equal(1, fixture.DeleteCalls);
				Assert.False(operation.IsCompleted);
				Assert.Equal(CancellationToken.None, fixture.DeleteToken);
				Assert.Equal("/files/sample.txt", fixture.DeletedPath);
				fixture.DeleteGate.SetResult();
				Assert.Empty(await operation.WaitAsync(TimeSpan.FromSeconds(10)));
				Assert.Equal(1, fixture.DeleteCalls);
			}

			Assert.Equal(1, fixture.Output.AsyncDisposals);
			Assert.Equal(0, fixture.Output.SyncDisposals);
		}
		finally
		{
			callbackGate.TrySetResult();
		}
	}

	private static async Task<List<T>> ReadAllAsync<T>(IAsyncEnumerable<T> source, Action<T> received = null)
	{
		var result = new List<T>();

		await foreach(var item in source)
		{
			received?.Invoke(item);
			result.Add(item);
		}

		return result;
	}

	private sealed class UploadController : ServiceControllerBase<object, IDataService<object>>
	{
		public IAsyncEnumerable<string> Upload(string path, Func<Zongsoft.IO.FileInfo, CancellationToken, ValueTask<string>> uploaded, CancellationToken cancellation) => this.UploadAsync(path, uploaded, cancellation);
	}

	private sealed class UploadFixture : IFileSystem, IFile, IDisposable
	{
		public const string Content = "multipart-upload-payload";

		public UploadFixture()
		{
			this.Scheme = "upload" + Guid.NewGuid().ToString("N");
			this.Accessor = new WebFileAccessor(this.Scheme + ":/files/");
			this.Context = new DefaultHttpContext();
			const string BOUNDARY = "async-upload-boundary";
			var body = Encoding.UTF8.GetBytes($"--{BOUNDARY}\r\nContent-Disposition: form-data; name=\"upload\"; filename=\"sample.txt\"\r\nContent-Type: text/plain\r\n\r\n{Content}\r\n--{BOUNDARY}--\r\n");
			this.Context.Request.ContentType = "multipart/form-data; boundary=" + BOUNDARY;
			this.Context.Request.ContentLength = body.Length;
			this.Context.Request.Body = new MemoryStream(body, false);
			FileSystem.Providers.Add(this);
		}

		public string Scheme { get; }
		public IFile File => this;
		public IDirectory Directory => throw new NotSupportedException();
		public WebFileAccessor Accessor { get; }
		public DefaultHttpContext Context { get; }
		public UploadStream Output { get; } = new();
		public CancellationToken OpenToken { get; private set; }
		public TaskCompletionSource OpenEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource OpenGate { get; set; }
		public Exception OpenFailure { get; set; }
		public TaskCompletionSource DeleteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource DeleteGate { get; set; }
		public Exception DeleteFailure { get; set; }
		public CancellationToken DeleteToken { get; private set; }
		public string DeletedPath { get; private set; }
		public int DeleteCalls { get; private set; }

		public string GetUrl(string path) => this.Scheme + ":" + path;
		public string GetUrl(Zongsoft.IO.Path path) => path.Url;

		public void Dispose()
		{
			this.OpenGate?.TrySetResult();
			this.Output.WriteGate?.TrySetResult();
			this.Output.DisposeGate?.TrySetResult();
			this.DeleteGate?.TrySetResult();
			FileSystem.Providers.Remove(this.Scheme);
			this.Context.Request.Body.Dispose();
		}

		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null) => throw new NotSupportedException("Synchronous file open is forbidden by this test provider.");
		public async ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			this.OpenToken = cancellation;
			Assert.Equal(FileMode.CreateNew, mode);
			Assert.Equal(FileAccess.Write, access);
			this.OpenEntered.TrySetResult();

			if(this.OpenGate != null)
				await this.OpenGate.Task.WaitAsync(cancellation);

			if(this.OpenFailure != null)
				throw this.OpenFailure;

			return this.Output;
		}

		public Zongsoft.IO.FileInfo GetInfo(string path) => throw new NotSupportedException();
		public ValueTask<Zongsoft.IO.FileInfo> GetInfoAsync(string path, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool Delete(string path) => throw new NotSupportedException();
		public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default)
		{
			this.DeleteCalls++;
			this.DeleteToken = cancellation;
			this.DeletedPath = path;
			this.DeleteEntered.TrySetResult();

			if(this.DeleteGate != null)
				await this.DeleteGate.Task.WaitAsync(cancellation);

			if(this.DeleteFailure != null)
				throw this.DeleteFailure;

			return true;
		}

		public bool Exists(string path) => throw new NotSupportedException();
		public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default) => throw new NotSupportedException();
		public void Copy(string source, string destination, bool overwrite = true) => throw new NotSupportedException();
		public ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default) => throw new NotSupportedException();
		public void Move(string source, string destination) => throw new NotSupportedException();
		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	private sealed class UploadStream : MemoryStream
	{
		public CancellationToken WriteToken { get; private set; }
		public int AsyncDisposals { get; private set; }
		public int SyncDisposals { get; private set; }
		public TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource WriteGate { get; set; }
		public Exception WriteFailure { get; set; }
		public TaskCompletionSource DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource DisposeGate { get; set; }
		public Exception DisposeFailure { get; set; }
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation) => this.WriteAsync(buffer.AsMemory(offset, count), cancellation).AsTask();
		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
		{
			this.WriteToken = cancellation;
			this.WriteEntered.TrySetResult();

			if(this.WriteGate != null)
				await this.WriteGate.Task.WaitAsync(cancellation);

			if(this.WriteFailure != null)
				throw this.WriteFailure;

			base.Write(buffer.ToArray(), 0, buffer.Length);
		}

		public override async ValueTask DisposeAsync()
		{
			this.AsyncDisposals++;
			this.DisposeEntered.TrySetResult();

			if(this.DisposeGate != null)
				await this.DisposeGate.Task;

			base.Dispose(true);

			if(this.DisposeFailure != null)
				throw this.DisposeFailure;
		}

		protected override void Dispose(bool disposing)
		{
			this.SyncDisposals++;
			throw new NotSupportedException("Synchronous output disposal is forbidden by this test stream.");
		}
	}
}

[CollectionDefinition("WebFileAccessor", DisableParallelization = true)]
public class WebFileAccessorCollection;
