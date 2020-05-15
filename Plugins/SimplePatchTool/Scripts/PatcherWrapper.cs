#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.Diagnostics;
using System.IO;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki/Integrating-SimplePatchTool" )]
	public class PatcherWrapper : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
		[System.Serializable] public class FloatEvent : UnityEvent<float> { }
		[System.Serializable] public class StringEvent : UnityEvent<string> { }
		[System.Serializable] public class PatchStageEvent : UnityEvent<PatchStage> { }
		[System.Serializable] public class PatchMethodEvent : UnityEvent<PatchMethod> { }
		[System.Serializable] public class VersionInfoEvent : UnityEvent<VersionInfo> { }

		public enum StartMode { DoNothing = 0, CheckForUpdates = 1, ApplyPatch = 2 };

#pragma warning disable 0649
		[Header( "Behaviour" )]
		[SerializeField]
		private StartMode onAwake = StartMode.CheckForUpdates;
#pragma warning restore 0649

		[Header( "Patcher Parameters" )]
		[Tooltip( "Download URL of the VersionInfo (click the Help button to learn more)" )]
		public string VersionInfoURL;

		[Tooltip( "Patched directory (relative to root path), only if we aren't patching the root directory that the currently running executable resides at" )]
		public string RelativeRootPath;

		[Tooltip( "Name of this app's executable (e.g. MyApp.exe). If left blank, then this value will automatically be determined by looking at the currently running executable's name. ExecutableName is used while launching the app via the LaunchApp function" )]
		public string ExecutableName;

		[Tooltip( "While checking for updates:\ntrue: only version number is checked (faster)\nfalse: hashes and sizes of the files are checked (verifying integrity of files)" )]
		public bool CheckVersionOnly = true;

		[Tooltip( "Is this app gonna update itself. If set to true, you'll need to generate a self patcher. See README for more info about self patchers" )]
		public bool IsSelfPatchingApp = true;

		[Tooltip( "Should the app be restarted by the self patcher after a successful update" )]
		public bool RestartAppAfterSelfPatching = true;

		[Tooltip( "Name of the self patcher's executable (if this is a self patching app)" )]
		public string SelfPatcherExecutable = "SelfPatcher.exe";

		[Tooltip( "Should repair patch be used when possible" )]
		public bool UseRepairPatch = true;

		[Tooltip( "Should incremental patches be used when possible" )]
		public bool UseIncrementalPatch = true;

		[Tooltip( "Should installer patch be used when possible" )]
		public bool UseInstallerPatch = true;

		[Tooltip( "Paths (relative to root path) that SimplePatchTool should ignore while patching the app" )]
		public string[] AdditionalIgnoredPaths;

		[Tooltip( "Should the patcher abort if there are other instances of this app running" )]
		public bool CheckForMultipleRunningInstances = true;

		[Tooltip( "Should verify whether or not all patch files exist on the server before executing the patch (not all CDNs support this)" )]
		public bool VerifyFilesOnServer = false;

		[Tooltip( "Should SimplePatchTool run silently (not log anything)" )]
		public bool SilentMode = false;

		[Tooltip( "Should SimplePatchTool log progress information" )]
		public bool LogProgress = true;

		[Tooltip( "Should SimplePatchTool logs be logged to the log file" )]
		public bool LogToFile = true;

		[Tooltip( "Should SimplePatchTool logs be logged to console" )]
		public bool LogToConsole = false;

		[Header( "XML Verifier Keys (Optional)" )]
		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded VersionInfo.info" )]
		public string VersionInfoRSA;

		[TextArea]
		[Tooltip( "Public RSA key that will be used to verify downloaded PatchInfo.info" )]
		public string PatchInfoRSA;

		[HideInInspector] public StringEvent LogReceived;
		[HideInInspector] public FloatEvent CurrentProgressPercentageChanged, OverallProgressPercentageChanged;
		[HideInInspector] public StringEvent CurrentProgressTextChanged, OverallProgressTextChanged;
		[HideInInspector] public PatchStageEvent PatchStageChanged;
		[HideInInspector] public PatchMethodEvent PatchMethodChanged;
		[HideInInspector] public StringEvent CurrentVersionDetermined, NewVersionDetermined;
		[HideInInspector] public VersionInfoEvent VersionInfoFetched;
		[HideInInspector] public UnityEvent CheckForUpdatesStarted, PatchStarted;
		[HideInInspector] public StringEvent CheckForUpdatesFailed, PatchFailed, SelfPatchingFailed;
		[HideInInspector] public UnityEvent AppIsUpToDate, UpdateAvailable, PatchSuccessful;

		public string RootPath
		{
			get
			{
				string rootPath = Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() );

				RelativeRootPath = RelativeRootPath.Trim();
				if( !string.IsNullOrEmpty( RelativeRootPath ) )
					rootPath = Path.Combine( rootPath, RelativeRootPath );

				return rootPath;
			}
		}

		public string ExecutablePath
		{
			get
			{
				ExecutableName = ExecutableName.Trim();
				if( string.IsNullOrEmpty( ExecutableName ) )
					return PatchUtils.GetCurrentExecutablePath();
				else
					return Path.Combine( RootPath, ExecutableName );
			}
		}

		private SimplePatchTool m_patcher = null;
		public SimplePatchTool Patcher
		{
			get
			{
				if( m_patcher == null )
					InitializePatcher();

				return m_patcher;
			}
		}

		private void Start()
		{
			if( onAwake == StartMode.CheckForUpdates )
				CheckForUpdates();
			else if( onAwake == StartMode.ApplyPatch )
				ApplyPatch();

			string currentVersion = PatchUtils.GetCurrentAppVersion( null, RootPath );
			if( !string.IsNullOrEmpty( currentVersion ) )
				CurrentVersionDetermined.Invoke( currentVersion );
		}

		public void CheckForUpdates()
		{
			if( !Patcher.CheckForUpdates( CheckVersionOnly ) )
				Debug.LogError( "Patcher is already running or something went wrong" );
		}

		public void ApplyPatch()
		{
#if UNITY_EDITOR
			if( IsSelfPatchingApp )
			{
				Debug.LogError( "Can't self patch while testing on editor" );
				return;
			}
#endif

			if( !Patcher.Run( IsSelfPatchingApp ) )
				Debug.LogError( "Patcher is already running or something went wrong" );
		}

		public void RunSelfPatcherExecutable()
		{
			if( !IsSelfPatchingApp )
				return;

#if UNITY_EDITOR
			Debug.LogError( "Can't self patch while testing on editor" );
#else
			string postSelfPatchExecutable = RestartAppAfterSelfPatching ? PatchUtils.GetCurrentExecutablePath() : null;
			if( !Patcher.ApplySelfPatch( PatchUtils.GetDefaultSelfPatcherExecutablePath( SelfPatcherExecutable ), postSelfPatchExecutable ) )
				Debug.LogError( "Patcher is already running or something went wrong" );
#endif
		}

		public void LaunchApp()
		{
			FileInfo executable = new FileInfo( ExecutablePath );
			if( executable.Exists )
			{
				Process.Start( new ProcessStartInfo( executable.FullName ) { WorkingDirectory = executable.DirectoryName } );
				Process.GetCurrentProcess().Kill();
			}
			else
				Debug.LogError( "Executable doesn't exist at " + executable.FullName );
		}

		public void Cancel()
		{
			Patcher.Cancel();
		}

		private void InitializePatcher()
		{
			if( m_patcher != null )
				return;

			m_patcher = SPTUtils.CreatePatcher( RootPath, VersionInfoURL ).CheckForMultipleRunningInstances( CheckForMultipleRunningInstances ).
				UseRepairPatch( UseRepairPatch ).UseIncrementalPatch( UseIncrementalPatch ).UseInstallerPatch( UseInstallerPatch ).
				VerifyFilesOnServer( VerifyFilesOnServer ).SilentMode( SilentMode ).LogProgress( LogProgress ).LogToFile( LogToFile );

			if( !string.IsNullOrEmpty( VersionInfoRSA ) )
				m_patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, VersionInfoRSA ) );

			if( !string.IsNullOrEmpty( PatchInfoRSA ) )
				m_patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, PatchInfoRSA ) );

			PatcherListener listener = new PatcherListener();
			listener.OnStart += () =>
			{
				if( m_patcher.Operation == PatchOperation.CheckingForUpdates )
					CheckForUpdatesStarted.Invoke();
				else if( m_patcher.Operation == PatchOperation.Patching || m_patcher.Operation == PatchOperation.SelfPatching )
					PatchStarted.Invoke();
			};
			listener.OnLogReceived += ( log ) =>
			{
				LogReceived.Invoke( log );

				if( LogToConsole )
					Debug.Log( log );
			};
			listener.OnProgressChanged += ( progress ) =>
			{
				CurrentProgressPercentageChanged.Invoke( progress.Percentage );
				CurrentProgressTextChanged.Invoke( progress.ProgressInfo );
			};
			listener.OnOverallProgressChanged += ( progress ) =>
			{
				OverallProgressPercentageChanged.Invoke( progress.Percentage );
				OverallProgressTextChanged.Invoke( progress.ProgressInfo );
			};
			listener.OnPatchStageChanged += PatchStageChanged.Invoke;
			listener.OnPatchMethodChanged += PatchMethodChanged.Invoke;
			listener.OnVersionInfoFetched += ( versionInfo ) =>
			{
				for( int i = 0; i < AdditionalIgnoredPaths.Length; i++ )
				{
					if( !string.IsNullOrEmpty( AdditionalIgnoredPaths[i] ) )
						versionInfo.AddIgnoredPath( AdditionalIgnoredPaths[i] );
				}

				VersionInfoFetched.Invoke( versionInfo );
			};
			listener.OnVersionFetched += ( currentVersion, newVersion ) =>
			{
				CurrentVersionDetermined.Invoke( currentVersion );
				NewVersionDetermined.Invoke( newVersion );
			};
			listener.OnFinish += () =>
			{
				if( m_patcher.Operation == PatchOperation.CheckingForUpdates )
				{
					if( m_patcher.Result == PatchResult.AlreadyUpToDate )
						AppIsUpToDate.Invoke();
					else if( m_patcher.Result == PatchResult.Success )
						UpdateAvailable.Invoke();
					else
					{
						CheckForUpdatesFailed.Invoke( m_patcher.FailDetails );

						if( LogToConsole )
							Debug.LogError( m_patcher.FailDetails );
					}
				}
				else if( m_patcher.Operation == PatchOperation.Patching || m_patcher.Operation == PatchOperation.SelfPatching )
				{
					if( m_patcher.Result == PatchResult.AlreadyUpToDate )
						AppIsUpToDate.Invoke();
					else if( m_patcher.Result == PatchResult.Success )
					{
						PatchSuccessful.Invoke();

						if( m_patcher.Operation == PatchOperation.Patching )
							CurrentVersionDetermined.Invoke( m_patcher.NewVersion );
					}
					else
					{
						PatchFailed.Invoke( m_patcher.FailDetails );

						if( LogToConsole )
							Debug.LogError( m_patcher.FailDetails );
					}
				}
				else
				{
					if( m_patcher.Result == PatchResult.AlreadyUpToDate )
						AppIsUpToDate.Invoke();
					else if( m_patcher.Result == PatchResult.Failed )
					{
						SelfPatchingFailed.Invoke( m_patcher.FailDetails );

						if( LogToConsole )
							Debug.LogError( m_patcher.FailDetails );
					}
				}
			};

			m_patcher.SetListener( listener );
		}
#endif
	}
}