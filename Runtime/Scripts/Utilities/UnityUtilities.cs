using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace jKnepel.ProteusNet.Utilities
{
    public static class UnityUtilities
    {
	    public static void DebugByteMessage(IEnumerable<byte> bytes, string msg, bool inBinary = false)
	    {
		    msg = bytes.Aggregate(msg, (current, d) => current + Convert.ToString(d, inBinary ? 2 : 16).PadLeft(inBinary ? 8 : 2, '0') + " ");
		    Debug.Log(msg);
	    }

	    public static void DebugByteMessage(byte bytes, string msg, bool inBinary = false)
	    {
		    DebugByteMessage(new []{ bytes }, msg, inBinary);
	    }
	    
	    public static T LoadScriptableObject<T>(string name, string path = "Assets/Resources/") where T : ScriptableObject
	    {
		    T configuration = null;
		    
#if UNITY_EDITOR
        	var fullPath = $"{path}{name}.asset";
	        configuration = AssetDatabase.LoadAssetAtPath<T>(fullPath);
	        
        	if (!configuration)
        	{
        		var allSettings = AssetDatabase.FindAssets($"t:{name}.asset");
        		if (allSettings.Length > 0)
        		{
        			configuration = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(allSettings[0]));
        		}
        	}
#endif
		    
		    if (!configuration)
		    {
        		configuration = Resources.Load<T>(Path.GetFileNameWithoutExtension(name));
		    }

        	return configuration;
        }
	    
		public static T LoadOrCreateScriptableObject<T>(string name, string path = "Assets/Resources/") where T : ScriptableObject
		{
			T configuration = LoadScriptableObject<T>(name, path);

#if UNITY_EDITOR
			if (!configuration)
			{
				var fullPath = $"{path}{name}.asset";
                var baseDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(baseDir) && !Directory.Exists(baseDir))
                	Directory.CreateDirectory(baseDir);
				var uniquePath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
				var dir = Path.GetDirectoryName(uniquePath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				configuration = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(configuration, uniquePath);
				AssetDatabase.SaveAssets();
			}
#endif

			if (!configuration)
			{
				configuration = ScriptableObject.CreateInstance<T>();
			}

			return configuration;
		}
		
#if UNITY_EDITOR
		public static bool IsPrefab(MonoBehaviour go) => PrefabUtility.GetPrefabInstanceHandle(go) != null;
		public static bool IsPrefabRoot(MonoBehaviour go) => PrefabUtility.GetCorrespondingObjectFromSource(go) == null && IsPrefab(go);
		public static bool IsPrefabInstance(MonoBehaviour go) => PrefabUtility.GetCorrespondingObjectFromSource(go) != null && IsPrefab(go);
		public static bool IsPrefabInEdit(MonoBehaviour go)
		{
			var stage = PrefabStageUtility.GetPrefabStage(go.gameObject);
			return stage != null && stage.assetPath != null;
		}
#endif
	}
}
