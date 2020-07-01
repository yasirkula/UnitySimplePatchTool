# Simple Patch Tool for Unity

### THIS PROJECT IS NO LONGER MAINTAINED.

---

![screenshot](Images/launcher-demo.png)

**Available on Asset Store:** https://assetstore.unity.com/packages/tools/network/simple-patch-tool-124296

**Forum Thread:** https://forum.unity.com/threads/simplepatchtool-open-source-patching-solution-for-standalone-platforms.542465/

**[Support the Developer ☕](https://yasirkula.itch.io/unity3d)**

This plugin is the Unity port of [SimplePatchTool](https://github.com/yasirkula/SimplePatchTool), a general-purpose patcher library for **standalone** applications.

## LICENSE

SimplePatchTool is licensed under the [MIT License](LICENSE); however, it uses external libraries that are governed by the licenses indicated below:

- LZMA SDK - [Public Domain](https://www.7-zip.org/sdk.html)
- Octodiff - [Apache License, Version 2.0](https://github.com/OctopusDeploy/Octodiff/blob/master/LICENSE.txt)
- SharpZipLib - [MIT License](https://github.com/icsharpcode/SharpZipLib/blob/master/LICENSE.txt)

## INSTALLATION

There are 5 ways to install this plugin:

- import [SimplePatchTool.unitypackage](https://github.com/yasirkula/UnitySimplePatchTool/releases) via *Assets-Import Package*
- clone/[download](https://github.com/yasirkula/UnitySimplePatchTool/archive/master.zip) this repository and move the *Plugins* folder to your Unity project's *Assets* folder
- import it from [Asset Store](https://assetstore.unity.com/packages/tools/network/simple-patch-tool-124296)
- *(via Package Manager)* add the following line to *Packages/manifest.json*:
  - `"com.yasirkula.simplepatchtool": "https://github.com/yasirkula/UnitySimplePatchTool.git",`
- *(via [OpenUPM](https://openupm.com))* after installing [openupm-cli](https://github.com/openupm/openupm-cli), run the following command:
  - `openupm add com.yasirkula.simplepatchtool`

## REQUIREMENTS

- in **Edit-Project Settings-Player**, change **Api Compatibility Level** to **.NET 2.0** or higher (i.e. don't use *.NET 2.0 Subset* or *.NET Standard 2.0*)
- *(optional)* in **Edit-Project Settings-Player**, enable **Run In Background** so that SimplePatchTool can continue running while the application is minimized/not focused

## DOCUMENTATION

Wiki available at: https://github.com/yasirkula/UnitySimplePatchTool/wiki
