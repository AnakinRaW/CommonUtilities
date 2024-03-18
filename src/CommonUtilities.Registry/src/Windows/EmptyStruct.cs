namespace AnakinRaW.CommonUtilities.Registry.Windows;

// From https://github.com/microsoft/vs-threading

internal readonly struct EmptyStruct
{
    internal static EmptyStruct Instance => default;
}