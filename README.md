# Simple Patch Tool for Unity

**Available on Asset Store:** https://www.assetstore.unity3d.com/en/#!/content/124296

**Forum Thread:** https://forum.unity.com/threads/simplepatchtool-open-source-patching-solution-for-standalone-platforms.542465/

This plugin is a Unity port of [SimplePatchTool](https://github.com/yasirkula/SimplePatchTool), a general-purpose patcher library for **standalone** applications. Before using this plugin, you are recommended to first see SimplePatchTool's documentation: https://github.com/yasirkula/SimplePatchTool

## LICENSE

SimplePatchTool is licensed under the [MIT License](LICENSE); however, it uses external libraries that are governed by the licenses indicated below:

- LZMA SDK - [Public Domain](https://www.7-zip.org/sdk.html)
- Octodiff - [Apache License, Version 2.0](https://github.com/OctopusDeploy/Octodiff/blob/master/LICENSE.txt)
- SharpZipLib - [MIT License](https://github.com/icsharpcode/SharpZipLib/blob/master/LICENSE.txt)

## HOW TO

- import **SimplePatchTool.unitypackage** to your project
- in **Edit-Project Settings-Player**, change **Api Compatibility Level** to **.NET 2.0** or higher (i.e. don't use *.NET 2.0 Subset*)
- *(optional)* in **Edit-Project Settings-Player**, enable **Run In Background** so that SimplePatchTool can continue running while the application is minimized/not focused
- you can now use **Window-Simple Patch Tool** to [create projects, generate patches and so on](https://github.com/yasirkula/SimplePatchTool/wiki):

![editor_window](Images/editor-window.png)

### Updating Dll's

This plugin uses SimplePatchTool's **SimplePatchToolCore** and **SimplePatchToolSecurity** modules without any modifications; so, if you want, you can make any changes to [these modules](https://github.com/yasirkula/SimplePatchTool), rebuild them and replace the dll files at *Plugins/SimplePatchTool/DLL* with the updated ones.

### Unity-specific Changes

To avoid any *Mono* related issues while applying patches in Unity, you should make the following changes to *SimplePatchTool* in your codes:

```csharp
SimplePatchTool patcher = new SimplePatchTool( ... )
// default IDownloadHandler implementation doesn't support https in Unity
.UseCustomDownloadHandler( () => new CookieAwareWebClient() )
// default implementation (DriveInfo.AvailableFreeSpace) throws NotImplementedException in Unity 5.6.2,
// so skip this stage until a Unity-compatible solution is found
.UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue );
```

Alternatively, you can call the [SPTUtils.CreatePatcher( string rootPath, string versionInfoURL )](Plugins/SimplePatchTool/Scripts/SPTUtils.cs) function which returns a Unity-compatible *SimplePatchTool* instance.

## EXAMPLES

Some of the example scenes use the [PatcherUI](Plugins/SimplePatchTool/Demo/PatcherUI.cs) prefab to show *SimplePatchTool*'s progress to the user; feel free to use it in your own projects, as well:

![patcher_ui](Images/patcher-ui.png)

If you [sign your *VersionInfo* and/or *PatchInfo* files with private RSA key(s)](https://github.com/yasirkula/SimplePatchTool/wiki/Signing-&-Verifying-Patches), you can paste their corresponding public RSA key(s) to the **Version Info RSA** and/or **Patch Info RSA** variables in the demo scenes.

### [PatcherControlPanelDemo](Plugins/SimplePatchTool/Demo/PatcherControlPanelDemo.cs)

![patcher_ui](Images/control-panel-demo.png)

This scene lets you tweak some variables at runtime to quickly test some patches with different configurations. It can run on the Editor.

### [SelfPatchingAppDemo](Plugins/SimplePatchTool/Demo/SelfPatchingAppDemo.cs)

~~**Video tutorial:** https://www.youtube.com/watch?v=Gjl6my7rVSI~~ (legacy tutorial)

This scene allows you to quickly create and test a self patching app. It can't run on the Editor.

You can test this scene as follows:

- follow [these steps](https://github.com/yasirkula/SimplePatchTool/wiki/Generating-versionInfoURL) and paste VersionInfo's url to the **Version Info URL** variable of *SelfPatchingAppUI* in the Inspector
- tweak the value of **Check Version Only** as you like
- [create a project for this app](https://github.com/yasirkula/SimplePatchTool/wiki/Project:-Create)
- as this app uses self patching, we need a self patcher executable: [create a self patcher executable](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Self-Patcher-Executable) and put it inside the *SelfPatcher* directory of the project
- enter the name of the self patcher executable to the **Self Patcher Executable** variable of *SelfPatchingAppUI* in the Inspector
- create a subdirectory called `1.0` inside the *Versions* directory of the project
- build this scene into the `1.0` subdirectory
- follow [these steps](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches#a-using-projectmanager-recommended) to create a patch, upload it to the server, update download links in VersionInfo and etc.
- you've created your first patch, great! We should now create a second patch to test the patcher. First, make some changes to the scene (e.g. add some cubes around, change the background of the camera)
- build the scene into another subdirectory called `1.1` inside the *Versions* directory of the project
- create another patch following [these steps](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches#a-using-projectmanager-recommended)
- if you launch the 1.0 version of the app now, you'll see that it'll detect the 1.1 update and prompt you to update itself to that version, well done!
- also, if you had previously set *Check Version Only* to *false*, try deleting a redundant file from the app's Data directory (e.g. something from the *Mono/etc* subdirectory). When you launch the app, it will automatically detect the absence of the file and prompt you to update/repair itself

### [LauncherDemo](Plugins/SimplePatchTool/Demo/LauncherDemo.cs)

![launcher_ui](Images/launcher-demo.png)

~~**Video tutorial:** https://www.youtube.com/watch?v=P7iUQ-n3EQA~~ (legacy tutorial)

This scene allows you to quickly create and test a launcher that can self patch itself in addition to patching and launching a main app. Launcher first checks if it is up-to-date (if not, self patches itself) and then checks if the main app is up-to-date (if not, patches it). If you don't provide a VersionInfo url for one of these patches, that patch will be skipped. This scene can't run on the Editor.

You can test this scene as follows (you are recommended to test the [SelfPatchingAppDemo](#selfpatchingappdemo) scene first):

- see the [recommended project structure for launchers](https://github.com/yasirkula/SimplePatchTool/wiki/Recommended-Project-Structure)
- [generate versionInfoURL's](https://github.com/yasirkula/SimplePatchTool/wiki/Generating-versionInfoURL) for the launcher and the main app and paste them to the **Launcher Version Info URL** and **Main App Version Info URL** variables of *LauncherUI* in the Inspector
- decide a **Main App Subdirectory** (let's say *MainApp*) and **Main App Executable** (let's say *MainApp.exe*)
- [create a project for the launcher](https://github.com/yasirkula/SimplePatchTool/wiki/Project:-Create)
- add `MainApp/` to the *IgnoredPaths* of the project's *Settings.xml* (also give it a meaningful `<Name>` like *Launcher*)
- as the launcher uses self patching, we need a self patcher executable: [create a self patcher executable](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Self-Patcher-Executable) and put it inside the *SelfPatcher* directory of the project
- enter the name of the self patcher executable to the **Self Patcher Executable** variable of *LauncherUI* in the Inspector
- create a subdirectory called `1.0` inside the *Versions* directory of the project
- build this scene into the `1.0` subdirectory
- follow [these steps](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches#a-using-projectmanager-recommended) to create a patch for the launcher, upload it to the server, update download links in VersionInfo and etc.
- you've created your launcher's first patch, awesome! Now let's generate a patch for the main app, as well
- [create another project](https://github.com/yasirkula/SimplePatchTool/wiki/Project:-Create), this time for the main app
- create a subdirectory called `0.1` inside the *Versions* directory of the project
- build another one of your projects, name the executable as *MainApp.exe* and move the generated executables and libraries into the `0.1` subdirectory
- follow [the same steps](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches#a-using-projectmanager-recommended) to create a patch for the main app (you are recommended to keep the launcher's and the main app's patch files in separate directories on the server for clarity)
- if you launch your launcher now, launcher should be able to detect the absence of the main app and prompt you to download/patch it (try not to launch the executable inside the `1.0` subdirectory because it will be used to create incremental patches later on, instead create a copy of that directory on some other location)
- after letting the launcher download the main app, try deleting a file from *MainApp* and hit the *Repair Game* button in the launcher to test repairing the main app
- make some changes to the launcher's user interface and build the scene into another subdirectory called `1.1` inside the *Versions* directory of the launcher's project
- [create another patch](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches#a-using-projectmanager-recommended) for the launcher
- if you launch the 1.0 version of the launcher now, you'll see that it'll detect the 1.1 update and prompt you to update itself to that version, well done!
- feel free to create a new version of the main app, as well

### [MultiGameLauncherDemo](Plugins/SimplePatchTool/Demo/MultiGameLauncherDemo.cs)

![launcher_ui](Images/multi-game-launcher-demo.png)

An example scene to demonstrate how you can use SimplePatchTool with your own Steam-like game hubs. Each game's configuration is stored in the *Games* variable of the *LauncherUI* object. You are recommended to test the [LauncherDemo](#launcherdemo) scene prior to testing this scene.