using System;
using UnityEngine;

// =====================================================
// ECHOFORM — Readme
// Define os dados e as secções apresentados no guia inicial do projeto
// através de um ScriptableObject.
// =====================================================

public class Readme : ScriptableObject
{
    public Texture2D icon;
    public string title;
    public Section[] sections;
    public bool loadedLayout;

    [Serializable]
    public class Section
    {
        public string heading, text, linkText, url;
    }
}
