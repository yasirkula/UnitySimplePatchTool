#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine.UI;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#endif
using Debug = UnityEngine.Debug;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki/Multi-game-Launcher-Tutorial" )]
	public class MultiGameLauncherDemo : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
#pragma warning disable 0649
		[System.Serializable]
		public class GameConfiguration
		{
			[SerializeField]
			private string m_name;
			public string Name { get { return m_name; } }

			[SerializeField]
			[Tooltip( "Name of the game's directory" )]
			private string m_subdirectory = "MyGame";
			public string Subdirectory { get { return m_subdirectory; } }

			[SerializeField]
			[Tooltip( "Game's executable file" )]
			private string m_executable = "Game.exe";
			public string Executable { get { return m_executable; } }

			[SerializeField]
			[Tooltip( "Game's thumbnail image" )]
			private Sprite m_icon;
			public Sprite Icon { get { return m_icon; } }

			[SerializeField]
			[Tooltip( "Game's VersionInfo URL" )]
			private string m_versionInfoURL;
			public string VersionInfoURL { get { return m_versionInfoURL; } }

			public string ExecutablePath { get { return Path.Combine( m_subdirectory, m_executable ); } }

			public void TrimLinks()
			{
				m_name = m_name.Trim();
				m_versionInfoURL = m_versionInfoURL.Trim();
				m_subdirectory = m_subdirectory.Trim();
				m_executable = m_executable.Trim();
			}
		}

		[Header( "Patcher Parameters" )]
		[SerializeField]
		[Tooltip( "Launcher's VersionInfo URL (for self patching)" )]
		private string launcherVersionInfoURL;

		[SerializeField]
		[Tooltip( "Name of the self patcher's executable" )]
		private string selfPatcherExecutable = "SelfPatcher.exe";

		[SerializeField]
		[Tooltip( "Should SimplePatchTool logs be logged to console" )]
		private bool logToConsole = true;

		[Header( "Game Configurations" )]
		[SerializeField]
		[Tooltip( "Subdirectory in which the main app resides" )]
		private string gamesSubdirectory = "Games";

		[SerializeField]
		private GameConfiguration[] games;

		[Header( "XML Verifier Keys (Optional)" )]
		[SerializeField]
		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded VersionInfo.info" )]
		private string versionInfoRSA;

		[SerializeField]
		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded PatchInfo.info" )]
		private string patchInfoRSA;

		[Header( "Other Variables" )]
		[SerializeField]
		[Tooltip( "Patch notes will be fetched from here" )]
		private string patchNotesURL;

		[SerializeField]
		private string forumURL;

		[SerializeField]
		private string websiteURL;

		[Header( "Internal Variables" )]
		[SerializeField]
		private MultiGameLauncherGameHolder gameHolderPrefab;

		[SerializeField]
		private RectTransform gameHolderParent;

		[SerializeField]
		private RectTransform gamesPanel;

		[SerializeField]
		private RectTransform updateLauncherPanel;

		[SerializeField]
		private RectTransform patcherProgressPanel;

		[SerializeField]
		private Text patchNotesText;

		[SerializeField]
		private Text versionCodeText;

		[SerializeField]
		private Text patcherLogText;

		[SerializeField]
		private Text patcherProgressText;

		[SerializeField]
		private Slider patcherProgressbar;

		[SerializeField]
		private Slider patcherOverallProgressbar;

		[SerializeField]
		private Button updateLauncherButton;

		[SerializeField]
		private Button forumButton;

		[SerializeField]
		private Button websiteButton;
#pragma warning restore 0649

		private string launcherDirectory;
		private string gamesDirectory;
		private string selfPatcherPath;

		private MultiGameLauncherGameHolder[] gameHolders;

#if UNITY_EDITOR
		private readonly bool isEditor = true;
#else
		private readonly bool isEditor = false;
