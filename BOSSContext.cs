using Microsoft.EntityFrameworkCore;
using BOSSUploadAPI.DbModels;

namespace BOSSUploadAPI;

public class BOSSContext : DbContext
{
	public BOSSContext(DbContextOptions<BOSSContext> options) : base(options)
	{ }

	public DbSet<DumpEntry> DumpEntries { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<DumpEntry>(x => {
			x.ToTable("dump_entry");
			x.Property(y => y.Hash).HasColumnName("hash");
			x.Property(y => y.UploadDate).HasColumnName("upload_date");
			x.Property(y => y.Type)
				.HasConversion<int>()
				.HasColumnName("type");
			x.HasKey(x => x.Hash);
		});
	}
}
