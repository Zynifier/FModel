using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(StructPropertyConverter))]
public class StructProperty : FPropertyTagType<FScriptStruct>
{
    public StructProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
    {
        Value = tagData?.ForceFallbackStruct == true
            ? new FScriptStruct(tagData.Struct != null
                ? new FStructFallback(Ar, tagData.Struct)
                : new FStructFallback(Ar, tagData.StructType!))
            : new FScriptStruct(Ar, tagData?.StructType, tagData?.Struct, type);
    }

    public StructProperty(FScriptStruct value) => Value = value;

    public override string ToString() => Value is null
        ? "(null struct)"
        : Value.ToString().SubstringBeforeLast(')') + ", StructProperty)";
}
