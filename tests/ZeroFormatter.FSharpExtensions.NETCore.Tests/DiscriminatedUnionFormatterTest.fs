module ZeroFormatter.Tests.DiscriminatedUnionFormatterTest

open System
open Xunit
open ZeroFormatter
open ZeroFormatter.FSharp

type TestUnion =
  | A
  | B of int
  | C of int64 * float32

[<Fact>]
let ``discriminated union`` () =

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
