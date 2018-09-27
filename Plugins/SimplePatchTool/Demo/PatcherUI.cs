#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
#endif
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SimplePatchToolUnity
{
	[HelpURL( "https://github.com/yasirkula/UnitySimplePatchTool" )]
	public class PatcherUI : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
		[SerializeField]
		[Tooltip( "Should SimplePatchTool logs be logged to console" )]
		private bool logToConsole = true;

		[SerializeField]
		private GameObject patchPanel;

		[SerializeField]
		private GameObject patchResultPanel;

		[SerializeField]
		private Text logText;

		[SerializeField]
		private Slider progressSlider;

		[SerializeField]
		private Text progressText;

		[SerializeField]
		private Text patchResultText;

		[SerializeField]
		private Button patcherButton;

		[SerializeField]
		private Text patcherButtonLabel;

		public SimplePatchTool Patcher { get; private set; }

		private void Awake()
		{
			DontDestroyOnLoad( gameObject );
			patcherButton.onClick.AddListener( PatchResultButtonClicked );
		}

		public void Initialize( SimplePatchTool patcher )
		{
			if( patcher == null )
				return;

			patchPanel.SetActive( true );
			patchResultPanel.SetActive( false );

			logText.text = string.Empty;
			progressText.text = string.Empty;
			progressSlider.value = 0f;
			patcherButtonLabel.text = "Cancel";

			Patcher = patcher;
			StartCoroutine( PatchCoroutine() );
		}

		private IEnumerator PatchCoroutine()
		{
			while( Patcher.IsRunning )
			{
				FetchLogsFromPatcher();
				yield return null;
			}

			FetchLogsFromPatcher();

			string resultText, resultButtonLabel;
			if( Patcher.Result == PatchResult.Failed )
			{
				resultText = Patcher.FailDetails;
				resultButtonLabel = "Dismiss"; // "Try Again";
			}
			else if( Patcher.Result == PatchResult.AlreadyUpToDate )
			{
				resultText = "Already up-to-date";
				resultButtonLabel = "Dismiss";
			}
			else if( Patcher.Operation == PatchOperation.SelfPatching )
			{
				resultText = "Restart the game to finish updating";
				resultButtonLabel = "Restart Now";
			}
			else
			{
				resultText = "Patch is successful";
				resultButtonLabel = "Dismiss";
			}

			patchResultText.text = resultText;
			patcherButtonLabel.text = resultButtonLabel;

			patcherButton.interactable = true;
			patchPanel.SetActive( false );
			patchResultPanel.SetActive( true );
		}

		private void FetchLogsFromPatcher()
		{
			string log = Patcher.FetchLog();
			while( log != null )
			{
				if( logToConsole )
					Debug.Log( log );

				logText.text = log;
				log = Patcher.FetchLog();
			}

			IOperationProgress progress = Patcher.FetchProgress();
			while( progress != null )
			{
				if( logToConsole )
					Debug.Log( string.Concat( progress.Percentage, "% ", progress.ProgressInfo ) );

				progressText.text = progress.ProgressInfo;
				progressSlider.value = progress.Percentage;

				progress = Patcher.FetchProgress();
			}
		}

		private void PatchResultButtonClicked()
		{
			if( Patcher.IsRunning )
			{
				Patcher.Cancel();
				patcherButton.interactable = false;
			}
			//else if( patcher.Result == PatchResult.Failed )
			//{
			//	patcher.Run( patcher.Operation == PatchOperation.SelfPatching );
			//	Initialize( patcher );
			//}
			else if( Patcher.Result == PatchResult.Success && Patcher.Operation == PatchOperation.SelfPatching )
			{
				string selfPatcherPath = SPTUtils.SelfPatcherExecutablePath;
				if( !string.IsNullOrEmpty( selfPatcherPath ) && File.Exists( selfPatcherPath ) )
					Patcher.ApplySelfPatch( selfPatcherPath, PatchUtils.GetCurrentExecutablePath() );
				else
					patchResultText.text = "Self patcher does not exist!";
			}
			else
				Destroy( gameObject );
		}
#endif
	}
}