#endif

		private void Awake()
		{
			if( isEditor )
			{
				Debug.LogWarning( "Can't test the launcher on Editor!" );
				Destroy( this );

				return;
			}

			launcherVersionInfoURL = launcherVersionInfoURL.Trim();
			patchNotesURL = patchNotesURL.Trim();
			forumURL = forumURL.Trim();
			websiteURL = websiteURL.Trim();
			versionInfoRSA = versionInfoRSA.Trim();
			patchInfoRSA = patchInfoRSA.Trim();

			patchNotesText.text = "";
			patcherLogText.text = "";
			patcherProgressText.text = "";
			patcherProgressbar.value = 0;
			patcherOverallProgressbar.value = 0;

			forumButton.onClick.AddListener( ForumButtonClicked );
			websiteButton.onClick.AddListener( WebsiteButtonClicked );

			launcherDirectory = Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() );
			gamesDirectory = Path.Combine( launcherDirectory, gamesSubdirectory );
			selfPatcherPath = PatchUtils.GetDefaultSelfPatcherExecutablePath( selfPatcherExecutable );

			string currentVersion = PatchUtils.GetCurrentAppVersion();
			versionCodeText.text = string.IsNullOrEmpty( currentVersion ) ? "" : ( "v" + currentVersion );

			gameHolders = new MultiGameLauncherGameHolder[games.Length];
			for( int i = 0; i < games.Length; i++ )
			{
				games[i].TrimLinks();

				MultiGameLauncherGameHolder gameHolder = (MultiGameLauncherGameHolder) Instantiate( gameHolderPrefab, gameHolderParent, false );
				gameHolder.Initialize( games[i] );
				gameHolder.OnPlayButtonClicked += PlayButtonClicked;
				gameHolder.OnPatchButtonClicked += PatchButtonClicked;
				gameHolder.PlayButtonSetEnabled( File.Exists( Path.Combine( gamesDirectory, games[i].ExecutablePath ) ) );
				gameHolders[i] = gameHolder;

				StartCoroutine( CheckForUpdates( gameHolder ) );
			}

			// To resolve a Unity bug
			gamesPanel.gameObject.SetActive( false );
			gamesPanel.gameObject.SetActive( true );

			if( !string.IsNullOrEmpty( patchNotesURL ) )
				StartCoroutine( FetchPatchNotes() );

			// Can't test the launcher on Editor
			StartLauncherPatch();
		}

		private void PlayButtonClicked( MultiGameLauncherGameHolder gameHolder )
		{
			FileInfo game = new FileInfo( Path.Combine( gamesDirectory, gameHolder.Configuration.ExecutablePath ) );
			if( game.Exists )
				Process.Start( new ProcessStartInfo( game.FullName ) { WorkingDirectory = game.DirectoryName } );
			else
				Debug.LogError( game.FullName + " does not exist!" );
		}

		private void PatchButtonClicked( MultiGameLauncherGameHolder gameHolder )
		{
			ExecutePatch( gameHolder );
		}

		private void ForumButtonClicked()
		{
			if( !string.IsNullOrEmpty( forumURL ) )
				Application.OpenURL( forumURL );
		}

		private void WebsiteButtonClicked()
		{
			if( !string.IsNullOrEmpty( websiteURL ) )
				Application.OpenURL( websiteURL );
		}

		private void StartLauncherPatch()
		{
			if( string.IsNullOrEmpty( launcherVersionInfoURL ) )
				return;

			SimplePatchTool patcher = InitializePatcher( launcherDirectory, launcherVersionInfoURL );
			PatcherListener patcherListener = new PatcherListener();
			patcherListener.OnVersionInfoFetched += ( versionInfo ) => versionInfo.AddIgnoredPath( gamesSubdirectory + "/" );
			patcherListener.OnVersionFetched += ( currVersion, newVersion ) => versionCodeText.text = "v" + currVersion;
			patcherListener.OnFinish += () =>
			{
				if( patcher.Result == PatchResult.Success )
				{
					updateLauncherPanel.gameObject.SetActive( true );
					updateLauncherButton.onClick.AddListener( () => patcher.ApplySelfPatch( selfPatcherPath, PatchUtils.GetCurrentExecutablePath() ) );
				}
				else if( patcher.Result == PatchResult.AlreadyUpToDate )
					Debug.Log( "Launcher is up-to-date!" );
				else
					Debug.LogError( "Something went wrong with launcher's patch: " + patcher.FailDetails );
			};

			if( !patcher.SetListener( patcherListener ).Run( true ) ) // true: Self patching
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private IEnumerator CheckForUpdates( MultiGameLauncherGameHolder gameHolder )
		{
			if( string.IsNullOrEmpty( gameHolder.Configuration.VersionInfoURL ) )
				yield break;

			// Check if there are any updates for the game
			SimplePatchTool patcher = InitializePatcher( Path.Combine( gamesDirectory, gameHolder.Configuration.Subdirectory ), gameHolder.Configuration.VersionInfoURL );

			// = checkVersionOnly =
			// true (default): only version number (e.g. 1.0) is compared against VersionInfo to see if there is an update
			// false: hashes and sizes of the local files are compared against VersionInfo (if there are any different/missing files, we'll patch the app)
			if( patcher.CheckForUpdates( true ) )
			{
				while( patcher.IsRunning )
					yield return null;

				if( patcher.Result == PatchResult.Success )
				{
					Debug.Log( "There is an update for " + gameHolder.Configuration.Name );
					gameHolder.PatchButtonSetVisible( true );
				}
			}
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private void ExecutePatch( MultiGameLauncherGameHolder gameHolder )
		{
			if( string.IsNullOrEmpty( gameHolder.Configuration.VersionInfoURL ) )
				return;

			SimplePatchTool patcher = InitializePatcher( Path.Combine( gamesDirectory, gameHolder.Configuration.Subdirectory ), gameHolder.Configuration.VersionInfoURL );
			PatcherListener patcherListener = new PatcherListener();
			patcherListener.OnLogReceived += ( log ) =>
			{
				if( logToConsole )
					Debug.Log( log );

				patcherLogText.text = log;
			};
			patcherListener.OnProgressChanged += ( progress ) =>
			{
				if( logToConsole )
					Debug.Log( string.Concat( progress.Percentage, "% ", progress.ProgressInfo ) );

				patcherProgressText.text = progress.ProgressInfo;
				patcherProgressbar.value = progress.Percentage;
			};
			patcherListener.OnOverallProgressChanged += ( progress ) => patcherOverallProgressbar.value = progress.Percentage;
			patcherListener.OnFinish += () =>
			{
				HidePatcherProgressPanel();

				for( int i = 0; i < gameHolders.Length; i++ )
					gameHolders[i].PatchButtonSetEnabled( true );

				if( patcher.Result == PatchResult.Success || patcher.Result == PatchResult.AlreadyUpToDate )
				{
					Debug.Log( gameHolder.Configuration.Name + " is " + ( patcher.Result == PatchResult.Success ? "patched!" : "already up-to-date" ) );
					gameHolder.PlayButtonSetEnabled( true );
					gameHolder.PatchButtonSetVisible( false );
				}
				else
					Debug.LogError( "Something went wrong with " + gameHolder.Configuration.Name + "'s patch: " + patcher.FailDetails );
			};

			if( patcher.SetListener( patcherListener ).Run( false ) ) // false: Not self patching
			{
				ShowPatcherProgressPanel();

				for( int i = 0; i < gameHolders.Length; i++ )
					gameHolders[i].PatchButtonSetEnabled( false );

				gameHolder.PlayButtonSetEnabled( false );
			}
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private SimplePatchTool InitializePatcher( string rootPath, string versionInfoURL )
		{
			SimplePatchTool patcher = SPTUtils.CreatePatcher( rootPath, versionInfoURL );

			if( !string.IsNullOrEmpty( versionInfoRSA ) )
				patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );

			if( !string.IsNullOrEmpty( patchInfoRSA ) )
				patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );

			return patcher;
		}

		private void ShowPatcherProgressPanel()
		{
			patcherProgressPanel.gameObject.SetActive( true );
			gamesPanel.sizeDelta -= new Vector2( 0f, patcherProgressPanel.sizeDelta.y );
		}

		private void HidePatcherProgressPanel()
		{
			patcherProgressPanel.gameObject.SetActive( false );
			gamesPanel.sizeDelta += new Vector2( 0f, patcherProgressPanel.sizeDelta.y );
		}

		private IEnumerator FetchPatchNotes()
		{
			// I find WWW more reliable while fetching patch notes since UnityWebRequest fails
			// with "Cannot connect to destination host" error message half the time for me.
			// But WWW is deprecated on Unity 2018.3, so use UnityWebRequest in that situation
#if UNITY_2018_3_OR_NEWER
			UnityWebRequest webRequest = UnityWebRequest.Get( patchNotesURL );
			yield return webRequest.SendWebRequest();
			
			bool webRequestError = webRequest.isHttpError || webRequest.isNetworkError;
			if( !webRequestError )
				patchNotesText.text = webRequest.downloadHandler.text;
			else
				Debug.LogError( "Can't fetch patch notes: " + webRequest.error );
#else
			WWW webRequest = new WWW( patchNotesURL );
			yield return webRequest;

			if( string.IsNullOrEmpty( webRequest.error ) )
				patchNotesText.text = webRequest.text;
			else
				Debug.LogError( "Can't fetch patch notes: " + webRequest.error );
#endif
		}
#endif
	}
}