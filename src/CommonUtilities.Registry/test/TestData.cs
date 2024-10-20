using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;

namespace AnakinRaW.CommonUtilities.Registry.Test;

internal static class TestData
{
    private static readonly object[][] s_testValueTypes;

    private static readonly object[][] s_testObjects;

    private static readonly object[][] s_testEnvironment;

    private static readonly object[][] s_testExpandableStrings;

    private static readonly object[][] s_testValueNames;

    internal const string DefaultValue = "default";

    static TestData()
    {
        var rand = new Random(-55);

        s_testValueTypes =
        [
            ["Test_01", (byte)rand.Next(byte.MinValue, sbyte.MaxValue)],
            ["Test_02", (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue)],
            ["Test_03", (short)rand.Next(short.MinValue, short.MaxValue)],
            ["Test_04", (ushort)rand.Next(ushort.MinValue, ushort.MaxValue)],
            ["Test_05", (int)rand.Next(int.MinValue, int.MaxValue)],
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
        var obj = new object();
        s_testObjects =
        [
            // Standard Random Numbers
            [0, (byte)rand.Next(byte.MinValue, byte.MaxValue), RegistryValueKind.String],
            [1, (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue), RegistryValueKind.String],
            [2, (short)rand.Next(short.MinValue, short.MaxValue), RegistryValueKind.String],
            [3, (ushort)rand.Next(ushort.MinValue, ushort.MaxValue), RegistryValueKind.String],
            [4, (char)rand.Next(char.MinValue, char.MaxValue), RegistryValueKind.String],
            [5, (int)rand.Next(int.MinValue, int.MaxValue), RegistryValueKind.DWord],
            // Random Numbers that can fit into Int32
            [6, (uint)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [7, (long)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [8, (long)(rand.NextDouble() * int.MinValue), RegistryValueKind.String],
            [9, (ulong)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [10, (decimal)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [11, (decimal)(rand.NextDouble() * int.MinValue), RegistryValueKind.String],
            [12, (float)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [13, (float)(rand.NextDouble() * int.MinValue), RegistryValueKind.String],
            [14, (double)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String],
            [15, (double)(rand.NextDouble() * int.MinValue), RegistryValueKind.String],
            // Random Numbers that can't fit into Int32 but can fit into Int64
            [16, (uint)(rand.NextDouble() * (uint.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String],
            [17, (long)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String],
            [18, (long)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String],
            [19, (ulong)(rand.NextDouble() * (long.MaxValue - (ulong)int.MaxValue) + int.MaxValue), RegistryValueKind.String
            ],
            [20, (decimal)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String
            ],
            [21, (decimal)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String
            ],
            [22, (float)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String],
            [23, (float)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String],
            [24, (double)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String],
            [25, (double)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String],
            // Random Numbers that can't fit into Int32 or Int64
            [26, (ulong)(rand.NextDouble() * (ulong.MaxValue - long.MaxValue) + long.MaxValue), RegistryValueKind.String
            ],
            [27, decimal.MaxValue, RegistryValueKind.String],
            [28, decimal.MinValue, RegistryValueKind.String],
            [29, float.MaxValue, RegistryValueKind.String],
            [30, float.MinValue, RegistryValueKind.String],
            [31, double.MaxValue, RegistryValueKind.String],
            [32, double.MinValue, RegistryValueKind.String],
            // Various other types
            [33, "Hello World", RegistryValueKind.String],
            [34, "Hello %path5% World", RegistryValueKind.String],
            [35, new[] { "Hello World", "Hello %path% World" }, RegistryValueKind.MultiString],
            [36, obj, RegistryValueKind.String],
            [37, bytes, RegistryValueKind.Binary]
        ];

        var envs = new List<object[]>();
        int counter = 0;
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            envs.Add(["ExpandedTest_" + counter, entry.Key, entry.Value]);
            ++counter;
        }
        s_testEnvironment = envs.ToArray();

        const string sysRootVar = "%Systemroot%";
        const string pathVar = "%path%";
        const string tmpVar = "%tmp%";

        s_testExpandableStrings =
        [
            [
                sysRootVar + @"\mydrive\mydirectory\myfile.xxx",
                Environment.ExpandEnvironmentVariables(sysRootVar) + @"\mydrive\mydirectory\myfile.xxx",
                RegistryValueOptions.None
            ],
            [
                sysRootVar + @"\mydrive\mydirectory\myfile.xxx",
                sysRootVar + @"\mydrive\mydirectory\myfile.xxx",
                RegistryValueOptions.DoNotExpandEnvironmentNames
            ],
            [
                tmpVar + @"\gfdhghdfgk\fsdfds\dsd.yyy",
                Environment.ExpandEnvironmentVariables(tmpVar) + @"\gfdhghdfgk\fsdfds\dsd.yyy",
                RegistryValueOptions.None
            ],
            [
                tmpVar + @"\gfdhghdfgk\fsdfds\dsd.yyy",
                tmpVar + @"\gfdhghdfgk\fsdfds\dsd.yyy",
                RegistryValueOptions.DoNotExpandEnvironmentNames
            ],
            [
                pathVar + @"\rwerew.zzz",
                Environment.ExpandEnvironmentVariables(pathVar) + @"\rwerew.zzz",
                RegistryValueOptions.None
            ],
            [
                pathVar + @"\rwerew.zzz",
                pathVar + @"\rwerew.zzz",
                RegistryValueOptions.DoNotExpandEnvironmentNames
            ],
            [
                sysRootVar + @"\mydrive\" + pathVar + @"\myfile.xxx",
                Environment.ExpandEnvironmentVariables(sysRootVar) + @"\mydrive\" + Environment.ExpandEnvironmentVariables(pathVar) + @"\myfile.xxx",
                RegistryValueOptions.None
            ],
            [
                sysRootVar + @"\mydrive\" + pathVar + @"\myfile.xxx",
                sysRootVar + @"\mydrive\" + pathVar + @"\myfile.xxx",
                RegistryValueOptions.DoNotExpandEnvironmentNames
            ]
        ];

        s_testValueNames =
        [
            [string.Empty],
            [null],
            [new string('a', 256)] // the name length limit is 255 but prior to V4 the limit is 16383
        ];
    }

    public static IEnumerable<object[]> TestValueTypes { get { return s_testValueTypes; } }

    public static IEnumerable<object[]> TestObjects { get { return s_testObjects; } }

    public static IEnumerable<object[]> TestEnvironment { get { return s_testEnvironment; } }

    public static IEnumerable<object[]> TestExpandableStrings { get { return s_testExpandableStrings; } }

    public static IEnumerable<object[]> TestValueNames { get { return s_testValueNames; } }
}