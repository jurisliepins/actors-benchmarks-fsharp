namespace Actors.Benchmarks.FSharp

open System.Threading
open BenchmarkDotNet.Attributes
open Akka.FSharp
open BenchmarkDotNet.Running

module AkkaSystem = Akka.FSharp.System
module AkkaConfig = Akka.FSharp.Configuration

module Sequential =
    type Ping = Ping
    type Pong = Pong

    type [<Struct>] MessageStruct =
        | PingStruct
        | PongStruct
    
    let mailboxProcessorObj (actorCount: int) =
        let event = new ManualResetEvent(false);
        let rec childBody idx parentRef (mailbox: MailboxProcessor<obj>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Pong as pong -> handlePong pong
                | message -> unhandled message }
            and handlePing (ping: Ping) =
                if idx < actorCount then
                    let childRef = MailboxProcessor.Start(childBody (idx + 1) mailbox)
                    childRef.Post(ping)
                else
                    mailbox.Post(Pong)
                receive ()
            and handlePong (pong: Pong) =
                parentRef.Post(pong)
            and unhandled message =
                printfn $"Child unhandled message %A{message}"
            receive ()
        let rec parentBody (mailbox: MailboxProcessor<obj>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Pong as pong -> handlePong pong
                | message -> unhandled message }
            and handlePing (ping: Ping) =
                let childRef = MailboxProcessor.Start(childBody 1 mailbox)
                childRef.Post(ping)
                receive ()
            and handlePong (pong: Pong) =
                event.Set() |> ignore
            and unhandled message =
                printfn $"Parent unhandled message %A{message}"
            receive ()
        let parentRef = MailboxProcessor.Start(parentBody)
        parentRef.Post(Ping)
        event.WaitOne() |> ignore
        ()

    let mailboxProcessorStruct (actorCount: int) =
        let event = new ManualResetEvent(false);
        let rec childBody idx parentRef (mailbox: MailboxProcessor<MessageStruct>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | PingStruct -> return! handlePing ()
                | PongStruct -> handlePong () }
            and handlePing () =
                if idx < actorCount then
                    let childRef = MailboxProcessor.Start(childBody (idx + 1) mailbox)
                    childRef.Post(PingStruct)
                else
                    mailbox.Post(PongStruct)
                receive ()
            and handlePong () =
                parentRef.Post(PongStruct)
            receive ()
        let rec parentBody (mailbox: MailboxProcessor<MessageStruct>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | PingStruct -> return! handlePing ()
                | PongStruct -> handlePong () }
            and handlePing () =
                let childRef = MailboxProcessor.Start(childBody 1 mailbox)
                childRef.Post(PingStruct)
                receive ()
            and handlePong () =
                event.Set() |> ignore
            receive ()
        let parentRef = MailboxProcessor.Start(parentBody)
        parentRef.Post(PingStruct)
        event.WaitOne() |> ignore
        ()

    let akkaObj (actorCount: int) =
        let event = new ManualResetEvent(false)
        let system = AkkaSystem.create "akka-system" (AkkaConfig.defaultConfig ())
        let rec childBody idx (mailbox: Actor<obj>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Pong as pong -> handlePong pong
                | message -> unhandled message }
            and handlePing (ping: Ping) =
                if idx < actorCount then
                    let childRef = spawn mailbox $"child-%d{idx}" (childBody (idx + 1))
                    childRef <! ping
                else
                    mailbox.Context.Self <! Pong
                receive ()
            and handlePong (pong: Pong) =
                mailbox.Context.Parent <! pong
            and unhandled message =
                printfn $"Child unhandled message %A{message}"
            receive ()
        let rec parentBody (mailbox: Actor<obj>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Pong as pong -> handlePong pong
                | message -> unhandled message }
            and handlePing (ping: Ping) =
                let childRef = spawn mailbox $"child-%d{0}" (childBody 1)
                childRef <! ping
                receive ()
            and handlePong (pong: Pong) =
                event.Set() |> ignore
            and unhandled message =
                printfn $"Parent unhandled message %A{message}"
            receive ()
        let parentRef = spawn system "parent" (parentBody)
        parentRef <! Ping
        event.WaitOne() |> ignore
        ()
            
    let akkaStruct (actorCount: int) =
        let event = new ManualResetEvent(false)
        let system = AkkaSystem.create "akka-system" (AkkaConfig.defaultConfig ())
        let rec childBody idx (mailbox: Actor<MessageStruct>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | PingStruct -> return! handlePing ()
                | PongStruct -> handlePong () }
            and handlePing () =
                if idx < actorCount then
                    let childRef = spawn mailbox $"child-%d{idx}" (childBody (idx + 1))
                    childRef <! PingStruct
                else
                    mailbox.Context.Self <! PongStruct
                receive ()
            and handlePong () =
                mailbox.Context.Parent <! PongStruct
            receive ()
        let rec parentBody (mailbox: Actor<MessageStruct>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | PingStruct -> return! handlePing ()
                | PongStruct -> handlePong () }
            and handlePing () =
                let childRef = spawn mailbox $"child-%d{0}" (childBody 1)
                childRef <! PingStruct
                receive ()
            and handlePong () =
                event.Set() |> ignore
            receive ()
        let parentRef = spawn system "parent" (parentBody)
        parentRef <! PingStruct
        event.WaitOne() |> ignore
        ()

