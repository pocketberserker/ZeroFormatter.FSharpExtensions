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
