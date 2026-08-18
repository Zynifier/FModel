using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.EdGraph;

public class UEdGraphNode : UObject
{
    public UEdGraphPinReference?[] Pins = [];
    public int PinSerializationVersion;
    public long PinDataOffset;
    public long PinDataEndOffset;
    public long ExportEndOffset;
    public bool AttemptedPinDeserialization;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        PinSerializationVersion = (int) FBlueprintsObjectVersion.Get(Ar);
        PinDataOffset = Ar.Position;
        ExportEndOffset = validPos;
        // UEdGraphNode::Serialize always serializes the optimized pin array in
        // editor builds. PKG_FilterEditorOnly only removes selected fields from
        // each pin (handled by UEdGraphPin), not the owning node's pin array.
        if (PinSerializationVersion >= (int) FBlueprintsObjectVersion.Type.EdGraphPinOptimized)
        {
            AttemptedPinDeserialization = true;
            UEdGraphPin.SerializeAsOwningNode(Ar, ref Pins);
        }
        PinDataEndOffset = Ar.Position;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Pins));
        serializer.Serialize(writer, Pins);

        writer.WritePropertyName("PinParseDiagnostics");
        serializer.Serialize(writer, new
        {
            PinSerializationVersion,
            PinDataOffset,
            PinDataEndOffset,
            ExportEndOffset,
            AttemptedPinDeserialization,
            BytesConsumed = PinDataEndOffset - PinDataOffset,
            BytesRemaining = ExportEndOffset - PinDataEndOffset
        });
    }
}
