# Simple Patch Tool for Unity

This plugin is a Unity port of the [SimplePatchTool library](https://github.com/yasirkula/SimplePatchTool), a general-purpose patcher library for **standalone** applications. Before using this plugin, you should first see SimplePatchTool's documentation: https://github.com/yasirkula/SimplePatchTool

## LICENSE

SimplePatchTool is licensed under the [MIT License](LICENSE); however, it uses external libraries that are governed by the licenses indicated below:

- LZMA SDK - [Public Domain](https://www.7-zip.org/sdk.html)
- Octodiff - [Apache License, Version 2.0](https://github.com/OctopusDeploy/Octodiff/blob/master/LICENSE.txt)
- SharpZipLib - [MIT License](https://github.com/icsharpcode/SharpZipLib/blob/master/LICENSE.txt)

## HOW TO

- import **SimplePatchTool.unitypackage** to your project
- in **Edit-Project Settings-Player**, change **Api Compatibility Level** to **.NET 2.0** or higher (i.e. don't use *.NET 2.0 Subset*)
- you can now use **Window-Simple Patch Tool** to create/update patches, sign/verify xml files and generate RSA key pair:

![editor_window](Images/editor-window.png)

**CREATE:** see https://github.com/yasirkula/SimplePatchTool#d1-creating-patches. If you'd like to use the [console app](https://github.com/yasirkula/SimplePatchTool#d1-creating-patches) to create patches, you can click the **Generate Console Command** button and copy&paste the logged *Patcher* command to the console app

**UPDATE:** see https://github.com/yasirkula/SimplePatchTool#d2-updating-patches

**SECURITY:** see https://github.com/yasirkula/SimplePatchTool#d3-signingverifying-patches-optional

## IMPLEMENTATION

This plugin uses SimplePatchTool's **SimplePatchToolCore** and **SimplePatchToolSecurity** modules without any modifications; so, if you want, you can make any changes to [these modules](https://github.com/yasirkula/SimplePatchTool), rebuild them and replace the dll files at *Plugins/SimplePatchTool/DLL* with their updated versions.

To avoid any *Mono* related issues while applying patches in Unity, you should make the following changes to *SimplePatchTool* in your codes:

```csharp
SimplePatchTool patcher = new SimplePatchTool( ... )
// default IDownloadHandler implementation doesn't support https in Unity
.UseCustomDownloadHandler( () => new CookieAwareWebClient() )
// default implementation (DriveInfo.AvailableFreeSpace) throws NotImplementedException in Unity 5.6.2,
// so skip this stage until a Unity-compatible solution is found
.UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue );
```

Please see *Plugins/SimplePatchTool/Demo/* for an example patcher implementation. The demo scene uses the [PatcherUI](Plugins/SimplePatchTool/Demo/PatcherUI.cs) prefab to show *SimplePatchTool*'s progress to the user:

![patcher_ui](Images/patcher-ui.png)

To test the demo scene with xml verification (assuming that you've signed the xml files with a private RSA key), change the extension of your public RSA key file to *.bytes* and import it to Unity. Then, assign it to the *Version Info Verifier* and/or *Patch Info Verifier* variables of the *Patcher Control Panel* object.

SimplePatchTool comes bundled with a self patcher executable on Windows platform. To add self patching support to macOS and/or Linux platforms, or to use a custom self patcher executable in your projects, you need to follow these steps:

- [build the self patcher executable](https://github.com/yasirkula/SimplePatchTool#f1-creating-self-patcher-executable)
- move the self patcher executable and any of its dependencies to the following directory (these files will [automatically be copied](Plugins/SimplePatchTool/Editor/PatcherPostProcessBuild.cs) to a subdirectory called *SPPatcher* after building the project to standalone):
  - **Windows:** Plugins/SimplePatchTool/Editor/Windows
  - **macOS:** Plugins/SimplePatchTool/Editor/OSX
  - **Linux:** Plugins/SimplePatchTool/Editor/Linux
- update the name of the self patcher executable in **SPTUtils.SelfPatcherExecutablePath** property
- you can now run the self patcher like this: `patcher.ApplySelfPatch( SPTUtils.SelfPatcherExecutablePath );`
- or like this, if you want to automatically restart the game/launcher after self patching is complete: `patcher.ApplySelfPatch( SPTUtils.SelfPatcherExecutablePath, PatchUtils.GetCurrentExecutablePath() );`

## EXAMPLES

### Creating a self-patching app and testing it on local file system

- make sure that your target platform's self patcher executable is set up
- create a *C#* script called *SelfPatchingExample* and add it to an **empty object** in your first scene:

```csharp
using SimplePatchToolCore;
using SimplePatchToolUnity;
using System.Collections;
using System.IO;
using UnityEngine;

public class SelfPatchingExample : MonoBehaviour
{
	[SerializeField]
	private string versionInfoURL;

	[SerializeField]
	private PatcherUI patcherUiPrefab;

	private SimplePatchTool patcher;
	private static bool executed = false;

	// SimplePatchTool only works on standalone platforms
	// Self patching is not supported on Editor
#if !UNITY_EDITOR && UNITY_STANDALONE
	private void Awake()
	{
		if( executed )
		{
			Destroy( gameObject );
			return;
		}

		DontDestroyOnLoad( gameObject );
		executed = true;

		patcher = new SimplePatchTool( Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() ), versionInfoURL ).
			UseRepair( true ).UseIncrementalPatch( true ).
			UseCustomDownloadHandler( () => new CookieAwareWebClient() ). // to support https in Unity
			UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue ). // DriveInfo.AvailableFreeSpace is not supported on Unity
			LogProgress( true );

		patcher.CheckForUpdates();
		StartCoroutine( CheckForUpdatesCoroutine() );

	}

	private IEnumerator CheckForUpdatesCoroutine()
	{
		while( patcher.IsRunning )
			yield return null;

		if( patcher.Result == PatchResult.Success ) // There is an update
		{
			if( patcher.Run( true ) ) // self patch the app
				Instantiate( patcherUiPrefab ).Initialize( patcher ); // show progress on a PatcherUI instance
			else
				Debug.LogError( "Something went wrong" );
		}
	}
#endif
}
```

- enter a value to the *Version Info URL* variable from the Inspector like this: `file://C:\Users\USERNAME\Desktop\PatchFiles\VersionInfo.info` (we will create the VersionInfo at that path in a moment)
- assign *Plugins/SimplePatchTool/Demo/PatcherUI* prefab to the *Patcher Ui Prefab* variable
- build your project to an empty directory (let's say *Build1*)
- open **Window-Simple Patch Tool**, click the little button next to the *Root path* variable and select the *Build1* directory
- set *Output path* as `C:\Users\USERNAME\Desktop\PatchFiles`
- click the **Create Patch** button and wait for the *Operation successful...* log to appear in the console ([it can take some time](https://stackoverflow.com/questions/12292593/why-is-lzma-sdk-7-zip-so-slow))
- make some changes to your project (e.g. add cubes to the scene, change the camera background color and etc.) and then build the project to another empty directory (let's say *Build2*)
- open *Simple Patch Tool* window again and change *Root path* to the *Build2* directory, while giving *Output path* the same value as before 
- set *Previous version path* as the *Build1* directory and change *Project version* to *1.1*
- if you attempt to create the patch now, you'll receive the following error: `ERROR: directory C:\Users\USERNAME\Desktop\PatchFiles is not empty`. We don't need those old patch files, so you can safely clear the *PatchFiles* directory
- now click the **Create Patch** button and wait for the process to finish
- open *PatchFiles/VersionInfo.info* with Notepad and change its *BaseDownloadURL* like this: `<BaseDownloadURL>file://C:\Users\USERNAME\Desktop\PatchFiles\RepairFiles\</BaseDownloadURL>`
- move *1_0__1_1.info* and *1_0__1_1.patch* into the *RepairFiles* directory
- run the *Build1* version and see the magic happen!