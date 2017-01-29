namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter

[<TestFixture>]
module DiscriminatedUnionFormatterTest =

  [<SetUp>]
  let setup () =
    ZeroFormatter.FSharp.Register<Formatters.DefaultResolver>()

  type TestUnion =
    | A
    | B of int
    | C of int64 * float32

  [<Test>]
  let ``discriminated union`` () =

    let input = A
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    actual |> should equal input

    let input = B 100
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    actual |> should equal input

    let input = C(99999999L, -123.43f)
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnion>(xs)
    actual |> should equal input
