## Intro
In order to complete these steps you will need to have a good text editor (VSCode, Notepad++ etc.) and ideally an understanding of XML and/or Rimworld's Def system.

There are two important concepts to understand first:

* **Disco program**: A 'layer' that affects the disco floor. Most disco programs change the color of the dance floor, but some might not (such as the *Music* programs).
* **Disco sequence**: A sequence of disco programs arranged in an interesting way. For example, a sequence could add a checkerboard program, then a color program, and finally a song program.

## Setting up a mod
*TODO WRITEME*

## Creating a program
First, let's create a new program. Here are the steps for that:

Create a new .xml def file in your mod's `Defs` folder. Call it `MyProgram.xml`.
Open it and copy-paste the following xml code:
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
* `<programClass>Disco.Programs.Solid</programClass>` Tells *Disco!* what C# class to use for this program. In this case, we are using the `Solid` class, which simply displays a single solid color.
* `<inputs>` A list of inputs for the program class. In this case since we are using the `Solid` class we want to tell the class what color to display.
* `<color>(0, 1, 0, 1)</color>` We are assigning the value *(0, 1, 0, 1)* to the input field *color*. *(0, 1, 0, 1)* means green; (r, g, b, a).

You can now test this program out. Save your file, open up Rimworld. Enable *Development mode*. Make sure that your mod is active in the Mods menu, and check that there are no errors in the log.
Now open up a save file with a disco floor and DJ stand, or make a new save. You can use *God mode* to build the floor and DJ stand instantly and for free.

Select the DJ stand. Click on the *Toggle debugging mode* button. Now click *Set program*, and you will see a list of all available programs. You should see your `MyDiscoDef` among them. Click on it.

If the disco floor turns solid green, congratulations! You have now made a simple disco floor program.

However, it isn't very interesting. To make more complex programs, check out the built-in programs in `Disco!/Defs/DiscoPrograms.xml` and see if you can adapt them into your own custom program.
