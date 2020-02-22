using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class SceneGizmos
{
    public static void EnableGizmos()
    {
        int val = 1;
        Assembly asm = Assembly.GetAssembly(typeof(Editor));
        Type type = asm.GetType("UnityEditor.AnnotationUtility");
        if (type != null)
        {
            MethodInfo getAnnotations = type.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo setGizmoEnabled = type.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            var annotations = getAnnotations.Invoke(null, null);
            foreach (object annotation in (IEnumerable)annotations)
            {
                Type annotationType = annotation.GetType();
                FieldInfo classIdField = annotationType.GetField("classID", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo scriptClassField = annotationType.GetField("scriptClass", BindingFlags.Public | BindingFlags.Instance);
                if (classIdField != null && scriptClassField != null)
                {
                    int classId = (int)classIdField.GetValue(annotation);
                    string scriptClass = (string)scriptClassField.GetValue(annotation);
                    setGizmoEnabled.Invoke(null, new object[] { classId, scriptClass, val });
                }
            }
        }
    }
}