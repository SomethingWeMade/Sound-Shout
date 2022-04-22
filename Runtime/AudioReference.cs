using UnityEngine;

[CreateAssetMenu]
public class AudioReference : ScriptableObject
{
    [HideInInspector] public string fullEventPath;
    public bool is3D;
    public bool looping;

    public override string ToString()
    {
        return fullEventPath;
    }

    #region Editor Spreadsheet Things
#if UNITY_EDITOR
    
    [Header("Spreadsheet")] 
    [TextArea] public string parameters;
    [TextArea] public string description;
    [TextArea] public string feedback;
    
    public ImplementationStatus implementationStatus = ImplementationStatus.TODO;
    [HideInInspector] public string category;
    [HideInInspector] public string eventName;

    public enum ImplementationStatus
    {
        Delete, TODO, FMODReady, UnityReady, Feedback, Iterate, Done 
    };
    
#endif
    #endregion
}
