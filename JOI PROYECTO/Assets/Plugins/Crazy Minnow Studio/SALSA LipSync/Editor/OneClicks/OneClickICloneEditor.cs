using UnityEditor;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	/// <summary>
	/// RELEASE NOTES:
	/// 	2.5.0 (2020-08-20):
	/// 		REQUIRES: SALSA LipSync Suite v2.5.0+, does NOT work with prior versions.
	/// 		REQUIRES: OneClickBase v2.5.0+
	/// 		+ Support for Eyes module v2.5.0+
	/// 		+ Adds Advanced Dynamics Silence Analyzer to character.
	/// 		~ Tweaks to SALSA settings.
	///		2.3.0 (2020-02-02):
	/// 		~ updated to operate with SALSA Suite v2.3.0+
	/// 		NOTE: Does not work with prior versions of SALSA Suite (before v2.3.0)
	/// 	2.1.6 (2019-10-31):
	/// 		! corrected Eyes bone searches to work with current Izzy.
	/// 		~ removed fix-axis application on Eyes (head) module, now requires jaw
	/// 			to be unlinked in mecanim setup if animations are used.
	/// 	2.1.5 (2019-10-24):
	/// 		! corrected oo bone rotation.
	/// 		+ support for changes in the free Izzy model.
	/// 	2.1.4 (2019-07-23):
	/// 		! corrected check for prefab code implementation.
	/// 	2.1.3 (2019-07-07):
	/// 		~ durations added to bone component types.
	/// 	2.1.2 (2019-07-03):
	/// 		- confirmed operation with Base 2.1.2
	///			~ updated viseme (oo).
	/// 	2.1.1 (2019-06-28):
	/// 		+ 2018.4+ check for prefab and warn > then unpack or cancel.
	/// 	2.1.0:
	/// 		~ convert from editor code to full engine code and move to Plugins.
	///		2.0.0-BETA : Initial release.
	/// ==========================================================================
	/// PURPOSE: This script provides simple, simulated lip-sync input to the
	///		Salsa component from text/string values. For the latest information
	///		visit crazyminnowstudio.com.
	/// ==========================================================================
	/// DISCLAIMER: While every attempt has been made to ensure the safe content
	///		and operation of these files, they are provided as-is, without
	///		warranty or guarantee of any kind. By downloading and using these
	///		files you are accepting any and all risks associated and release
	///		Crazy Minnow Studio, LLC of any and all liability.
	/// ==========================================================================
	/// </summary>
	public class OneClickICloneEditor : Editor
	{
		[MenuItem("GameObject/Crazy Minnow Studio/SALSA LipSync/One-Clicks/iClone")]
		public static void OneClickSetup()
		{
			GameObject go = Selection.activeGameObject;

#if UNITY_2018_3_OR_NEWER
				if (PrefabUtility.IsPartOfAnyPrefab(go))
				{
					if (EditorUtility.DisplayDialog(
													OneClickBase.PREFAB_ALERT_TITLE,
													OneClickBase.PREFAB_ALERT_MSG,
													"YES", "NO"))
					{
						PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
						ApplyOneClick(go);
					}
				}
				else
				{
					ApplyOneClick(go);
				}
#else
			ApplyOneClick(go);
#endif
		}

		private static void ApplyOneClick(GameObject go)
		{
			OneClickIClone.Setup(go, AssetDatabase.LoadAssetAtPath<AudioClip>(OneClickBase.RESOURCE_CLIP));
			OneClickiCloneEyes.Setup(Selection.activeGameObject);
		}
	}
}