using jKnepel.ProteusNet.Managing;
using System;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
#endif

namespace jKnepel.ProteusNet.Modules
{
    public abstract class Module : IDisposable
    {
        public abstract string Name { get; }
        public ModuleConfiguration ModuleConfiguration { get; }

        protected INetworkManager NetworkManager { get; }

        protected Module(INetworkManager networkManager, ModuleConfiguration moduleConfig)
        {
            NetworkManager = networkManager;
            ModuleConfiguration = moduleConfig;
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload += SearchForModuleGUI;
            SearchForModuleGUI();
#endif
        }
        
        ~Module()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= SearchForModuleGUI;
#endif
            Dispose(false);
        }
        
        public void Dispose()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload -= SearchForModuleGUI;
#endif
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

#if UNITY_EDITOR
        private bool _hasGUI;
        private bool _showModule;
        
        public void RenderModuleGUI(Action onRemoveModule)
        {
            if (_hasGUI)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                _showModule = EditorGUILayout.Foldout(_showModule, Name, true);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                if (GUILayout.Button("Remove Module"))
                    onRemoveModule?.Invoke();
                EditorGUILayout.EndHorizontal();
            
                if (_showModule)
                    ModuleGUI();
                
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Name);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                if (GUILayout.Button("Remove Module"))
                    onRemoveModule?.Invoke();
                EditorGUILayout.EndHorizontal();
            }
        }

        protected virtual void ModuleGUI() {}

        private void SearchForModuleGUI()
        {
            var t = GetType().GetMethod("ModuleGUI", BindingFlags.Instance | BindingFlags.NonPublic);
            _hasGUI = t is not null && t.GetBaseDefinition().DeclaringType != t.DeclaringType;
        }
#endif
    }
}
