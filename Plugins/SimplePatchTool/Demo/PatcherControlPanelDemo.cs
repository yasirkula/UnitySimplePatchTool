#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using SimplePatchToolSecurity;
using UnityEngine.UI;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool/wiki/Testing-Patches" )]
	public class PatcherControlPanelDemo : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
#pragma warning disable 0649
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
		private PatcherUI runningPatcher;

		[Header( "Internal Variables" )]
		[SerializeField]
		private InputField versionInfoURLInput;

		[SerializeField]
		private InputField rootPathInput;

		[SerializeField]
		private Toggle selfPatchingInput;

		[SerializeField]
		private Toggle repairPatchInput;

		[SerializeField]
		private Toggle incrementalPatchInput;

		[SerializeField]
		private Toggle installerPatchInput;

		[SerializeField]
		private Toggle verifyServerFilesInput;

		[SerializeField]
		private Toggle checkMultipleInstancesInput;

		[SerializeField]
		private Button patchButton;
#pragma warning restore 0649

		private void Awake()
		{
			patchButton.onClick.AddListener( PatchButtonClicked );

#if UNITY_EDITOR
			selfPatchingInput.isOn = false;
			selfPatchingInput.interactable = false;
#endif

			DontDestroyOnLoad( gameObject );
		}

		private void PatchButtonClicked()
		{
			bool isPatcherActive = runningPatcher != null && !runningPatcher.Equals( null );
			if( !isPatcherActive || !runningPatcher.Patcher.IsRunning )
			{
#if UNITY_EDITOR
				if( selfPatchingInput.isOn )
				{
					Debug.LogWarning( "Can't self patch while testing on editor" );
					selfPatchingInput.isOn = false;
				}
#endif

				SimplePatchTool patcher = SPTUtils.CreatePatcher( rootPathInput.text, versionInfoURLInput.text ).
					UseRepairPatch( repairPatchInput.isOn ).UseIncrementalPatch( incrementalPatchInput.isOn ).UseInstallerPatch( installerPatchInput.isOn ).
					VerifyFilesOnServer( verifyServerFilesInput.isOn ).CheckForMultipleRunningInstances( checkMultipleInstancesInput.isOn );

				if( !string.IsNullOrEmpty( versionInfoRSA ) )
					patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );

				if( !string.IsNullOrEmpty( patchInfoRSA ) )
					patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );

				if( patcher.Run( selfPatchingInput.isOn ) )
				{
					Debug.Log( "Started patching..." );
					if( !isPatcherActive )
						runningPatcher = Instantiate( patcherUiPrefab );

					runningPatcher.Initialize( patcher, selfPatcherExecutable );
				}
				else
					Debug.Log( "Operation could not be started; maybe it is already executing?" );
			}
			else
				Debug.LogWarning( "An instance of SimplePatchTool is already running, cancel/dismiss it first!" );
		}
#endif
	}
}