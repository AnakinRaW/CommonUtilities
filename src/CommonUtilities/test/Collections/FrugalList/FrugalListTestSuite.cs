using AnakinRaW.CommonUtilities.Testing.Collections;

namespace AnakinRaW.CommonUtilities.Test.Collections.FrugalList;

public abstract class FrugalListTestSuite<T> : IListTestSuite<T>
{
    protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
    protected override bool NonGenericEnumerator_Empty_Current_UndefinedOperation_Throw => true;
    protected override bool NonGenericEnumerator_Current_UndefinedOperation_Throws => true;
    protected override bool Enumerator_Empty_UsesSingletonInstance => true;
}