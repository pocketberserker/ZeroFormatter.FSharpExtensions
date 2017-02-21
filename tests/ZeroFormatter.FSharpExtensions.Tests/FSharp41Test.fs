namespace ZeroFormatter.Tests

open FsUnit
open NUnit.Framework
open ZeroFormatter
open ZeroFormatter.FSharp

[<TestFixture>]
module FSharp41Test =

  [<Test>]
  let ``struct tuple`` () =
    let input = struct (1, 2)
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<struct (int * int)>(xs)
    |> should equal input

  [<Test>]
  let ``value tuple some`` () =
    let input = Some(struct (100, 99999999L, -123.43))
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<(struct (int * int64 * float)) option>(xs)
    |> should equal input

  [<Test>]
  let ``value tuple none`` () =
    let xs = ZeroFormatterSerializer.Serialize<(struct (int * int64 * float)) option>(None)
    ZeroFormatterSerializer.Deserialize<(struct (int * int64 * float)) option>(xs)
    |> should equal None

  [<ZeroFormattable; Struct>]
  type MyRecord = {
    [<Index(0)>]
    MyProperty1: int
    [<Index(1)>]
    MyProperty2: int64
    [<Index(2)>]
    MyProperty3: float32
  }

  [<Test>]
  let ``struct record`` () =
    let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
    |> should equal input

  [<Struct>]
  type TestUnion =
    | A
    | B of int
    | C of int64 * float32

  [<Test>]
  let ``struct union`` () =
    let input = A
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    |> should equal input

    let input = B 100
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    |> should equal input

    let input = C(99999999L, -123.43f)
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    |> should equal input

  [<Test>]
  let ``result(struct generic union)`` () =
    let input: Result<int, int> = Ok 1
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<Result<int, int>>(xs)
    |> should equal input

    let input: Result<int, int> = Error 2
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<Result<int, int>>(xs)
    |> should equal input
