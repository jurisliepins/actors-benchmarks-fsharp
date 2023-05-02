namespace Actors.Benchmarks.FSharp

open System.Threading
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

module Sequential =
    type Ping = Ping
    type Pong = Pong
    let mailboxProcessor (count: int) =
        let event = new ManualResetEvent(false);
        let rec childBody idx parentRef (mailbox: MailboxProcessor<obj>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | :? Ping as ping -> return! handlePing ping
                | :? Pong as pong -> handlePong pong
                | message -> unhandled message }
            and handlePing (ping: Ping) =
                if idx < count then
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
                let childRef = MailboxProcessor.Start(childBody 0 mailbox)
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
        
    type [<Struct>] PingStruct = PingStruct
    type [<Struct>] PongStruct = PongStruct
    type [<Struct>] MessageStruct = PingStruct | PongStruct 
    let mailboxProcessorStruct (count: int) =
        let event = new ManualResetEvent(false);
        let rec childBody idx parentRef (mailbox: MailboxProcessor<MessageStruct>) =
            let rec receive () = async {
                match! mailbox.Receive() with
                | PingStruct -> return! handlePing ()
                | PongStruct -> handlePong () }
            and handlePing () =
                if idx < count then
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
                let childRef = MailboxProcessor.Start(childBody 0 mailbox)
                childRef.Post(PingStruct)
                receive ()
            and handlePong () =
                event.Set() |> ignore
            receive ()
        let parentRef = MailboxProcessor.Start(parentBody)
        parentRef.Post(PingStruct)
        event.WaitOne() |> ignore
        ()

[<MemoryDiagnoser>]
type SequentialBenchmarks() =
    
    [<Params(1, 10, 100, 1_000, 10_000, 100_000, 1_000_000)>]
    member val Count = 0 with get, set
    
    [<Benchmark>]
    member __.MailboxProcessor() = Sequential.mailboxProcessor __.Count
    
    [<Benchmark>]
    member __.MailboxProcessorStruct() = Sequential.mailboxProcessorStruct __.Count
    
module Program =
    [<EntryPoint>]
    let main args =
        BenchmarkRunner.Run<SequentialBenchmarks>() |> ignore
        0