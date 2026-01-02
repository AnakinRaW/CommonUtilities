using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.CommonUtilities.SimplePipeline;

/// <summary>
/// Represents a base class for pipelines that execute steps using a <see cref="IStepRunner"/>.
/// </summary>
/// <typeparam name="TStepRunner">The type of the step runner used to execute the steps.</typeparam>
/// <remarks>
/// <para>
/// Running the pipeline may throw a <see cref="StepFailureException"/> if one or many steps produce errors.
/// </para>
/// <para>
/// This class provides functionality for managing a step runner, executing steps, handling errors, 
/// and disposing resources. Derived classes must implement the <see cref="CreateRunner"/> method 
/// to provide a specific step runner implementation.
/// </para>
/// </remarks>
public abstract class StepRunnerPipelineBase<TStepRunner> : Pipeline where TStepRunner : class, IStepRunner
{ 
    private readonly Lazy<TStepRunner> _stepRunnerLazy;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepRunnerPipelineBase{TStepRunner}"/> class with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies for the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    protected StepRunnerPipelineBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _stepRunnerLazy = new Lazy<TStepRunner>(EnsureRunner, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Gets the step runner used to execute the steps in the pipeline.
    /// </summary>
    /// <value>
    /// The step runner of type <typeparamref name="TStepRunner"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// The step runner is lazily initialized when accessed for the first time, using <see cref="CreateRunner"/>. 
    /// It is guaranteed to be non-<see langword="null"/> once initialized.
    /// </para>
    /// <para>
    /// Derived classes can use this property to add steps to the runner or to 
    /// perform operations specific to the step runner implementation.
    /// </para>
    /// </remarks>
    protected internal TStepRunner StepRunner => _stepRunnerLazy.Value;

    private TStepRunner EnsureRunner()
    {
        return CreateRunner() ?? throw new InvalidOperationException("CreateRunner must not return null.");
    }

    /// <summary>
    /// Gets a value indicating whether the step runner has been initialized.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the step runner has been initialized; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// When this property returns <see langword="true"/>, the <see cref="StepRunner"/> property is guaranteed 
    /// to return a non-<see langword="null"/> value.
    /// </remarks>
    protected internal bool IsStepRunnerInitialized => _stepRunnerLazy.IsValueCreated;

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should terminate execution immediately 
    /// upon encountering an error.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the pipeline should stop execution on the first error; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// When set to <see langword="true"/>, the pipeline will cancel further processing as soon as an error occurs.
    /// This is useful for scenarios where continuing execution after an error is not desirable.
    /// </remarks>
    public bool FailFast { get; protected set; } = false;

    /// <summary>
    /// Creates an instance of the step runner used to execute the steps in the pipeline.
    /// </summary>
    /// <returns>An instance of <typeparamref name="TStepRunner"/> representing the step runner.</returns>
    /// <remarks>
    /// Derived classes must implement this method to provide a specific implementation of the step runner.
    /// The returned step runner must not be <see langword="null"/>
    /// </remarks>
    protected abstract TStepRunner CreateRunner();
    
    /// <summary>
    /// Executes the pipeline asynchronously, running all steps added to <see cref="StepRunner"/>.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous execution of the pipeline.
    /// </returns>
    /// <remarks>
    /// This method attaches an error handler to the step runner, executes the steps asynchronously,
    /// and ensures that any failed steps throw an exception after execution.
    /// </remarks>
    /// <exception cref="StepFailureException">
    /// Thrown when if any executed step failed excluding those which represent a cancelled Step.
    /// </exception>
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        try
        {
            StepRunner.Error += OnError!;
            await StepRunner.RunAsync(token).ConfigureAwait(false);
        }
        finally
        {
            StepRunner.Error -= OnError!;
        }

        StepRunner.ExecutedSteps.ThrowStepFailureExceptionForFailedSteps();
    }

    /// <summary>
    /// Handles errors that occur during the execution of the <see cref="IStepRunner"/>.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <see cref="IStepRunner"/> instance.</param>
    /// <param name="e">The <see cref="StepRunnerErrorEventArgs"/> containing details about the error.</param>
    /// <remarks>
    /// This method updates the pipeline's state based on the error details, including whether the pipeline
    /// should be cancelled or marked as failed. If <see cref="FailFast"/> is enabled or the error indicates
    /// cancellation, the pipeline will be cancelled immediately.
    /// </remarks>
    protected virtual void OnError(object sender, StepRunnerErrorEventArgs e)
    {
        Cancelled |= e.Cancel;
        Failed |= !e.Cancel;

        if (FailFast || e.Cancel)
            Cancel();
    }

    /// <summary>
    /// Releases the resources used by the <see cref="StepRunnerPipelineBase{TStepRunner}"/> instance.
    /// </summary>
    /// <remarks>
    /// This method ensures that any resources associated with the pipeline, including the step runner, are properly disposed of.
    /// If the step runner is initialized and implements <see cref="IDisposable"/>, it will be disposed of.
    /// </remarks>
    protected override void DisposeResources()
    {
        base.DisposeResources();
        if (IsStepRunnerInitialized)
        {
            StepRunner.Error -= OnError!;
            if (StepRunner is IDisposable disposableRunner)
                disposableRunner.Dispose();
        }
    }
}