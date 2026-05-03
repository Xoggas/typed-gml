using System.Security.Cryptography;
using System.Text;

namespace TypedGML.CLI.GameMaker;

internal static class GuidRegistry
{
    private static readonly Guid Namespace =
        new("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    public static Guid For(string name) => CreateV5(Namespace, name);

    private static Guid CreateV5(Guid ns, string name)
    {
        var namespaceBytes = ToNetworkByteOrder(ns);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var bytes = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, bytes, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, bytes, namespaceBytes.Length, nameBytes.Length);

        var hash = SHA1.HashData(bytes);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return FromNetworkByteOrder(hash[..16]);
    }

    private static byte[] ToNetworkByteOrder(Guid guid)
    {
        var bytes = guid.ToByteArray();
        SwapByteOrder(bytes);
        return bytes;
    }

    private static Guid FromNetworkByteOrder(byte[] bytes)
    {
        SwapByteOrder(bytes);
        return new Guid(bytes);
    }

    private static void SwapByteOrder(byte[] bytes)
    {
        (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
        (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
        (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
        (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
    }
}
