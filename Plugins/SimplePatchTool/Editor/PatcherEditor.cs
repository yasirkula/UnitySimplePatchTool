using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class PatcherEditor : EditorWindow
	{
		private const string PROJECT_PATH_HOLDER = "Library/SPT_ProjectPath.txt";

		private string projectRootPath;
		private Vector2 scrollPosition;

		private ProjectManager project;
		private bool? projectExists = null;

		[MenuItem( "Window/Simple Patch Tool" )]
		private static void Initialize()
		{
			PatcherEditor window = GetWindow<PatcherEditor>();
			window.titleContent = new GUIContent( "Patcher" );
			window.minSize = new Vector2( 300f, 310f );

			window.Show();
		}

		private void OnEnable()
		{
			projectRootPath = File.Exists( PROJECT_PATH_HOLDER ) ? File.ReadAllText( PROJECT_PATH_HOLDER ) : "";
			CheckProjectExists();
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnUpdate;
			File.WriteAllText( PROJECT_PATH_HOLDER, projectRootPath ?? "" );
		}

		private void OnGUI()
		{
			scrollPosition = GUILayout.BeginScrollView( scrollPosition );

			GUILayout.BeginVertical();
			GUILayout.Space( 5f );

			EditorGUI.BeginChangeCheck();
			projectRootPath = PathField( "Project directory: ", projectRootPath, true );
			if( EditorGUI.EndChangeCheck() )
				CheckProjectExists();

			GUILayout.Space( 5f );

			GUI.enabled = ( project == null || !project.IsRunning ) && projectExists.HasValue && !projectExists.Value;

			if( GUILayout.Button( "Create Project", GUILayout.Height( 30 ) ) )
			{
				project = new ProjectManager( projectRootPath );
				project.CreateProject();

				ProjectInfo projectInfo = project.LoadProjectInfo();
				projectInfo.IgnoredPaths.Add( "*output_log.txt" );
				project.SaveProjectInfo( projectInfo );

				EditorApplication.update -= OnUpdate;
				EditorApplication.update += OnUpdate;

				CheckProjectExists();

				EditorUtility.DisplayDialog( "Self Patcher", "If this is a self patching app (i.e. this app will update itself), you'll need to generate a self patcher. See README for more info.", "Got it!" );
			}

			GUI.enabled = ( project == null || !project.IsRunning ) && projectExists.HasValue && projectExists.Value;

			if( GUILayout.Button( "Generate Patch", GUILayout.Height( 30 ) ) )
			{
				project = new ProjectManager( projectRootPath );
				if( project.GeneratePatch() )
				{
					Debug.Log( "<b>Operation started</b>" );

					EditorApplication.update -= OnUpdate;
					EditorApplication.update += OnUpdate;
				}
				else
					Debug.LogWarning( "<b>Couldn't start the operation. Maybe it is already running?</b>" );
			}

			DrawHorizontalLine();

			if( GUILayout.Button( "Update Download Links", GUILayout.Height( 30 ) ) )
			{
				project = new ProjectManager( projectRootPath );
				project.UpdateDownloadLinks();

				EditorApplication.update -= OnUpdate;
				EditorApplication.update += OnUpdate;
			}

			DrawHorizontalLine();

			if( GUILayout.Button( "Sign XMLs", GUILayout.Height( 30 ) ) )
			{
				ProjectManager project = new ProjectManager( projectRootPath );
				SecurityUtils.SignXMLsWithKeysInDirectory( project.GetXMLFiles( true, true ), project.utilitiesPath );

				EditorUtility.DisplayDialog( "Security", "Don't share your private key with unknown parties!", "Got it!" );
				Debug.Log( "<b>Operation successful...</b>" );
			}

			if( GUILayout.Button( "Verify Signed XMLs", GUILayout.Height( 30 ) ) )
			{
				string[] invalidXmls;

				ProjectManager project = new ProjectManager( projectRootPath );
				if( !SecurityUtils.VerifyXMLsWithKeysInDirectory( project.GetXMLFiles( true, true ), project.utilitiesPath, out invalidXmls ) )
				{
					Debug.Log( "<b>The following XMLs could not be verified:</b>" );
					for( int i = 0; i < invalidXmls.Length; i++ )
						Debug.Log( invalidXmls[i] );
				}
				else
					Debug.Log( "<b>All XMLs are verified...</b>" );
			}

			GUI.enabled = true;

			DrawHorizontalLine();

			if( GUILayout.Button( "Help", GUILayout.Height( 25 ) ) )
				Application.OpenURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki" );

			if( GUILayout.Button( "Open Legacy Window", GUILayout.Height( 25 ) ) )
				PatcherEditorLegacy.Initialize();

			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}

		private void OnUpdate()
		{
			if( project == null )
			{
				EditorApplication.update -= OnUpdate;
				return;
			}

			string log = project.FetchLog();
			while( log != null )
			{
				Debug.Log( log );
				log = project.FetchLog();
			}

			if( !project.IsRunning )
			{
				if( project.Result == PatchResult.Failed )
					Debug.Log( "<b>Operation failed...</b>" );
				else
					Debug.Log( "<b>Operation successful...</b>" );

				project = null;
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

		private void CheckProjectExists()
		{
			projectRootPath = projectRootPath == null ? "" : projectRootPath.Trim();

			if( string.IsNullOrEmpty( projectRootPath ) )
				projectExists = null;
			else
			{
				DirectoryInfo projectDir = new DirectoryInfo( projectRootPath );
				if( !projectDir.Exists )
					projectExists = false;
				else
					projectExists = projectDir.GetFiles( PatchParameters.PROJECT_SETTINGS_FILENAME ).Length > 0;
			}
		}

		private void DrawHorizontalLine()
		{
			GUILayout.Space( 5 );
			GUILayout.Box( "", GUILayout.ExpandWidth( true ), GUILayout.Height( 1 ) );
			GUILayout.Space( 5 );
		}
	}
}