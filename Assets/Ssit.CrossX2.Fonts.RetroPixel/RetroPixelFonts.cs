
// ReSharper disable InconsistentNaming

using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Fonts.RetroPixel;

public static class RetroPixelFonts 
{
    private class SourceClass : IAssetsSource
    {
        public IFilesProvider FilesProvider { get; } = new EmbeddedFilesProvider(typeof(RetroPixelFonts).Assembly, "Ssit.CrossX2.Fonts.RetroPixel.Fonts");
        public string DriveName => "RetroPixel:";
    }
    
    public const string PxPlus_IBM_EGA_8x8 = nameof(PxPlus_IBM_EGA_8x8);
    public const string PxPlus_IBM_EGA_9x14 = nameof(PxPlus_IBM_EGA_9x14);
    public const string PxPlus_Tandy1K_II_200L = nameof(PxPlus_Tandy1K_II_200L);
    public const string PxPlus_ToshibaSat_9x16 = nameof(PxPlus_ToshibaSat_9x16);
    public const string PxPlus_HP_100LX_16x12 = nameof(PxPlus_HP_100LX_16x12);
    public const string PxPlus_AST_PremiumExec = nameof(PxPlus_AST_PremiumExec);

    public static readonly IAssetsSource Source = new SourceClass();
}