#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using SimplePatchToolSecurity;
using System.Collections;
using System.IO;
using UnityEngine.UI;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki/Self-Patching-App-Tutorial" )]
	public class SelfPatchingAppDemo : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
#pragma warning disable 0649
		[Header( "Patcher Parameters" )]
		[SerializeField]
		private string versionInfoURL;

		[SerializeField]
		[Tooltip( "While checking for updates:\ntrue: only version number is checked (faster)\nfalse: hashes and sizes of the files are checked (verifying integrity of files)" )]
		private bool checkVersionOnly = true;

		[SerializeField]
		[Tooltip( "Name of the self patcher's executable" )]
		private string selfPatcherExecutable = "SelfPatcher.exe";

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
		private PatcherUI patcherUiPrefab;

		[Header( "Internal Variables" )]
		[SerializeField]
		private GameObject updatePanel;

		[SerializeField]
		private Button updateButton;

		[SerializeField]
		private Button dismissButton;
#pragma warning restore 0649

		private SimplePatchTool patcher;
		private static bool executed = false;

#if UNITY_EDITOR
		private readonly bool isEditor = true;
#else
		private readonly bool isEditor = false;
#endif

		private void Awake()
		{
			// SimplePatchTool can continue running even when the scene changes, but we don't want to run another instance
			// of SimplePatchTool if we return to this GameObject's scene. We run SimplePatchTool only once
			if( executed )
			{
				Destroy( gameObject );
				return;
			}

			executed = true;
			updatePanel.SetActive( false );

			if( isEditor )
			{
				Debug.LogWarning( "Can't self patch while testing on editor" );
				Destroy( gameObject );

				return;
			}

			InitializePatcher();

			updateButton.onClick.AddListener( UpdateButtonClicked );
			dismissButton.onClick.AddListener( DismissButtonClicked );

			DontDestroyOnLoad( gameObject );
		}

		private void InitializePatcher()
		{
			patcher = SPTUtils.CreatePatcher( Path.GetDirectoryName( PatchUtils.GetCurrentExecutablePath() ), versionInfoURL );

			if( !string.IsNullOrEmpty( versionInfoRSA ) )
				patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );

			if( !string.IsNullOrEmpty( patchInfoRSA ) )
				patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );

			// = checkVersionOnly =
			// true (default): only version number (e.g. 1.0) is compared against VersionInfo to see if there is an update
			// false: hashes and sizes of the local files are compared against VersionInfo (if there are any different/missing files, we'll patch the app)
			if( patcher.CheckForUpdates( checkVersionOnly ) )
				StartCoroutine( CheckForUpdatesCoroutine() );
			else
				Debug.LogError( "Something went wrong" );
		}

		private IEnumerator CheckForUpdatesCoroutine()
		{
			while( patcher.IsRunning )
				yield return null;

			if( patcher.Result == PatchResult.Success ) // There is an update, show the update panel
				updatePanel.SetActive( true );
			else if( patcher.Result == PatchResult.AlreadyUpToDate )
				Destroy( gameObject );
			else
				Debug.LogError( "ERROR: " + patcher.FailDetails );
		}

		private void UpdateButtonClicked()
		{
			if( patcher != null && !patcher.IsRunning && patcher.Operation == PatchOperation.CheckingForUpdates && patcher.Result == PatchResult.Success )
			{
				if( patcher.Run( true ) ) // Start patching in self patching mode
					Instantiate( patcherUiPrefab ).Initialize( patcher, selfPatcherExecutable ); // Show progress on a PatcherUI instance
				else
					Debug.LogError( "Something went wrong" );
			}

			Destroy( gameObject );
		}

		private void DismissButtonClicked()
		{
			Destroy( gameObject );
		}
#endif
	}
}