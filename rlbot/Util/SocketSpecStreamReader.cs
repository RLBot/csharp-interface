using System.Net.Sockets;

namespace rlbot.Util;

/**
 * https://wiki.rlbot.org/framework/sockets-specification/
 */
class SocketSpecStreamReader
{
    private BufferedStream _bufferedStream;
    private byte[] _ushortReader = new byte[2];
    private byte[] _payloadReader = new byte[ushort.MaxValue];

    public SocketSpecStreamReader(NetworkStream stream)
    {
        _bufferedStream = new BufferedStream(stream, 4 + ushort.MaxValue);
    }

    internal TypedPayload ReadOne()
    {
        DataType dataType;
        ushort payloadSize;

        _bufferedStream.ReadExactly(_ushortReader);
        dataType = ReadDataType(_ushortReader);

        _bufferedStream.ReadExactly(_ushortReader);
        payloadSize = ReadPayloadSize(_ushortReader);

        _bufferedStream.ReadExactly(_payloadReader, 0, payloadSize);

        return new() { Type = dataType, Payload = new(_payloadReader, 0, payloadSize) };
    }

    internal IEnumerable<TypedPayload> ReadAll()
    {
        while (true)
        {
            TypedPayload payload;

            try
            {
                payload = ReadOne();
            }
            catch (Exception)
            {
                break;
            }

            yield return payload;
        }
    }

    private static DataType ReadDataType(Span<byte> bytes)
    {
        return (DataType)((bytes[0] << 8) | bytes[1]);
    }

    private static ushort ReadPayloadSize(Span<byte> bytes)
    {
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }
}
