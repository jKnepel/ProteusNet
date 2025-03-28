using UnityEditor;

namespace jKnepel.ProteusNet.Utilities
{
    public class SavedBool
    {
        private bool _value;
        private readonly string _name;
        private bool _loaded;

        public SavedBool(string name, bool value)
        {
            _name = name;
            _loaded = false;
            _value = value;
        }

        private void Load()
        {
            if (_loaded)
                return;
            _loaded = true;
            _value = EditorPrefs.GetBool(_name, _value);
        }

        public bool Value
        {
            get
            {
                Load();
                return _value;
            }
            set
            {
                Load();
                if (_value == value)
                    return;
                _value = value;
                EditorPrefs.SetBool(_name, value);
            }
        }

        public static implicit operator bool(SavedBool s) => s.Value;
    }
}
