namespace ZeroFormatter.Tests

open System
open FsUnit
open NUnit.Framework
open ZeroFormatter
open ZeroFormatter.FSharp

[<TestFixture>]
module UnitFormatterTest =

  [<Test>]
  let ``unit value`` () =
    let xs = ZeroFormatterSerializer.Serialize<unit>(())
    let actual = ZeroFormatterSerializer.Deserialize<unit>(xs)
    actual |> should equal ()
