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

  [<Union(typeof<CA>, typeof<CB>, typeof<CC>)>]
  type TestUnionC =
    [<UnionKey>]
    abstract member Key: int

  and [<ZeroFormattable; Struct>] CA =
    [<IgnoreFormat>]
    member __.Key = 0

    interface TestUnionC with
      [<IgnoreFormat>]
      override this.Key = this.Key

  and [<ZeroFormattable; Struct>] CB(v: int) =
    [<Index(0)>]
    member __.V = v
    [<IgnoreFormat>]
    member __.Key = 1

    interface TestUnionC with
      [<IgnoreFormat>]
      override this.Key = this.Key

  and [<ZeroFormattable; Struct>] CC(v0: int64, v1: float32) =
    [<Index(0)>]
    member __.V0 = v0
    [<Index(1)>]
    member __.V1 = v1
    [<IgnoreFormat>]
    member __.Key = 2

    interface TestUnionC with
      [<IgnoreFormat>]
      override this.Key = this.Key

  [<Test>]
  let compatibility () =

    let input = A
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnionC>(xs)
    match actual with
    | :? CA ->
      Assert.Pass()
    | v ->
      Assert.Fail(sprintf "expected B, but was: %A" v)

    let xs = ZeroFormatterSerializer.Serialize(actual)
    match ZeroFormatterSerializer.Deserialize<TestUnion>(xs) with
    | A ->
      Assert.Pass()
    | v ->
      Assert.Fail(sprintf "expected A, but was: %A" v)

    let input = B 100
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnionC>(xs)
    match actual with
    | :? CB as actual ->
      actual.V |> should equal 100
    | v ->
      Assert.Fail(sprintf "expected CB, but was: %A" v)

    let xs = ZeroFormatterSerializer.Serialize(actual)
    match ZeroFormatterSerializer.Deserialize<TestUnion>(xs) with
    | B v ->
      let expected = actual :?> CB
      v |> should equal expected.V
    | v ->
      Assert.Fail(sprintf "expected B, but was: %A" v)

    let input = C(99999999L, -123.43f)
    let xs = ZeroFormatterSerializer.Serialize(input)
    let actual = ZeroFormatterSerializer.Deserialize<TestUnionC>(xs)
    match actual with
    | :? CC as actual ->
      actual.V0 |> should equal 99999999L
      actual.V1 |> should equal -123.43f
    | v ->
      Assert.Fail(sprintf "expected CC, but was: %A" v)

    let xs = ZeroFormatterSerializer.Serialize(actual)
    match ZeroFormatterSerializer.Deserialize<TestUnion>(xs) with
    | C(v0, v1) ->
      let expected = actual :?> CC
      v0 |> should equal expected.V0
      v1 |> should equal expected.V1
    | v ->
      Assert.Fail(sprintf "expected C, but was: %A" v)
