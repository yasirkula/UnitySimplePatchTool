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
				UseCustomDownloadHandler( () => new CookieAwareWebClient() ). // To support https in Unity
				UseCustomFreeSpaceCalculator( ( drive ) => long.MaxValue ); // DriveInfo.AvailableFreeSpace is not supported in Unity
		}

		private void Update()
		{
			if( OnUpdate != null )
				OnUpdate();
		}
	}

	public static class SPTExtensions
	{
		public class PatcherWaitForFinishHandle : CustomYieldInstruction
		{
			private readonly SimplePatchTool patcher;
			public override bool keepWaiting { get { return patcher.IsRunning; } }

			public PatcherWaitForFinishHandle( SimplePatchTool patcher )
			{
				this.patcher = patcher;
			}
		}

		public static PatcherWaitForFinishHandle CheckForUpdatesCoroutine( this SimplePatchTool patcher, bool checkVersionOnly = true )
		{
			if( patcher == null )
				return null;

			if( !patcher.IsRunning && !patcher.CheckForUpdates( checkVersionOnly ) )
				return null;

			return new PatcherWaitForFinishHandle( patcher );
		}

		public static PatcherWaitForFinishHandle RunCoroutine( this SimplePatchTool patcher, bool selfPatching )
		{
			if( patcher == null )
				return null;

			if( !patcher.IsRunning && !patcher.Run( selfPatching ) )
				return null;

			return new PatcherWaitForFinishHandle( patcher );
		}
	}
}
#endif