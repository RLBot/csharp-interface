using Google.FlatBuffers;

namespace RLBot.Util;

public class TypedPayload
{
    public DataType Type;
    public ArraySegment<byte> Payload;

    public static TypedPayload FromFlatBufferBuilder(
        DataType type,
        FlatBufferBuilder builder
    ) =>
        new TypedPayload
        {
            Type = type,
            Payload = builder.DataBuffer.ToArraySegment(
                builder.DataBuffer.Position,
                builder.DataBuffer.Length - builder.DataBuffer.Position
            ),
        };
}
