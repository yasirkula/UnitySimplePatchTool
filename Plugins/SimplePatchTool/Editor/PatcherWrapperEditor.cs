using UnityEditor;
using UnityEngine;

namespace SimplePatchToolUnity
{
	[CustomEditor( typeof( PatcherWrapper ) )]
	public class PatcherWrapperEditor : Editor
	{
		private static GUIStyle m_helpLabelStyle = null;
		private static GUIStyle HelpLabelStyle
		{
			get
			{
				if( m_helpLabelStyle == null )
				{
					m_helpLabelStyle = new GUIStyle( EditorStyles.wordWrappedLabel )
					{
						richText = true,
						alignment = TextAnchor.MiddleLeft
					};
				}

				return m_helpLabelStyle;
			}
		}

		private SerializedProperty logReceived;
		private SerializedProperty currentProgressPercentageChanged, overallProgressPercentageChanged;
		private SerializedProperty currentProgressTextChanged, overallProgressTextChanged;
		private SerializedProperty patchStageChanged;
		private SerializedProperty patchMethodChanged;
		private SerializedProperty currentVersionDetermined, newVersionDetermined;
		private SerializedProperty versionInfoFetched;
		private SerializedProperty checkForUpdatesStarted, patchStarted;
		private SerializedProperty checkForUpdatesFailed, patchFailed, selfPatchingFailed;
		private SerializedProperty appIsUpToDate, updateAvailable, patchSuccessful;

		private void OnEnable()
		{
			logReceived = serializedObject.FindProperty( "LogReceived" );
			currentProgressPercentageChanged = serializedObject.FindProperty( "CurrentProgressPercentageChanged" );
			overallProgressPercentageChanged = serializedObject.FindProperty( "OverallProgressPercentageChanged" );
			currentProgressTextChanged = serializedObject.FindProperty( "CurrentProgressTextChanged" );
			overallProgressTextChanged = serializedObject.FindProperty( "OverallProgressTextChanged" );
			patchStageChanged = serializedObject.FindProperty( "PatchStageChanged" );
			patchMethodChanged = serializedObject.FindProperty( "PatchMethodChanged" );
			currentVersionDetermined = serializedObject.FindProperty( "CurrentVersionDetermined" );
			newVersionDetermined = serializedObject.FindProperty( "NewVersionDetermined" );
			versionInfoFetched = serializedObject.FindProperty( "VersionInfoFetched" );
			checkForUpdatesStarted = serializedObject.FindProperty( "CheckForUpdatesStarted" );
			patchStarted = serializedObject.FindProperty( "PatchStarted" );
			checkForUpdatesFailed = serializedObject.FindProperty( "CheckForUpdatesFailed" );
			patchFailed = serializedObject.FindProperty( "PatchFailed" );
			selfPatchingFailed = serializedObject.FindProperty( "SelfPatchingFailed" );
			appIsUpToDate = serializedObject.FindProperty( "AppIsUpToDate" );
			updateAvailable = serializedObject.FindProperty( "UpdateAvailable" );
			patchSuccessful = serializedObject.FindProperty( "PatchSuccessful" );
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawPropertiesExcluding( serializedObject, "m_Script" );

			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Events", EditorStyles.boldLabel );

			EditorGUILayout.LabelField( "Patcher outputted a log <i>(string: log)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( logReceived );

			EditorGUILayout.LabelField( "Percentage [0-100] of the currently running operation (e.g. downloading a file) has changed <i>(float: percentage)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( currentProgressPercentageChanged );

			EditorGUILayout.LabelField( "Overall patch percentage [0-100] has changed <i>(float: percentage)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( overallProgressPercentageChanged );

			EditorGUILayout.LabelField( "Information about the currently running operation (e.g. download stats) has changed <i>(string: information)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( currentProgressTextChanged );

			EditorGUILayout.LabelField( "Information about overall patch progress has changed <i>(string: information)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( overallProgressTextChanged );

			EditorGUILayout.LabelField( "Patcher's stage has changed (e.g. DownloadingFiles, DeletingObsoleteFiles) <i>(PatchStage: new stage)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( patchStageChanged );

			EditorGUILayout.LabelField( "Preferred patch method has changed (RepairPatch, IncrementalPatch or InstallerPatch) <i>(PatchMethod: new patch method)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( patchMethodChanged );

			EditorGUILayout.LabelField( "App's current version number (e.g. 1.0) is fetched <i>(string: current version number)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( currentVersionDetermined );

			EditorGUILayout.LabelField( "The latest version number on the server (e.g. 1.2) is fetched <i>(string: latest version number)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( newVersionDetermined );

			EditorGUILayout.LabelField( "VersionInfo is fetched from the server <i>(VersionInfo: fetched VersionInfo)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( versionInfoFetched );

			EditorGUILayout.LabelField( "Started checking for updates", HelpLabelStyle );
			EditorGUILayout.PropertyField( checkForUpdatesStarted );

			EditorGUILayout.LabelField( "Started patching the app", HelpLabelStyle );
			EditorGUILayout.PropertyField( patchStarted );

			EditorGUILayout.LabelField( "Checking for updates has failed <i>(string: error message)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( checkForUpdatesFailed );

			EditorGUILayout.LabelField( "Failed to patch the app <i>(string: error message)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( patchFailed );

			EditorGUILayout.LabelField( "Failed to start the self patcher's executable <i>(string: error message)</i>", HelpLabelStyle );
			EditorGUILayout.PropertyField( selfPatchingFailed );

			EditorGUILayout.LabelField( "App is up-to-date", HelpLabelStyle );
			EditorGUILayout.PropertyField( appIsUpToDate );

			EditorGUILayout.LabelField( "A new version is available, can start patching by calling the ApplyPatch function", HelpLabelStyle );
			EditorGUILayout.PropertyField( updateAvailable );

			EditorGUILayout.LabelField( "App patched successfully. <b>However, if this is a self patching app, then the patch must be applied to the app with the self patcher by calling the RunSelfPatcherExecutable function</b>", HelpLabelStyle );
			EditorGUILayout.PropertyField( patchSuccessful );

			serializedObject.ApplyModifiedProperties();
		}
	}
}