using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;

namespace AnakinRaW.CommonUtilities.Testing;

public abstract class CommonTestBase
{
    protected readonly IServiceProvider ServiceProvider;

    protected readonly MockFileSystem FileSystem = new ();

    protected CommonTestBase()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IFileSystem>(FileSystem); 

        // ReSharper disable once VirtualMemberCallInConstructor
        SetupServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    protected virtual void SetupServices(IServiceCollection serviceCollection)
    {
    }
}