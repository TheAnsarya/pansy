using Pansy.Core;
using System.Text;
using Xunit;

namespace Pansy.Core.Tests;

public class PansyLoaderTests {
	[Fact]
	public void Load_InvalidMagic_ThrowsException() {
		var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

		Assert.Throws<System.IO.InvalidDataException>(() => new PansyLoader(data));
	}

	[Fact]
	public void Load_TooShort_ThrowsException() {
		var data = new byte[] { (byte)'P', (byte)'A', (byte)'N', (byte)'S', (byte)'Y' };

		Assert.Throws<System.IO.InvalidDataException>(() => new PansyLoader(data));
	}

	[Fact]
	public void PlatformConstants_Defined() {
		// Verify platform constants are accessible
		Assert.Equal(0x01, PansyLoader.PLATFORM_NES);
		Assert.Equal(0x02, PansyLoader.PLATFORM_SNES);
		Assert.Equal(0x03, PansyLoader.PLATFORM_GB);
		Assert.Equal(0x04, PansyLoader.PLATFORM_GBA);
		Assert.Equal(0x05, PansyLoader.PLATFORM_GENESIS);
		Assert.Equal(0x1f, PansyLoader.PLATFORM_CHANNEL_F);
		Assert.Equal(0xff, PansyLoader.PLATFORM_CUSTOM);
	}

	[Fact]
	public void Load_ChannelFPlatform_RoundtripsPlatformId() {
		var writer = new PansyWriter {
			Platform = PansyLoader.PLATFORM_CHANNEL_F,
			RomSize = 0x2000
		};

		var data = writer.Generate();
		var loader = new PansyLoader(data);

		Assert.Equal(PansyLoader.PLATFORM_CHANNEL_F, loader.Platform);
		Assert.Equal("Fairchild Channel F", PansyLoader.GetPlatformName(loader.Platform));
	}
}
