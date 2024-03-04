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
	}
}
