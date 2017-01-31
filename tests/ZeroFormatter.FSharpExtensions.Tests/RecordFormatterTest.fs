namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter

[<TestFixture>]
module RecordFormatterTest =

  [<SetUp>]
  let setup () =
    ZeroFormatter.FSharp.Register<Formatters.DefaultResolver>()

  //[<ZeroFormattable>]
  type MyRecord = {
    [<Index(0)>]
    MyProperty1: int
    [<Index(1)>]
    MyProperty2: int64
    [<Index(2)>]
    MyProperty3: float32
  }

  [<Test>]
  let record () =
    let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
    |> should equal input

  [<ZeroFormattable; Struct>]
  type MyStruct(v1: int, v2: int64, v3: float32) =
    [<Index(0)>]
    member __.MyProperty1 = v1
    [<Index(1)>]
    member __.MyProperty2 = v2
    [<Index(2)>]
    member __.MyProperty3 = v3

  [<Test>]
  let compatibility () =
    let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<MyStruct>(xs)
    actual.MyProperty1 |> should equal input.MyProperty1
    actual.MyProperty2 |> should equal input.MyProperty2
    actual.MyProperty3 |> should equal input.MyProperty3

    let xs = ZeroFormatterSerializer.Serialize(actual)
    ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
    |> should equal input
