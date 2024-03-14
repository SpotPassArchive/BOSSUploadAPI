using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using BOSSUploadAPI.Controllers;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using System;

namespace BOSSUploadAPI;

public static class Program {
	public static async Task<int> Main(string[] args) {
		try {
			APIConfig.Initialize(Environment.CurrentDirectory, "config.json");

			MainController.ProcessLock = new SemaphoreSlim(APIConfig.MaxConcurrentUploads);
			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
			
			builder.Services
				.AddCors(x => x.AddDefaultPolicy(y => y.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()))
				.AddControllers()
				.AddJsonOptions(x => {
					x.JsonSerializerOptions.WriteIndented = false;
					x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
				});

			builder.Host
				.UseSerilog();

			builder.WebHost
				.UseUrls($"http://0.0.0.0:{APIConfig.HostPort}");

			WebApplication app = builder.Build();

			app.UseCors();
			app.MapControllers();

			await app.RunAsync();

			APIConfig.Deinitialize();
			MainController.ProcessLock.Dispose();
			return 0;
		} catch (Exception ex) {
			Console.WriteLine("Application failed with the following exception:");
			Console.WriteLine($"Exception Message: {ex.Message}");
			Console.WriteLine("Stack Trace:");
			Console.WriteLine(ex.StackTrace);
			return 1;
		}
	}
}
