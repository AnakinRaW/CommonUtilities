using AnakinRaW.CommonUtilities.Collections;
using AnakinRaW.CommonUtilities.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Json;

public enum JsonTestEnum
{
    First,
    Second,
    Third
}

public sealed record JsonTestEntity
{
    public string Name { get; init; } = string.Empty;
    public int Number { get; init; }
}

public sealed class ValueListDictionaryJsonConverterStringIntTest : ValueListDictionaryJsonConverterTestBase<string, int>
{
    protected override string CreateKey(int seed) => "key" + seed;

    protected override int CreateValue(int seed) => seed;

    [Fact]
    public void Read_StringKeys_ReproducesDictionary()
    {
        const string json =
            """
            {
              "a": [1, 2, 3],
              "b": [4]
            }
            """;

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<string, int>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal([1, 2, 3], result.GetValues("a"));
        Assert.Equal([4], result.GetValues("b"));
    }

    [Fact]
    public void Read_DroppingComparer_UsesDefaultComparerOnRoundTrip()
    {
        var mutable = new ValueListDictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "Key", 1 } };
        var original = new ReadOnlyValueListDictionary<string, int>(mutable);

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<string, int>>(json, Options);

        Assert.NotNull(result);
        // The original matched case-insensitively; the deserialized one uses the default comparer.
        Assert.Equal([1], result.GetValues("Key"));
        Assert.False(result.ContainsKey("key"));
    }
}

public sealed class ValueListDictionaryJsonConverterIntStringTest : ValueListDictionaryJsonConverterTestBase<int, string>
{
    protected override int CreateKey(int seed) => seed + 1;

    protected override string CreateValue(int seed) => "v" + seed;

    [Fact]
    public void Read_IntKeys_ParsesPropertyNames()
    {
        const string json = """{ "1": ["x"], "2": ["y", "z"] }""";

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<int, string>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(["x"], result.GetValues(1));
        Assert.Equal(["y", "z"], result.GetValues(2));
    }
}

public sealed class ValueListDictionaryJsonConverterGuidStringTest : ValueListDictionaryJsonConverterTestBase<Guid, string>
{
    protected override Guid CreateKey(int seed) => new(seed, 0, 0, new byte[8]);

    protected override string CreateValue(int seed) => "v" + seed;

    [Fact]
    public void Read_GuidKeys_ParsesPropertyNames()
    {
        var key = Guid.NewGuid();
        var json = $$"""{ "{{key}}": ["x"] }""";

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<Guid, string>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(["x"], result.GetValues(key));
    }

    [Fact]
    public void Read_InvalidGuidKey_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ReadOnlyValueListDictionary<Guid, string>>("""{ "not-a-guid": ["x"] }""", Options));
    }
}

public sealed class ValueListDictionaryJsonConverterEnumIntTest : ValueListDictionaryJsonConverterTestBase<JsonTestEnum, int>
{
    protected override JsonTestEnum CreateKey(int seed) => (JsonTestEnum)seed;

    protected override int CreateValue(int seed) => seed;

    [Fact]
    public void Read_EnumKeyMatchingName_ReproducesDictionary()
    {
        const string json = """{ "First": [1], "Second": [2, 3] }""";

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<JsonTestEnum, int>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal([1], result.GetValues(JsonTestEnum.First));
        Assert.Equal([2, 3], result.GetValues(JsonTestEnum.Second));
    }

    [Fact]
    public void Read_EnumKeyDifferentCasing_IsCaseInsensitive()
    {
        // Enum key names are matched case-insensitively on read, like the built-in enum-key converter.
        const string json = """{ "first": [1] }""";

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<JsonTestEnum, int>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal([1], result.GetValues(JsonTestEnum.First));
    }

    [Fact]
    public void Write_EnumKey_UsesCanonicalName()
    {
        var dictionary = new ValueListDictionary<JsonTestEnum, int> { { JsonTestEnum.First, 1 } };

        var json = JsonSerializer.Serialize(dictionary, Options);

        Assert.Equal("""{"First":[1]}""", json);
    }

