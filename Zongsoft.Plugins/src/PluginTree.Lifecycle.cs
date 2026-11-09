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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Plugins;

partial class PluginTree
{
	private readonly object _lifecycleSync = new();
	private readonly AsyncLocal<Invocation> _invocation = new();
	private Task _unloading = Task.CompletedTask;
	private Plugin _unloadingPlugin;
	private TaskCompletionSource _disposing;

	/// <summary>异步卸载全部插件；插件卸载事件可以取消对应插件的卸载。</summary>
	/// <param name="cancellation">取消等待的通知，已开始的卸载继续执行。</param>
	/// <returns>插件卸载及资源释放完成的操作。</returns>
	public ValueTask UnloadAsync(CancellationToken cancellation = default) => this.QueueUnload(null, false, cancellation);

	/// <summary>异步卸载指定插件及其子插件和依赖方。</summary>
	/// <param name="plugin">当前插件树中的插件。</param>
	/// <param name="cancellation">取消等待的通知，已开始的卸载继续执行。</param>
	/// <returns>插件卸载及资源释放完成的操作。</returns>
	public ValueTask UnloadAsync(Plugin plugin, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(plugin);
		if(!ReferenceEquals(plugin.PluginTree, this))
			throw new ArgumentException(null, nameof(plugin));
		return this.QueueUnload(plugin, false, cancellation);
	}

	/// <summary>等待已开始的卸载并释放全部插件；最终释放不能由卸载事件取消。</summary>
	/// <returns>全部插件资源和插件树注册释放完成的操作。</returns>
	public ValueTask DisposeAsync() => this.QueueUnload(null, true, CancellationToken.None);

	private ValueTask QueueUnload(Plugin plugin, bool disposing, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		if(_invocation.Value is { Active: true })
			throw new InvalidOperationException();

		Task previous;
		TaskCompletionSource completion;

		lock(_lifecycleSync)
		{
			if(disposing && _disposing != null)
				return new(_disposing.Task);

			ObjectDisposedException.ThrowIf(_disposing != null, this);
			if(!disposing && !_unloading.IsCompleted && ReferenceEquals(plugin, _unloadingPlugin))
				return new(cancellation.CanBeCanceled ? _unloading.WaitAsync(cancellation) : _unloading);

			previous = _unloading;
			_unloadingPlugin = plugin;
			completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
			_unloading = completion.Task;

			if(disposing)
				_disposing = completion;
		}

		_ = this.UnloadCoreAsync(previous, completion, plugin, disposing);
		return new(cancellation.CanBeCanceled ? completion.Task.WaitAsync(cancellation) : completion.Task);
	}

	private async Task UnloadCoreAsync(Task previous, TaskCompletionSource completion, Plugin plugin, bool disposing)
	{
		var invocation = new Invocation();
		var parent = _invocation.Value;
		_invocation.Value = invocation;
		Exception failure = null;

		try
		{
			await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
			var disposal = new PluginDisposal(this.Plugins);
			await this.Loader.UnloadAsync(plugin, disposal, disposing).ConfigureAwait(false);

			if(this.Plugins.Count == 0)
				this.Status = PluginTreeStatus.None;
			disposal.ThrowIfFailed();
		}
		catch(Exception exception)
		{
			failure = exception;
		}
		finally
		{
			if(disposing)
			{
				System.Runtime.Loader.AssemblyLoadContext.Default.Resolving -= this.Default_Resolving;
				_instances.TryRemove(this.Options, out _);
				this.Root.Children.Clear();
				GC.SuppressFinalize(this);
			}

			invocation.Active = false;
			_invocation.Value = parent;
		}

		if(failure == null)
			completion.TrySetResult();
		else
		{
			completion.TrySetException(failure);
			_ = completion.Task.Exception;
		}

	}

	private sealed class Invocation
	{
		public volatile bool Active = true;
	}
}
