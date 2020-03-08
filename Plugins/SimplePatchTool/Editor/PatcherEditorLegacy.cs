using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class PatcherEditorLegacy : EditorWindow
	{
		private readonly string[] TABS = new string[] { "CREATE", "UPDATE", "SECURITY" };
		private const string CONSOLE_COMMAND_IGNORED_PATHS_HOLDER = "PATH/TO/ignoredPaths.txt";

		// Create fields
		private string c_RootPath = "", c_PrevRoot = "", c_PrevOutputPath = "", c_OutputPath = "", c_Name = "", c_Version = "", c_IgnoredPaths = "*output_log.txt\n";
		private bool c_CreateRepair = true, c_CreateInstaller = true, c_SkipUnchangedPatchFiles = false;
		private CompressionFormat c_RepairComp = CompressionFormat.LZMA, c_IncrementalComp = CompressionFormat.LZMA, c_InstallerComp = CompressionFormat.LZMA;

		// Update fields
		private string u_versionInfoPath = "", u_downloadLinksPath = "", u_downloadLinksContents = "\n\n";

		// Security fields
		private string s_xmlPath = "", s_publicKeyPath = "", s_PrivateKeyPath = "";

		// Labels of the path fields
		private readonly GUIContent rootPathGUI = new GUIContent( "Root path: ", "Path of the up-to-date (latest) version/build of your application" );
		private readonly GUIContent prevRootGUI = new GUIContent( "Previous version path: ", "(Optional) Path of the previous version/build of your application. Providing a path will create an incremental patch. If this is the first release of your app, leave it blank" );
		private readonly GUIContent prevOutputPathGUI = new GUIContent( "Previous version output path: ", "(Optional) Path of the directory that holds the previous version's patch files (the root directory that holds VersionInfo.info and folders like RepairPatch). If provided, repair patch files for unchanged files will simply be copied from the previous version's patch files instead of being recalculated. If this is the first release of your app, leave it blank" );
		private readonly GUIContent outputPathGUI = new GUIContent( "Output path: ", "Patch files will be generated in this directory, must be empty" );
		private readonly GUIContent createIncrementalGUI = new GUIContent( "Create incremental patch: ", "This value shows whether or not an incremental patch will be generated. For an incremental patch to be generated, \"Previous version path\" must not be blank" );
		private readonly GUIContent skipUnchangedPatchFilesGUI = new GUIContent( "Skip unchanged patch files: ", "If set to true, patch files won't be generated for files that didn't change since the last version. This could reduce bandwidth usage while uploading the generated patch files to the server. Note that if \"Previous version output path\" is blank, this value will have no effect" );
		private readonly GUIContent versionInfoPathGUI = new GUIContent( "VersionInfo path: ", "Path of the VersionInfo.info file on your computer" );
		private readonly GUIContent downloadLinksPathGUI = new GUIContent( "Download links holder: ", "Path of the download links holder file on your computer" );
		private readonly GUIContent xmlPathGUI = new GUIContent( "XML file: ", "Path of the xml file to sign" );
		private readonly GUIContent publicKeyPathGUI = new GUIContent( "Public RSA key: ", "Path of the public RSA key" );
		private readonly GUIContent privateKeyPathGUI = new GUIContent( "Private RSA key: ", "Path of the private RSA key" );

		private int activeTab;
		private Vector2 scrollPosition;

		private PatchCreator patchCreator;

		public static void Initialize()
		{
			PatcherEditorLegacy window = GetWindow<PatcherEditorLegacy>();
			window.titleContent = new GUIContent( "Patcher" );
			window.minSize = new Vector2( 300f, 355f );

			window.c_Name = PatchUtils.IsProjectNameValid( Application.productName ) ? Application.productName : "MyProject";
			window.c_Version = Application.version;

			window.Show();
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
		}

		private void OnGUI()
		{
			scrollPosition = GUILayout.BeginScrollView( scrollPosition );
			activeTab = GUILayout.Toolbar( activeTab, TABS, GUILayout.Height( 25f ) );
			GUILayout.BeginVertical();
			GUILayout.Space( 5f );

			if( activeTab == 0 )
				DrawCreateTab();
			else if( activeTab == 1 )
				DrawUpdateTab();
			else if( activeTab == 2 )
				DrawSecurityTab();

			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}

		private void DrawCreateTab()
		{
			c_RootPath = PathField( rootPathGUI, c_RootPath, true );
			c_PrevRoot = PathField( prevRootGUI, c_PrevRoot, true );
			c_PrevOutputPath = PathField( prevOutputPathGUI, c_PrevOutputPath, true );
			c_OutputPath = PathField( outputPathGUI, c_OutputPath, true );

			c_Name = EditorGUILayout.TextField( "Project name: ", c_Name );
			c_Version = EditorGUILayout.TextField( "Project version: ", c_Version );

			c_CreateRepair = EditorGUILayout.Toggle( "Create repair patch: ", c_CreateRepair );
			c_CreateInstaller = EditorGUILayout.Toggle( "Create installer patch: ", c_CreateInstaller );

			GUI.enabled = false;
			EditorGUILayout.Toggle( createIncrementalGUI, !string.IsNullOrEmpty( c_PrevRoot ) );

			if( !string.IsNullOrEmpty( c_PrevOutputPath ) )
				GUI.enabled = true;

			c_SkipUnchangedPatchFiles = EditorGUILayout.Toggle( skipUnchangedPatchFilesGUI, c_SkipUnchangedPatchFiles );
			GUI.enabled = true;

			c_RepairComp = (CompressionFormat) EditorGUILayout.EnumPopup( "Repair patch compression: ", c_RepairComp );
			c_IncrementalComp = (CompressionFormat) EditorGUILayout.EnumPopup( "Incremental patch compression: ", c_IncrementalComp );
			c_InstallerComp = (CompressionFormat) EditorGUILayout.EnumPopup( "Installer patch compression: ", c_InstallerComp );

			GUILayout.Label( "Ignored paths (one path per line): " );
			c_IgnoredPaths = EditorGUILayout.TextArea( c_IgnoredPaths );

			GUILayout.Space( 10f );

			if( GUILayout.Button( "Create Patch", GUILayout.Height( 35f ) ) )
			{
				c_RootPath = c_RootPath.Trim();
				c_PrevRoot = c_PrevRoot.Trim();
				c_PrevOutputPath = c_PrevOutputPath.Trim();
				c_OutputPath = c_OutputPath.Trim();
				c_IgnoredPaths = c_IgnoredPaths.Trim();
				c_Name = c_Name.Trim();
				c_Version = c_Version.Trim();

				if( c_RootPath.Length == 0 || c_OutputPath.Length == 0 || c_Name.Length == 0 || c_Version.Length == 0 )
					return;

				patchCreator = new PatchCreator( c_RootPath, c_OutputPath, c_Name, c_Version );
				patchCreator.CreateIncrementalPatch( c_PrevRoot.Length > 0, c_PrevRoot ).CreateRepairPatch( c_CreateRepair ).CreateInstallerPatch( c_CreateInstaller ).
					SetCompressionFormat( c_RepairComp, c_InstallerComp, c_IncrementalComp ).SetPreviousPatchFilesRoot( c_PrevOutputPath, c_SkipUnchangedPatchFiles );

				if( c_IgnoredPaths.Length > 0 )
					patchCreator.AddIgnoredPaths( c_IgnoredPaths.Replace( "\r", "" ).Split( '\n' ) );

				if( !EditorUtility.DisplayDialog( "Self Patcher Executable", "If this is a self patching app (i.e. this app will update itself), you'll first need to generate a self patcher executable. See README for more info.", "Got it!", "Oh, I forgot it!" ) )
					return;

				if( patchCreator.Run() )
				{
					Debug.Log( "<b>Patch creator started</b>" );

					EditorApplication.update -= OnUpdate;
					EditorApplication.update += OnUpdate;
				}
				else
					Debug.LogWarning( "<b>Couldn't start patch creator. Maybe it is already running?</b>" );
			}

			if( GUILayout.Button( "Generate Console Command", GUILayout.Height( 25f ) ) )
			{
				string command = string.Format( "Patcher create -root=\"{0}\" -out=\"{1}\" -name=\"{2}\" -version=\"{3}\" -compressionRepair=\"{4}\" -compressionIncremental=\"{5}\" -compressionInstaller=\"{6}\"", c_RootPath, c_OutputPath, c_Name, c_Version, c_RepairComp, c_IncrementalComp, c_InstallerComp );
				if( c_PrevRoot.Length > 0 )
					command += string.Concat( " -prevRoot=\"", c_PrevRoot, "\"" );
				if( c_PrevOutputPath.Length > 0 )
					command += string.Concat( " -prevPatchRoot=\"", c_PrevOutputPath, "\"" );
				if( c_IgnoredPaths.Length > 0 )
					command += string.Concat( " -ignoredPaths=\"", CONSOLE_COMMAND_IGNORED_PATHS_HOLDER, "\"" );
				if( !c_CreateRepair )
					command += " -dontCreateRepairPatch";
				if( !c_CreateInstaller )
					command += " -dontCreateInstallerPatch";
				if( c_SkipUnchangedPatchFiles )
					command += " -skipUnchangedPatchFiles";

				Debug.Log( command );

				if( c_IgnoredPaths.Length > 0 )
					Debug.Log( string.Concat( "You have to insert the following ignored path(s) to \"", CONSOLE_COMMAND_IGNORED_PATHS_HOLDER, "\":\n", c_IgnoredPaths ) );
			}

			if( GUILayout.Button( "Help", GUILayout.Height( 25f ) ) )
				Application.OpenURL( "https://github.com/yasirkula/SimplePatchTool/wiki/Legacy:-Generating-Patch#via-unity-plugin" );
		}

		private void DrawUpdateTab()
		{
			u_versionInfoPath = PathField( versionInfoPathGUI, u_versionInfoPath, false );
			u_downloadLinksPath = PathField( downloadLinksPathGUI, u_downloadLinksPath, false );

			GUILayout.Label( "Or, paste download links here (one link per line): " );
			u_downloadLinksContents = EditorGUILayout.TextArea( u_downloadLinksContents );

			if( GUILayout.Button( "Update Download Links", GUILayout.Height( 35f ) ) )
			{
				u_versionInfoPath = u_versionInfoPath.Trim();
				u_downloadLinksPath = u_downloadLinksPath.Trim();
				u_downloadLinksContents = u_downloadLinksContents.Trim();

				if( u_versionInfoPath.Length == 0 || ( u_downloadLinksPath.Length == 0 && u_downloadLinksContents.Length == 0 ) )
					return;

				PatchUpdater patchUpdater = new PatchUpdater( u_versionInfoPath, ( log ) => Debug.Log( log ) );
				bool updateSuccessful;
				if( u_downloadLinksPath.Length > 0 )
					updateSuccessful = patchUpdater.UpdateDownloadLinks( u_downloadLinksPath );
				else
				{
					Dictionary<string, string> downloadLinks = new Dictionary<string, string>();
					string[] downloadLinksSplit = u_downloadLinksContents.Replace( "\r", "" ).Split( '\n' );
					for( int i = 0; i < downloadLinksSplit.Length; i++ )
					{
						string downloadLinkRaw = downloadLinksSplit[i].Trim();
						if( string.IsNullOrEmpty( downloadLinkRaw ) )
							continue;

						int separatorIndex = downloadLinkRaw.LastIndexOf( ' ' );
						if( separatorIndex == -1 )
							continue;

						downloadLinks[downloadLinkRaw.Substring( 0, separatorIndex )] = downloadLinkRaw.Substring( separatorIndex + 1 );
					}

					updateSuccessful = patchUpdater.UpdateDownloadLinks( downloadLinks );
				}

				if( updateSuccessful )
					patchUpdater.SaveChanges();

				Debug.Log( "Result: " + updateSuccessful );
			}

			if( GUILayout.Button( "Help", GUILayout.Height( 25f ) ) )
				Application.OpenURL( "https://github.com/yasirkula/SimplePatchTool/wiki/Legacy:-Updating-Download-Links#via-unity-plugin" );
		}

		private void DrawSecurityTab()
		{
			s_xmlPath = PathField( xmlPathGUI, s_xmlPath, false );
			s_publicKeyPath = PathField( publicKeyPathGUI, s_publicKeyPath, false );
			s_PrivateKeyPath = PathField( privateKeyPathGUI, s_PrivateKeyPath, false );

			if( GUILayout.Button( "Sign XML", GUILayout.Height( 35f ) ) )
			{
				s_xmlPath = s_xmlPath.Trim();
				s_PrivateKeyPath = s_PrivateKeyPath.Trim();

				if( s_xmlPath.Length == 0 || s_PrivateKeyPath.Length == 0 )
					return;

				XMLSigner.SignXMLFile( s_xmlPath, File.ReadAllText( s_PrivateKeyPath ) );
			}

			if( GUILayout.Button( "Verify XML", GUILayout.Height( 35f ) ) )
			{
				s_xmlPath = s_xmlPath.Trim();
				s_publicKeyPath = s_publicKeyPath.Trim();

				if( s_xmlPath.Length == 0 || s_publicKeyPath.Length == 0 )
					return;

				Debug.Log( "Is genuine: " + XMLSigner.VerifyXMLFile( s_xmlPath, File.ReadAllText( s_publicKeyPath ) ) );
			}

			GUILayout.Space( 10f );

			EditorGUILayout.HelpBox( "Store your private key in a safe location and don't share it with unknown parties!", MessageType.Warning );

			if( GUILayout.Button( "Create RSA Key Pair", GUILayout.Height( 35f ) ) )
			{
				string selectedPath = EditorUtility.OpenFolderPanel( "Create keys at", "", "" );
				if( string.IsNullOrEmpty( selectedPath ) || !Directory.Exists( selectedPath ) )
					return;

				string publicKey, privateKey;
				SecurityUtils.CreateRSAKeyPair( out publicKey, out privateKey );

				File.WriteAllText( Path.Combine( selectedPath, "public.key" ), publicKey );
				File.WriteAllText( Path.Combine( selectedPath, "private.key" ), privateKey );

				AssetDatabase.Refresh();
			}

			if( GUILayout.Button( "Help", GUILayout.Height( 25f ) ) )
				Application.OpenURL( "https://github.com/yasirkula/SimplePatchTool/wiki/Signing-&-Verifying-Patches#built-in-method" );
		}

		private void OnUpdate()
		{
			if( patchCreator == null )
			{
				EditorApplication.update -= OnUpdate;
				return;
			}

			string log = patchCreator.FetchLog();
			while( log != null )
			{
				Debug.Log( log );
				log = patchCreator.FetchLog();
			}

			if( !patchCreator.IsRunning )
			{
				if( patchCreator.Result == PatchResult.Failed )
					Debug.Log( "<b>Operation failed...</b>" );
				else
					Debug.Log( "<b>Operation successful...</b>" );

				patchCreator = null;
				EditorApplication.update -= OnUpdate;
			}
		}

		private string PathField( GUIContent label, string path, bool isDirectory )
		{
			GUILayout.BeginHorizontal();
			path = EditorGUILayout.TextField( label, path );
			if( GUILayout.Button( "o", GUILayout.Width( 25f ) ) )
			{
				string selectedPath = isDirectory ? EditorUtility.OpenFolderPanel( "Choose a directory", "", "" ) : EditorUtility.OpenFilePanel( "Choose a file", "", "" );
				if( !string.IsNullOrEmpty( selectedPath ) )
					path = selectedPath;

				GUIUtility.keyboardControl = 0; // Remove focus from active text field
			}
			GUILayout.EndHorizontal();

			return path;
		}
	}
}