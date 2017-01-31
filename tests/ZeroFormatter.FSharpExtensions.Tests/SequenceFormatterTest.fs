namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter

[<TestFixture>]
module SequenceFormatterTest =

  [<Test>]
  let ``fsharp list`` () =
    let input: int list = []
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<int list>(xs)
    actual |> should equal input

    let input = [1]
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<int list>(xs)
    actual |> should equal input

  [<Test; Ignore("Current ZeroFormatter resolver can not search FSharpSeqFromatter")>]
  let ``fsharp map`` () =
    let input= Map.empty<int, bool>
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<Map<int, bool>>(xs)
    actual |> should equal input

    let input = Map.empty |> Map.add 0 true
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<Map<int, bool>>(xs)
    actual |> should equal input

  [<Test; Ignore("Current ZeroFormatter resolver can not search FSharpSeqFromatter")>]
  let ``fsharp set`` () =
    let input = Seq.empty<int>
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<Set<int>>(xs)
    actual |> should equal input

    let input = Seq.singleton 1
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<Set<int>>(xs)
    actual |> should equal input
