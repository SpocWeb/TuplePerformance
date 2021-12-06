# TuplePerformance
Simple Project to Benchmark and compare 8 alternative Implementations to return 2 Values from a .NET Function in C#. 

Surprisingly .NET applies heavy Optimizations on ValueTuple and KeyValuePair,
bringing down Execution Time by a Factor of 34 in Release-Mode!

In Release-Mode all Implementations have similar Speed, 
except for returning Tuple{int,int} 
which is 10 times slower. 

In Debug Mode only Methods using Out-Parameters are fast. 
The relative Speed-Factor from Debug to Release Build is given in the last Column '*'

|               Method |    Release |      Debug | * |
|:-------------------- |-----------:|-----------:|--:|
|Return Tuple          |  510.84 ms | 1,515.2 ms |  3|
|Return KeyValuePair   |   44.56 ms | 1,527.1 ms | 34|
|Return ValueTuple     |   51.28 ms | 1,418.6 ms | 28|
|Return NullableValue  |   48.41 ms | 1,527.0 ms | 29|
|2 out Parameters      |   43.83 ms |   560.4 ms | 14|
|1 out Parameter       |   48.41 ms |   586.5 ms | 13|
|Return One Value      |   49.72 ms |   523.8 ms | 11|

