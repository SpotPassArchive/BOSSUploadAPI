using Microsoft.EntityFrameworkCore;

namespace BOSSUploadAPI;

public static class BOSSContextFactory
{
	public static BOSSContext CreateBOSSContext()
	{
		DbContextOptions<BOSSContext> opts = new DbContextOptionsBuilder<BOSSContext>()
			.UseSqlite($"Data Source={APIConfig.SqliteDbPath}")
			.UseLoggerFactory(APIConfig.LoggerFactory)
			.Options;
		return new BOSSContext(opts);
	}
}
