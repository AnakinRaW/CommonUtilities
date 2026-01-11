using System;
using System.Collections;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Collections;

internal sealed class EmptyEnumerator<T> : IEnumerator<T>
{
    public static readonly EmptyEnumerator<T> Instance = new();

    public T Current => throw new InvalidOperationException("Enmeration has not started.");

    object? IEnumerator.Current => Current;

    private EmptyEnumerator() { }

    public bool MoveNext()
    {
        return false;
    }

    public void Reset() { }

    public void Dispose() { }
}