using RLBot.Flat;

namespace RLBot.Util;

public static class MediaColorExtensions
{
    public static ColorT ToFlatBuf(this System.Drawing.Color color) =>
        new() { R = color.R, G = color.G, B = color.B, A = color.A };
}
