## Intro
In order to complete these steps you will need to have a good text editor (VSCode, Notepad++ etc.) and ideally an understanding of XML and/or Rimworld's Def system.

There are two important concepts to understand first:

* **Disco program**: A 'layer' that affects the disco floor. Most disco programs change the color of the dance floor, but some might not (such as the *Music* programs).
* **Disco sequence**: A sequence of disco programs arranged in an interesting way. For example, a sequence could add a checkerboard program, then a color program, and finally a song program.

## Setting up a mod
First you must create an extension mod that will add the custom floor programs and sequences.
Here are the steps:
1. [Click here to download this mod template.](https://github.com/Epicguru/DiscoRimworldMod/raw/master/Example%20Disco%20Extension%20Mod.zip)
2. Open up steam, right click on Rimworld. Select `Properties > Local Files > Browse...`.
3. This will open up the Rimworld folder. Inside this folder there is another called `Mods`.
4. Extract the downloaded zip file into this `Mods` folder.
5. You can edit `Example Disco Extension Mod/About/About.xml` and change the mod name, author, description etc.

## Creating a program
Let's explore what a program is. You don't need to create new programs to create a new sequence, but it might be useful to understand them anyway.
Inside your mod's `Defs` folder, you will find a file called `My Disco Programs.xml`.
Open this file in a good text editor (like Notepad++ or VSCode). You will see the following:
``` XML
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Disco.ProgramDef ParentName="DSC_BDP">
    <defName>MyDiscoDef</defName>
    <programClass>Disco.Programs.Solid</programClass>
    <inputs>
      <color>(0, 1, 0, 1)</color>
    </inputs>
  </Disco.ProgramDef>
</Defs>
```
Here is a breakdown of what's going in this XML:
* `ParentName="DSC_BDP"` Boilerplate that inherits some useful properties.
* `<defName>MyDiscoDef</defName>` The def name of your program. Must be unique and not contain spaces.
* `<programClass>Disco.Programs.Solid</programClass>` Tells *Disco!* what C# class to use for this program. In this case, we are using the *Solid* class, which simply displays a single solid color. See [this help page](./Built-In-Programs.md) for a list of most program classes and their use.
* `<inputs>` A list of inputs for the program class. In this case since we are using the *Solid* class we want to tell the class what color to display.
* `<color>(0, 1, 0, 1)</color>` We are assigning the value *(0, 1, 0, 1)* to the input field *color*. *(0, 1, 0, 1)* means green; (r, g, b, a).

You can now test this program out. Save your file, open up Rimworld. Enable *Development mode*. Make sure that your mod is active in the Mods menu, and check that there are no errors in the log.
Now open up a save file with a disco floor and DJ stand, or make a new save. You can use *God mode* to build the floor and DJ stand instantly and for free.

Select the DJ stand. Click on the *Toggle debugging mode* button. Now click *Set program*, and you will see a list of all available programs. You should see your `MyDiscoDef` among them. Click on it.

If the disco floor turns solid green, congratulations! You have now made a simple disco floor program.

However, it isn't very interesting. To make more complex programs, check out the built-in programs in `Disco!/Defs/DiscoPrograms.xml` and see if you can adapt them into your own custom program.

## Creating a sequence
Programs are very basic and not very interesting on their own. Sequences allow you to combine many programs to create interesting effects.

Create a new file inside the `Defs` folder and call it `MyCustomSequence.xml`.

Paste in the following XML code:
``` XML
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Disco.SequenceDef ParentName="DSC_BDS">
    <defName>MyCustomSequence</defName>
    <label>A green circle on a red background.</label>
    
    <actions>

      <!-- Fade in to solid color background -->
      <li>
        <type>Start</type>
        <program>DSC_DP_Solid</program>
        <tint>(200, 60, 60, 255)</tint>
      </li>
      <li>
        <type>Add</type>
        <program>DSC_DP_FadeIn</program>
      </li>
      
      <!-- Wait for the fade-in to finish -->
      <li>
        <type>WaitLast</type>
      </li>

      <!-- Add a beat circle overlay -->
      <li>
        <type>Add</type>
        <program>DSC_DP_BeatCircle_130bpm</program>
        <tint>(20, 255, 20, 255)</tint>
        <blend>Normal</blend>
      </li>
      
      <!-- Add edge-volume effect -->
      <li>
        <type>Add</type>
        <program>DSC_DP_EdgeVolume</program>
        <tint>(20, 255, 20, 255)</tint>
        <blend>Normal</blend>
      </li>

      <!-- Add a random song -->
      <li>
        <type>Add</type>
        <blend>Ignore</blend>
        <randomFromGroup>Songs</randomFromGroup>
        <onEndAction>EndSequence</onEndAction>
        <addToMemory>true</addToMemory> <!-- Add it to memory! -->
      </li>

      <!-- Repeat 300 times... -->
      <li>
        <type>Repeat</type>
        <times>300</times>
        <actions>

          <!-- Wait 8 ticks. -->
          <li>
            <type>Wait</type>
            <ticks>8</ticks>
          </li>
          
          <!-- Spawn a ripple -->
          <li>
            <type>Add</type>
            <program>DSC_DP_Ripple_BW_Out_Fast</program>
            <tint>(20, 255, 20, 255)</tint>
            <blend>Normal</blend>
          </li>

          <!-- Wait another 20 ticks. -->
          <li>
            <type>Wait</type>
            <ticks>20</ticks>
          </li>

        </actions>
      </li>

      <!-- Wait for the song to end (was added to memory earlier) -->
      <li>
        <type>WaitMem</type>
      </li>

      <!-- Wait another 2 seconds then fade out -->
      <li>
        <type>Wait</type>
        <ticks>120</ticks>
      </li>
      <li>
        <type>Add</type>
        <program>DSC_DP_FadeOut</program>
      </li>
      <li>
        <type>WaitLast</type>
      </li>
      
    </actions>
  </Disco.SequenceDef>

</Defs>
```

Hopefully the XMl is sufficiently commented that you can get a general understanding of what is happening.
However, here is a more in-depth explaination:

### Actions
This is an action:
``` XML
<li>
  <type>Start</type>
  <program>DSC_DP_Solid</program>
  <tint>(200, 60, 60, 255)</tint>
</li>
```
Every actions is of a particular type. Here are all types and what they do:
Type | Description
--- | ---
`None` | Invalid, do not use.
`Wait` | Waits for `ticks` number of ticks.
`WaitLast` | Waits for the last added program to complete.
`WaitMem` | Waits for the last program to be stored in memory to finish.
`Start` | Sets the current floor program. This is the same as doing `Clear` then `Add`.
`Add` | Adds a new program on to the floor. The program is added on the top, unless `atBottom` is true, in which case it will be placed at the bottom. Remember that the default blend mode is *Multiply*.
`Repeat` | Loops through and runs all the `actions` `times` number of times.
`Clear` | Removes all programs from the dance floor.
`PickRandom` | Picks a random action from the `actions` list.
`MemAdd` | Puts the last added program on to the memory stack.
`MemRemove` | Removes the top item from the memory stack.
`TintMem` | Changes the `tint` of the top item in the memory stack.
`DestroyMem` | Removes the top item in the memory stack from the dance floor, also removing it from the memory stack.

`Start` and `Add` actions must supply a program to add to the dance floor.
The easiest way to specifying the program is by putting it's *defName* is the `program` field:
``` XML
<li>
  <type>Start</type>
  <program>DSC_DP_Solid</program> <!-- Starts the 'DSC_DP_Solid' program -->
</li>
```
You can also *start* or *add* a random program from a list:
``` XML
<li>
  <type>Start</type>
  <!-- Picks a random program from this list -->
  <randomFromList>
    <li>DSC_DP_Cycle_Vapourwave</li>
    <li>DSC_DP_Cycle_Sunrise</li>
    <li>DSC_DP_Cycle_Soul</li>
  </randomFromList>
</li>
```

### Blending
All actions have a blending mode. Blending changes how one program will affect the color of the program **underneith** it.
Here are the blend modes:
Mode | Description
--- | ---
`Multiply` (default) | The color of this top layer is multiplied with the bottom color. Useful for tinting and masking.
`Add` | The top color is added to the bottom color.
`Normal` | The top color is overlayed onto the bottom color. This is good for putting partially transparent images into the background.
`Override` | The top color completely replaces the bottom color, ignoring the bottom color.
`Ignore` | The top color is ignored and has no effect on the floor.

### More action options
#### Tinting
All programs can be tinted. Tinting means that the color that the program outputs is multiplied with the `tint` color.
In this example, the black-and-white `DSC_DP_Checkerboard_Grayscale` is tinted with `(200, 0, 0, 255)` (red).
``` XML
<li>
  <type>Start</type>
  <program>DSC_DP_Checkerboard_Grayscale</program>
  <tint>(200, 0, 0, 255)</tint>
</li>
```
#### Random chance
Most actions can have a random chance of being run or not. By default all actions have 100% chance, but you can change this using the `chance` tag.
In this example, this has a 50% chance of adding the `DSC_DP_Checkerboard_Grayscale` program.
``` XML
<li>
  <type>Add</type>
  <program>DSC_DP_Checkerboard_Grayscale</program>
  <chance>0.5</chance>
</li>
```

This documentation is WIP and is incomplete as of 08/04/2021.
