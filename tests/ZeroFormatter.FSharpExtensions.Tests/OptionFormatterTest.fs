namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter

[<TestFixture>]
module OptionFormatterTest =

  [<SetUp>]
  let setup () =
    ZeroFormatter.FSharp.Register<Formatters.DefaultResolver>()

  [<ZeroFormattable; Struct>]
  type MyStruct(x: int, y: int64, z: float32) =

    [<Index(0)>]
    member __.MyProperty1 with get(): int = x
    [<Index(1)>]
    member __.MyProperty2 with get(): int64 = y
    [<Index(2)>]
    member __.MyProperty3 with get(): float32 = z

  [<Test>]
  let ``struct some`` () =
    let input = Some(MyStruct(100, 99999999L, -123.43f))
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<MyStruct option>(xs)
    actual |> should equal input

  [<Test>]
  let ``struct none`` () =
    let xs = ZeroFormatterSerializer.Serialize<MyStruct option>(None)
    let actual = ZeroFormatterSerializer.Deserialize<MyStruct option>(xs)
    actual |> should equal None

  [<ZeroFormattable>]
  type MyObject() =

    abstract member MyProperty1: int with get, set
    [<Index(0)>]
    default val MyProperty1 = 0 with get, set

    abstract member MyProperty2: int64 with get, set
    [<Index(1)>]
    default val MyProperty2 = 0L with get, set

    abstract member MyProperty3: float32 with get, set
    [<Index(2)>]
    default val MyProperty3 = 0.f with get, set

  [<Test>]
  let ``object some`` () =
    let input = MyObject(MyProperty1 = 100, MyProperty2 = 99999999L, MyProperty3 = -123.43f)
    let xs = ZeroFormatterSerializer.Serialize(Some input)
    match ZeroFormatterSerializer.Deserialize<MyObject option>(xs) with
    | Some actual ->
      actual.MyProperty1 |> should equal input.MyProperty1
      actual.MyProperty2 |> should equal input.MyProperty2
      actual.MyProperty3 |> should equal input.MyProperty3
    | None -> Assert.Fail("expected Some, but was None")

  [<Test>]
  let ``object none`` () =
    let xs = ZeroFormatterSerializer.Serialize<MyObject option>(None)
    let actual = ZeroFormatterSerializer.Deserialize<MyObject option>(xs)
    actual |> should equal None
