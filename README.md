# ZeroFormatter Extensions for FSharp

[![Build status](https://ci.appveyor.com/api/projects/status/8471jv6ayhgfvcpr/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/zeroformatter-fsharpextensions/branch/master)
[![Build Status](https://travis-ci.org/pocketberserker/ZeroFormatter.FSharpExtensions.svg?branch=master)](https://travis-ci.org/pocketberserker/ZeroFormatter.FSharpExtensions)
[![NuGet Status](http://img.shields.io/nuget/v/ZeroFormatter.FSharpExtensions.svg?style=flat)](https://www.nuget.org/packages/ZeroFormatter.FSharpExtensions/)

ZeroFormatter.FSharpExtensions is a [ZeroFormatter](https://github.com/neuecc/ZeroFormatter) extension library for F#.

## Usage

```fsharp
open ZeroFormatter
open ZeroFormatter.FSharp

[<ZeroFormattable>]
type MyRecord = {
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

let mr = {
  Age = 99
  FirstName = "hoge"
  LastName = "huga"
  List = [ 1; 10; 100 ]
}

let bytes = ZeroFormatterSerializer.Serialize(mr);
let mr2 = ZeroFormatterSerializer.Deserialize<MyRecord>(bytes)
printfn "%s" mr2.FullName

type Character =
  | Human of name : string * birth : DateTime * age : int * faith : int
  | Monster of race : string * power : int * magic : int

let daemon = Monster("Demon", 9999, 1000)
let data = ZeroFormatterSerializer.Serialize(daemon)
match ZeroFormatterSerializer.Deserialize(data) with
| Human(name, birth, age, faith) ->
  ...
| Monster(race, power, magic) ->
  ...
```

## Null safety

You can use `'T option` instead of `Nullable<'T>` or `null`.

## Supported types

| F# | WireFormat | Note |
| --- | ---------- | ---- |
| Record | Struct | versioning is not supported. |
| DU | Union | versioning is not supported. |
| 'T list | Sequence | |
| Map<'K, 'V> | Sequence | |
| Set<'T> | Sequence | |
| unit | Int32 | always -1 |

