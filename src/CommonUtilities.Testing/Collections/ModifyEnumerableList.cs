using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Testing.Collections;

/// <summary>
/// Helper class to provide means to modify an enumerable, which is not the to be tested type.
/// </summary>
internal class ModifyEnumerableList<T>(Func<int, T> createT) : IListTestSuite<T>
{
    protected override T CreateT(int seed)
    {
        return createT(seed);
    }

    protected override IList<T> GenericIListFactory()
    {
        throw new NotImplementedException();
    }
}