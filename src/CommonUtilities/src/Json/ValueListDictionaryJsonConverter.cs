using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnakinRaW.CommonUtilities.Collections;

namespace AnakinRaW.CommonUtilities.Json;

/// <summary>
/// Converts a value-list-dictionary to and from a JSON object whose property names are the dictionary's keys
/// and whose values are JSON arrays of the values associated with each key.
/// </summary>
/// <remarks>
/// <para>
/// This <see cref="JsonConverterFactory"/> supports the mutable
/// <see cref="ValueListDictionary{TKey,TValue}"/> and <see cref="FrugalValueListDictionary{TKey,TValue}"/>
/// types, the read-only <see cref="ReadOnlyValueListDictionary{TKey,TValue}"/> and
/// <see cref="ReadOnlyFrugalValueListDictionary{TKey,TValue}"/> types, and each of their corresponding
/// interfaces. Deserializing one of the interfaces produces an instance of the matching concrete type.
/// </para>
/// <para>
/// Keys use the <see cref="JsonConverter{T}"/> that the active <see cref="JsonSerializerOptions"/> resolves
/// for the key type, with <see cref="JsonSerializerOptions.DictionaryKeyPolicy"/> applied on write. Supported
/// key types include <see cref="string"/>, the integral and floating-point primitives, <see cref="bool"/>,
/// <see cref="Guid"/>, and enumerations.
/// </para>
/// <para>
/// A deserialized dictionary uses the default key comparer. A value array that is empty or
/// <see langword="null"/> yields no entry for its key. If a property name appears more than once, the last
/// occurrence replaces any earlier value array.
/// </para>
/// </remarks>
public sealed class ValueListDictionaryJsonConverter : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return GetValueListDictionaryInterface(typeToConvert) is not null;
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionaryInterface =
            GetValueListDictionaryInterface(typeToConvert) ??
            throw new ArgumentException($"Type '{typeToConvert}' is not a value-list-dictionary.", nameof(typeToConvert));

        var arguments = dictionaryInterface.GetGenericArguments();
        var converterType = typeof(Converter<,,>).MakeGenericType(typeToConvert, arguments[0], arguments[1]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private static Type? GetValueListDictionaryInterface(Type typeToConvert)
    {
        if (typeToConvert is { IsInterface: true, IsGenericType: true } &&
            typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyValueListDictionary<,>))
        {
            return typeToConvert;
        }

        return typeToConvert.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyValueListDictionary<,>));
    }

    private sealed class Converter<TDictionary, TKey, TValue> : JsonConverter<TDictionary>
        where TKey : notnull
    {
        private readonly DictionaryKind _kind = DetermineKind(typeof(TDictionary));

        private static DictionaryKind DetermineKind(Type type)
        {
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

            if (definition == typeof(ReadOnlyFrugalValueListDictionary<,>) ||
                definition == typeof(IReadOnlyFrugalValueListDictionary<,>))
            {
                return DictionaryKind.ReadOnlyFrugal;
            }

            if (definition == typeof(FrugalValueListDictionary<,>) ||
                definition == typeof(IFrugalValueListDictionary<,>))
            {
                return DictionaryKind.Frugal;
            }

            if (definition == typeof(ReadOnlyValueListDictionary<,>) ||
                definition == typeof(IReadOnlyValueListDictionary<,>))
            {
                return DictionaryKind.ReadOnly;
            }

            // ValueListDictionary<,> and IValueListDictionary<,>.
            return DictionaryKind.Mutable;
        }

        public override TDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected start of object but got '{reader.TokenType}'.");

            var keyConverter = (JsonConverter<TKey>)options.GetConverter(typeof(TKey));

            var entries = new List<KeyValuePair<TKey, List<TValue>>>();
            // Maps each key to its slot in 'entries' so a repeated property name overwrites its value
            // (last one wins) at the original position, matching the built-in dictionary converter. The
            // default comparer is used here because that is the comparer the rebuilt dictionary will use.
            var indexByKey = new Dictionary<TKey, int>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return (TDictionary)Build(entries);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException($"Expected property name but got '{reader.TokenType}'.");

                var key = keyConverter.ReadAsPropertyName(ref reader, typeof(TKey), options);

                reader.Read();
                if (reader.TokenType is not (JsonTokenType.StartArray or JsonTokenType.Null))
                    throw new JsonException($"Expected start of array for key '{key}' but got '{reader.TokenType}'.");

                var values = JsonSerializer.Deserialize<List<TValue>>(ref reader, options) ?? [];
                if (indexByKey.TryGetValue(key, out var existingIndex))
                    entries[existingIndex] = new KeyValuePair<TKey, List<TValue>>(key, values);
                else
                {
                    indexByKey[key] = entries.Count;
                    entries.Add(new KeyValuePair<TKey, List<TValue>>(key, values));
                }
            }

            throw new JsonException("Unexpected end of JSON while reading object.");
        }

        private object Build(IReadOnlyList<KeyValuePair<TKey, List<TValue>>> entries)
        {
            switch (_kind)
            {
                case DictionaryKind.Frugal:
                    return BuildFrugal(entries);
                case DictionaryKind.ReadOnlyFrugal:
                    return new ReadOnlyFrugalValueListDictionary<TKey, TValue>(BuildFrugal(entries));
                case DictionaryKind.ReadOnly:
                    return new ReadOnlyValueListDictionary<TKey, TValue>(BuildMutable(entries));
                default:
                    return BuildMutable(entries);
            }
        }

        private static ValueListDictionary<TKey, TValue> BuildMutable(IReadOnlyList<KeyValuePair<TKey, List<TValue>>> entries)
        {
            var dictionary = new ValueListDictionary<TKey, TValue>();
            foreach (var entry in entries)
            {
                foreach (var value in entry.Value)
                    dictionary.Add(entry.Key, value);
            }

            return dictionary;
        }

        private static FrugalValueListDictionary<TKey, TValue> BuildFrugal(IReadOnlyList<KeyValuePair<TKey, List<TValue>>> entries)
        {
            var dictionary = new FrugalValueListDictionary<TKey, TValue>();
            foreach (var entry in entries)
            {
                foreach (var value in entry.Value)
                    dictionary.Add(entry.Key, value);
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options)
        {
            var dictionary = (IReadOnlyValueListDictionary<TKey, TValue>)value!;
            var keyConverter = (JsonConverter<TKey>)options.GetConverter(typeof(TKey));
            writer.WriteStartObject();
            foreach (var key in dictionary.Keys)
            {
                keyConverter.WriteAsPropertyName(writer, key, options);
                JsonSerializer.Serialize(writer, dictionary.GetValues(key), options);
            }
            writer.WriteEndObject();
        }
    }

    private enum DictionaryKind
    {
        Mutable,
        Frugal,
        ReadOnly,
        ReadOnlyFrugal
    }
}