#if UNITY_EDITOR || UNITY_STANDALONE
using SimplePatchToolCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SimplePatchToolUnity
{
	public class CookieAwareWebClient : IDownloadHandler
	{
		#region Helper Classes
		// Credit: https://robots.thoughtbot.com/avoiding-out-of-memory-crashes-on-mobile
		private class ToFileDownloadHandler : DownloadHandlerScript
		{
			private long expected = -1L;
			private long received = 0L;

			public long ContentLength { get { return expected; } }

			private FileStream fileStream;

			public ToFileDownloadHandler( byte[] buffer, string filepath ) : base( buffer )
			{
				fileStream = new FileStream( filepath, FileMode.Create, FileAccess.Write );
			}

			protected override byte[] GetData()
			{
				return null;
			}

			protected override bool ReceiveData( byte[] data, int dataLength )
			{
				if( data == null || data.Length <= 0 )
					return false;

				received += dataLength;

				if( fileStream != null )
					fileStream.Write( data, 0, dataLength );

				return true;
			}

			protected override float GetProgress()
			{
				if( expected <= 0L )
					return 0f;

				return (float) ( (double) received / expected );
			}

			protected override void CompleteContent()
			{
				DisposeStream();
			}

#if UNITY_2019_1_OR_NEWER
			protected override void ReceiveContentLengthHeader( ulong contentLength )
#else
			protected override void ReceiveContentLength( int contentLength )
#endif
			{
				expected = (long) contentLength;
			}

			public void Cancel()
			{
				DisposeStream();
			}

			public void DisposeStream()
			{
				try
				{
					if( fileStream != null )
					{
						fileStream.Dispose();
						fileStream = null;
					}
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}
			}
		}

		private class CookieContainer
		{
			private Dictionary<string, string> _cookies;

			public string this[string url]
			{
				get
				{
					string cookie;
					if( _cookies.TryGetValue( new Uri( url ).Host, out cookie ) )
						return cookie;

					return null;
				}
				set { _cookies[new Uri( url ).Host] = value; }
			}

			public CookieContainer()
			{
				_cookies = new Dictionary<string, string>();
			}
		}
		#endregion

		public event DownloadStringCompletedEventHandler OnDownloadStringComplete;
		public event DownloadFileCompletedEventHandler OnDownloadFileComplete;
		public event DownloadProgressChangedEventHandler OnDownloadFileProgressChange;

		public string DownloadedFilePath { get; set; }
		public string DownloadedFilename { get; set; }
		public long DownloadedFileSize { get; set; }
		public DownloadProgress Progress { get; set; }

		private UnityWebRequest webRequest;
		private bool downloadCancelled;

		private string pendingDownloadUrl;
		private string pendingDownloadPath;
		private object pendingUserState;

		private readonly CookieContainer cookies = new CookieContainer();

		public void DownloadString( string url, object userState )
		{
			pendingDownloadUrl = url;
			pendingDownloadPath = null;
			pendingUserState = userState;

			webRequest = null;
			downloadCancelled = false;

			SPTUtils.Instance.OnUpdate -= StartDownload;
			SPTUtils.Instance.OnUpdate += StartDownload;
		}

		public void DownloadFile( string url, string path, object userState )
		{
			pendingDownloadUrl = url;
			pendingDownloadPath = path;
			pendingUserState = userState;

			webRequest = null;
			downloadCancelled = false;

			SPTUtils.Instance.OnUpdate -= StartDownload;
			SPTUtils.Instance.OnUpdate += StartDownload;
		}

		private void StartDownload()
		{
			SPTUtils.Instance.OnUpdate -= StartDownload;

			if( pendingDownloadPath != null )
				SPTUtils.Instance.StartCoroutine( DownloadFileCoroutine() );
			else
				SPTUtils.Instance.StartCoroutine( DownloadStringCoroutine() );
		}

		public void Cancel()
		{
			if( !downloadCancelled && webRequest != null && !webRequest.isDone )
			{
				downloadCancelled = true;
				webRequest.Abort();
			}
		}

		private IEnumerator DownloadStringCoroutine()
		{
			try
			{
				webRequest = UnityWebRequest.Get( pendingDownloadUrl );
			}
			catch( Exception e )
			{
				if( OnDownloadFileComplete != null )
					OnDownloadFileComplete( false, e, pendingUserState );

				yield break;
			}

#if UNITY_2017_2_OR_NEWER
			yield return webRequest.SendWebRequest();
#else
			yield return webRequest.Send();
#endif

			if( OnDownloadStringComplete != null )
			{
#if UNITY_2017_1_OR_NEWER
				bool webRequestError = webRequest.isHttpError || webRequest.isNetworkError;
#else
				bool webRequestError = webRequest.isError;
#endif

				bool cancelled = downloadCancelled && webRequestError;
				Exception error = webRequestError ? new Exception( webRequest.error ) : null;
				string result = webRequestError ? null : webRequest.downloadHandler.text;

				OnDownloadStringComplete( cancelled, error, result, pendingUserState );
			}

			webRequest.Dispose();
			webRequest = null;
		}

		private IEnumerator DownloadFileCoroutine()
		{
			ToFileDownloadHandler downloadHandler;
			try
			{
				downloadHandler = new ToFileDownloadHandler( new byte[64 * 1024], pendingDownloadPath );
			}
			catch( Exception e )
			{
				if( OnDownloadFileComplete != null )
					OnDownloadFileComplete( false, e, pendingUserState );

				yield break;
			}

			try
			{
				webRequest = new UnityWebRequest( pendingDownloadUrl, UnityWebRequest.kHttpVerbGET, downloadHandler, null );

				string sentCookie = cookies[pendingDownloadUrl];
				if( sentCookie != null )
					webRequest.SetRequestHeader( "cookie", sentCookie );

#if UNITY_2017_2_OR_NEWER
				webRequest.SendWebRequest();
#else
				webRequest.Send();
#endif
			}
			catch( Exception e )
			{
				downloadHandler.DisposeStream();

				if( webRequest != null )
				{
					webRequest.Dispose();
					webRequest = null;
				}

				if( OnDownloadFileComplete != null )
					OnDownloadFileComplete( false, e, pendingUserState );

				yield break;
			}

			while( !webRequest.isDone )
			{
				if( OnDownloadFileProgressChange != null )
					OnDownloadFileProgressChange( (long) webRequest.downloadedBytes, ( (ToFileDownloadHandler) webRequest.downloadHandler ).ContentLength );

				yield return null;
			}

			downloadHandler.DisposeStream();

#if UNITY_2017_1_OR_NEWER
			bool webRequestError = webRequest.isHttpError || webRequest.isNetworkError;
#else
			bool webRequestError = webRequest.isError;
#endif

			bool cancelled = downloadCancelled && webRequestError;
			if( !cancelled )
			{
				if( OnDownloadFileProgressChange != null )
				{
					long contentLength = ( (ToFileDownloadHandler) webRequest.downloadHandler ).ContentLength;
					OnDownloadFileProgressChange( contentLength, contentLength );
				}
			}

			string receivedCookie = webRequest.GetResponseHeader( "set-cookie" );
			if( !string.IsNullOrEmpty( receivedCookie ) )
				cookies[webRequest.url] = receivedCookie;

			if( OnDownloadFileComplete != null )
			{
				Exception error = webRequestError ? new Exception( webRequest.error ) : null;
				OnDownloadFileComplete( cancelled, error, pendingUserState );
			}

			webRequest.Dispose();
			webRequest = null;
		}
	}
}
#endif