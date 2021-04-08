## A list of built-in C# program classes.

To understand what these program classes do, read [this guide](https://github.com/Epicguru/DiscoRimworldMod/blob/master/AddingFloorSequences.md).
Tips:
* See [this file](../blob/master/Defs/DiscoPrograms.xml) for examples of all built-in program classes being used.
* There are 60 ticks in a second.
* Colors are represented as vectors: (1, 0, 0.5, 1) means 100% red, 0% green, 50% blue and 100% alpha.

C# class name | Description | Inputs | Source code
--- | --- | --- | :---: 
`Disco.Programs.Solid` | Fills the dance floor with a single color. | **color:** The single color to use. *Default: (1, 1, 1, 1)* | [Link](./Source/Disco!/Programs/Solid.cs)
`Disco.Programs.Noise` | Fills the floor with [perlin noise](https://en.wikipedia.org/wiki/Perlin_noise). | **scale:** How 'smooth' the noise is. Lower values are smoother. *Default: 2*</br><br>**add:** How much to add on to each pixel. Higher values make the noise whiter. *Default: 0*</br><br>**multi:** How much to multiply each pixel by. Larger values increase difference between brightest and darkest pixels. *Default: 1*</br> | [Link](./Source/Disco!/Programs/Noise.cs)
`Disco.Programs.Fade`  | Fades from white to transparent or the other way around. Only useful when using the *multiply* blend mode. | **duration:** The duration of the fade, in ticks. *Default: 30*</br><br>**fadeIn:** Fade in (transparent to white) or out (white to transparent). *Default: true* | [Link](./Source/Disco!/Programs/Fade.cs)
`Disco.Programs.Ripple`| A moving 'ripple' that travels outwards or inwards. | **lowColor:** The floor color wherever there is not a ripple. *Default: (1, 1, 1, 0)*</br><br>**highColor:** The color of the ripple. *Default: (1, 1, 1, 1)*</br><br>**startRadius:** The initial radius of the ripple, measured in cells. *Default: -2*</br><br>**thickness:** How wide the ripple is, in cells. *Default: 2*</br><br>**radiusChangePerTick:** How much the radius changes each tick. Postive values move outwards, negative move inwards. *Default: 0.1*</br><br>**despawnAfterRadiusReaches:** The ripple will disapear and the program will end once the ripple reaches this radius. *Default: 22*</br><br>**circular:** If true, ripple is a circle. If false, it is a diamon shape. *Default: true*</br> | [Link](./Source/Disco!/Programs/Ripple.cs)
`Disco.Programs.ColorCycle`| Smoothly transitions between a sequence of colors. | **transitionTime:** The time, in ticks, between one color and the next. *Default: 80*</br><br>**colors:** *[list]* A list of the colors to transition through. Must have at least 2 colors. The final color will transition back to the first color. | [Link](./Disco!/Programs/ColorCycle.cs)

This is WIP documentation, and is incomplete as of 08/04/2021.
