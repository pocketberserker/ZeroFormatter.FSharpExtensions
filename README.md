# ZeroFormatter Extensions for FSharp

[![Build status](https://ci.appveyor.com/api/projects/status/8471jv6ayhgfvcpr/branch/master?svg=true)](https://ci.appveyor.com/project/pocketberserker/zeroformatter-fsharpextensions/branch/master)

ZeroFormatter.FSharpExtensions is a [ZeroFormatter](https://github.com/neuecc/ZeroFormatter) extension library for F#.

## Usage

```fsharp
open ZeroFormatter

ZeroFormatter.FSharp.Register()

// define type and initialize value ...

ZeroFormatterSerializer.Serialize(value)
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

