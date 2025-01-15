using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// Base implementation for an <see cref="IStep"/>.
/// </summary>
public abstract class PipelineStep : DisposableObject, IStep
{
    /// <summary>
    /// Returns the service provider of this step.
    /// </summary>
    protected readonly IServiceProvider Services;

    /// <summary>
    /// Returns the logger of this step.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Gets the exception that occurred during execution or <see langword="null"/> if no error occurred.
    /// </summary>
    public Exception? Error { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStep"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected PipelineStep(IServiceProvider serviceProvider)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
    
    /// <inheritdoc/>
    public void Run(CancellationToken token)
    {
        Logger?.LogTrace($"BEGIN: {this}");
        try
        {
            RunCore(token);
            Logger?.LogTrace($"END: {this}");
        }
        catch (OperationCanceledException ex)
        {
            Error = ex.InnerException;
            throw;
        }
        catch (StopRunnerException)
        {
            throw;
        }
        catch (AggregateException ex)
        {
            if (!ex.IsExceptionType<OperationCanceledException>())
                LogFaultException(ex);
            else
                Error = ex.InnerExceptions.FirstOrDefault(p => p.IsExceptionType<OperationCanceledException>())?.InnerException;
            throw;
        }
        catch (Exception e)
        {
            LogFaultException(e);
            throw;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Executes this step. 
    /// </summary>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract void RunCore(CancellationToken token);

    private void LogFaultException(Exception ex)
    { 
        Error = ex; 
        Logger?.LogError(ex, ex.InnerException?.Message ?? ex.Message);
    }
}