# Simple Patch Tool for Unity

**Available on Asset Store:** https://www.assetstore.unity3d.com/en/#!/content/124296

**Forum Thread:** https://forum.unity.com/threads/simplepatchtool-open-source-patching-solution-for-standalone-platforms.542465/

This plugin is a Unity port of the [SimplePatchTool library](https://github.com/yasirkula/SimplePatchTool), a general-purpose patcher library for **standalone** applications. Before using this plugin, you should first see SimplePatchTool's documentation: https://github.com/yasirkula/SimplePatchTool

## LICENSE

SimplePatchTool is licensed under the [MIT License](LICENSE); however, it uses external libraries that are governed by the licenses indicated below:

- LZMA SDK - [Public Domain](https://www.7-zip.org/sdk.html)
- Octodiff - [Apache License, Version 2.0](https://github.com/OctopusDeploy/Octodiff/blob/master/LICENSE.txt)
- SharpZipLib - [MIT License](https://github.com/icsharpcode/SharpZipLib/blob/master/LICENSE.txt)

## HOW TO

- import **SimplePatchTool.unitypackage** to your project
- in **Edit-Project Settings-Player**, change **Api Compatibility Level** to **.NET 2.0** or higher (i.e. don't use *.NET 2.0 Subset*)
- you can now use **Window-Simple Patch Tool** to [create/update patches, sign/verify xml files and generate RSA key pair](https://github.com/yasirkula/SimplePatchTool/wiki):

![editor_window](Images/editor-window.png)

### Updating Dll's

This plugin uses SimplePatchTool's **SimplePatchToolCore** and **SimplePatchToolSecurity** modules without any modifications; so, if you want, you can make any changes to [these modules](https://github.com/yasirkula/SimplePatchTool), rebuild them and replace the dll files at *Plugins/SimplePatchTool/DLL* with their updated versions.

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

### About Self Patcher Executable

SimplePatchTool comes bundled with a self patcher executable on Windows platform. To add self patching support to macOS and/or Linux platforms, or to use a custom self patcher executable in your projects, you need to follow these steps:

- [build the self patcher executable](https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Self-Patcher-Executable)
- move the self patcher executable and any of its dependencies to the following directory:
  - **Windows:** Plugins/SimplePatchTool/Editor/Windows
  - **macOS:** Plugins/SimplePatchTool/Editor/OSX
  - **Linux:** Plugins/SimplePatchTool/Editor/Linux
    - these files will [automatically be copied](Plugins/SimplePatchTool/Editor/PatcherPostProcessBuild.cs) to a subdirectory called *SPPatcher* after building the project to standalone (if you want, you can set **PatcherPostProcessBuild.ENABLED** to *false* to disable this feature)
- update the name of the self patcher executable in **SPTUtils.SelfPatcherExecutablePath** property
- you can now run the self patcher like this: `patcher.ApplySelfPatch( SPTUtils.SelfPatcherExecutablePath );`
- or like this, if you want to automatically restart the game/launcher after self patching is complete: `patcher.ApplySelfPatch( SPTUtils.SelfPatcherExecutablePath, PatchUtils.GetCurrentExecutablePath() );`

## EXAMPLES

### Demo Scene

Please see *Plugins/SimplePatchTool/Demo/* for an example patcher implementation. The demo scene uses the [PatcherUI](Plugins/SimplePatchTool/Demo/PatcherUI.cs) prefab to show *SimplePatchTool*'s progress to the user:

![patcher_ui](Images/patcher-ui.png)

To test the demo scene with xml verification (assuming that you've [signed the xml files with a private RSA key](https://github.com/yasirkula/SimplePatchTool/wiki/Signing-&-Verifying-Patches)), change the extension of your public RSA key file to *.bytes* and import it to Unity. Then, assign it to the *Version Info Verifier* and/or *Patch Info Verifier* variables of the *Patcher Control Panel* object.

### Creating a Self-patching App

In this example, you will see how to add self patching support to your Unity apps. Before starting, make sure that [your target platform's self patcher executable is set up](#about-self-patcher-executable).

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

	// SimplePatchTool only works on standalone platforms
	// Self patching is not supported on Editor
#if !UNITY_EDITOR && UNITY_STANDALONE
	private SimplePatchTool patcher;
	private static bool executed = false;
	
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

		// true (default): only version number (e.g. 1.0) is compared with VersionInfo to see if there is an update
		// false: hashes and sizes of the local files are compared against VersionInfo (if any file is different/missing, we'll patch the app)
		if( patcher.CheckForUpdates( true ) )
			StartCoroutine( CheckForUpdatesCoroutine() );
		else
			Debug.LogError( "Something went wrong" );
	}

	private IEnumerator CheckForUpdatesCoroutine()
	{
		while( patcher.IsRunning )
			yield return null;

		if( patcher.Result == PatchResult.Success ) // There is an update
		{
			if( patcher.Run( true ) ) // start patching in self patching mode
				Instantiate( patcherUiPrefab ).Initialize( patcher ); // show progress on a PatcherUI instance
			else
				Debug.LogError( "Something went wrong" );
		}
	}
#endif
}
```

- follow [these steps](https://github.com/yasirkula/SimplePatchTool/wiki/Before-Creating-Your-First-Patch) and paste VersionInfo's url to **Version Info URL** in the Inspector
- assign *Plugins/SimplePatchTool/Demo/PatcherUI* prefab to the **Patcher Ui Prefab** variable
- build your project to an empty directory (let's say *Build1*)
- open **Window-Simple Patch Tool**, click the little button next to the *Root path* variable and select the *Build1* directory
- point *Output path* to an empty directory (let's say *PatchFiles*)
- click the **Create Patch** button and wait for the *Operation successful...* log to appear in the console ([it can take some time](https://stackoverflow.com/questions/12292593/why-is-lzma-sdk-7-zip-so-slow))
- make some changes to the project (e.g. add cubes to the scene, change the camera background color and etc.) and then build the project to another empty directory (let's say *Build2*)
- open *Simple Patch Tool* window again and change *Root path* to the *Build2* directory
- give *Output path* the same value as before (*PatchFiles* directory)
- set *Previous version path* as the *Build1* directory and increase the value of *Project version* (e.g. if it was *1.0*, set it to *1.1*)
- if you attempt to create the patch now, you'll receive the following error: `ERROR: directory ...\PatchFiles is not empty`. We don't need those old patch files, so you can safely clear the *PatchFiles* directory
- now click the **Create Patch** button and wait for the process to finish
- [upload the *PatchFiles* directory to a server](https://github.com/yasirkula/SimplePatchTool/wiki/Hosting-Patch-Files)
- [update the download links inside *VersionInfo.info*](https://github.com/yasirkula/SimplePatchTool/wiki/Updating-Download-Links-in-VersionInfo)
- update the *VersionInfo.info* that [you've initially uploaded to the server of your choice](https://github.com/yasirkula/SimplePatchTool/wiki/Before-Creating-Your-First-Patch) with the one inside *PatchFiles* on your computer (the one with updated download links)
- all done! Run the *Build1* version and see the magic happen!