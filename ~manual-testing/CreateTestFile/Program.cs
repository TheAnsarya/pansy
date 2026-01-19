using Pansy.Core;

var writer = new PansyWriter {
	Platform = PansyLoader.PLATFORM_NES,
	RomSize = 0x8000,
	RomCrc32 = 0x12345678,
	ProjectName = "Test NES ROM",
	Author = "Test Author",
	ProjectVersion = "1.0"
};

// Add symbols
writer.AddSymbol(0x8000, "Reset");
writer.AddSymbol(0x8003, "NMI_Handler");
writer.AddSymbol(0x8006, "IRQ_Handler");
writer.AddSymbol(0x8010, "Main_Loop");
writer.AddSymbol(0x8050, "Update_Graphics");
writer.AddSymbol(0x8100, "Read_Controller");
writer.AddSymbol(0x8150, "Play_Sound");

// Add comments
writer.AddComment(0x8000, "Entry point from reset vector");
writer.AddComment(0x8010, "Main game loop starts here");
writer.AddComment(0x8050, "Updates PPU graphics registers");

// Mark code/data
writer.MarkAsCode(0x8000);
writer.MarkAsCode(0x8003);
writer.MarkAsCode(0x8006);
writer.MarkAsCode(0x8010);
writer.MarkAsCode(0x8050);
writer.MarkAsCode(0x8100);
writer.MarkAsCode(0x8150);

writer.MarkAsJumpTarget(0x8010);
writer.MarkAsJumpTarget(0x8050);

writer.MarkAsSubroutine(0x8050);
writer.MarkAsSubroutine(0x8100);
writer.MarkAsSubroutine(0x8150);

// Add memory regions
writer.AddMemoryRegion(new MemoryRegion(0x8000, 0xBFFF, 0x01, 0, "PRG-ROM Bank 0"));
writer.AddMemoryRegion(new MemoryRegion(0xC000, 0xFFFF, 0x01, 1, "PRG-ROM Bank 1"));

// Add cross-references
writer.AddCrossReference(new CrossReference(0x8015, 0x8050, CrossRefType.Jsr));
writer.AddCrossReference(new CrossReference(0x8020, 0x8100, CrossRefType.Jsr));
writer.AddCrossReference(new CrossReference(0x8025, 0x8150, CrossRefType.Jsr));
writer.AddCrossReference(new CrossReference(0x8070, 0x8010, CrossRefType.Jmp));

var data = writer.Generate();
var outputPath = Path.Combine(AppContext.BaseDirectory, "test.pansy");
File.WriteAllBytes(outputPath, data);

Console.WriteLine($"Created {outputPath} ({data.Length} bytes)");
