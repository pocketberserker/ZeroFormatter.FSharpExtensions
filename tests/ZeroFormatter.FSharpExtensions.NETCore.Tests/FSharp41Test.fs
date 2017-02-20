module ZeroFormatter.Tests.FSharp41Test

open Xunit
open ZeroFormatter
open ZeroFormatter.FSharp

[<Fact>]
let ``struct tuple`` () =
  let input = struct (1, 2)
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<struct (int * int)>(xs)
  Assert.Equal(input, actual)

[<Fact>]
let ``value tuple some`` () =
  let input = Some(struct (100, 99999999L, -123.43))
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<(struct (int * int64 * float)) option>(xs)
  Assert.Equal(input, actual)

[<Fact>]
let ``value tuple none`` () =
  let xs = ZeroFormatterSerializer.Serialize<(struct (int * int64 * float)) option>(None)
  let actual = ZeroFormatterSerializer.Deserialize<(struct (int * int64 * float)) option>(xs)
  Assert.Equal(None, actual)

[<ZeroFormattable; Struct>]
type MyRecord = {
  [<Index(0)>]
  MyProperty1: int
  [<Index(1)>]
  MyProperty2: int64
  [<Index(2)>]
  MyProperty3: float32
}

[<Fact>]
let ``struct record`` () =
  let input = { MyProperty1 = 100; MyProperty2 = 99999999L; MyProperty3 = -123.43f }
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<MyRecord>(xs)
  Assert.Equal(input.MyProperty1, actual.MyProperty1)
  Assert.Equal(input.MyProperty2, actual.MyProperty2)
  Assert.Equal(input.MyProperty3, actual.MyProperty3)