    [Fact]
    public void Read_UnknownEnumKey_Throws()
    {
        // A key that is not a valid enum name is rejected.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ReadOnlyValueListDictionary<JsonTestEnum, int>>("""{ "BAD": [2] }""", Options));
    }
}

public sealed class ValueListDictionaryJsonConverterBoolIntTest : ValueListDictionaryJsonConverterTestBase<bool, int>
{
    protected override bool CreateKey(int seed) => seed != 0;

    protected override int CreateValue(int seed) => seed;
}

public sealed class ValueListDictionaryJsonConverterDoubleIntTest : ValueListDictionaryJsonConverterTestBase<double, int>
{
    protected override double CreateKey(int seed) => seed + 0.5;

    protected override int CreateValue(int seed) => seed;
}

public sealed class ValueListDictionaryJsonConverterStringEntityTest : ValueListDictionaryJsonConverterTestBase<string, JsonTestEntity>
{
    protected override string CreateKey(int seed) => "key" + seed;

    protected override JsonTestEntity CreateValue(int seed) => new() { Name = "n" + seed, Number = seed };

    [Fact]
    public void RoundTrip_ComplexValues_PreservesNestedContent()
    {
        // Values can be complex objects, not just primitives.
        var original = new ValueListDictionary<string, JsonTestEntity>
        {
            { "a", new JsonTestEntity { Name = "one", Number = 1 } },
            { "a", new JsonTestEntity { Name = "two", Number = 2 } },
        };

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<string, JsonTestEntity>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(
            [new JsonTestEntity { Name = "one", Number = 1 }, new JsonTestEntity { Name = "two", Number = 2 }],
            result.GetValues("a"));
    }
}

