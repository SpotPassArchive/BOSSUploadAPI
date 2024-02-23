namespace BOSSUploadAPI.DbModels;

public enum DumpType {
	CTRPartitionA = 1,
	CTRPartitionB = 2,
	WUP = 3,
}

public class DumpEntry {
	public string Hash { get; set; } // sha256
	public long UploadDate { get; set; } // unix timestamp, UTC, in seconds
	public DumpType Type { get; set; }
}
