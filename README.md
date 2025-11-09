# WSSInterfacingCode
C# class that has all the methods to communicate with the WSS and software specific classes that implements some of the functionalities necessary for that program.

## Linux serial setup
- Install the native dependency that backs `System.IO.Ports`. On Debian/Ubuntu-based distros run `sudo apt install libudev1` (Unity build hosts may also require `libudev-dev`). Most other distros ship the equivalent package by default.
- Ensure your user can access `/dev/tty*` devices. Add yourself to the `dialout` (or distro-specific) group via `sudo usermod -a -G dialout $USER`, then log out and back in so the new group takes effect.
- You can confirm permissions with `ls -l /dev/ttyUSB0`; the owner or group should include your account after the step above. Without this, `SerialPort.Open()` will throw `UnauthorizedAccessException`.
- Unity on Linux/WSL uses the same transport class, so the same `libudev` dependency and group membership apply when running in the editor or player.

## Standalone C# apps
When building a separate .NET console/worker app that references the compiled `WSS_Core_Interface.dll`, ensure the project targets a modern framework (e.g., `net8.0`) and explicitly references the dependencies that live outside the base runtime. The sample below mirrors a working setup:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Security.Permissions" Version="7.0.0" />
    <PackageReference Include="System.IO.Ports" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="WSS_Core_Interface">
      <HintPath>..\lib\WSS_Core_Interface.dll</HintPath>
    </Reference>
    <Reference Include="Newtonsoft.Json">
      <HintPath>..\lib\Newtonsoft.Json.dll</HintPath>
    </Reference>
    <Reference Include="System.Buffers">
      <HintPath>..\lib\System.Buffers.dll</HintPath>
    </Reference>
    <Reference Include="System.Memory">
      <HintPath>..\lib\System.Memory.dll</HintPath>
    </Reference>
    <Reference Include="System.Numerics.Vectors">
      <HintPath>..\lib\System.Numerics.Vectors.dll</HintPath>
    </Reference>
    <Reference Include="System.Runtime.CompilerServices.Unsafe">
      <HintPath>..\lib\System.Runtime.CompilerServices.Unsafe.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

Copy the listed DLLs from this repository’s `bin` output into your app’s `lib` folder (or use NuGet where possible) so the runtime can resolve them when your executable starts.

## How to add a submodule to exiting project
1. Close other software using the code solution (Unity, Visual Studio, etc)
2. If using git desktop, open the command prompt by going to `Repository>Open in Command Prompt`
3. Use command `git submodule add <submoduleURL> <pathInProject>` 
	- if using a submodule inside of Unity make sure the path is inside the Assests folder
	- Ex HTTPS: `git submodule add https://github.com/cwru-non-academic/WSSInterfacingCode Assets\SubModules\WSSInterfacingModule`
 	- Ex SSH: `git submodule add git@github.com:cwru-non-academic/HFI_WSS_Interface.git Assets\SubModules\WSSInterfacingModule`
4. Click `Current repository>Add>Add existing repository..` and locate the folder where you installed the submodule `<pathInProject>` 
5. Remove old scripts from the project that are now part of the submodule
6. Open solution dependent software and let it refactor.
7. (Unity Only) Make sure all you scripts in the scene are still available and linked correctly.
8. (Unity only) Add `"com.unity.nuget.newtonsoft-json": "3.2.1",` to your package manager manifest under dependencies found in `<project>/Packages/manifest.json`

## How to commit a git project that has submodules (Git Desktop)
1. This only applies if the changes were made to the submodule, otherwise just commit as normal.
2. Inside of the git Desktop a warning will appear that says there are submodule chnages.
3. Changes to submodules must be commited first in the submodules repo.
4. If the submodule repo is already setup, just click open repository shortcut under the submodule changes warning.
5. If it is not setup, click the shortcut to add it to Git Desktop.
6. Once in the submodule repo, commit the changes as normal.
7. Return to the main repo and commit the changes there (The chnage in commit ID will be not be selected by default. select it and then commit). 

## How to commit a git project that has submodules (Git CMD)
`git clone --recursive git@github.com:cwru-non-academic/HFI_WSS_Interface.git`

## How to pull a submodule change (Git Desktop)
1. Make sure the git submodule repo is already part of your repos in git desktop
2. If not add it by following `How to add a submodule to exiting project`
3. Go to the submodule repo and pull changes as normal. 
4. Pulling a submodule will add a change in commit ID to the main repo. 
	- Multiple changes in commit ID can be commited to the main repo as a single commit and together with other changes to the main repo.

## How to add Newtonsoft json package
1. Open Window> Package Manager
2. Click the top left plus sign> add package by name `com.unity.nuget.newtonsoft-json`
3. Leave version empty and click add
