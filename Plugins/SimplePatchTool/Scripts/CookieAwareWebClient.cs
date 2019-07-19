#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using SimplePatchToolCore;

namespace SimplePatchToolUnity
{
	public class CookieAwareWebClient : IDownloadHandler
	{
		#region Helper Classes
		private class WebOp : IDisposable
		{
			public enum OpType { DownloadString, DownloadFile }

			private UnityWebRequest webRequest;

			public readonly string url;
			public readonly OpType op;
			public readonly object userState;
			public readonly object additionalData;

			public bool CanStart { get { return webRequest == null || webRequest.isModifiable; } }
			public bool Cancelled { get; private set; }

			public WebOp( string url, OpType op, object userState )
			{
				this.url = url;
				this.op = op;
				this.userState = userState;
				this.additionalData = null;
			}

			public WebOp( string url, OpType op, object userState, object additionalData )
			{
				this.url = url;
				this.op = op;
				this.userState = userState;
				this.additionalData = additionalData;
			}

			public UnityWebRequest CreateWebRequest( CookieContainer cookies )
			{
				if( op == OpType.DownloadString )
					webRequest = UnityWebRequest.Get( url );
				else
				{
					webRequest = new UnityWebRequest( url, UnityWebRequest.kHttpVerbGET, new ToFileDownloadHandler( new byte[64 * 1024], (string) additionalData ), null );

					string cookie = cookies[url];
					if( cookie != null )
						webRequest.SetRequestHeader( "cookie", cookie );
				}

				return webRequest;
			}

			public void Dispose()
			{
				if( webRequest != null )
				{
					ToFileDownloadHandler fileDownloadHandler = webRequest.downloadHandler as ToFileDownloadHandler;
					if( fileDownloadHandler != null )
						fileDownloadHandler.DisposeStream();

					webRequest.Dispose();
					webRequest = null;
				}
			}

			public void Cancel()
			{
				Cancelled = true;

				ToFileDownloadHandler fileDownloadHandler = webRequest.downloadHandler as ToFileDownloadHandler;
				if( fileDownloadHandler != null )
					fileDownloadHandler.Cancel();

				webRequest.Abort();
			}
		}

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
				if( fileStream != null )
				{
					fileStream.Dispose();
					fileStream = null;
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

		private readonly object syncObj;
		private WebOp webOp;

		private readonly CookieContainer cookies;

		public CookieAwareWebClient()
		{
			syncObj = new object();
			cookies = new CookieContainer();

			SPTUtils.Instance.OnUpdate += OnUpdate;
		}

		~CookieAwareWebClient()
		{
			SPTUtils.Instance.OnUpdate -= OnUpdate;
		}

		public void DownloadString( string url, object userState )
		{
			webOp = new WebOp( url, WebOp.OpType.DownloadString, userState );
		}

		public void DownloadFile( string url, string path, object userState )
		{
			webOp = new WebOp( url, WebOp.OpType.DownloadFile, userState, path );
		}

		private IEnumerator DownloadString()
		{
			WebOp _webOp = webOp;
			try
			{
				Exception error = null;
				UnityWebRequest webRequest = null;

				try
				{
					webRequest = webOp.CreateWebRequest( cookies );
				}
				catch( Exception e )
				{
					error = e;
					webRequest = null;
				}

				if( webRequest != null )
#if UNITY_2017_2_OR_NEWER
					yield return webRequest.SendWebRequest();
#else
					yield return webRequest.Send();
#endif

				lock( syncObj )
				{
					webOp = null;
				}

				if( OnDownloadStringComplete != null )
				{
					if( error == null )
					{
#if UNITY_2017_1_OR_NEWER
						bool webRequestError = webRequest.isHttpError || webRequest.isNetworkError;
#else
						bool webRequestError = webRequest.isError;
#endif

						bool cancelled = _webOp.Cancelled && webRequestError;
						error = webRequestError ? new Exception( webRequest.error ) : null;
						string result = webRequestError ? null : webRequest.downloadHandler.text;

						OnDownloadStringComplete( cancelled, error, result, _webOp.userState );
					}
					else
						OnDownloadStringComplete( false, error, null, _webOp.userState );
				}
			}
			finally
			{
				if( _webOp != null )
					_webOp.Dispose();
			}
		}

		private IEnumerator DownloadFile()
		{
			WebOp _webOp = webOp;
			try
			{
				Exception error = null;
				UnityWebRequest webRequest = null;

				try
				{
					webRequest = webOp.CreateWebRequest( cookies );
#if UNITY_2017_2_OR_NEWER
					webRequest.SendWebRequest();
#else
					webRequest.Send();
#endif
				}
				catch( Exception e )
				{
					error = e;
					webRequest = null;
				}

				if( webRequest != null )
				{
					while( !webRequest.isDone )
					{
						if( OnDownloadFileProgressChange != null )
							OnDownloadFileProgressChange( (long) webRequest.downloadedBytes, ( (ToFileDownloadHandler) webRequest.downloadHandler ).ContentLength );

						yield return null;
					}
				}

				lock( syncObj )
				{
					webOp = null;
				}

				if( error == null )
				{
#if UNITY_2017_1_OR_NEWER
					bool webRequestError = webRequest.isHttpError || webRequest.isNetworkError;
#else
					bool webRequestError = webRequest.isError;
#endif

					bool cancelled = _webOp.Cancelled && webRequestError;
					if( !cancelled )
					{
						if( OnDownloadFileProgressChange != null )
						{
							long contentLength = ( (ToFileDownloadHandler) webRequest.downloadHandler ).ContentLength;
							OnDownloadFileProgressChange( contentLength, contentLength );
						}
					}

					string cookie = webRequest.GetResponseHeader( "set-cookie" );
					if( cookie != null && cookie.Length > 0 )
						cookies[webRequest.url] = cookie;

					if( OnDownloadFileComplete != null )
					{
						error = webRequestError ? new Exception( webRequest.error ) : null;
						OnDownloadFileComplete( cancelled, error, _webOp.userState );
					}
				}
				else if( OnDownloadFileComplete != null )
					OnDownloadFileComplete( false, error, _webOp.userState );
			}
			finally
			{
				if( _webOp != null )
					_webOp.Dispose();
			}
		}

		public void Cancel()
		{
			lock( syncObj )
			{
				if( webOp != null )
					webOp.Cancel();
			}
		}

		private void OnUpdate()
		{
			if( webOp != null && webOp.CanStart )
			{
				if( webOp.op == WebOp.OpType.DownloadString )
					SPTUtils.Instance.StartCoroutine( DownloadString() );
				else
					SPTUtils.Instance.StartCoroutine( DownloadFile() );
			}
		}
	}
}
#endif