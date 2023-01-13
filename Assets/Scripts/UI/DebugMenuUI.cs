using UnityEngine;

public class DebugMenuUI : MonoBehaviour
{
    public void RefreshState()
    {
        InGameRunner.Instance.LoadData();
    }
}