using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using BOSSUploadAPI.DbModels;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Linq;
using System.IO;
using System;

namespace BOSSUploadAPI.Controllers;

file static class Validators {
	public static bool CTR(Stream s) {
		if (s.Length < 4153344) return false;
		using BinaryReader br = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);
		br.BaseStream.Seek(0, SeekOrigin.Begin);
		bool magicMatches = br.ReadUInt32() == (BitConverter.IsLittleEndian ? 0x45564153 : 0x53415645);
		s.Seek(0, SeekOrigin.Begin);
		return magicMatches;
	}

	public static bool WUP(Stream s) {
		if (s.Length < 0x104) return false; // check that we have a header at least
		s.Seek(0x4, SeekOrigin.Begin);
		byte[] indicesTable = new byte[256];
		s.Read(indicesTable);
		int minSize = 0x104;
		for (int i = 255; i >= 0; i--) { /* iterate in reverse in case the slot data is not sequential */
			if ((indicesTable[i] & 0x80) == 0x80) { /* enabled slot */
				minSize += (i + 1) * 0x1000;
				break;
			}
		}
		return s.Length >= minSize;
	}
}

[ApiController]
[Route("/")]
public class MainController : ControllerBase
{
	public static SemaphoreSlim ProcessLock;

	private async Task<MemoryStream> DownloadDataToMemoryAsync() {
		byte[] tempBuf = new byte[(int)this.Request.ContentLength!];
		MemoryStream ms = new MemoryStream(tempBuf);

		try {
			await this.Request.Body.CopyToAsync(ms);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		} catch {
			await ms.DisposeAsync();
			throw;
		}
	}

	public async Task<IActionResult> UploadAsync(DumpType type, Func<Stream, bool> validator, int directSizeLimit = 0) {
		if (!await ProcessLock.WaitAsync(5000)) return StatusCode(503, "The server is overloaded. Please try again later.");
	
		try {
			long? len = this.Request.ContentLength;
			if (len is null || len == 0 || len > APIConfig.MaxUploadSize || (directSizeLimit != 0 && len < directSizeLimit))
				return StatusCode(400, "Invalid dump file");

			using MemoryStream tmpStream = await DownloadDataToMemoryAsync();
			if (!validator(tmpStream)) return StatusCode(400, "Invalid dump file");
			string hash = SHA256.HashData(tmpStream.ToArray()).ToHexString();

			await using (BOSSContext ctx = BOSSContextFactory.CreateBOSSContext()) {
				// dupe check
				DumpEntry existing = await ctx.DumpEntries
					.AsNoTracking()
					.Where(x => x.Hash == hash)
					.FirstOrDefaultAsync();

				if (existing is not null)
					return existing.Type == type ?
						StatusCode(200, new UploadResponse(type, hash, UploadResponseType.AlreadyExists)) :
						StatusCode(400, "Cannot upload the same file using different endpoints");

				// upload the file
				(string DirName, string Extension) dumpUpload = type switch {
					DumpType.CTRPartitionA => ("ctr_partition_a", "bin"),
					DumpType.CTRPartitionB => ("ctr_partition_b", "bin"),
					DumpType.WUP => ("wup", "db"),
					_ => throw new ArgumentException("Invalid dump type.")
				};
				string uploadDir = Path.Join(APIConfig.DumpDirectory, dumpUpload.DirName);
				if (!Directory.Exists(uploadDir))
					Directory.CreateDirectory(uploadDir);

				string uploadPath = Path.Join(uploadDir, $"{hash}.{dumpUpload.Extension}");
				tmpStream.Seek(0, SeekOrigin.Begin);
				
				await using (FileStream uploadFile = System.IO.File.OpenWrite(uploadPath))
					await tmpStream.CopyToAsync(uploadFile);

				// add the db entry
				DumpEntry entry = new DumpEntry { Type = type, Hash = hash, UploadDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
				ctx.DumpEntries.Add(entry);
				await ctx.SaveChangesAsync();

				return StatusCode(200, new UploadResponse(type, hash, UploadResponseType.Success));
			}
		}
		catch (Exception e) {
			e.WriteToLog();
			return StatusCode(500, "Failed processing upload. If this occurs repeatedly, please report this error.");
		} finally {
			ProcessLock.Release();
		}
	}

	public async Task<IActionResult> GetStatsAsync(params DumpType[] statTypes)
	{
		try {
			await using BOSSContext ctx = BOSSContextFactory.CreateBOSSContext();
			long dumpCount = await ctx.DumpEntries
				.Where(x => statTypes.Contains(x.Type))
				.LongCountAsync();
			return StatusCode(200, dumpCount);
			
		} catch (Exception e) {
			e.WriteToLog();
			return StatusCode(500, "Failed processing request");
		}
	}
	[HttpPost("upload/ctr/partition-a")]
	[DisableRequestSizeLimit]
	[DisableFormValueModelBinding]
	public async Task<IActionResult> CTRUploadPartitionAAsync() =>
		await UploadAsync(DumpType.CTRPartitionA, Validators.CTR, 4153344);

	[HttpPost("upload/ctr/partition-b")]
	[DisableRequestSizeLimit]
	[DisableFormValueModelBinding]
	public async Task<IActionResult> CTRUploadPartitionBAsync() =>
		await UploadAsync(DumpType.CTRPartitionB, Validators.CTR, 4153344);

	[HttpPost("upload/wup")]
	[DisableRequestSizeLimit]
	[DisableFormValueModelBinding]
	public async Task<IActionResult> WUPUploadAsync() =>
		await UploadAsync(DumpType.WUP, Validators.WUP);

	[HttpGet("stats/ctr")]
	public async Task<IActionResult> CTRGetStatsAsync() =>
		await GetStatsAsync(DumpType.CTRPartitionA, DumpType.CTRPartitionB);

	[HttpGet("stats/wup")]
	public async Task<IActionResult> WUPGetStatsAsync() =>
		await GetStatsAsync(DumpType.WUP);
}
