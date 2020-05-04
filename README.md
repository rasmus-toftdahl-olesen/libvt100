# libvt100
![](https://github.com/rasmus-toftdahl-olesen/libvt100/workflows/.NET%20Core/badge.svg)

A purely .net/C# library for parsing a VT100/ANSI stream

When writing anything that needs to communicate with a terminal in some way it is almost always speaking some dialect of VT100 or ANSI.

This library aims solely at parsing a stream of VT100/ANSI data and then letting the host application do the rendering. Many other project also parse VT100/ANSI data but their parser is always tangled up with the actual rendering of the data, making reuse in other projects problematic.

## Projects Using libvt100:

* [winprint](https://github.com/tig/winprint) - Uses libvt100 to parse and print source code files that have been 'syntax highlighted' by [Pygments](https://github.com/pygments) and output using Pygment's `terminal256 Formatter`. The `terminal256` formatter generates ANSI escapes for formatting. This code takes an input document:

```csharp
_screen = new DynamicScreen(_minLineLen);
    IAnsiDecoder vt100 = new AnsiDecoder();
    vt100.Encoding = Encoding;
    vt100.Subscribe(_screen);
    var bytes = vt100.Encoding.GetBytes(Document);
    if (bytes != null && bytes.Length > 0) {
        vt100.Input(bytes);
    }
```

Then the `winprint` rendering engine simply enumerates over this (psuedocode):

```csharp
    foreach (var line in _screen){
        SmartlyDrawLine(line);
    }
```

Real code is here:

https://github.com/tig/winprint/blob/eee9768506f08e2f5af75f90d9571dc94d8d42fa/src/WinPrint.Core/ContentTypeEngines/AnsiCte.cs#L196

A cool thing about this is features like line-wrap and `/t` expansion are handled by `libvt100`.

Hopefully other projects will start using libvt100 and we can stop reinventing the wheel in each project.
