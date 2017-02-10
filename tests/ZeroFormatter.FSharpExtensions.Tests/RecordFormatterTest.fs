namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter
open ZeroFormatter.FSharp

[<TestFixture>]
module RecordFormatterTest =

  [<ZeroFormattable>]
  type MyRecord = {
    [<Index(0)>]
    MyProperty1: int
    [<Index(1)>]
    MyProperty2: int64
    [<Index(2)>]
    MyProperty3: float32
  }

  [<Test>]
  let ``simple record`` () =
    let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
    |> should equal input

  [<ZeroFormattable>]
  type MutableRecord = {
    [<Index(0)>]
    mutable Value: int
  }

  [<Test>]
  let ``mutable record`` () =
    let input = { Value = 100 }
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MutableRecord>(xs)
    |> should equal input

    input.Value <- -1
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MutableRecord>(xs)
    |> should equal input

  [<ZeroFormattable>]
  type MyRecord2 = {
    [<Index(0)>]
    Age: int
    [<Index(1)>]
    FirstName: string
    [<Index(2)>]
    LastName: string
    [<Index(3)>]
    List: int list
  }
  with
    member this.FullName = this.FirstName + this.LastName

  [<Test>]
  let ``record with ignore property`` () =
    let input = {
      Age = 99
      FirstName = "hoge"
      LastName = "huga"
      List = [ 1; 10; 100 ]
    }
    let xs = ZeroFormatterSerializer.Serialize(input)
    ZeroFormatterSerializer.Deserialize<MyRecord2>(xs)
    |> should equal input
