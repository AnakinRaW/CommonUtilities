using System;
using System.Collections;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Registry.Test;

internal static class TestData
{
    internal enum MyEnum
    {
        A,
        B
    }

    private static readonly object[][] STestValueTypes;
    private static readonly object[][] STestEnvironment;

    internal const string DefaultValue = "default";

    static TestData()
    {
        var rand = new Random(-55);

        STestValueTypes =
        [
            ["Test_01", (byte)rand.Next(byte.MinValue, sbyte.MaxValue)],
            ["Test_02", (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue)],
            ["Test_03", (short)rand.Next(short.MinValue, short.MaxValue)],
            ["Test_04", (ushort)rand.Next(ushort.MinValue, ushort.MaxValue)],
            ["Test_05", rand.Next(int.MinValue, int.MaxValue)],
            ["Test_06", (uint)rand.Next(0, int.MaxValue)],
            ["Test_07", (long)rand.Next(int.MinValue, int.MaxValue)],
            ["Test_08", (ulong)rand.Next(0, int.MaxValue)],
            ["Test_09", new decimal(((double)decimal.MaxValue) * rand.NextDouble())],
            ["Test_10", new decimal(((double)decimal.MinValue) * rand.NextDouble())],
            ["Test_11", new decimal(((double)decimal.MinValue) * rand.NextDouble())],
            ["Test_12", new decimal(((double)decimal.MaxValue) * rand.NextDouble())],
            ["Test_13", int.MaxValue *rand.NextDouble()],
            ["Test_14", int.MinValue * rand.NextDouble()],
            ["Test_15", int.MaxValue * (float)rand.NextDouble()],
            ["Test_16", int.MinValue * (float)rand.NextDouble()]
        ];

        var bytes = new byte[rand.Next(0, 100)];
        rand.NextBytes(bytes);

        var envs = new List<object[]>();
        var counter = 0;
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            envs.Add(["ExpandedTest_" + counter, entry.Key, entry.Value]);
            ++counter;
        }

        STestEnvironment = envs.ToArray();
    }

    public static IEnumerable<object[]> TestValueTypes => STestValueTypes;

    public static IEnumerable<object[]> TestEnvironment => STestEnvironment;
}