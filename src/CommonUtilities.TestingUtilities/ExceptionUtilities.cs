using System.Reflection;

namespace AnakinRaW.CommonUtilities.Testing;

public static class ExceptionUtilities
{
    public static void AssertThrows_IgnoreTargetInvocationException<T>(Func<object?> action) where T : Exception
    {
        AssertThrows_IgnoreTargetInvocationException(typeof(T), action);
    }

    public static void AssertThrows_IgnoreTargetInvocationException(Type expectedException, Func<object?> action)
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
}