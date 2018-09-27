#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool" )]
	public class LauncherDemo : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
		[Header( "Patcher Parameters" )]
		[SerializeField]
		[Tooltip( "Launcher's VersionInfo URL (for self patching)" )]
		private string launcherVersionInfoURL;

		[SerializeField]
		[Tooltip( "Main app's VersionInfo URL (to patch the mainAppSubdirectory)" )]
		private string mainAppVersionInfoURL;

		[SerializeField]
		[Tooltip( "Subdirectory in which the main app resides" )]
		private string mainAppSubdirectory = "MainApp";

		[SerializeField]
		[Tooltip( "The file in mainAppSubdirectory that will be launched when Play is pressed" )]
		private string mainAppExecutable = "MyApp.exe";

		[SerializeField]
		[Tooltip( "Should SimplePatchTool logs be logged to console" )]
		private bool logToConsole = true;

		[Header( "XML Verifier Keys (Optional)" )]
		[SerializeField]
		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded VersionInfo'es" )]
		private string versionInfoRSA;

		[SerializeField]
		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded PatchInfo'es" )]
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
		private Text patcherLogText;

		[SerializeField]
		private Text patcherProgressText;

		[SerializeField]
		private Slider patcherProgressbar;

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

		private SimplePatchTool patcher;
		private bool isPatchingLauncher;

		private void Awake()
		{
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

			patchButton.onClick.AddListener( PatchButtonClicked );
			repairButton.onClick.AddListener( RepairButtonClicked );
			playButton.onClick.AddListener( PlayButtonClicked );
			forumButton.onClick.AddListener( ForumButtonClicked );
			websiteButton.onClick.AddListener( WebsiteButtonClicked );

			if( !string.IsNullOrEmpty( patchNotesURL ) )
				StartCoroutine( FetchPatchNotes() );

			if( !StartLauncherPatch() )
				StartMainAppPatch( true );
		}

		private void PatchButtonClicked()
		{
#if UNITY_EDITOR
			Debug.LogWarning( "Can't test the launcher on Editor!" );
			return;
#else
			if( patcher != null && !patcher.IsRunning )
				StartCoroutine( ExecutePatch() );
#endif
		}

		private void RepairButtonClicked()
		{
#if UNITY_EDITOR
			Debug.LogWarning( "Can't test the launcher on Editor!" );
			return;
#else
			StartMainAppPatch( false );
#endif
		}

		private void PlayButtonClicked()
		{
			if( patcher != null && patcher.IsRunning && patcher.Operation != PatchOperation.CheckingForUpdates )
				return;

			string mainAppDirectory = Path.Combine( Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() ), mainAppSubdirectory );
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

			InitializePatcher( Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() ), launcherVersionInfoURL );
			StartCoroutine( CheckForUpdates( false ) );

			return true;
		}

		private bool StartMainAppPatch( bool checkForUpdates )
		{
			if( string.IsNullOrEmpty( mainAppVersionInfoURL ) )
				return false;

			if( patcher != null && patcher.IsRunning )
				return false;

			isPatchingLauncher = false;

			InitializePatcher( Path.Combine( Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() ), mainAppSubdirectory ), mainAppVersionInfoURL );
			StartCoroutine( checkForUpdates ? CheckForUpdates( true ) : ExecutePatch() );

			return true;
		}

		private void InitializePatcher( string rootPath, string versionInfoURL )
		{
			patcher = SPTUtils.CreatePatcher( rootPath, versionInfoURL ).UseRepairPatch( true ).UseIncrementalPatch( true ).LogProgress( true );

			if( !string.IsNullOrEmpty( versionInfoRSA ) )
				patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );

			if( !string.IsNullOrEmpty( patchInfoRSA ) )
				patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );
		}

		private IEnumerator CheckForUpdates( bool checkVersionOnly )
		{
			patchButton.interactable = false;
			playButton.interactable = true;

			patcher.LogProgress( false );

			// = checkVersionOnly =
			// true (default): only version number (e.g. 1.0) is compared against VersionInfo to see if there is an update
			// false: hashes and sizes of the local files are compared against VersionInfo (if there are any different/missing files, we'll patch the app)
			if( patcher.CheckForUpdates( checkVersionOnly ) )
			{
				Debug.Log( "Checking for updates..." );

				while( patcher.IsRunning )
				{
					FetchLogsFromPatcher();
					yield return null;
				}

				FetchLogsFromPatcher();

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
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private IEnumerator ExecutePatch()
		{
			patchButton.interactable = false;
			playButton.interactable = false;

			patcher.LogProgress( true );
			if( patcher.Run( isPatchingLauncher ) )
			{
				Debug.Log( "Executing patch..." );

				while( patcher.IsRunning )
				{
					FetchLogsFromPatcher();
					yield return null;
				}

				FetchLogsFromPatcher();
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
					// Otherwise, we have just updated the main app successfully!
					if( patcher.Operation == PatchOperation.SelfPatching )
					{
						string selfPatcherPath = SPTUtils.SelfPatcherExecutablePath;
						if( !string.IsNullOrEmpty( selfPatcherPath ) && File.Exists( selfPatcherPath ) )
							patcher.ApplySelfPatch( selfPatcherPath, PatchUtils.GetCurrentExecutablePath() );
						else
							patcherLogText.text = "Self patcher does not exist!";
					}
				}
				else
				{
					// An error occurred, user can click the Patch button to try again
					patchButton.interactable = true;
				}
			}
			else
				Debug.LogWarning( "Operation could not be started; maybe it is already executing?" );
		}

		private void FetchLogsFromPatcher()
		{
			string log = patcher.FetchLog();
			while( log != null )
			{
				if( logToConsole )
					Debug.Log( log );

				patcherLogText.text = log;
				log = patcher.FetchLog();
			}

			IOperationProgress progress = patcher.FetchProgress();
			while( progress != null )
			{
				if( logToConsole )
					Debug.Log( string.Concat( progress.Percentage, "% ", progress.ProgressInfo ) );

				patcherProgressText.text = progress.ProgressInfo;
				patcherProgressbar.value = progress.Percentage;

				progress = patcher.FetchProgress();
			}
		}

		private IEnumerator FetchPatchNotes()
		{
			WWW webRequest = new WWW( patchNotesURL );
			yield return webRequest;

			if( string.IsNullOrEmpty( webRequest.error ) )
				patchNotesText.text = webRequest.text;
			else
				Debug.LogError( "Can't fetch patch notes: " + webRequest.error );
		}
#endif
	}
}