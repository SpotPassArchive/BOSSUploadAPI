using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using Serilog;
using System;

namespace BOSSUploadAPI;

public static class APIConfig
{
	public static string AppBasePath { get; private set; }
	public static ILoggerFactory LoggerFactory { get; private set; }
	// config stuff for runtime
	public static string DumpDirectory { get; private set; }
	public static string SqliteDbPath { get; private set; }
	public static string CTRProcessor { get; private set; }
	public static long MaxUploadSize { get; private set; }
	public static int HostPort { get; private set; }
	public static int MaxConcurrentUploads { get; private set; }

	public static void Initialize(string basePath, string configFileName)
	{
		AppBasePath = basePath;
		Log.Logger = new LoggerConfiguration()
			.Enrich
				.FromLogContext()
			.WriteTo
				.File(Path.Join(AppBasePath, "log", "log_.log"), rollingInterval: RollingInterval.Day, shared: true, retainedFileTimeLimit: TimeSpan.FromDays(7))
			.WriteTo
				.Console()
			.CreateLogger();

		LoggerFactory = new LoggerFactory()
			.AddSerilog();

		IConfiguration cfg = new ConfigurationBuilder()
			.SetBasePath(basePath)
			.AddJsonFile(configFileName)
			.Build();

		DumpDirectory = cfg.GetValue<string>("dump_dir");
		SqliteDbPath = cfg.GetValue<string>("db_file");
		CTRProcessor = cfg.GetValue<string>("ctr_processor");
		MaxUploadSize = cfg.GetValue<long>("max_ul_size");
		HostPort = cfg.GetValue<int>("port");
		MaxConcurrentUploads = cfg.GetValue<int>("max_concurrent_uls");

		if (string.IsNullOrWhiteSpace(DumpDirectory) || string.IsNullOrWhiteSpace(SqliteDbPath) || string.IsNullOrWhiteSpace(CTRProcessor))
			throw new ArgumentException("Dump directory, database path or CTR processor executable name were not provided in configuration.");
		else if (!File.Exists(SqliteDbPath))
			throw new FileNotFoundException("Could not find the sqlite3 database file at the provided path in the configuration.", SqliteDbPath);
		else if (MaxUploadSize <= 0)
			throw new ArgumentException("Invalid max upload size in configuration.");

		SqliteDbPath = new FileInfo(SqliteDbPath).FullName;

		if (!Directory.Exists(DumpDirectory))
			Directory.CreateDirectory(DumpDirectory);
	}

	public static void Deinitialize()
	{
		Log.CloseAndFlush();
		LoggerFactory.Dispose();
	}
}
