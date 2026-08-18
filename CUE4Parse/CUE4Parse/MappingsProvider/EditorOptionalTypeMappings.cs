namespace CUE4Parse.MappingsProvider;

/// <summary>
/// Builds targeted schema views for optional editor-data packages. Shipping
/// mappings omit WITH_EDITORONLY_DATA fields, but .o packages use the editor
/// reflection layout when serializing unversioned properties.
/// </summary>
public static class EditorOptionalTypeMappings
{
    public static Struct Apply(Struct mappings)
    {
        var cache = new Dictionary<Struct, Struct>();
        return Clone(mappings, cache);
    }

    private static Struct Clone(Struct source, Dictionary<Struct, Struct> cache)
    {
        if (cache.TryGetValue(source, out var existing)) return existing;

        Struct result;
        if (source.Name == "EdGraphNode")
        {
            result = BuildEdGraphNode(source);
        }
        else if (source.Name == "EdGraph")
        {
            result = BuildEdGraph(source);
        }
        else if (source.Name == "NiagaraNodeFunctionCall")
        {
            // Three editor-only fields are present in the reflected layout but
            // absent from shipping mappings. They are unset in cooked .o data;
            // preserving their slots realigns the inherited EdGraphNode fields.
            result = new Struct(source.Context, source.Name, source.SuperType,
                new Dictionary<int, PropertyInfo>(source.Properties), source.PropertyCount + 3);
        }
        else if (source.Name == "NiagaraScript")
        {
            result = BuildNiagaraScript(source);
        }
        else if (source.Name == "NiagaraVMExecutableData")
        {
            result = BuildNiagaraVMExecutableData(source);
        }
        else if (source.Name == "NiagaraParameters")
        {
            var properties = new Dictionary<int, PropertyInfo>();
            Add(properties, 0, "Parameters", "ArrayProperty", ReflectedStruct("NiagaraVariable"));
            result = new Struct(source.Context, source.Name, source.SuperType, properties, source.PropertyCount);
        }
        else
        {
            result = new Struct(source.Context, source.Name, source.SuperType,
                new Dictionary<int, PropertyInfo>(source.Properties), source.PropertyCount);
        }

        cache[source] = result;
        result.Super = new Lazy<Struct?>(() => source.Super.Value is { } super ? Clone(super, cache) : null);
        return result;
    }

    private static Struct BuildEdGraphNode(Struct source)
    {
        var properties = new Dictionary<int, PropertyInfo>();

        CopyRange(source, properties, 0, 8, 0);
        Add(properties, 9, "bCanResizeNode", "BoolProperty");
        Copy(source, properties, 9, 10);
        Add(properties, 11, "bUnrelated", "BoolProperty");
        Copy(source, properties, 10, 12);
        Add(properties, 13, "bCommentBubblePinned", "BoolProperty");
        Add(properties, 14, "bCommentBubbleVisible", "BoolProperty");
        Add(properties, 15, "bCommentBubbleMakeVisible", "BoolProperty");
        Add(properties, 16, "bCanRenameNode", "BoolProperty");
        Add(properties, 17, "NodeUpgradeMessage", "TextProperty");
        CopyRange(source, properties, 11, 14, 18);

        return new Struct(source.Context, source.Name, source.SuperType, properties, 22);
    }

    private static Struct BuildEdGraph(Struct source)
    {
        var properties = new Dictionary<int, PropertyInfo>(source.Properties);
        Add(properties, 5, "SubGraphs", "ArrayProperty", new PropertyType("ObjectProperty"));
        Add(properties, 6, "GraphGuid", "StructProperty", structType: "Guid");
        Add(properties, 7, "InterfaceGuid", "StructProperty", structType: "Guid");
        return new Struct(source.Context, source.Name, source.SuperType, properties, 8);
    }

