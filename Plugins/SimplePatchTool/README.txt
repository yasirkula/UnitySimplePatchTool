= Simple Patch Tool =

Online documentation & example code available at: https://github.com/yasirkula/UnitySimplePatchTool
E-mail: yasirkula@gmail.com

1. ABOUT
SimplePatchTool is a C# library for patching standalone applications with binary diff and self patching support.

2. SETUP
- set "Api Compatibility Level" to .NET 2.0 or higher (i.e. don't use .NET 2.0 Subset or .NET Standard 2.0) in Edit-Project Settings-Player
- (optional) in Edit-Project Settings-Player, enable "Run In Background" so that SimplePatchTool can continue running while the application is minimized/not focused

3. USAGE
- integrate SimplePatchTool to your project: https://github.com/yasirkula/SimplePatchTool/wiki/Integrating-SimplePatchTool (you can also use the PatcherWrapper component for simple integrations)
- use Window-Simple Patch Tool to create your first patch and push it to the server of your choice: https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Patches
- whenever you update the app, create another patch and push it to the server
- each time you push a new patch to the server, your clients will automatically fetch it and keep themselves up-to-date

Or, for starters, you can inspect the [example scenes](#examples) first: https://github.com/yasirkula/UnitySimplePatchTool#examples

4. SELF PATCHER EXECUTABLE
Self patching applications (apps that update themselves) need a companion app called self patcher executable to be able to patch themselves because it is not possible patch an application that is already running. Instead, application will apply the patch files to cache directory and then launch the self patcher executable while also closing itself so that self patcher executable can update the app's files using the files in the cache.

To create a self patcher executable, see: https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Self-Patcher-Executable

- if you are creating patches via the new SimplePatchTool-projects (ProjectManager)(recommended): move the self patcher executable's files to your project's SelfPatcher directory
- if you are creating patches via Legacy method: after building your Unity project, create a subdirectory called SPPatcher inside your build directory and move the self patcher executable's files there. Note that you must repeat this step for each new build of your project

5. PatcherWrapper COMPONENT
For simple patcher integrations, you can use the Patcher Wrapper component to quickly create a customizable patcher with a number of properties and events. Most of these customization options have tooltips or explanatory texts to help you understand what is what. PatcherWrapper also has the following functions and properties:

string RootPath { get; }: calculated root path of the application
string ExecutablePath { get; }: calculated path of the application's executable
SimplePatchTool Patcher { get; }: the SimplePatchTool instance this component is using to check for updates/apply patches. You should not call SetListener on this instance since it would prevent PatcherWrapper's events from working

void CheckForUpdates(): starts checking for updates
void ApplyPatch(): starts updating the application
void RunSelfPatcherExecutable(): if this is a self patching app, starts the self patcher executable to finalize the update. This should only be called if the patcher reports a successful patch (i.e. the Patch Successful event is an ideal place to call this function)
void LaunchApp(): launches the app (i.e. starts the app located at ExecutablePath). Can be useful for launchers launching the main app
void Cancel(): cancels the currently running operation