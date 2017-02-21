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

[<Struct>]
type TestUnion =
  | A
  | B of int
  | C of int64 * float32

[<Fact>]
let ``struct union`` () =
  let input = A
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
  Assert.Equal(input, actual)

  let input = B 100
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
  Assert.Equal(input, actual)

  let input = C(99999999L, -123.43f)
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
  Assert.Equal(input, actual)

[<Fact>]
let ``result(struct generic union)`` () =
  let input: Result<int, int> = Ok 1
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<Result<int, int>>(xs)
  Assert.Equal(input, actual)

  let input: Result<int, int> = Error 2
  let xs = ZeroFormatterSerializer.Serialize(input)
  let actual = ZeroFormatterSerializer.Deserialize<Result<int, int>>(xs)
  Assert.Equal(input, actual)
