# 🔎 Stencil Debugger for Unity's Universal URP

Stencil Debugger is a utility for visualizing the stencil buffer in Unity URP. This is useful for debugging purposes when working on rendering effects that make use of the stencil buffer.

![Stencil Debugger](Assets~/Images/stencil.png)
## Installation

1. Open the package manager and select the `Install package from git URL...` option found under the top left dropdown.
    ![From Git URL](Assets~/Images/giturl.png)
2. Enter the following link `https://github.com/alexanderameye/stencil-debugger.git`.
    ![Git Input URL](Assets~/Images/gitinput.png)
3. Click `Install`.

## Usage

After importing the package, you can check compatibility with your project through the *Window > Stencil Debugger > Compatibility* window.

- Add the Stencil Debug Renderer Feature to your renderer

![Renderer Feature](Assets~/Images/rendererfeature.png)

## Limitations

- Only tested on Unity 6 and Unity 2022, including support for Render Graph.
- Only allows for up to 10 stencil values to be displayed.
- Not optimized for performance. In fact, the implementation is brute force/naïve. Only use it in the editor (it's for debugging) and do not include it in your build.

## Attributions

The `draw_digit` method in the `StencilDebug` shader for drawing digits was written by [Freya Holmér](https://gist.github.com/FreyaHolmer/71717be9f3030c1b0990d3ed1ae833e3).
 
## Contact

[@ameye.dev](https://bsky.app/profile/ameye.dev) • [https://ameye.dev](https://ameye.dev)

> 💛 Feeling appreciative of this free package? Check out [Linework](https://assetstore.unity.com/packages/slug/294140?aid=1011l3n8v&pubref=stencil-debugger) 💛

