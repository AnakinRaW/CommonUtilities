using AnakinRaW.CommonUtilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace AnakinRaW.CommonUtilities.Test.Collections;

// From https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Collections/DebugView.Tests.cs
// and https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/Diagnostics/DebuggerAttributes.cs

public class DebugViewTests
{
    public static IEnumerable<object[]> TestDebuggerAttributes_ValueListDictionaryInput()
    {
        yield return [new ValueListDictionary<int, string>(), Array.Empty<KeyValuePair<string,string>>()];
        yield return [new FrugalValueListDictionary<int, string>(), Array.Empty<KeyValuePair<string,string>>()];
        yield return [new ReadOnlyValueListDictionary<int, string>(new ValueListDictionary<int, string>()), Array.Empty<KeyValuePair<string, string>>()];
        yield return [new ReadOnlyFrugalValueListDictionary<int, string>(new FrugalValueListDictionary<int, string>()), Array.Empty<KeyValuePair<string, string>>()];

        yield return
        [
            new ValueListDictionary<int, string>{{1, "One"}, {2, "Two"}, {1, " Three"}},
                new KeyValuePair<string, string>[]
                {
                    new ("[1]", "ValueCount = 2"),
                    new ("[2]", "ValueCount = 1"),
                }
        ];
        yield return
        [
            new FrugalValueListDictionary<int, string>{{1, "One"}, {2, "Two"}, {1, " Three"}},
            new KeyValuePair<string, string>[]
            {
                new ("[1]", "ValueCount = 2"),
                new ("[2]", "ValueCount = 1"),
            }
        ];
        yield return
        [
            new ReadOnlyValueListDictionary<int, string>(new ValueListDictionary<int, string>{{1, "One"}, {2, "Two"}, {1, " Three"}}),
            new KeyValuePair<string, string>[]
            {
                new ("[1]", "ValueCount = 2"),
                new ("[2]", "ValueCount = 1"),
            }
        ];
        yield return
        [
            new ReadOnlyFrugalValueListDictionary<int, string>(new FrugalValueListDictionary<int, string>{{1, "One"}, {2, "Two"}, {1, " Three"}}),
            new KeyValuePair<string, string>[]
            {
                new ("[1]", "ValueCount = 2"),
                new ("[2]", "ValueCount = 1"),
            }
        ];
    }

    public static IEnumerable<object[]> TestDebuggerAttributes_FrugalListsInput()
    {
        yield return [new FrugalList<int>()];
        yield return [new FrugalList<int> { 1, 2 }];

        yield return [new ImmutableFrugalList<int>()];
        yield return [new ImmutableFrugalList<int>([1,2])];
    }

    public static IEnumerable<object[]> TestDebuggerAttributes_Inputs()
    {
        return TestDebuggerAttributes_ValueListDictionaryInput()
            .Select(t => new[] { t[0] })
            .Concat(TestDebuggerAttributes_FrugalListsInput());
    }

