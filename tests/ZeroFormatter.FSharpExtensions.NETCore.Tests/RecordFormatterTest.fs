module ZeroFormatter.Tests.RecordFormatterTest

open System
open Xunit
open ZeroFormatter
open ZeroFormatter.FSharp

[<ZeroFormattable>]
type MyRecord = {
  [<Index(0)>]
  MyProperty1: int
  [<Index(1)>]
  MyProperty2: int64
  [<Index(2)>]
  MyProperty3: float32
}

[<Fact>]
let ``simple record`` () =
  let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
  Assert.Equal(input, actual)
