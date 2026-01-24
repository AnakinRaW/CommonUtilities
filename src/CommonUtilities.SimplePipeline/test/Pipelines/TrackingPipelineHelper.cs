using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.SimplePipeline.Test.Pipelines;

internal sealed class TrackingPipelineHelper(List<string> callOrder, string? throwOnMethod)
{
    public void OnExecuteStarted()
    {
        callOrder.Add("OnExecuteStarted");
        if (throwOnMethod == "OnExecuteStarted")
            throw new InvalidOperationException("OnExecuteStarted threw");
    }

    public void OnRunnerExecuted()
    {
        callOrder.Add("OnRunnerExecuted");
        if (throwOnMethod == "OnRunnerExecuted")
            throw new InvalidOperationException("OnRunnerExecuted threw");
    }

    public void OnExecuteCompleted()
    {
        callOrder.Add("OnExecuteCompleted");
        if (throwOnMethod == "OnExecuteCompleted")
            throw new InvalidOperationException("OnExecuteCompleted threw");
    }
}
