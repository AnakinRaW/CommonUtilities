using System.Net;

namespace Sklavenwalker.CommonUtilities.DownloadManager.Engines;

internal class WrappedWebException : WebException
{
    public WrappedWebException(int errorCode, string message)
        : base(message)
    {
        HResult = errorCode;
    }
    
    public static void Throw(int errorCode, string functionName, string message)
    {
        var errorCode1 = -2147024896 | errorCode;
        if (string.IsNullOrEmpty(message))
            message = "unspecified";
        throw new WrappedWebException(errorCode1, $"Function: {functionName}, HR: {errorCode1}, Message: {message}");
    }
}