using System.Text.Json.Serialization;
using BOSSUploadAPI.DbModels;

namespace BOSSUploadAPI;

public enum UploadResponseType {
	Success = 0,
	AlreadyExists = 1,
}

public class UploadResponse {
	[JsonPropertyName("hash")]
	public string Hash { get; set; }

	[JsonPropertyName("dump_type")]
	public DumpType DumpType { get; set; }

	[JsonPropertyName("upload_result")]
	public UploadResponseType Result { get; set; }

	public UploadResponse(DumpType dumpType, string hash, UploadResponseType result) {
		this.Hash = hash;
		this.DumpType = dumpType;
		this.Result = result;
	}
}
