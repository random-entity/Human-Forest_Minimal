using UnityEngine;

public delegate void OnUpdate();

public class EventManager : MonoBehaviour
{
    public static event OnUpdate OnUpdatePM2SV;
    public static event OnUpdate OnUpdateSVListRef;

    public static void InvokeOnUpdatePM2SV()
    {
        OnUpdatePM2SV();
    }

    public static void InvokeOnUpdateSVListRef()
    {
        OnUpdateSVListRef?.Invoke();
    }
}