public abstract class ValueListDictionaryJsonConverterTestBase<TKey, TValue>
    where TKey : notnull
{
    protected static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add((JsonConverter)Activator.CreateInstance(typeof(ValueListDictionaryJsonConverter))!);
        return options;
    }

    protected abstract TKey CreateKey(int seed);

    protected abstract TValue CreateValue(int seed);

    // Canonical content shared by every case: key0 -> [v0, v1], key1 -> [v2].
    private IReadOnlyList<KeyValuePair<TKey, TValue[]>> ExpectedEntries()
    {
        return
        [
            new KeyValuePair<TKey, TValue[]>(CreateKey(0), [CreateValue(0), CreateValue(1)]),
            new KeyValuePair<TKey, TValue[]>(CreateKey(1), [CreateValue(2)]),
        ];
    }

    public static IEnumerable<object[]> Variants()
    {
        yield return [typeof(ValueListDictionary<TKey, TValue>)];
        yield return [typeof(FrugalValueListDictionary<TKey, TValue>)];
        yield return [typeof(ReadOnlyValueListDictionary<TKey, TValue>)];
        yield return [typeof(ReadOnlyFrugalValueListDictionary<TKey, TValue>)];
        yield return [typeof(IValueListDictionary<TKey, TValue>)];
        yield return [typeof(IReadOnlyValueListDictionary<TKey, TValue>)];
        yield return [typeof(IFrugalValueListDictionary<TKey, TValue>)];
        yield return [typeof(IReadOnlyFrugalValueListDictionary<TKey, TValue>)];
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void Serialize_AnyInputType_ProducesSameJsonAsRuntimeDictionaryOfLists(Type declaredType)
    {
        var dictionary = CreateVariant(declaredType);

        var runtimeEquivalent = new Dictionary<TKey, List<TValue>>();
        foreach (var entry in ExpectedEntries())
            runtimeEquivalent[entry.Key] = [.. entry.Value];

        var actual = JsonSerializer.Serialize(dictionary, declaredType, Options);
        var expected = JsonSerializer.Serialize(runtimeEquivalent, Options);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void Deserialize_AnyOutputType_ReproducesContent(Type targetType)
    {
        var json = JsonSerializer.Serialize(
            CreateVariant(typeof(ValueListDictionary<TKey, TValue>)),
            typeof(ValueListDictionary<TKey, TValue>),
            Options);

        var result = (IReadOnlyValueListDictionary<TKey, TValue>?)
            JsonSerializer.Deserialize(json, targetType, Options);

        Assert.NotNull(result);
        foreach (var entry in ExpectedEntries())
            Assert.Equal(entry.Value, result.GetValues(entry.Key));
    }

    [Fact]
    public void RoundTrip_PreservesContent()
    {
        var original = CreateVariant(typeof(ReadOnlyValueListDictionary<TKey, TValue>));

        var json = JsonSerializer.Serialize(original, typeof(ReadOnlyValueListDictionary<TKey, TValue>), Options);
        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>(json, Options);

        Assert.NotNull(result);
        foreach (var entry in ExpectedEntries())
            Assert.Equal(entry.Value, result.GetValues(entry.Key));
    }

    [Fact]
    public void Read_Null_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>("null", Options);
        Assert.Null(result);
    }

    [Fact]
    public void Read_EmptyObject_ReturnsEmptyDictionary()
    {
        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>("{}", Options);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void Read_EmptyValueList_DropsKey()
    {
        // A key whose JSON array is empty cannot be represented (every key holds at least one value),
        // so it is dropped rather than retained with an empty list.
        var json = JsonSerializer.Serialize(new Dictionary<TKey, List<TValue>> { [CreateKey(0)] = [] }, Options);

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.False(result.ContainsKey(CreateKey(0)));
    }

    [Fact]
    public void Read_NullValueList_DropsKey()
    {
        // An explicit null array is read leniently as "no values", so the key is likewise dropped.
        var json = JsonSerializer.Serialize(new Dictionary<TKey, List<TValue>?> { [CreateKey(0)] = null }, Options);

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.False(result.ContainsKey(CreateKey(0)));
    }

    [Fact]
    public void Read_DuplicateKeys_LastValueWins()
    {
        // A repeated property name is malformed for this format (our own writer never emits one), but the
        // last occurrence wins, matching the built-in dictionary converter. Build the document by stitching
        // two single-entry objects so the key is formatted correctly for every key type.
        var first = JsonSerializer.Serialize(
            new Dictionary<TKey, List<TValue>> { [CreateKey(0)] = [CreateValue(0), CreateValue(1)] }, Options);
        var second = JsonSerializer.Serialize(
            new Dictionary<TKey, List<TValue>> { [CreateKey(0)] = [CreateValue(2)] }, Options);
        var duplicated = first[..^1] + "," + second[1..];

        var result = JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>(duplicated, Options);

        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.Equal([CreateValue(2)], result.GetValues(CreateKey(0)));
    }

    [Fact]
    public void Read_RootIsNotObject_Throws()
    {
        // The runtime's dictionary converters require a JSON object; an array root must fail.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>("[]", Options));
    }

    [Fact]
    public void Read_ValueIsNotAnArray_Throws()
    {
        // A well-formed object whose value is a scalar instead of the expected array. Building it
        // through the built-in dictionary converter yields a correctly formatted (culture-invariant)
        // property name for every key type.
        var malformed = JsonSerializer.Serialize(new Dictionary<TKey, int> { [CreateKey(0)] = 5 }, Options);

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ReadOnlyValueListDictionary<TKey, TValue>>(malformed, Options));
    }

    private object CreateVariant(Type declaredType)
    {
        var mutable = new ValueListDictionary<TKey, TValue>();
        var frugal = new FrugalValueListDictionary<TKey, TValue>();
        foreach (var entry in ExpectedEntries())
        {
            foreach (var value in entry.Value)
            {
                mutable.Add(entry.Key, value);
                frugal.Add(entry.Key, value);
            }
        }

        if (declaredType == typeof(FrugalValueListDictionary<TKey, TValue>)
            || declaredType == typeof(IFrugalValueListDictionary<TKey, TValue>))
            return frugal;
        if (declaredType == typeof(ReadOnlyValueListDictionary<TKey, TValue>)
            || declaredType == typeof(IReadOnlyValueListDictionary<TKey, TValue>))
            return new ReadOnlyValueListDictionary<TKey, TValue>(mutable);
        if (declaredType == typeof(ReadOnlyFrugalValueListDictionary<TKey, TValue>)
            || declaredType == typeof(IReadOnlyFrugalValueListDictionary<TKey, TValue>))
            return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(frugal);

        // ValueListDictionary<,> and IValueListDictionary<,>.
        return mutable;
    }
}