    [Theory]
    [MemberData(nameof(TestDebuggerAttributes_ValueListDictionaryInput))]
    public static void TestDebuggerAttributes_ValueListDictionary(IReadOnlyValueListDictionary<int, string> obj, KeyValuePair<string, string>[] expected)
    {
        DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
        var info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        var itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State == DebuggerBrowsableState.RootHidden);
        var items = (DebugViewValueListDictionaryItem<int, string>[])itemProperty.GetValue(info.Instance)!;
        var formatted = items
            .Select(DebuggerAttributes.ValidateDebugViewValueListDictionaryItem)
            .Select(formattedResult => new KeyValuePair<string, string>(formattedResult.Key, formattedResult.Value))
            .ToList();
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [MemberData(nameof(TestDebuggerAttributes_FrugalListsInput))]
    public static void TestDebuggerAttributes_FrugalList(IEnumerable<int> obj)
    {
        DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
        var info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        var itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State == DebuggerBrowsableState.RootHidden);
        var items = itemProperty.GetValue(info.Instance) as int[];
        Assert.Equal(obj, items);
    }

    [Theory]
    [MemberData(nameof(TestDebuggerAttributes_Inputs))]
    public static void TestDebuggerAttributes_Null(object obj)
    {
        var tie = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.CreateDebuggerTypeProxyWithNullArgument(obj.GetType()));
        Assert.IsType<ArgumentNullException>(tie.InnerException);
    }

    internal static class DebuggerAttributes
    {
        internal class DebuggerAttributeInfo
        {
            public required object Instance { get; init; }
            public required IEnumerable<PropertyInfo> Properties { get; init; }
        }

        internal class DebuggerDisplayResult
        {
            public required string Value { get; init; }
            public required string Key { get; init; }
            public required string Type { get; init; }
        }

        internal static Type GetProxyType(object obj)
        {
            return GetProxyType(obj.GetType());
        }

        internal static Type GetProxyType(Type type)
        {
            var cad = FindAttribute(type, attributeType: typeof(DebuggerTypeProxyAttribute));

            var proxyType = cad.ConstructorArguments[0].ArgumentType == typeof(Type) 
                ? (Type)cad.ConstructorArguments[0].Value! 
                : Type.GetType((string)cad.ConstructorArguments[0].Value!)!;
            if (type.GenericTypeArguments.Length > 0)
            {
                proxyType = proxyType.MakeGenericType(type.GenericTypeArguments);
            }

            return proxyType;
        }

        internal static void CreateDebuggerTypeProxyWithNullArgument(Type type)
        {
            var proxyType = GetProxyType(type);
            Activator.CreateInstance(proxyType, [null]);
        }

        internal static string ValidateDebuggerDisplayReferences(object obj)
        {
            var cad = FindAttribute(obj.GetType(), attributeType: typeof(DebuggerDisplayAttribute));

            // Get the text of the DebuggerDisplayAttribute
            var attrText = (string)cad.ConstructorArguments[0].Value!;

            return EvaluateDisplayString(attrText, obj);
        }

        internal static DebuggerAttributeInfo ValidateDebuggerTypeProxyProperties(object obj)
        {
            var proxyType = GetProxyType(obj);

            // Create an instance of the proxy type, and make sure we can access all of the instance properties
            // on the type without exception
            var proxyInstance = Activator.CreateInstance(proxyType, obj) ?? throw new InvalidOperationException();
            var properties = GetDebuggerVisibleProperties(proxyType);
            return new DebuggerAttributeInfo
            {
                Instance = proxyInstance,
                Properties = properties
            };
        }

        internal static DebuggerBrowsableState? GetDebuggerBrowsableState(MemberInfo info)
        {
            var debuggerBrowsableAttribute = info.CustomAttributes
                .SingleOrDefault(a => a.AttributeType == typeof(DebuggerBrowsableAttribute));
            // Enums in attribute constructors are boxed as ints, so cast to int? first.
            return (DebuggerBrowsableState?)(int?)debuggerBrowsableAttribute?.ConstructorArguments.Single().Value;
        }

        internal static IEnumerable<PropertyInfo> GetDebuggerVisibleProperties(Type debuggerAttributeType)
        {
            // The debugger doesn't evaluate non-public members of type proxies. GetGetMethod returns null if the getter is non-public.
            var visibleProperties = debuggerAttributeType.GetProperties()
                .Where(pi => pi.GetGetMethod() != null && GetDebuggerBrowsableState(pi) != DebuggerBrowsableState.Never);
            return visibleProperties;
        }

        internal static DebuggerDisplayResult ValidateDebugViewValueListDictionaryItem<TKey, TValue>(DebugViewValueListDictionaryItem<TKey, TValue> obj)
        {
            var cad = FindAttribute(obj.GetType(), attributeType: typeof(DebuggerDisplayAttribute));

            // Get the text of the DebuggerDisplayAttribute
            var formattedValue = ValidateDebuggerDisplayReferences(obj.ValueList);

            var formattedKey = FormatDebuggerDisplayNamedArgument(nameof(DebuggerDisplayAttribute.Name), cad, obj);
            var formattedType = FormatDebuggerDisplayNamedArgument(nameof(DebuggerDisplayAttribute.Type), cad, obj);


            return new DebuggerDisplayResult { Value = formattedValue, Key = formattedKey, Type = formattedType };
        }

        private static string FormatDebuggerDisplayNamedArgument(string argumentName, CustomAttributeData debuggerDisplayAttributeData, object obj)
        {
            var namedAttribute = debuggerDisplayAttributeData.NamedArguments!.FirstOrDefault(na => na.MemberName == argumentName);
            if (namedAttribute != default)
            {
                var value = (string?)namedAttribute.TypedValue.Value;
                if (!string.IsNullOrEmpty(value))
                    return EvaluateDisplayString(value!, obj);
            }
            return "";
        }


        private static CustomAttributeData FindAttribute(Type type, Type attributeType)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var attributes = t.GetTypeInfo().CustomAttributes
                    .Where(a => a.AttributeType == attributeType)
                    .ToArray();
                if (attributes.Length != 0)
                    return attributes.Length > 1 
                        ? throw new InvalidOperationException($"Expected one {attributeType.Name} on {type} but found more.") 
                        : attributes[0];
            }
            throw new InvalidOperationException($"Expected one {attributeType.Name} on {type}.");
        }

        private static string EvaluateDisplayString(string displayString, object obj)
        {
            var objType = obj.GetType();
            var segments = displayString.Split('{', '}');

            if (segments.Length % 2 == 0)
                throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} lacks a closing brace.");

            if (segments.Length == 1)
                throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} doesn't reference any expressions.");

            var sb = new StringBuilder();

            for (var i = 0; i < segments.Length; i += 2)
            {
                var literal = segments[i];
                sb.Append(literal);

                if (i + 1 < segments.Length)
                {
                    var reference = segments[i + 1];
                    var noQuotes = reference.EndsWith(",nq");

                    reference = reference.Replace(",nq", string.Empty);

                    // Evaluate the reference.
                    if (!TryEvaluateReference(obj, reference, out var member))
                        throw new InvalidOperationException($"The DebuggerDisplayAttribute for {objType} contains the expression \"{reference}\".");

                    var memberString = GetDebuggerMemberString(member, noQuotes);

                    sb.Append(memberString);
                }
            }

            return sb.ToString();
        }

        private static string GetDebuggerMemberString(object? member, bool noQuotes)
        {
            var memberString = "null";
            if (member != null)
            {
                memberString = member.ToString();
                if (member is string)
                {
                    if (!noQuotes) 
                        memberString = '"' + memberString + '"';
                }
                else if (!IsPrimitiveType(member)) 
                    memberString = '{' + memberString + '}';
            }

            return memberString!;
        }

        private static bool IsPrimitiveType(object obj) =>
            obj is byte or sbyte or short or ushort or int or uint or long or ulong or float or double;

        private static bool TryEvaluateReference(object obj, string reference, out object? member)
        {
            var pi = GetProperty(obj, reference);
            if (pi != null)
            {
                member = pi.GetValue(obj);
                return true;
            }

            var fi = GetField(obj, reference);
            if (fi != null)
            {
                member = fi.GetValue(obj);
                return true;
            }

            member = null;
            return false;
        }

        private static FieldInfo? GetField(object obj, string fieldName)
        {
            for (var t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                var fi = t.GetTypeInfo().GetDeclaredField(fieldName);
                if (fi != null)
                    return fi;
            }
            return null;
        }

        private static PropertyInfo? GetProperty(object obj, string propertyName)
        {
            for (var t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                var pi = t.GetTypeInfo().GetDeclaredProperty(propertyName);
                if (pi != null)
                    return pi;
            }
            return null;
        }
    }
}