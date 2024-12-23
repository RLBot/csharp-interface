using Microsoft.Extensions.Logging;

namespace RLBot.Util;

/**
 * Communicates with bots and scripts over TCP according to the specification
 * defined at https://wiki.rlbot.org/framework/sockets-specification/
 */
internal class SocketSpecStreamWriter(Stream stream)
{
    private static readonly ILogger Logger = Logging.GetLogger("SocketSpecStreamWriter");

    private readonly byte[] _dataTypeBuffer = new byte[2];
    private readonly byte[] _messageLengthBuffer = new byte[2];
    private readonly byte[] _messageBuffer = new byte[4 + ushort.MaxValue];

    private void PrepareMessageLength(ushort length) =>
        WriteBigEndian(length, _messageLengthBuffer);

    private void PrepareDataType(DataType dataType) =>
        WriteBigEndian((ushort)dataType, _dataTypeBuffer);

    private static void WriteBigEndian(ushort value, byte[] buffer)
    {
        buffer[0] = (byte)((value >> 8) & 0xFF);
        buffer[1] = (byte)(value & 0xFF);
    }

    internal void Write(TypedPayload message)
    {
        if (message.Payload.Count > ushort.MaxValue)
        {
            // Can't send if the message size is bigger than our header can describe.
            Logger.LogError(
                $"Cannot send message because size of {message.Payload.Count} cannot be described by a ushort."
            );
            return;
        }

        if (message.Payload.Array == null)
        {
            Logger.LogWarning("Cannot payload was null.");
            return;
        }

        PrepareDataType(message.Type);
        PrepareMessageLength((ushort)message.Payload.Count);

        Array.Copy(_dataTypeBuffer, 0, _messageBuffer, 0, 2);
        Array.Copy(_messageLengthBuffer, 0, _messageBuffer, 2, 2);
        Array.Copy(
            message.Payload.Array,
            message.Payload.Offset,
            _messageBuffer,
            4,
            message.Payload.Count
        );

        stream.Write(_messageBuffer, 0, 4 + message.Payload.Count);
    }

    internal void Send() => stream.Flush();
}
