using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Steps;

/// <summary>
/// Base implementation for an <see cref="IStep"/>
/// </summary>
public abstract class PipelineStep : IStep
{
    internal bool IsDisposed { get; private set; }

    /// <summary>
    /// The service provider of this step.
    /// </summary>
    protected IServiceProvider Services { get; }

    /// <summary>
    /// The logger of this step.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <summary>
    /// The exception, if any, that occurred during execution.
    /// </summary>
    public Exception? Error { get; internal set; }

    /// <summary>
    /// Initializes a new <see cref="PipelineStep"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected PipelineStep(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        Services = serviceProvider;
        Logger = serviceProvider.GetService<ILogger>();
    }

    /// <inheritdoc/>
    ~PipelineStep()
    {
        Dispose(false);
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
        catch (RunnerException ex)
        {
            Error = ex;
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

    /// <summary>
    /// Disposes managed resources of this instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> is this instance gets disposed; <see langword="false"/> if it get's finalized.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return; 
        IsDisposed = true;
    }
        
    private void LogFaultException(Exception ex)
    { 
        Error = ex; 
        Logger?.LogError(ex, ex.InnerException?.Message ?? ex.Message);
    }
}