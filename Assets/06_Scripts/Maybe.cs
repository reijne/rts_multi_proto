using System;

public struct Maybe<T>
{
    private readonly T _value;
    private bool hasValue;

    public void ifJust(Action<T> some)
    {
        if (hasValue)
            some(_value);
    }

    public void ifNone(Action none)
    {
        if (!hasValue)
        {
            none();
        }
    }

    public void CaseOf(Action<T> some, Action none)
    {
        if (hasValue)
            some(_value);
        else if (none != null)
            none();
    }

    private Maybe(T value)
    {
        _value = value;
        hasValue = true;
    }

    public static Maybe<T> Of(T value) => new Maybe<T>(value);

    public static Maybe<T> None => new Maybe<T>();
}
