#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using System.Collections;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class PatcherListener : PatcherAsyncListener
	{
		protected override void Initialize()
		{
			SPTUtils.Instance.OnUpdate -= RunOnMainThread;
			SPTUtils.Instance.OnUpdate += RunOnMainThread;
		}

		private void RunOnMainThread()
		{
			SPTUtils.Instance.OnUpdate -= RunOnMainThread;
			SPTUtils.Instance.StartCoroutine( RefresherCoroutine() );
		}

		private IEnumerator RefresherCoroutine()
		{
			while( Refresh() )
				yield return new WaitForSecondsRealtime( RefreshInterval * 0.001f ); // RefreshInterval: in milliseconds
		}
	}
}
#endif