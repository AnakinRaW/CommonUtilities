using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Base implementation for an <see cref="IPipeline"/>
/// </summary>
public abstract class Pipeline : DisposableObject, IPipeline
{
    private CancellationTokenSource? _linkedCancellationTokenSource;

    /// <summary>
    /// The service provider of the <see cref="SimplePipeline{TRunner}"/>.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// The logger of the <see cref="SimplePipeline{TRunner}"/>.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Gets a value indicating whether the preparation of the <see cref="Pipeline"/> was successful.
    /// </summary>
    protected bool? PrepareSuccessful { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline has encountered a failure.
    /// </summary>
    public bool PipelineFailed { get; protected set; }

    /// <summary>
    /// Gets a value indicating the pipeline shall abort execution on the first received error.
    /// </summary>
    protected virtual bool FailFast => false;

    /// <summary>
    /// 
    /// </summary>
    protected Pipeline(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
    
    /// <inheritdoc/>
    public async Task<bool> PrepareAsync()
    {
        ThrowIfDisposed();
        if (PrepareSuccessful.HasValue)
            return PrepareSuccessful.Value;
        PrepareSuccessful = await PrepareCoreAsync().ConfigureAwait(false);
        return PrepareSuccessful.Value;
    }

    /// <inheritdoc/>
    public virtual async Task RunAsync(CancellationToken token = default)
    {
        ThrowIfDisposed();
        token.ThrowIfCancellationRequested();
        if (!await PrepareAsync().ConfigureAwait(false))
            return;

        try
        {

            try
            {
                _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                await RunCoreAsync(_linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                if (_linkedCancellationTokenSource is not null)
                {
                    _linkedCancellationTokenSource.Dispose();
                    _linkedCancellationTokenSource = null;
                }
            }
        }
        catch (Exception)
        {
            PipelineFailed = true;
            throw;
        }
    }

    /// <summary>
    /// Cancels the pipeline
    /// </summary>
    public void Cancel()
    {
        _linkedCancellationTokenSource?.Cancel();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Performs the actual preparation of this instance.
    /// </summary>
    /// <returns><see langword="true"/> if the planning was successful; <see langword="false"/> otherwise.</returns>
    protected abstract Task<bool> PrepareCoreAsync();

    /// <summary>
    /// Implements the run logic of this instance.
    /// </summary>
    /// <remarks>It's assured this instance is already prepared when this method gets called.</remarks>
    /// <param name="token">Provided <see cref="CancellationToken"/> to allow cancellation.</param>
    protected abstract Task RunCoreAsync(CancellationToken token);

    /// <summary>
    /// Throws an <see cref="StepFailureException"/> if any of the passed steps ended with an error that is not the result of cancellation.
    /// </summary>
    /// <param name="steps">The steps that were executed by the pipeline.</param>
    /// <exception cref="StepFailureException">If any of <paramref name="steps"/> has an error that is not the result of cancellation.</exception>
    protected void ThrowIfAnyStepsFailed(IEnumerable<IStep> steps)
    {
        var failedBuildSteps = steps
            .Where(p => p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>())
            .ToList();

        if (failedBuildSteps.Any())
            throw new StepFailureException(failedBuildSteps);
    }

    /// <summary>
    /// The default event handler that can be used when an error occurs within a step.
    /// <see cref="PipelineFailed"/> is set to <see langword="true"/>. When <see cref="FailFast"/> is <see langword="true"/>, the pipeline gets cancelled.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnError(object sender, StepErrorEventArgs e)
    {
        PipelineFailed = true;
        if (FailFast || e.Cancel)
            Cancel();
    }
}