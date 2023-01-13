using System;

public class CallbackValue<T>
{
    public Action<T> OnChanged;
    
    public CallbackValue()
    {

    }
    public CallbackValue(T cachedValue)
    {
        _mCachedValue = cachedValue;
    }

    public T Value
    {
        get => _mCachedValue;
        set
        {
            if (_mCachedValue != null && _mCachedValue.Equals(value))
            {
                return;
            }
            _mCachedValue = value;
            OnChanged?.Invoke(_mCachedValue);
        }
    }

    public void ForceSet(T value)
    {
        _mCachedValue = value;
        OnChanged?.Invoke(_mCachedValue);
    }

    public void SetNoCallback(T value)
    {
        _mCachedValue = value;
    }

    private T _mCachedValue;
}