using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelBase : MonoBehaviour
{
    [SerializeField]
    private UnityEvent<bool> onVisibilityChange;
    private bool _showing;
    
    private GameManager _gameManager;
    private CanvasGroup _canvasGroup;
    private List<UIPanelBase> _uiPanelsInChildren = new();

    protected GameManager Manager
    {
        get
        {
            if (_gameManager != null)
            {
                return _gameManager;
            }
                
            return _gameManager = GameManager.Instance;
        }
    }
        
    public virtual void Start()
    {
        var children = GetComponentsInChildren<UIPanelBase>(true); // Note that this won't detect children in GameObjects added during gameplay, if there were any.
        foreach (var child in children)
            if (child != this)
                _uiPanelsInChildren.Add(child);
    }

    protected CanvasGroup MyCanvasGroup
    {
        get
        {
            if (_canvasGroup != null) return _canvasGroup;
            return _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void Toggle()
    {
        if (_showing)
            Hide();
        else
            Show();
    }
    
    public void Show()
    {
        Show(true);
    }

    public void Show(bool propagateToChildren)
    {
        MyCanvasGroup.alpha = 1;
        MyCanvasGroup.interactable = true;
        MyCanvasGroup.blocksRaycasts = true;
        _showing = true;
        onVisibilityChange?.Invoke(true);
        if (!propagateToChildren)
            return;
        foreach (UIPanelBase child in _uiPanelsInChildren)
            child.onVisibilityChange?.Invoke(true);
    }

    public void Hide() // Called by some serialized events, so we can't just have targetAlpha as an optional parameter.
    {
        Hide(0);
    }

    public void Hide(float targetAlpha)
    {
        MyCanvasGroup.alpha = targetAlpha;
        MyCanvasGroup.interactable = false;
        MyCanvasGroup.blocksRaycasts = false;
        _showing = false;
        onVisibilityChange?.Invoke(false);
        foreach (UIPanelBase child in _uiPanelsInChildren)
            child.onVisibilityChange?.Invoke(false);
    }
}