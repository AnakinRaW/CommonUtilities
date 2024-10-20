using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AnakinRaW.CommonUtilities.Registry.Test;

public abstract partial class RegistryTestsBase : IDisposable
{
    private const string CurrentUserKeyName = "HKEY_CURRENT_USER";
    private const char MarkerChar = '\uffff';


    public abstract bool IsCaseSensitive { get; }
    public abstract bool HasPathLimits { get; }
    public abstract bool HasTypeLimits { get; }


    protected string TestRegistryKeyName { get; }

    protected IRegistryKey TestRegistryKey { get; }

    protected IRegistry Registry { get; }

    protected abstract IRegistry CreateRegistry();

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected RegistryTestsBase()
    {
        // Create a unique name for this test class
        TestRegistryKeyName = CreateUniqueKeyName();

        // Cleanup the key in case a previous run of this test crashed and left
        // the key behind.  The key name is specific enough to corefx that we don't
        // need to worry about it being a real key on the user's system used
        // for another purpose.
        RemoveKeyIfExists(TestRegistryKeyName);

        // Then create the key.
        Registry = CreateRegistry();
        TestRegistryKey = Registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default).CreateSubKey(TestRegistryKeyName)!;
        Assert.NotNull(TestRegistryKey);
    }

    public void Dispose()
    {
        TestRegistryKey.Dispose();
        RemoveKeyIfExists(TestRegistryKeyName);
    }

    public static readonly object[][] TestRegistrySubKeyNames =
    [
        [@"Foo", @"Foo"],
        [@"Foo\Bar", @"Foo\Bar"],

        // Multiple/trailing slashes should be removed.
        [@"Foo", @"Foo\"],
        [@"Foo", @"Foo\\"],
        [@"Foo", @"Foo\\\"],
        [@"Foo", @"Foo\\\\"],
        [@"Foo\Bar", @"Foo\\Bar"],
        [@"Foo\Bar", @"Foo\\\Bar"],
        [@"Foo\Bar", @"Foo\\\\Bar"],
        [@"Foo\Bar", @"Foo\Bar\"],
        [@"Foo\Bar", @"Foo\Bar\\"],
        [@"Foo\Bar", @"Foo\Bar\\\"],
        [@"Foo\Bar", @"Foo\\Bar\"],
        [@"Foo\Bar", @"Foo\\Bar\\"],
        [@"Foo\Bar", @"Foo\\Bar\\\"],
        [@"Foo\Bar", @"Foo\\\Bar\\\"],
        [@"Foo\Bar", @"Foo\\\\Bar\\\\"],

        // The name fix-up implementation uses a mark-and-sweep approach.
        // If there are multiple slashes, any extra slash chars will be
        // replaced with a marker char ('\uffff'), and then all '\uffff'
        // chars will be removed, including any pre-existing '\uffff' chars.
        InsertMarkerChar(@"Foo", @"{0}Foo\\"),
        InsertMarkerChar(@"Foo", @"Foo{0}\\"),
        InsertMarkerChar(@"Foo", @"Foo\\{0}"),
        InsertMarkerChar(@"Foo", @"Fo{0}o\\"),
        InsertMarkerChar(@"Foo", @"{0}Fo{0}o{0}\\{0}"),
        InsertMarkerChar(@"Foo", @"{0}Foo\\\"),
        InsertMarkerChar(@"Foo", @"Foo{0}\\\"),
        InsertMarkerChar(@"Foo", @"Foo\\\{0}"),
        InsertMarkerChar(@"Foo", @"Fo{0}o\\\"),
        InsertMarkerChar(@"Foo", @"{0}Fo{0}o{0}\\\{0}"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Foo\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo{0}\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\{0}Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\Bar{0}"),
        InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\B{0}ar"),
        InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\B{0}ar"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\\{0}B{0}ar{0}"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Foo\\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo{0}\\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\\{0}Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\\Bar{0}"),
        InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\\Bar"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\\\B{0}ar"),
        InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\\B{0}ar"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\\\{0}B{0}ar{0}"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Foo\Bar\\"),
        InsertMarkerChar(@"Foo\Bar", @"Foo{0}\Bar\\"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\{0}Bar\\"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\Bar{0}\\"),
        InsertMarkerChar(@"Foo\Bar", @"Foo\Bar\\{0}"),
        InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\B{0}ar\\"),
        InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\{0}B{0}ar{0}\\{0}"),

        // If there aren't multiple slashes, any '\uffff' chars should remain.
        InsertMarkerChar(@"{0}Foo"),
        InsertMarkerChar(@"Foo{0}"),
        InsertMarkerChar(@"Fo{0}o"),
        InsertMarkerChar(@"{0}Fo{0}o{0}"),
        InsertMarkerChar(@"{0}Foo\"),
        InsertMarkerChar(@"Foo{0}\"),
        InsertMarkerChar(@"Fo{0}o\"),
        InsertMarkerChar(@"{0}Fo{0}o{0}\"),
        InsertMarkerChar(@"{0}Foo\Bar"),
        InsertMarkerChar(@"Foo{0}\Bar"),
        InsertMarkerChar(@"Foo\{0}Bar"),
        InsertMarkerChar(@"Foo\Bar{0}"),
        InsertMarkerChar(@"Fo{0}o\Bar"),
        InsertMarkerChar(@"Foo\B{0}ar"),
        InsertMarkerChar(@"Fo{0}o\B{0}ar"),
        InsertMarkerChar(@"{0}Fo{0}o{0}\{0}B{0}ar{0}"),
        InsertMarkerChar(@"{0}Foo\Bar\"),
        InsertMarkerChar(@"Foo{0}\Bar\"),
        InsertMarkerChar(@"Foo\{0}Bar\"),
        InsertMarkerChar(@"Foo\Bar{0}\"),
        InsertMarkerChar(@"Fo{0}o\Bar\"),
        InsertMarkerChar(@"Foo\B{0}ar\"),
        InsertMarkerChar(@"Fo{0}o\B{0}ar\"),
        InsertMarkerChar(@"{0}Fo{0}o{0}\{0}B{0}ar{0}\")
    ];
    
    protected virtual void RemoveKeyIfExists(string keyName)
    {
    }

    protected void CreateTestRegistrySubKey(string expected)
    {
        Assert.Empty(TestRegistryKey.GetSubKeyNames());

        using var key = TestRegistryKey.CreateSubKey(expected);
        Assert.NotNull(key);
        Assert.Single(TestRegistryKey.GetSubKeyNames());
        Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
    }

    protected void Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(string expected, Func<IRegistryKey> createSubKey)
    {
        CreateTestRegistrySubKey(expected);

        using var key = createSubKey();
        Assert.NotNull(key);
        Assert.Single(TestRegistryKey.GetSubKeyNames()!);
        Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
    }

    protected void Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, Func<IRegistryKey> createSubKey)
    {
        Assert.Null(TestRegistryKey.OpenSubKey(expected));
        Assert.Empty(TestRegistryKey.GetSubKeyNames()!);

        using var key = createSubKey();
        Assert.NotNull(key);
        Assert.Single(TestRegistryKey.GetSubKeyNames()!);
        Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
    }

    protected void Verify_DeleteSubKey_KeyExists_KeyDeleted(string expected, Action deleteSubKey)
    {
        CreateTestRegistrySubKey(expected);

        deleteSubKey();
        Assert.Null(TestRegistryKey.OpenSubKey(expected));
    }

    protected void Verify_DeleteSubKey_KeyDoesNotExists_DoesNotThrow(string expected, Action deleteSubKey)
    {
        Assert.Null(TestRegistryKey.OpenSubKey(expected));
        Assert.Empty(TestRegistryKey.GetSubKeyNames()!);

        deleteSubKey();
    }

    protected void Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(string expected, Func<IRegistryKey> openSubKey)
    {
        CreateTestRegistrySubKey(expected);

        using var key = openSubKey();
        Assert.NotNull(key);
        Assert.Single(TestRegistryKey.GetSubKeyNames()!);
        Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
    }

    protected void Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(string expected, Func<IRegistryKey> openSubKey)
    {
        Assert.Null(TestRegistryKey.OpenSubKey(expected));
        Assert.Empty(TestRegistryKey.GetSubKeyNames()!);

        Assert.Null(openSubKey());
    }

    private string CreateUniqueKeyName()
    {
        return "commonutulitiestest_" + GetType().Name;
    }
    private static object[] InsertMarkerChar(string expected, string format)
    {
        var result = string.Format(format, MarkerChar);
        return [expected, result];
    }

    private static object[] InsertMarkerChar(string format)
    {
        var result = string.Format(format, MarkerChar);
        var expected = result.TrimEnd('\\');
        return [expected, result];
    }
}