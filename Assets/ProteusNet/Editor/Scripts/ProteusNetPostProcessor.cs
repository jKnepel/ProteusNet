using jKnepel.ProteusNet.Components;
using UnityEditor;
using UnityEngine;

namespace jKnepel.ProteusNet
{
    public class ProteusNetPostProcessor : AssetPostprocessor
    {
        private static string[] _lastProcesses;
        private static ProcessType _lastProcess;

        private enum ProcessType
        {
            Imported,
            Deleted,
            Moved
        }
        
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        ) {
            /*
            Debug.Log($"Imported {importedAssets.Length}");
            Debug.Log($"Delete {deletedAssets.Length}");
            Debug.Log($"Moved {movedAssets.Length}");
            Debug.Log($"MovedFrom {movedFromAssetPaths.Length}");
            */

            ProcessType type;
            if (importedAssets.Length != 0)
                type = ProcessType.Imported;
            else if (deletedAssets.Length != 0)
                type = ProcessType.Deleted;
            else
                return; // dont generate prefabs if process was moving
            
            // unity calls postprocessor twice, only the first call is relevant
            var currentProcessedAssets = type == ProcessType.Imported ? importedAssets : deletedAssets;
            if (_lastProcess == type && _lastProcesses != null)
            {
                if (currentProcessedAssets.Length != _lastProcesses.Length) return;
                var isRepeated = true;
                for (var i = 0; i < currentProcessedAssets.Length; i++)
                {
                    if (currentProcessedAssets[i] == _lastProcesses[i]) continue;
                    isRepeated = false;
                    break;
                }
                if (isRepeated) return;
            }
            
            // TODO : except if its delete (double delete)

            _lastProcesses = currentProcessedAssets;
            _lastProcess = type;
            
            // only calls where a network object or the prefabs collection was processed is relevant
            // check if import process was relevant before generating
            if (importedAssets.Length == 1)
            {
                var imported = importedAssets[0];
                if (imported.EndsWith(".prefab"))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(imported);
                    if (go == null || !go.TryGetComponent<NetworkObject>(out _))
                        return;
                } 
                else if (imported.EndsWith(".asset"))
                {
                    var go = AssetDatabase.LoadAssetAtPath<NetworkObjectPrefabs>(imported);
                    if (go != null)
                        return;
                }
            }
            
            NetworkObjectPrefabs.Instance.RegenerateNetworkObjectPrefabs();
            
            // TODO : handle corrupted variants
            // TODO : optimize delete asset calls
        }
    }
}
