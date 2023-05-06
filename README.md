# Actors Benchmarks FSharp

Benchmarking the native FSharp MailboxProcessor against Akka.NET. 

## Sequential Benchmark

Single parent actor creates a single child actor and sends it a `ping` message. Child actor creates another child actor. This continues recursivery until `ActorCount` number of children have been created. Once the last child is created it sends a `pong` message to its parent. That parent sends a `pong` to its parent and so on. Once `pong` reaches the original parent, the test ends. 
```
parent -ping-> child 1 -ping-> ... -ping-> child N
       <-pong-         <-pong-     <-pong-
```
This scenario is optimised of heavy actor creation rather than a large volume of messages. 

There are 2 types of message benchmarks for each actor framework:
* Obj where messages are regular reference types.
* Struct where messages are structs.

|                 Method | ActorCount |          Mean |         Error |         StdDev |        Median |     Gen0 |    Gen1 |    Gen2 |  Allocated |
|----------------------- |----------- |--------------:|--------------:|---------------:|--------------:|---------:|--------:|--------:|-----------:|
|    MailboxProcessorObj |          1 |      13.15 us |      0.231 us |       0.216 us |      13.20 us |   1.2360 |  0.0153 |       - |    5.02 KB |
| MailboxProcessorStruct |          1 |      12.76 us |      0.231 us |       0.216 us |      12.80 us |   1.2207 |  0.0153 |       - |    4.98 KB |
|                AkkaObj |          1 | 176,599.95 us | 19,989.916 us |  56,053.869 us | 177,267.60 us | 765.6250 | 93.7500 | 15.6250 | 4587.36 KB |
|             AkkaStruct |          1 | 168,768.66 us | 28,624.823 us |  83,951.651 us | 199,021.42 us | 781.2500 | 93.7500 | 15.6250 | 4588.36 KB |
|    MailboxProcessorObj |         10 |     109.32 us |      2.061 us |       2.205 us |     108.65 us |   7.2021 |  0.1221 |       - |   29.14 KB |
| MailboxProcessorStruct |         10 |     120.79 us |      2.404 us |       6.859 us |     121.69 us |   7.0801 |       - |       - |   28.98 KB |
|                AkkaObj |         10 | 249,109.24 us | 40,767.854 us | 119,565.059 us | 231,851.40 us | 796.8750 | 78.1250 | 15.6250 | 4775.69 KB |
|             AkkaStruct |         10 |  54,395.27 us |  6,167.175 us |  17,892.092 us |  55,149.53 us | 812.5000 |       - |       - | 4775.95 KB |

