﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Animations;

#if PEGI && UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PEGI_Styles))]
public class PEGI_StylesDrawer : Editor
{
    public override void OnInspectorGUI() => ((PEGI_Styles)target).Inspect(serializedObject);
}

[CustomEditor(typeof(SpeedAnimationController))]
public class SpeedAnimationControllerDrawer : Editor
{
    public override void OnInspectorGUI() => ((SpeedAnimationController)target).Inspect(serializedObject);
}

[CustomEditor(typeof(ISTD_Explorer))]
public class ISTD_ExplorerDrawer : Editor
{
    public override void OnInspectorGUI() => ((ISTD_Explorer)target).Inspect(serializedObject);
}

[CustomEditor(typeof(GodMode))]
public class GodModeDrawer : Editor
{
    public override void OnInspectorGUI() => ((GodMode)target).Inspect(serializedObject);
}

#endif

