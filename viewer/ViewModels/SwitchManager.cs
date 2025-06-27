using ReactiveUI;
using System;
using System.Reflection;

namespace viewer.ViewModels;

public abstract class SwitchManager<TObj, TList> : ReactiveObject
{
    protected TList[] list = null;

    public int Count
    {
        get;
        protected set;
    } = 0;

    protected static TObj? instance;
    public static ref TObj Instance
    {
        get
        {
            if (instance == null) instance = (TObj)typeof(TObj).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[0],
                new ParameterModifier[0]).Invoke(null);
            return ref instance!;
        }
    }

    public void SetCurrent() => SetItem();
    public void SetNext() => SetItem(true);

    private void SetItem(bool add = false)
    {
        if (list == null) return;

        int index = GetIndex();
        SetItem((index + (add ? 1 : 0)) % Count);
    }

    protected abstract void SetItem(int index);

    public virtual void GetList() => Count = list != null ? list.Length : 0;

    protected abstract int GetIndex();
}