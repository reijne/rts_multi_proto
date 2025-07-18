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

    public void ifNothing(Action nothing)
    {
        if (!hasValue)
        {
            nothing();
        }
    }

    public TResult caseOf<TResult>(Func<T, TResult> some, Func<TResult> nothing)
    {
        if (hasValue)
            return some(_value);

        return nothing();
    }

    private Maybe(T value)
    {
        _value = value;
        hasValue = true;
    }

    public static Maybe<T> of(T value) => new Maybe<T>(value);

    public static Maybe<T> fromNullable(T? value) =>
        value != null ? of(value) : nothing;

    public static Maybe<T> nothing => new Maybe<T>();
}