module Parallel =
    type Start = Start
    type Stop = Stop
    type Ping = Ping of int
    type Pong = Pong of int

    type [<Struct>] MessageStruct =
        | StartStruct
        | StopStruct
        | PingStruct of Idx: int
        | PongStruct of idx: int
    
    let mailboxProcessorObj (messageCount: int) =
        let event = new ManualResetEvent(false);
        let rec childBody (parentRef: MailboxProcessor<obj>) (mailbox: MailboxProcessor<obj>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Stop as stop -> handleStop stop
                | message -> unhandled message }
            and handlePing (Ping idx: Ping) =
                parentRef.Post(Pong idx)
                if idx >= (messageCount - 1) then
                    mailbox.Post(Stop)
                receive ()
            and handleStop (stop: Stop) =
                ()
            and unhandled message =
                printfn $"Child unhandled message %A{message}"
            receive ()
        let rec parentBody (mailbox: MailboxProcessor<obj>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | :? Start as start -> return! handleStart start
                | :? Stop as stop -> handleStop stop
                | :? Pong as pong -> return! handlePong pong
                | message -> unhandled message }
            and handleStart (start: Start) =
                let childRef = MailboxProcessor.Start(childBody mailbox)
                for message in 0..messageCount - 1 do
                    childRef.Post(Ping message)
                receive ()
            and handleStop (stop: Stop) =
                event.Set() |> ignore
            and handlePong (Pong idx: Pong) =
                if idx >= messageCount - 1 then
                    mailbox.Post(Stop)    
                    receive ()
                else
                    receive ()
            and unhandled message =
                printfn $"Parent unhandled message %A{message}"
            receive ()
        let parentRef = MailboxProcessor.Start(parentBody)
        parentRef.Post(Start)
        event.WaitOne() |> ignore
        ()
    
    let mailboxProcessorStruct (messageCount: int) =
        let event = new ManualResetEvent(false);
        let rec childBody (parentRef: MailboxProcessor<MessageStruct>) (mailbox: MailboxProcessor<MessageStruct>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | StartStruct -> return! handleStart ()
                | StopStruct -> handleStop ()
                | PingStruct idx -> return! handlePing idx
                | PongStruct idx -> handlePong idx }
            and handleStart () =
                receive ()
            and handleStop () =
                ()
            and handlePing (idx: int) =
                parentRef.Post(PongStruct idx)
                if idx >= (messageCount - 1) then
                    mailbox.Post(StopStruct)
                receive ()
            and handlePong (idx: int) =
                ()
            receive ()
        let rec parentBody (mailbox: MailboxProcessor<MessageStruct>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | StartStruct -> return! handleStart ()
                | StopStruct -> handleStop ()
                | PingStruct idx -> handlePing idx
                | PongStruct idx -> return! handlePong idx }
            and handleStart () =
                let childRef = MailboxProcessor.Start(childBody mailbox)
                for message in 0..messageCount - 1 do
                    childRef.Post(PingStruct message)
                receive ()
            and handleStop () =
                event.Set() |> ignore
            and handlePing (idx: int) =
                ()
            and handlePong (idx: int) =
                if idx >= messageCount - 1 then
                    mailbox.Post(StopStruct)                            
                receive ()
            receive ()
        let parentRef = MailboxProcessor.Start(parentBody)
        parentRef.Post(StartStruct)
        event.WaitOne() |> ignore
        ()
    
    let akkaObj (messageCount: int) =
        let event = new ManualResetEvent(false)
        let system = AkkaSystem.create "akka-system" (AkkaConfig.defaultConfig ())
        let rec childBody (mailbox: Actor<obj>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Stop as stop -> handleStop stop
                | message -> unhandled message }
            and handlePing (Ping idx: Ping) =
                mailbox.Context.Parent <! (Pong idx)
                if idx >= (messageCount - 1) then
                    mailbox.Context.Self <! Stop
                receive ()
            and handleStop (stop: Stop) =
                ()
            and unhandled message =
                printfn $"Child unhandled message %A{message}"
            receive ()
        let rec parentBody (mailbox: Actor<obj>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | :? Start as start -> return! handleStart start
                | :? Stop as stop -> handleStop stop
                | :? Pong as pong -> return! handlePong pong
                | message -> unhandled message }
            and handleStart (start: Start) =
                let childRef = spawn mailbox "child" childBody
                for message in 0..messageCount - 1 do
                    childRef <! (Ping message)
                receive ()
            and handleStop (stop: Stop) =
                event.Set() |> ignore
            and handlePong (Pong idx: Pong) =
                if idx >= messageCount - 1 then
                    mailbox.Context.Self <! Stop    
                    receive ()
                else
                    receive ()
            and unhandled message =
                printfn $"Parent unhandled message %A{message}"
            receive ()
        let parentRef = spawn system "parent" parentBody
        parentRef <! Start
        event.WaitOne() |> ignore
        ()
    
    let akkaStruct (messageCount: int) =
        let event = new ManualResetEvent(false)
        let system = AkkaSystem.create "akka-system" (AkkaConfig.defaultConfig ())
        let rec childBody (mailbox: Actor<MessageStruct>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | StartStruct -> return! handleStart ()
                | StopStruct -> handleStop ()
                | PingStruct idx -> return! handlePing idx
                | PongStruct idx -> handlePong idx }
            and handleStart () =
                receive ()
            and handleStop () =
                ()
            and handlePing (idx: int) =
                mailbox.Context.Parent <! (PongStruct idx)
                if idx >= (messageCount - 1) then
                    mailbox.Context.Self <! StopStruct
                receive ()
            and handlePong (idx: int) =
                ()
            receive ()
        let rec parentBody (mailbox: Actor<MessageStruct>) =
            let rec receive () = actor {
                match! mailbox.Receive() with
                | StartStruct -> return! handleStart ()
                | StopStruct -> handleStop ()
                | PingStruct idx -> handlePing idx
                | PongStruct idx -> return! handlePong idx }
            and handleStart () =
                let childRef = spawn mailbox "child" childBody
                for message in 0..messageCount - 1 do
                    childRef <! (PingStruct message)
                receive ()
            and handleStop () =
                event.Set() |> ignore
            and handlePing (idx: int) =
                ()
            and handlePong (idx: int) =
                if idx >= messageCount - 1 then
                    mailbox.Context.Self <! StopStruct                            
                receive ()
            receive ()
        let parentRef = spawn system "parent" parentBody
        parentRef <! StartStruct
        event.WaitOne() |> ignore
        ()

