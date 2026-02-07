# YapYapRomajiDisplay

Displays Romaji for various spell/voice command keys in YAPYAP.

## How to Build

This project relies on game DLLs that cannot be distributed with the source code. To build this project locally, follow these steps:

1.  Clone this repository.
2.  Create a file named `GamePath.props` in the project root directory (next to the `.sln` file).
3.  Paste the following XML content into `GamePath.props` and adjust the paths to match your local installation of YAPYAP and BepInEx:

    ```xml
    <Project>
      <PropertyGroup>
        <!-- Path to the game root folder -->
        <GameDir>C:\Program Files (x86)\Steam\steamapps\common\YAPYAP</GameDir>
        
        <!-- Path to the Managed folder inside game data (usually derived from GameDir) -->
        <GameManagedDir>$(GameDir)\yapyap_Data\Managed</GameManagedDir>
        
        <!-- Path to your BepInEx installation profile folder (from r2modman or manual install) -->
        <BepInExDir>C:\Users\YOUR_USERNAME\AppData\Roaming\r2modmanPlus-local\Yapyap\profiles\Default\BepInEx</BepInExDir>
        
        <!-- Path to BepInEx core folder (usually derived from BepInExDir) -->
        <BepInExCoreDir>$(BepInExDir)\core</BepInExCoreDir>
      </PropertyGroup>
    </Project>
    ```

4.  Save the file.
5.  Open `YapYapRomaji.sln` in Visual Studio or your preferred IDE.
6.  Build the project. The references should automatically resolve using the paths defined in `GamePath.props`.

## Dependencies

*   BepInEx
*   Harmony
*   YAPYAP Game Assemblies (Assembly-CSharp, UnityEngine, etc.)
