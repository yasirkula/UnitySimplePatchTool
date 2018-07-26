using SimplePatchToolCore;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using SimplePatchToolSecurity;

namespace SimplePatchToolUnity
{
	public class PatcherControlPanel : MonoBehaviour
	{
		[SerializeField]
		private PatcherUI patcherUiPrefab;
		private PatcherUI runningPatcher;

		[SerializeField]
		private InputField versionInfoURLInput;

		[SerializeField]
		private InputField rootPathInput;

		[SerializeField]
		private Toggle selfPatchingInput;

		[SerializeField]
		private Toggle repairInput;

		[SerializeField]
		private Toggle incrementalPatchInput;

		[SerializeField]
		private Button patchButton;

		[Header( "Public RSA Keys" )]
		[SerializeField]
		private TextAsset versionInfoVerifier;

		[SerializeField]
		private TextAsset patchInfoVerifier;

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

				SimplePatchTool patcher = new SimplePatchTool( rootPathInput.text, versionInfoURLInput.text ).
					UseRepair( repairInput.isOn ).UseIncrementalPatch( incrementalPatchInput.isOn ).
					UseCustomDownloadHandler( () => new CookieAwareWebClient() ). // to support https in Unity
					UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue ). // DriveInfo.AvailableFreeSpace is not supported on Unity
					LogProgress( true );

				if( versionInfoVerifier != null )
				{
					string versionInfoRSA = versionInfoVerifier.text;
					patcher.UseVersionInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, versionInfoRSA ) );
				}

				if( patchInfoVerifier != null )
				{
					string patchInfoRSA = patchInfoVerifier.text;
					patcher.UsePatchInfoVerifier( ( ref string xml ) => XMLSigner.VerifyXMLContents( xml, patchInfoRSA ) );
				}

				if( patcher.Run( selfPatchingInput.isOn ) )
				{
					Debug.Log( "Started patching..." );
					if( !isPatcherActive )
						runningPatcher = Instantiate( patcherUiPrefab );

					runningPatcher.Initialize( patcher );
				}
				else
					Debug.Log( "Operation could not be started; maybe it is already executing?" );
			}
			else
				Debug.LogWarning( "An instance of SimplePatchTool is already running, cancel/dismiss it first!" );
		}
	}
}