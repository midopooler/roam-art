﻿//SEE: https://github.com/Unity-Technologies/Graphics/blob/v10.10.1/com.unity.render-pipelines.universal/Editor/SavedParameter.cs

//#if !UNITY_2021_1_OR_NEWER
//#if USING_URP


using System;
using UnityEditor;
using UnityEngine.Assertions;

namespace ShaderCrew.TheToonShader
{
    public class SavedParameter<T>
        where T : IEquatable<T>
    {
        public delegate void SetParameter(string key, T value);
        public delegate T GetParameter(string key, T defaultValue);

        readonly string m_Key;
        bool m_Loaded;
        T m_Value;

        readonly SetParameter m_Setter;
        readonly GetParameter m_Getter;

        public SavedParameter(string key, T value, GetParameter getter, SetParameter setter)
        {
            Assert.IsNotNull(setter);
            Assert.IsNotNull(getter);

            m_Key = key;
            m_Loaded = false;
            m_Value = value;
            m_Setter = setter;
            m_Getter = getter;
        }

        void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = m_Getter(m_Key, m_Value);
        }

        public T value
        {
            get
            {
                Load();
                return m_Value;
            }
            set
            {
                Load();

                if (m_Value.Equals(value))
                    return;

                m_Value = value;
                m_Setter(m_Key, value);
            }
        }
    }

    // Pre-specialized class for easier use and compatibility with existing code
    public sealed class SavedBool : SavedParameter<bool>
    {
        public SavedBool(string key, bool value)
            : base(key, value, EditorPrefs.GetBool, EditorPrefs.SetBool) { }
    }

    public sealed class SavedInt : SavedParameter<int>
    {
        public SavedInt(string key, int value)
            : base(key, value, EditorPrefs.GetInt, EditorPrefs.SetInt) { }
    }

    public sealed class SavedFloat : SavedParameter<float>
    {
        public SavedFloat(string key, float value)
            : base(key, value, EditorPrefs.GetFloat, EditorPrefs.SetFloat) { }
    }

    public sealed class SavedString : SavedParameter<string>
    {
        public SavedString(string key, string value)
            : base(key, value, EditorPrefs.GetString, EditorPrefs.SetString) { }
    }
}

//#endif
//#endif