public sealed class JsonHolder
{
    public int Id { get; set; }

    public ValueListDictionary<string, int> Map { get; set; } = new();

    public string Name { get; set; } = string.Empty;
}

// Scenarios that must hold however the converter is wired up. Each subclass plugs in one wiring (registered
// in options, or applied via a [JsonConverter] attribute) and inherits all the tests below.
public abstract class ValueListDictionaryJsonConverterScenarioTestBase
{
    protected abstract JsonSerializerOptions Options { get; }

    protected abstract JsonSerializerOptions CamelCaseOptions { get; }

    // Serializes a map using the wiring under test.
    protected abstract string SerializeMap(ValueListDictionary<string, int> map, JsonSerializerOptions options);

    // Serializes the same data as a plain Dictionary, in the same shape, as the expected baseline.
    protected abstract string SerializeMapViaRuntime(Dictionary<string, List<int>> map, JsonSerializerOptions options);

    // Serializes and reads back a map held as an object member; returns the member plus its siblings.
    protected abstract (int Id, string Name, ValueListDictionary<string, int> Map) RoundTripMember(
        int id, string name, ValueListDictionary<string, int> map);

    // Serializes and reads back a map whose values are themselves lists.
    protected abstract ValueListDictionary<string, List<int>> RoundTripNested(ValueListDictionary<string, List<int>> map);

    [Fact]
    public void DictionaryKeyPolicy_CamelCase_MatchesRuntimeDictionary()
    {
        var map = new ValueListDictionary<string, int> { { "MyKey", 1 }, { "OtherKey", 2 } };
        var runtimeEquivalent = new Dictionary<string, List<int>> { ["MyKey"] = [1], ["OtherKey"] = [2] };

        var actual = SerializeMap(map, CamelCaseOptions);
        var expected = SerializeMapViaRuntime(runtimeEquivalent, CamelCaseOptions);

        Assert.Equal(expected, actual);
        Assert.Contains("\"myKey\":[1]", actual);
        Assert.Contains("\"otherKey\":[2]", actual);
    }

    [Fact]
    public void Dictionary_AsClassMember_RoundTrips()
    {
        // The dictionary works as a property of a larger object, not only at the document root.
        var map = new ValueListDictionary<string, int>
        {
            { "a", 1 },
            { "a", 2 },
            { "b", 3 }
        };

        var (id, name, result) = RoundTripMember(7, "holder", map);

        Assert.Equal(7, id);
        Assert.Equal("holder", name);
        Assert.Equal([1, 2], result.GetValues("a"));
        Assert.Equal([3], result.GetValues("b"));
    }

    [Fact]
    public void RoundTrip_NestedCollectionValues_PreservesContent()
    {
        // The value type is itself a collection, producing a JSON object of arrays of arrays.
        var map = new ValueListDictionary<string, List<int>>
        {
            { "a", [1, 2] },
            { "a", [3] },
            { "b", [4, 5] },
        };

        var result = RoundTripNested(map);

        Assert.Equal(2, result.GetValues("a").Count);
        Assert.Equal([1, 2], result.GetValues("a")[0]);
        Assert.Equal([3], result.GetValues("a")[1]);
        Assert.Equal([4, 5], result.GetValues("b")[0]);
    }
}

// Wiring 1: the converter is registered in JsonSerializerOptions.Converters.
public sealed class ValueListDictionaryJsonConverterOptionsScenarioTest : ValueListDictionaryJsonConverterScenarioTestBase
{
    protected override JsonSerializerOptions Options { get; } = CreateOptions(null);

