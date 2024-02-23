using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace BOSSUploadAPI;

public class LoopService : BackgroundService
{
	private List<Func<CancellationToken, Task>> LoopInits { get; init; }

	public LoopService()
	{
		this.LoopInits = new List<Func<CancellationToken, Task>>();
	}

	public LoopService AddAsyncLoop(string serviceName, Func<TimeSpan, string, CancellationToken, Task> func, TimeSpan timeSpan)
	{
		Func<CancellationToken, Task> loop = async (CancellationToken tk) =>
		{
			Serilog.Log.Information("[{serviceName}] Service started", serviceName);

			do
			{
				try
				{
					await func(timeSpan, serviceName, tk);
					await Task.Delay(timeSpan, tk);
				}
				catch (TaskCanceledException) // delay was canceled
				{
					Serilog.Log.Information("[{serviceName}] Canceled", serviceName);
				}
				catch (Exception ex)
				{
					Serilog.Log.Error("[{serviceName}] Error", serviceName);
					ex.WriteToLog();
				}
			} while (!tk.IsCancellationRequested);

			Serilog.Log.Information("[{serviceName}] Stopping", serviceName);
		};

		this.LoopInits.Add(loop);
		Serilog.Log.Information("[{servicename}] Registered with interval of {interval}", serviceName, timeSpan);

		return this;
	}

	public LoopService AddLoop(string serviceName, Action<TimeSpan, string> func, TimeSpan timeSpan)
	{
		Func<CancellationToken, Task> loop = async (CancellationToken tk) =>
		{
			Serilog.Log.Information("[{serviceName}] Service started", serviceName);

			do
			{
				try
				{
					func(timeSpan, serviceName);
					await Task.Delay(timeSpan, tk);
				}
				catch (TaskCanceledException) // delay was canceled
				{
					Serilog.Log.Information("[{serviceName}] Canceled", serviceName);
				}
				catch (Exception ex)
				{
					Serilog.Log.Error("[{serviceName}] Error", serviceName);
					ex.WriteToLog();
				}
			} while (!tk.IsCancellationRequested);

			Serilog.Log.Information("[{serviceName}] Stopping", serviceName);
		};

		this.LoopInits.Add(loop);
		Serilog.Log.Information("[{servicename}] Registered with interval of {interval}", serviceName, timeSpan);

		return this;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		List<Task> tasks = new List<Task>();

		Parallel.ForEach(this.LoopInits, (task, state, i) =>
		{
			tasks.Add(this.LoopInits[(int)i](stoppingToken));
		});

		await Task.WhenAll(tasks);
	}

	public override async Task StopAsync(CancellationToken stoppingToken)
	{
		await base.StopAsync(stoppingToken);
	}
}
