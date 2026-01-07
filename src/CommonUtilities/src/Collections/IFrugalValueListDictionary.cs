namespace AnakinRaW.CommonUtilities.Collections;

/// <summary>
/// Represents a mutable generic collection that maps keys to lists of values,
/// using a memory-efficient representation optimized for one value per key.
/// </summary>
/// <remarks>
/// <para>
/// This interface combines the memory-efficient storage of <see cref="IReadOnlyFrugalValueListDictionary{TKey, TValue}"/>
/// with the mutation capabilities of <see cref="IValueListDictionary{TKey, TValue}"/>.
/// </para>
/// <para>
/// The underlying storage uses <see cref="ImmutableFrugalList{T}"/>, which is optimized for cases where
/// most keys have zero or one value.
/// </para>
/// <para>
/// All methods returning <see cref="ImmutableFrugalList{T}"/> return snapshots.
/// The returned lists are not affected by subsequent modifications to the dictionary.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <seealso cref="IReadOnlyFrugalValueListDictionary{TKey, TValue}"/>
/// <seealso cref="IValueListDictionary{TKey, TValue}"/>
/// <seealso cref="ImmutableFrugalList{T}"/>
public interface IFrugalValueListDictionary<TKey, TValue> : 
    IReadOnlyFrugalValueListDictionary<TKey, TValue>, 
    IValueListDictionary<TKey, TValue> 
    where TKey : notnull;