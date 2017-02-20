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
