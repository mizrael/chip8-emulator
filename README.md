# Chip8 emulator
This is a simple Chip8 emulator written in C# with .NET5.

Roms were downloaded from https://www.zophar.net/pdroms/chip8.html.

## Rendering
I managed to get a [MonoGame version](/Chip8Emulator.MonoGame) up and running along with a [Blazor WASM](Chip8Emulator.BlazorWasm) one as well.

I deployed the Blazor version on Github Pages, you can find it here: https://mizrael.github.io/chip8-emulator/.

The deployment was handled using the approach explained [here](https://www.davidguida.net/how-to-deploy-blazor-webassembly-on-github-pages-using-github-actions/?share=facebook).

## Keyboard layout
This is the original keyboard layout of the Chip8:
| 1 	| 2 	| 3 	| C 	|
|---	|---	|---	|---	|
| 4 	| 5 	| 6 	| D 	|
| 7 	| 8 	| 9 	| E 	|
| A 	| 0 	| B 	| F 	|


which I mapped to this configuration instead:
| 1 	| 2 	| 3 	| 4 	|
|---	|---	|---	|---	|
| Q 	| W 	| E 	| R 	|
| A 	| S 	| D 	| F 	|
| Z 	| X 	| C 	| V 	|