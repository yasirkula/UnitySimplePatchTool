using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class PatcherEditor : EditorWindow
	{
		private readonly string[] TABS = new string[] { "CREATE", "UPDATE", "SECURITY" };

		// Create fields
		private string c_RootPath = "", c_PrevRoot = "", c_OutputPath = "", c_IgnoredFiles = "", c_Name = "", c_Version = "";
		private bool c_CreateRepair = true, c_IgnoreOutputLog = true;

		// Update fields
		private string u_versionInfoPath = "", u_downloadLinksPath = "";

		// Security fields
		private string s_xmlPath = "", s_publicKeyPath = "", s_PrivateKeyPath = "";

		private int activeTab;
		private Vector2 scrollPosition;

		private PatchCreator patchCreator;

		[MenuItem( "Window/Simple Patch Tool", priority = 20 )]
		private static void Initialize()
		{
			PatcherEditor window = GetWindow<PatcherEditor>();
			window.titleContent = new GUIContent( "Patcher" );
			window.minSize = new Vector2( 300f, 275f );

			window.c_Name = Application.productName;
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
			c_RootPath = PathField( "Root path: ", c_RootPath, true );
			c_PrevRoot = PathField( "Previous version path: ", c_PrevRoot, true );
			c_OutputPath = PathField( "Output path: ", c_OutputPath, true );
			c_IgnoredFiles = PathField( "Ignored files holder: ", c_IgnoredFiles, false );

			c_Name = EditorGUILayout.TextField( "Project name: ", c_Name );
			c_Version = EditorGUILayout.TextField( "Project version: ", c_Version );

			c_CreateRepair = EditorGUILayout.Toggle( "Create repair patch: ", c_CreateRepair );
			c_IgnoreOutputLog = EditorGUILayout.Toggle( "Ignore output_log.txt: ", c_IgnoreOutputLog );

			GUILayout.Space( 10f );

			if( GUILayout.Button( "Create Patch", GUILayout.Height( 35f ) ) )
			{
				c_RootPath = c_RootPath.Trim();
				c_PrevRoot = c_PrevRoot.Trim();
				c_OutputPath = c_OutputPath.Trim();
				c_IgnoredFiles = c_IgnoredFiles.Trim();
				c_Name = c_Name.Trim();
				c_Version = c_Version.Trim();

				if( string.IsNullOrEmpty( c_RootPath ) || string.IsNullOrEmpty( c_OutputPath ) || string.IsNullOrEmpty( c_Name ) || string.IsNullOrEmpty( c_Version ) )
					return;

				patchCreator = new PatchCreator( c_RootPath, c_OutputPath, c_Name, c_Version );
				patchCreator.CreateIncrementalPatch( c_PrevRoot.Length > 0, c_PrevRoot ).CreateRepairPatch( c_CreateRepair );
				if( c_IgnoredFiles.Length > 0 )
					patchCreator.LoadIgnoredPathsFromFile( c_IgnoredFiles );
				if( c_IgnoreOutputLog )
					patchCreator.AddIgnoredPath( "*output_log.txt" );

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
				string command = string.Format( "Patcher create -root=\"{0}\" -out=\"{1}\" -name=\"{2}\" -version=\"{3}\"", c_RootPath, c_OutputPath, c_Name, c_Version );
				if( c_PrevRoot.Length > 0 )
					command += string.Concat( " -prevRoot=\"", c_PrevRoot, "\"" );
				if( c_IgnoredFiles.Length > 0 )
					command += string.Concat( " -ignoredPaths=\"", c_IgnoredFiles, "\"" );
				if( !c_CreateRepair )
					command += " -dontCreateRepairPatch";

				Debug.Log( command );

				if( c_IgnoreOutputLog )
				{
					if( c_IgnoredFiles.Length > 0 )
						Debug.Log( string.Concat( "You have to add *output_log.txt to ", c_IgnoredFiles, " manually" ) );
					else
						Debug.Log( "To ignore output_log.txt, you have to provide an Ignored files holder and add *output_log.txt to it manually" );
				}
			}
		}

		private void DrawUpdateTab()
		{
			u_versionInfoPath = PathField( "VersionInfo path: ", u_versionInfoPath, false );
			u_downloadLinksPath = PathField( "Download links holder: ", u_downloadLinksPath, false );

			if( GUILayout.Button( "Update Download Links", GUILayout.Height( 35f ) ) )
			{
				u_versionInfoPath = u_versionInfoPath.Trim();
				u_downloadLinksPath = u_downloadLinksPath.Trim();

				if( string.IsNullOrEmpty( u_versionInfoPath ) || string.IsNullOrEmpty( u_downloadLinksPath ) )
					return;

				PatchUpdater patchUpdater = new PatchUpdater( u_versionInfoPath, ( log ) => Debug.Log( log ) );
				if( patchUpdater.UpdateDownloadLinks( u_downloadLinksPath ) )
				{
					patchUpdater.SaveChanges();
					Debug.Log( "Result: True" );
				}
				else
					Debug.Log( "Result: False" );
			}
		}

		private void DrawSecurityTab()
		{
			s_xmlPath = PathField( "XML file: ", s_xmlPath, false );
			s_publicKeyPath = PathField( "Public RSA key: ", s_publicKeyPath, false );
			s_PrivateKeyPath = PathField( "Private RSA key: ", s_PrivateKeyPath, false );

			if( GUILayout.Button( "Sign XML", GUILayout.Height( 35f ) ) )
			{
				s_xmlPath = s_xmlPath.Trim();
				s_PrivateKeyPath = s_PrivateKeyPath.Trim();

				if( string.IsNullOrEmpty( s_xmlPath ) || string.IsNullOrEmpty( s_PrivateKeyPath ) )
					return;

				XMLSigner.SignXMLFile( s_xmlPath, File.ReadAllText( s_PrivateKeyPath ) );
			}

			if( GUILayout.Button( "Verify XML", GUILayout.Height( 35f ) ) )
			{
				s_xmlPath = s_xmlPath.Trim();
				s_publicKeyPath = s_publicKeyPath.Trim();

				if( string.IsNullOrEmpty( s_xmlPath ) || string.IsNullOrEmpty( s_publicKeyPath ) )
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

				File.WriteAllText( Path.Combine( selectedPath, "rsa_public.bytes" ), publicKey );
				File.WriteAllText( Path.Combine( selectedPath, "rsa_private.bytes" ), privateKey );

				AssetDatabase.Refresh();
			}
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

		private string PathField( string label, string path, bool isDirectory )
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