    protected override JsonSerializerOptions CamelCaseOptions { get; } = CreateOptions(JsonNamingPolicy.CamelCase);

    private static JsonSerializerOptions CreateOptions(JsonNamingPolicy? keyPolicy)
    {
        var options = new JsonSerializerOptions { DictionaryKeyPolicy = keyPolicy };
        options.Converters.Add(new ValueListDictionaryJsonConverter());
        return options;
    }

    protected override string SerializeMap(ValueListDictionary<string, int> map, JsonSerializerOptions options)
        => JsonSerializer.Serialize(map, options);

    protected override string SerializeMapViaRuntime(Dictionary<string, List<int>> map, JsonSerializerOptions options)
        => JsonSerializer.Serialize(map, options);

    protected override (int Id, string Name, ValueListDictionary<string, int> Map) RoundTripMember(
        int id, string name, ValueListDictionary<string, int> map)
    {
        var holder = new JsonHolder { Id = id, Name = name, Map = map };
        var json = JsonSerializer.Serialize(holder, Options);
        var result = JsonSerializer.Deserialize<JsonHolder>(json, Options)!;
        return (result.Id, result.Name, result.Map);
    }

    protected override ValueListDictionary<string, List<int>> RoundTripNested(ValueListDictionary<string, List<int>> map)
    {
        var json = JsonSerializer.Serialize(map, Options);
        return JsonSerializer.Deserialize<ValueListDictionary<string, List<int>>>(json, Options)!;
    }
}

public sealed class AnnotatedJsonHolder
{
    public int Id { get; set; }

    [JsonConverter(typeof(ValueListDictionaryJsonConverter))]
    public ValueListDictionary<string, int> Map { get; set; } = new();

    public string Name { get; set; } = string.Empty;
}

public sealed class AnnotatedNestedJsonHolder
{
    [JsonConverter(typeof(ValueListDictionaryJsonConverter))]
    public ValueListDictionary<string, List<int>> Map { get; set; } = new();
}

public sealed class MapHolder
{
    public int Id { get; set; }

    public Dictionary<string, List<int>> Map { get; set; } = new();

    public string Name { get; set; } = string.Empty;
}

// Wiring 2: the converter is applied via a [JsonConverter] attribute on the member. The options below
// intentionally do not register it, so the attribute is the only thing selecting the converter.
public sealed class ValueListDictionaryJsonConverterAnnotationScenarioTest : ValueListDictionaryJsonConverterScenarioTestBase
{
    protected override JsonSerializerOptions Options { get; } = new();

    protected override JsonSerializerOptions CamelCaseOptions { get; } = new() { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };

    protected override string SerializeMap(ValueListDictionary<string, int> map, JsonSerializerOptions options)
        => JsonSerializer.Serialize(new AnnotatedJsonHolder { Map = map }, options);

    protected override string SerializeMapViaRuntime(Dictionary<string, List<int>> map, JsonSerializerOptions options)
        => JsonSerializer.Serialize(new MapHolder { Map = map }, options);

    protected override (int Id, string Name, ValueListDictionary<string, int> Map) RoundTripMember(
        int id, string name, ValueListDictionary<string, int> map)
    {
        var holder = new AnnotatedJsonHolder { Id = id, Name = name, Map = map };
        var json = JsonSerializer.Serialize(holder, Options);
        var result = JsonSerializer.Deserialize<AnnotatedJsonHolder>(json, Options)!;
        return (result.Id, result.Name, result.Map);
    }

    protected override ValueListDictionary<string, List<int>> RoundTripNested(ValueListDictionary<string, List<int>> map)
    {
        var holder = new AnnotatedNestedJsonHolder { Map = map };
        var json = JsonSerializer.Serialize(holder, Options);
        var result = JsonSerializer.Deserialize<AnnotatedNestedJsonHolder>(json, Options)!;
        return result.Map;
    }
}
