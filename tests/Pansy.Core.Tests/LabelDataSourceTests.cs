using System.IO;
using System.Linq;
using Xunit;

namespace Pansy.Core.Tests;

public class LabelDataSourceTests : IDisposable {
	private readonly string _tempDir;

	public LabelDataSourceTests() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"pansy_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(_tempDir);
	}

	public void Dispose() {
		if (Directory.Exists(_tempDir)) {
			Directory.Delete(_tempDir, true);
		}
	}

	// ========================================================================
	// LabelDataCache - Save/Load
	// ========================================================================

	[Fact]
	public void Save_And_Load_RoundTrips() {
		var cache = new LabelDataCache(_tempDir);
		var db = new GameLabelDatabase {
			Crc32 = 0xdeadbeef,
			Title = "Test Game",
			Platform = PansyLoader.PLATFORM_NES,
			Source = "DataCrystal",
			SourceUrl = "https://datacrystal.romhacking.net/wiki/Test",
			FetchedAt = DateTimeOffset.UtcNow,
			RamLabels = {
				["$0010"] = new LabelDatabaseEntry { Name = "PlayerHP", Description = "Player health", Type = SymbolType.Label },
				["$0020"] = new LabelDatabaseEntry { Name = "PlayerMP", Description = "Player magic", Type = SymbolType.Label },
			},
			RomLabels = {
				["$C000"] = new LabelDatabaseEntry { Name = "MainLoop", Description = "Main game loop", Type = SymbolType.Function },
			},
		};

		cache.Save(db);
		var loaded = cache.Load(0xdeadbeef);

		Assert.NotNull(loaded);
		Assert.Equal("Test Game", loaded.Title);
		Assert.Equal(PansyLoader.PLATFORM_NES, loaded.Platform);
		Assert.Equal("DataCrystal", loaded.Source);
		Assert.Equal(2, loaded.RamLabels.Count);
		Assert.Equal("PlayerHP", loaded.RamLabels["$0010"].Name);
		Assert.Single(loaded.RomLabels);
	}

	[Fact]
	public void Load_NonExistent_ReturnsNull() {
		var cache = new LabelDataCache(_tempDir);
		Assert.Null(cache.Load(0x12345678));
	}

	[Fact]
	public void Exists_SavedEntry_ReturnsTrue() {
		var cache = new LabelDataCache(_tempDir);
		cache.Save(new GameLabelDatabase { Crc32 = 0xaabbccdd });

		Assert.True(cache.Exists(0xaabbccdd));
		Assert.False(cache.Exists(0x11223344));
	}

	[Fact]
	public void IsStale_FreshEntry_ReturnsFalse() {
		var cache = new LabelDataCache(_tempDir);
		cache.Save(new GameLabelDatabase {
			Crc32 = 0x01020304,
			FetchedAt = DateTimeOffset.UtcNow,
		});

		Assert.False(cache.IsStale(0x01020304, TimeSpan.FromDays(7)));
	}

	[Fact]
	public void IsStale_OldEntry_ReturnsTrue() {
		var cache = new LabelDataCache(_tempDir);
		cache.Save(new GameLabelDatabase {
			Crc32 = 0x01020304,
			FetchedAt = DateTimeOffset.UtcNow.AddDays(-30),
		});

		Assert.True(cache.IsStale(0x01020304, TimeSpan.FromDays(7)));
	}

	[Fact]
	public void IsStale_NonExistent_ReturnsTrue() {
		var cache = new LabelDataCache(_tempDir);
		Assert.True(cache.IsStale(0x99999999, TimeSpan.FromDays(7)));
	}

	[Fact]
	public void ListCached_ReturnsAllSaved() {
		var cache = new LabelDataCache(_tempDir);
		cache.Save(new GameLabelDatabase { Crc32 = 0x11111111 });
		cache.Save(new GameLabelDatabase { Crc32 = 0x22222222 });
		cache.Save(new GameLabelDatabase { Crc32 = 0x33333333 });

		var list = cache.ListCached().ToList();

		Assert.Equal(3, list.Count);
		Assert.Contains(0x11111111u, list);
		Assert.Contains(0x22222222u, list);
		Assert.Contains(0x33333333u, list);
	}

	[Fact]
	public void ListCached_EmptyDir_ReturnsEmpty() {
		var emptyDir = Path.Combine(_tempDir, "empty");
		var cache = new LabelDataCache(emptyDir);
		Assert.Empty(cache.ListCached());
	}

	// ========================================================================
	// ToMergeLabels
	// ========================================================================

	[Fact]
	public void ToMergeLabels_ConvertsAllEntries() {
		var db = new GameLabelDatabase {
			RamLabels = {
				["$0010"] = new LabelDatabaseEntry { Name = "PlayerHP", Type = SymbolType.Label, Description = "Health" },
			},
			RomLabels = {
				["$C000"] = new LabelDatabaseEntry { Name = "MainLoop", Type = SymbolType.Function },
			},
		};

		var labels = LabelDataCache.ToMergeLabels(db);

		Assert.Equal(2, labels.Count);
		Assert.Contains(labels, l => l.Address == 0x0010 && l.Name == "PlayerHP" && l.Description == "Health");
		Assert.Contains(labels, l => l.Address == 0xc000 && l.Name == "MainLoop");
	}

	[Fact]
	public void ToMergeLabels_ParsesHexFormats() {
		var db = new GameLabelDatabase {
			RamLabels = {
				["$FF"] = new LabelDatabaseEntry { Name = "Dollar" },
				["0xFF"] = new LabelDatabaseEntry { Name = "Prefix0x" },
				["FF"] = new LabelDatabaseEntry { Name = "Plain" },
			},
		};

		var labels = LabelDataCache.ToMergeLabels(db);

		Assert.Equal(3, labels.Count);
		Assert.All(labels, l => Assert.Equal(0xffu, l.Address));
	}

	[Fact]
	public void ToMergeLabels_IntegrationWithMergeEngine() {
		var db = new GameLabelDatabase {
			RamLabels = {
				["$0010"] = new LabelDatabaseEntry { Name = "PlayerHP", Type = SymbolType.Label, Description = "HP" },
			},
		};

		var labels = LabelDataCache.ToMergeLabels(db);
		var engine = new LabelMergeEngine();
		engine.AddDatabaseLabels(labels);

		Assert.Equal("PlayerHP", engine.Labels[0x10].Name);
		Assert.Equal(LabelSource.InternetDatabase, engine.Labels[0x10].Source);
	}

	// ========================================================================
	// Cache File Naming
	// ========================================================================

	[Fact]
	public void CacheFile_NamedByLowercaseHex() {
		var cache = new LabelDataCache(_tempDir);
		cache.Save(new GameLabelDatabase { Crc32 = 0xaabbccdd });

		Assert.True(File.Exists(Path.Combine(_tempDir, "aabbccdd.json")));
	}
}
