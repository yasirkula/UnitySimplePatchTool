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
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki/Launcher-Tutorial" )]
	public class LauncherDemo : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
#pragma warning disable 0649
		[Header( "Patcher Parameters" )]
		[SerializeField]
		[Tooltip( "Launcher's VersionInfo URL (for self patching)" )]
		private string launcherVersionInfoURL;

		[SerializeField]
		[Tooltip( "Main app's VersionInfo URL (to patch Main App Subdirectory)" )]
		private string mainAppVersionInfoURL;

		[SerializeField]
		[Tooltip( "Subdirectory in which the main app resides" )]
		private string mainAppSubdirectory = "MainApp";

		[SerializeField]
		[Tooltip( "The file in Main App Subdirectory that will be launched when Play is pressed" )]
		private string mainAppExecutable = "MyApp.exe";

		[SerializeField]
		[Tooltip( "Name of the self patcher's executable" )]
		private string selfPatcherExecutable = "SelfPatcher.exe";

		[SerializeField]
		[Tooltip( "Should SimplePatchTool logs be logged to console" )]
		private bool logToConsole = true;

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
		private Button patchButton;

		[SerializeField]
		private Button repairButton;

		[SerializeField]
		private Button playButton;

		[SerializeField]
		private Button forumButton;

		[SerializeField]
		private Button websiteButton;
#pragma warning restore 0649

		private string launcherDirectory;
		private string mainAppDirectory;
		private string selfPatcherPath;

		private SimplePatchTool patcher;
		private PatcherListener patcherListener;

		private bool isPatchingLauncher;

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
			mainAppVersionInfoURL = mainAppVersionInfoURL.Trim();
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

			patchButton.onClick.AddListener( PatchButtonClicked );
			repairButton.onClick.AddListener( RepairButtonClicked );
			playButton.onClick.AddListener( PlayButtonClicked );
			forumButton.onClick.AddListener( ForumButtonClicked );
			websiteButton.onClick.AddListener( WebsiteButtonClicked );

			launcherDirectory = Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() );
			mainAppDirectory = Path.Combine( launcherDirectory, mainAppSubdirectory );
			selfPatcherPath = PatchUtils.GetDefaultSelfPatcherExecutablePath( selfPatcherExecutable );

			string currentVersion = PatchUtils.GetCurrentAppVersion();
			versionCodeText.text = string.IsNullOrEmpty( currentVersion ) ? "" : ( "v" + currentVersion );

			patcherListener = new PatcherListener();
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
			patcherListener.OnVersionInfoFetched += ( versionInfo ) =>
			{
				if( isPatchingLauncher )
					versionInfo.AddIgnoredPath( mainAppSubdirectory + "/" );
			};
			patcherListener.OnVersionFetched += ( currVersion, newVersion ) =>
			{
				if( isPatchingLauncher )
					versionCodeText.text = "v" + currVersion;
			};
			patcherListener.OnFinish += () =>
			{
				if( patcher.Operation == PatchOperation.CheckingForUpdates )
					CheckForUpdatesFinished();
				else
					PatchFinished();
			};

			if( !string.IsNullOrEmpty( patchNotesURL ) )
				StartCoroutine( FetchPatchNotes() );

			if( !StartLauncherPatch() )
				StartMainAppPatch( true );
		}

		private void OnDestroy()
		{
			if( patcher != null )
			{
				// Stop the patcher since this script can no longer control it
				patcher.SetListener( null );
				patcher.Cancel();
				patcher = null;
			}
		}

		private void PatchButtonClicked()
		{
			if( patcher != null && !patcher.IsRunning )
				ExecutePatch();
		}

		private void RepairButtonClicked()
		{
			StartMainAppPatch( false );
		}

		private void PlayButtonClicked()
		{
			if( patcher != null && patcher.IsRunning && patcher.Operation != PatchOperation.CheckingForUpdates )
				return;

			FileInfo mainApp = new FileInfo( Path.Combine( mainAppDirectory, mainAppExecutable ) );
			if( mainApp.Exists )
			{
				Process.Start( new ProcessStartInfo( mainApp.FullName ) { WorkingDirectory = mainApp.DirectoryName } );
				Process.GetCurrentProcess().Kill();
			}
			else
				Debug.LogWarning( "Main app executable does not exist!" );
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

		private bool StartLauncherPatch()
		{
			if( string.IsNullOrEmpty( launcherVersionInfoURL ) )
				return false;

			if( patcher != null && patcher.IsRunning )
				return false;

			isPatchingLauncher = true;

			InitializePatcher( launcherDirectory, launcherVersionInfoURL );
			CheckForUpdates( false );

			return true;
		}

		private bool StartMainAppPatch( bool checkForUpdates )
		{
			if( string.IsNullOrEmpty( mainAppVersionInfoURL ) )
				return false;

			if( patcher != null && patcher.IsRunning )
				return false;

			isPatchingLauncher = false;

			InitializePatcher( mainAppDirectory, mainAppVersionInfoURL );

			if( checkForUpdates )
				CheckForUpdates( true );
			else
				ExecutePatch();

			return true;
		}

		private void InitializePatcher( string rootPath, string versionInfoURL )
		{
			patcher = SPTUtils.CreatePatcher( rootPath, versionInfoURL ).SetListener( patcherListener );

			if( !string.IsNullOrEmpty( versionInfoRSA ) )
				patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );

			if( !string.IsNullOrEmpty( patchInfoRSA ) )
				patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );
		}

		private void CheckForUpdates( bool checkVersionOnly )
		{
			// = checkVersionOnly =
			// true (default): only version number (e.g. 1.0) is compared against VersionInfo to see if there is an update
			// false: hashes and sizes of the local files are compared against VersionInfo (if there are any different/missing files, we'll patch the app)
			if( patcher.CheckForUpdates( checkVersionOnly ) )
			{
				Debug.Log( "Checking for updates..." );

				patchButton.interactable = false;
				playButton.interactable = true;
			}
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private void ExecutePatch()
		{
			if( patcher.Operation == PatchOperation.ApplyingSelfPatch )
				ApplySelfPatch();
			else if( patcher.Run( isPatchingLauncher ) )
			{
				Debug.Log( "Executing patch..." );

				patchButton.interactable = false;
				playButton.interactable = false;
			}
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private void ApplySelfPatch()
		{
			patcher.ApplySelfPatch( selfPatcherPath, PatchUtils.GetCurrentExecutablePath() );
		}

		private void CheckForUpdatesFinished()
		{
			if( patcher.Result == PatchResult.AlreadyUpToDate )
			{
				// If launcher is already up-to-date, check if there is an update for the main app
				if( isPatchingLauncher )
					StartMainAppPatch( true );
			}
			else if( patcher.Result == PatchResult.Success )
			{
				// There is an update, enable the Patch button
				patchButton.interactable = true;
			}
			else
			{
				// An error occurred, user can click the Patch button to try again
				patchButton.interactable = true;
			}
		}

		private void PatchFinished()
		{
			playButton.interactable = true;

			if( patcher.Result == PatchResult.AlreadyUpToDate )
			{
				// If launcher is already up-to-date, check if there is an update for the main app
				if( isPatchingLauncher )
					StartMainAppPatch( true );
			}
			else if( patcher.Result == PatchResult.Success )
			{
				// If patcher was self patching the launcher, start the self patcher executable
				// Otherwise, we have just updated the main app successfully
				if( patcher.Operation == PatchOperation.SelfPatching )
					ApplySelfPatch();
			}
			else
			{
				// An error occurred, user can click the Patch button to try again
				patchButton.interactable = true;
			}
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