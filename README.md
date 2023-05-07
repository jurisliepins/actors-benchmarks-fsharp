# Actors Benchmarks FSharp

Benchmarking the native FSharp MailboxProcessor against Akka.NET. 

2 types of message benchmarks for each actor framework: obj where messages are regular reference types and struct where messages are structs.

## Sequential Benchmark

Parent actor creates a single child actor and sends it a `ping` message. Child actor creates another child actor. This continues recursivery until `ActorCount` number of children have been created. Once the last child is created it sends a `pong` message to its parent. That parent sends a `pong` to its parent and so on. Once `pong` reaches the original parent, the test ends. 
```
parent -ping-> child 1 -ping-> ... -ping-> child N
       <-pong-         <-pong-     <-pong-
```
This scenario is optimised for heavy actor creation rather than a large volume of messages. 

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

## Parallel Benchmark

Parent actor creates a single child actor and sends it `MessageCount` number of `ping` messages and waits for it to reply with a `pong` for each of those. Once all `pong`s are received, the test ends.
```
parent -ping 1-> child 
       <-pong 1-
         ...
       -ping N->
       <-pong N-
```
This scenario is optimised for a large volume of message passing and light actor creation.

|                 Method | MessageCount |          Mean |         Error |        StdDev |      Gen0 |     Gen1 |    Gen2 |   Allocated |
|----------------------- |------------- |--------------:|--------------:|--------------:|----------:|---------:|--------:|------------:|
|    MailboxProcessorObj |            1 |      15.38 us |      0.248 us |      0.232 us |    1.3733 |   0.0153 |       - |      5.6 KB |
| MailboxProcessorStruct |            1 |      16.31 us |      0.228 us |      0.213 us |    1.3733 |   0.0305 |       - |     5.66 KB |
|                AkkaObj |            1 | 163,845.80 us | 26,321.808 us | 77,610.445 us |  765.6250 |  62.5000 | 15.6250 |  4587.27 KB |
|             AkkaStruct |            1 |  83,111.84 us | 12,765.682 us | 37,439.534 us |  781.2500 |  93.7500 | 31.2500 |  4589.17 KB |
|    MailboxProcessorObj |           10 |      18.62 us |      0.194 us |      0.172 us |    3.9368 |        - |       - |     16.1 KB |
| MailboxProcessorStruct |           10 |      22.50 us |      0.215 us |      0.191 us |    3.9673 |        - |       - |    16.13 KB |
|                AkkaObj |           10 | 201,306.15 us | 27,442.231 us | 80,914.037 us |  875.0000 | 265.6250 | 15.6250 |  4591.32 KB |
|             AkkaStruct |           10 | 157,865.50 us | 21,671.845 us | 61,125.825 us |  828.1250 | 140.6250 | 15.6250 |  4590.06 KB |
|    MailboxProcessorObj |         1000 |     519.84 us |     11.831 us |     34.136 us |  282.2266 |   0.9766 |       - |  1149.84 KB |
| MailboxProcessorStruct |         1000 |     724.30 us |     14.468 us |     22.947 us |  281.2500 |        - |       - |  1143.74 KB |
|                AkkaObj |         1000 | 177,509.96 us | 20,969.592 us | 61,829.315 us |  812.5000 |  62.5000 | 15.6250 |  4868.87 KB |
|             AkkaStruct |         1000 |  78,787.98 us | 10,848.073 us | 31,985.787 us |  781.2500 |  93.7500 | 31.2500 |  4884.97 KB |
|    MailboxProcessorObj |        10000 |   5,536.28 us |    104.458 us |    185.673 us | 2789.0625 | 992.1875 | 70.3125 | 11551.19 KB |
| MailboxProcessorStruct |        10000 |   6,400.27 us |    123.432 us |    160.496 us | 2757.8125 |  85.9375 | 85.9375 | 11525.91 KB |
|                AkkaObj |        10000 | 163,407.95 us | 15,927.082 us | 46,961.361 us | 1343.7500 |  93.7500 |       - |  7317.43 KB |
|             AkkaStruct |        10000 | 134,991.03 us | 17,028.025 us | 50,207.516 us | 1406.2500 |  31.2500 |       - |  7484.87 KB |
