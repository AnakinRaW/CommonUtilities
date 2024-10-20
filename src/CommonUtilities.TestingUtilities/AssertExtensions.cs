using System.Reflection;
using System.Runtime.InteropServices;

namespace AnakinRaW.CommonUtilities.Testing;

public static class AssertExtensions
{
    private static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

    public static void Throws_IgnoreTargetInvocationException<T>(Func<object?> action) where T : Exception
    {
        Throws_IgnoreTargetInvocationException(typeof(T), action);
    }

    public static void Throws_IgnoreTargetInvocationException(Type expectedException, Func<object?> action)
    {
        if (expectedException.IsAssignableFrom(typeof(Exception)))
            throw new ArgumentException("Type argument must be assignable from System.Exception", nameof(expectedException));
        try
        {
            action();
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException?.GetType() != expectedException)
                Assert.Fail($"Expected exception of type {expectedException.Name} but got {e.InnerException?.GetType().Name}");
            return;
        }
        catch (Exception e)
        {
            if (e.GetType() == expectedException)
                return;
            Assert.Fail($"Expected exception of type {expectedException.Name} but got {e.GetType().Name}");
        }
        Assert.Fail($"Excepted exception of type {expectedException.Name} but non was thrown.");
    }

    public static T Throws<T>(string? expectedParamName, Action action) where T : ArgumentException
    {
        T exception = Assert.Throws<T>(action);
        Assert.Equal(expectedParamName, exception.ParamName);
        return exception;
    }

    public static T Throws<T>(string netCoreParamName, string? netFxParamName, Action action)
        where T : ArgumentException
    {
        var exception = Assert.Throws<T>(action);

        if (netFxParamName == null && IsNetFramework)
        {
            // Param name varies between .NET Framework versions -- skip checking it
            return exception;
        }

        var expectedParamName = IsNetFramework ? netFxParamName : netCoreParamName;

        Assert.Equal(expectedParamName, exception.ParamName);
        return exception;
    }
}