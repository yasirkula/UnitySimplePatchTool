using SimplePatchToolCore;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SimplePatchToolUnity
{
	public class PatcherPostProcessBuild : MonoBehaviour
	{
		private const bool ENABLED = true;

#if UNITY_STANDALONE
#pragma warning disable 0162
		[PostProcessBuild]
		public static void OnPostprocessBuild( BuildTarget target, string buildPath )
		{
			if( !ENABLED )
				return;

			string selfPatcherDirectory = null;
#if UNITY_STANDALONE_WIN
			selfPatcherDirectory = Application.dataPath + "/Plugins/SimplePatchTool/Editor/Windows";
#elif UNITY_STANDALONE_OSX
			selfPatcherDirectory = Application.dataPath + "/Plugins/SimplePatchTool/Editor/OSX";
#elif UNITY_STANDALONE_LINUX
			selfPatcherDirectory = Application.dataPath + "/Plugins/SimplePatchTool/Editor/Linux";
#endif

			if( !string.IsNullOrEmpty( selfPatcherDirectory ) && Directory.Exists( selfPatcherDirectory ) )
				CopyDirectory( selfPatcherDirectory, Path.Combine( Path.GetDirectoryName( buildPath ), PatchParameters.SELF_PATCHER_DIRECTORY ) );
		}
#pragma warning restore 0162
#endif

		private static void CopyDirectory( string fromAbsolutePath, string toAbsolutePath )
		{
			Directory.CreateDirectory( toAbsolutePath );
			CopyDirectory( new DirectoryInfo( fromAbsolutePath ), new DirectoryInfo( toAbsolutePath ), PatchUtils.GetPathWithTrailingSeparatorChar( toAbsolutePath ) );
		}

		private static void CopyDirectory( DirectoryInfo from, DirectoryInfo to, string targetAbsolutePath )
		{
			FileInfo[] files = from.GetFiles();
			for( int i = 0; i < files.Length; i++ )
			{
				FileInfo fileInfo = files[i];
				if( !fileInfo.Name.EndsWith( ".meta" ) ) // Don't copy .meta files
					fileInfo.CopyTo( targetAbsolutePath + fileInfo.Name, true );
			}

			DirectoryInfo[] subDirectories = from.GetDirectories();
			for( int i = 0; i < subDirectories.Length; i++ )
			{
				DirectoryInfo directoryInfo = subDirectories[i];
				string directoryAbsolutePath = targetAbsolutePath + directoryInfo.Name;
				Directory.CreateDirectory( directoryAbsolutePath );
				CopyDirectory( directoryInfo, new DirectoryInfo( directoryAbsolutePath ), directoryAbsolutePath + Path.DirectorySeparatorChar );
			}
		}
	}
}