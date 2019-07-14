#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine.UI;
#endif
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class MultiGameLauncherGameHolder : MonoBehaviour
	{
		// SimplePatchTool works on only standalone platforms
#if UNITY_EDITOR || UNITY_STANDALONE
		public delegate void ButtonClickDelegate( MultiGameLauncherGameHolder gameHolder );

#pragma warning disable 0649
		[SerializeField]
		private Image iconHolder;

		[SerializeField]
		private Text nameHolder;

		[SerializeField]
		private Button patchButton;

		[SerializeField]
		private Button playButton;
#pragma warning restore 0649

		public MultiGameLauncherDemo.GameConfiguration Configuration { get; private set; }

		public event ButtonClickDelegate OnPatchButtonClicked;
		public event ButtonClickDelegate OnPlayButtonClicked;

		private void Awake()
		{
			patchButton.onClick.AddListener( () =>
			{
				if( OnPatchButtonClicked != null )
					OnPatchButtonClicked( this );
			} );

			playButton.onClick.AddListener( () =>
			{
				if( OnPlayButtonClicked != null )
					OnPlayButtonClicked( this );
			} );

			patchButton.gameObject.SetActive( false );
		}

		public void Initialize( MultiGameLauncherDemo.GameConfiguration configuration )
		{
			iconHolder.sprite = configuration.Icon;
			nameHolder.text = configuration.Name;

			Configuration = configuration;
		}

		public void PlayButtonSetEnabled( bool value )
		{
			playButton.interactable = value;
		}

		public void PatchButtonSetEnabled( bool value )
		{
			patchButton.interactable = value;
		}

		public void PatchButtonSetVisible( bool value )
		{
			patchButton.gameObject.SetActive( value );
		}
#endif
	}
}