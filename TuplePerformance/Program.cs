using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using org.structs.root.Extensions.maybe;

namespace TuplePerformance
{
    /// <summary> Measures the Performance of alternative Implementations returning 2 Values </summary>
    /// <remarks>
    /// Results differ vastly between Debug- and Release-Builds:
    ///
    /// 
    /// </remarks>
    [RankColumn]
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetTwoNumbers1() => GetTwoNumbers1(out _, out _);

        public static void GetTwoNumbers1(out int number1, out int number2)
        {
            number1 = 1;
            number2 = 2;
        }

        /// <summary> Not useful, because anonymous Types cannot be used outside the Method without knowing their Property Names </summary>
        public static dynamic GetAnonType()
        {
            var ret = new {a = 1, b = 2};
            return ret;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] public static int GetTwoNumbers4() => GetTwoNumbers4(out _);
        public static int GetTwoNumbers4(out int number1)
        {
            number1 = 1;
            return 2;
        }
    
        public static int Number = 3;

        public static int GetOneNumber() => Number;

        public static int? GetNullableNumber() => Number;

        public static Maybe<int> GetMaybeOneNumber() => Number;

        public static KeyValuePair<int, int> GetTwoNumbers2() => new(1, 2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Tuple<int, int> GetTwoNumbers3() => new(1, 2);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ValueTuple<int, int> GetTwoNumbers5() => new(1, 2);

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// With Check:
        /// Version 4:  6.99 ns 1 out Parameter:
        /// Version 1:  7.16 ns 2 out Parameters:
        /// Version 3: 14.66 ns return Tuple:
        /// Version 5: 15.10 ns return ValueTuple:
        /// Version 2: 16.10 ns return KeyValuePair:
        /// 
        /// Without Check:
        /// Version 4:  6.01 ns 1 out Parameter:
        /// Version 1:  5.20 ns 2 out Parameters:
        /// Version 3:  9.88 ns return Tuple:
        /// Version 2: 10.40 ns return KeyValuePair:
        /// Version 5: 11.63 ns return ValueTuple:
        ///
        /// </remarks>
        static void Main()
        {
            var summary = BenchmarkRunner.Run<Program>(
#if DEBUG
                new BenchmarkDotNet.Configs.DebugBuildConfig()
#endif //DEBUG
            );
            new Program().Timing();
            Console.WriteLine();
            new Program().Timing();
            Console.Read();
        }

        const int Max = 100_000_000;

        public  void Timing()
        {
            var s6 = Stopwatch.StartNew();
            ReturnOneNumber();
            s6.Stop();
            Console.WriteLine(@"Version 6: return one Number:"
                              + (s6.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s7 = Stopwatch.StartNew();
            MaybeReturnOneNumber();
            s7.Stop();
            Console.WriteLine(@"Version 3: return Maybe:"
                              + (s7.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s8 = Stopwatch.StartNew();
            ReturnNullableNumber();
            s8.Stop();
            Console.WriteLine(@"Version 8: return Nullable Number:"
                              + (s8.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s5 = Stopwatch.StartNew();
            ReturnValueTuple();
            s5.Stop();
            Console.WriteLine(@"Version 5: return ValueTuple:"
                              + (s5.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s4 = Stopwatch.StartNew();
            OutParameter1();
            s4.Stop();
            Console.WriteLine(@"Version 4: 1 out Parameter:"
                              + (s4.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s1 = Stopwatch.StartNew();
            OutParameters2();
            s1.Stop();
            Console.WriteLine(@"Version 1: 2 out Parameters:"
                              + (s1.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s2 = Stopwatch.StartNew();
            ReturnKeyValuePair();
            s2.Stop();
            Console.WriteLine(@"Version 2: return KeyValuePair:"
                              + (s2.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));

            var s3 = Stopwatch.StartNew();
            ReturnTuple();
            s3.Stop();
            Console.WriteLine(@"Version 3: return Tuple:"
                              + (s3.Elapsed.TotalMilliseconds * 1000000 / Max).ToString("0.00 ns"));
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void ReturnTuple()
        {
            var tuple = new Tuple<int, int>(0,0);
            for (int i = Max; --i >= 0;)
            {
                tuple = GetTwoNumbers3();
            if (tuple.Item1 + tuple.Item2 != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void ReturnKeyValuePair()
        {
            var pair = new KeyValuePair<int, int>();
            for (int i = Max; --i >= 0;)
            {
                pair = GetTwoNumbers2();
            if (pair.Key + pair.Value != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void OutParameters2()
        {
            int a, b = a = 0;
            for (int i = Max; --i >= 0;)
            {
                GetTwoNumbers1(out a, out b);
            if (a + b != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void OutParameter1()
        {
            int a, b = a = 0;
            for (int i = Max; --i >= 0;)
            {
                a = GetTwoNumbers4(out b);
            if (a + b != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void ReturnValueTuple()
        {
            (int, int) tuple = (0,0);
            for (int i = Max; --i >= 0;)
            {
                tuple = GetTwoNumbers5();
            if (tuple.Item1 + tuple.Item2 != Number) { throw new Exception(); }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public  void MaybeReturnOneNumber()
        {
            Maybe<int> tuple = 0;
            for (int i = Max; --i >= 0;)
            {
                tuple = GetMaybeOneNumber();
            if (tuple != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void ReturnOneNumber()
        {
            int tuple = 0;
            for (int i = Max; --i >= 0;)
            {
                tuple = GetOneNumber();
                if (tuple != Number) { throw new Exception(); }
            }
        }

        [Benchmark][MethodImpl(MethodImplOptions.NoInlining)]
        public  void ReturnNullableNumber()
        {
            int? tuple = 0;
            for (int i = Max; --i >= 0;)
            {
                tuple = GetNullableNumber();
                if (tuple != Number) { throw new Exception(); }
            }
        }
    }
}