[<MemoryDiagnoser>]
type SequentialBenchmarks() =
    
    [<Params(1, 10)>]
    member val ActorCount = 0 with get, set
    
    [<Benchmark>]
    member __.MailboxProcessorObj() = Sequential.mailboxProcessorObj __.ActorCount
    
    [<Benchmark>]
    member __.MailboxProcessorStruct() = Sequential.mailboxProcessorStruct __.ActorCount
    
    [<Benchmark>]
    member __.AkkaObj() = Sequential.akkaObj __.ActorCount
    
    [<Benchmark>]
    member __.AkkaStruct() = Sequential.akkaStruct __.ActorCount
    
[<MemoryDiagnoser>]
type ParallelBenchmarks() =
    
    [<Params(1, 10, 1000, 10_000, 100_000)>]
    member val MessageCount = 0 with get, set
    
    [<Benchmark>]
    member __.MailboxProcessorObj() = Parallel.mailboxProcessorObj __.MessageCount
    
    [<Benchmark>]
    member __.MailboxProcessorStruct() = Parallel.mailboxProcessorStruct __.MessageCount
    
    [<Benchmark>]
    member __.AkkaObj() = Parallel.akkaObj __.MessageCount
    
    [<Benchmark>]
    member __.AkkaStruct() = Parallel.akkaStruct __.MessageCount
    
module Program =
    [<EntryPoint>]
    let main args =
        // BenchmarkRunner.Run<SequentialBenchmarks>() |> ignore
        BenchmarkRunner.Run<ParallelBenchmarks>() |> ignore
        0
        