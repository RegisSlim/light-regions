#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class GlobalKeyEventHandler
{
    public static event Action<Event> OnKeyEvent;
    public static bool RegistrationSucceeded = false;

    static GlobalKeyEventHandler()
    {
        RegistrationSucceeded = false;
        string msg = "";
        try
        {
            System.Reflection.FieldInfo info = typeof(EditorApplication).GetField(
                "globalEventHandler",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
                );
            if (info != null)
            {
                EditorApplication.CallbackFunction value = (EditorApplication.CallbackFunction)info.GetValue(null);

                value -= OnKeyPressed;
                value += OnKeyPressed;

                info.SetValue(null, value);

                RegistrationSucceeded = true;
            }
            else
            {
                msg = "globalEventHandler not found";
            }
        }
        catch (Exception e)
        {
            msg = e.Message;
        }
        finally
        {
            if (!RegistrationSucceeded)
            {
                Debug.LogWarning("GlobalKeyEventHandler: error while registering for globalEventHandler: " + msg);
            }
        }
    }

    private static void OnKeyPressed()
    {
        OnKeyEvent?.Invoke(Event.current);
    }
}
#endif

