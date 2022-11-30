namespace DotnetInject.Core;

internal static class Extensions
{
    public static void WriteCString(this BinaryWriter bw, string str)
        => bw.Write((str + "\0").ToArray());
}