    private static Struct BuildNiagaraScript(Struct source)
    {
        var properties = new Dictionary<int, PropertyInfo>();
        CopyRange(source, properties, 0, 3, 0);
        Add(properties, 4, "LastGeneratedVMId", "StructProperty", structType: "NiagaraVMExecutableDataId");
        CopyRange(source, properties, 4, 5, 5);
        Add(properties, 7, "CachedDefaultDataInterfaces", "ArrayProperty",
            new PropertyType("StructProperty", "NiagaraScriptDataInterfaceInfo"));
        return new Struct(source.Context, source.Name, source.SuperType, properties, 8);
    }

    private static Struct BuildNiagaraVMExecutableData(Struct source)
    {
        var properties = new Dictionary<int, PropertyInfo>();

        CopyRange(source, properties, 0, 2, 0);
        Add(properties, 3, "Parameters", "StructProperty", structType: "NiagaraParameters");
        Add(properties, 4, "InternalParameters", "StructProperty", structType: "NiagaraParameters");
        Add(properties, 5, "ExternalDependencies", "ArrayProperty",
            new PropertyType("StructProperty", "NiagaraCompileDependency"));
        Add(properties, 6, "BakedRapidIterationParameters", "ArrayProperty",
            ReflectedStruct("NiagaraVariable"));
        Add(properties, 7, "CompileTagsEditorOnly", "ArrayProperty",
            new PropertyType("StructProperty", "NiagaraCompilerTag"));
        CopyRange(source, properties, 3, 6, 8);
        Add(properties, 12, "DataSetToParameters", "MapProperty",
            new PropertyType("NameProperty"), valueType: new PropertyType("StructProperty", "NiagaraParameters"));
        Add(properties, 13, "AdditionalExternalFunctions", "ArrayProperty",
            new PropertyType("StructProperty", "NiagaraFunctionSignature"));
        CopyRange(source, properties, 7, 11, 14);
        Add(properties, 19, "LastHlslTranslationGPU", "StrProperty");
        Copy(source, properties, 12, 20);
        Add(properties, 21, "ParameterCollectionPaths", "ArrayProperty", new PropertyType("StrProperty"));
        CopyRange(source, properties, 13, 14, 22);
        Add(properties, 24, "bReadsAttributeData", "BoolProperty");
        Add(properties, 25, "AttributesWritten", "ArrayProperty",
            ReflectedStruct("NiagaraVariableBase"));
        Add(properties, 26, "StaticVariablesWritten", "ArrayProperty",
            ReflectedStruct("NiagaraVariable"));
        Add(properties, 27, "ErrorMsg", "StrProperty");
        Add(properties, 28, "LastCompileEvents", "ArrayProperty",
            new PropertyType("StructProperty", "NiagaraCompileEvent"));
        Copy(source, properties, 15, 29);
        Add(properties, 30, "LastExperimentalAssemblyScript", "StrProperty");
        CopyRange(source, properties, 16, 17, 31);

        return new Struct(source.Context, source.Name, source.SuperType, properties, 33);
    }

    private static void CopyRange(Struct source, Dictionary<int, PropertyInfo> target,
        int sourceStart, int sourceEnd, int targetStart)
    {
        for (var sourceIndex = sourceStart; sourceIndex <= sourceEnd; sourceIndex++)
            Copy(source, target, sourceIndex, targetStart + sourceIndex - sourceStart);
    }

    private static void Copy(Struct source, Dictionary<int, PropertyInfo> target, int sourceIndex, int targetIndex)
    {
        if (!source.Properties.TryGetValue(sourceIndex, out var property)) return;
        var clone = (PropertyInfo) property.Clone();
        clone.Index = targetIndex;
        target[targetIndex] = clone;
    }

    private static void Add(Dictionary<int, PropertyInfo> target, int index, string name, string type,
        PropertyType? innerType = null, string? structType = null, PropertyType? valueType = null)
    {
        var propertyType = new PropertyType(type, structType, innerType) { ValueType = valueType };
        target[index] = new PropertyInfo(index, name, propertyType, 1);
    }

    private static PropertyType ReflectedStruct(string structType) =>
        new("StructProperty", structType) { ForceFallbackStruct = true };
}
