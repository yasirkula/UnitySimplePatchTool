= Simple Patch Tool =

Online documentation & example code available at: https://github.com/yasirkula/UnitySimplePatchTool
E-mail: yasirkula@gmail.com

1. ABOUT
SimplePatchTool is a C# library for patching standalone applications with binary diff and self patching support.

2. SETUP
- set "Api Compatibility Level" to .NET 2.0 or higher (i.e. don't use .NET 2.0 Subset or .NET Standard 2.0) in Edit-Project Settings-Player
- (optional) in Edit-Project Settings-Player, enable "Run In Background" so that SimplePatchTool can continue running while the application is minimized/not focused

3. USAGE
Wiki available at: https://github.com/yasirkula/UnitySimplePatchTool/wiki

4. SELF PATCHER
Self patching applications (apps that update themselves) need a companion app called self patcher to be able to patch themselves because apps can't patch themselves in conventional ways while they are still running. Instead, application will apply the patch files to cache directory and then launch the self patcher while also closing itself so that the self patcher can update the app's files using the files in the cache.

To create a self patcher, see: https://github.com/yasirkula/SimplePatchTool/wiki/Creating-Self-Patcher-Executable

While creating patches, you'll learn where to put the self patcher's files at on the wiki.