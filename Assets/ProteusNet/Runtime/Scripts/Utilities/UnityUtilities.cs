using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace jKnepel.ProteusNet.Utilities
{
    public static class UnityUtilities
    {
	    public static void DebugByteMessage(byte[] bytes, string msg, bool inBinary = false)
	    {   
		    foreach (byte d in bytes)
			    msg += Convert.ToString(d, inBinary ? 2 : 16).PadLeft(inBinary ? 8 : 2, '0') + " ";
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
        	string fullPath = path + name + ".asset";

	        if (EditorApplication.isCompiling)
	        {
		        Debug.LogError("Can not load scriptable object when editor is compiling!");
		        return null;
	        }
	        if (EditorApplication.isUpdating)
	        {
		        Debug.LogError("Can not load scriptable object when editor is updating!");
		        return null;
	        }

	        configuration = AssetDatabase.LoadAssetAtPath<T>(fullPath);
	        
        	if (!configuration)
        	{
        		string[] allSettings = AssetDatabase.FindAssets($"t:{name}.asset");
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
				string fullPath = path + name + ".asset";
				configuration = ScriptableObject.CreateInstance<T>();
				string dir = Path.GetDirectoryName(fullPath);
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				AssetDatabase.CreateAsset(configuration, fullPath);
				AssetDatabase.SaveAssets();
			}
#endif

			if (!configuration)
			{
				configuration = ScriptableObject.CreateInstance<T>();
			}

			return configuration;
		}
	}
}
