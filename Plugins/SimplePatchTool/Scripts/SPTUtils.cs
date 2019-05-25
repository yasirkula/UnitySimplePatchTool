#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class SPTUtils : MonoBehaviour
	{
		public static SPTUtils Instance { get; private set; }

		public delegate void OnUpdateEventHandler();
		public event OnUpdateEventHandler OnUpdate;

		[RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
		private static void Initialize()
		{
			Instance = new GameObject( "PatcherUtils" ).AddComponent<SPTUtils>();
			DontDestroyOnLoad( Instance.gameObject );
		}

		public static SimplePatchTool CreatePatcher( string rootPath, string versionInfoURL )
		{
			return new SimplePatchTool( rootPath, versionInfoURL ).
				UseCustomDownloadHandler( () => new CookieAwareWebClient() ). // to support https in Unity
				UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue ); // DriveInfo.AvailableFreeSpace is not supported in Unity
		}

		private void Update()
		{
			if( OnUpdate != null )
				OnUpdate();
		}
	}
}
#endif