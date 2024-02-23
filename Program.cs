using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
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
			.AddControllers()
			.AddJsonOptions(x => {
				x.JsonSerializerOptions.WriteIndented = false;
				x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			});

		builder.Host
			.UseSerilog();

		WebApplication app = builder.Build();

		app.MapControllers();

		await app.RunAsync();

		APIConfig.Deinitialize();
		MainController.ProcessLock.Dispose();
		return 0;
	}
}
