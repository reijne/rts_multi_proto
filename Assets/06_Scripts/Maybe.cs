using System;

public struct Maybe<T>
{
    private readonly T _value;
    private bool hasValue;

    private Maybe(T value)
    {
        _value = value;
        hasValue = true;
    }

    public static Maybe<T> Nothing => new Maybe<T>();

    public override string ToString()
    {
        return hasValue ? $"Just({_value})" : "Nothing";
    }

    public static Maybe<T> of(T value)
    {
        if (value == null)
            throw new Exception(
                "Value == null: cannot initialize Maybe.of(null)"
            );

        return new Maybe<T>(value);
    }

    public static Maybe<T> fromNullable(T reference)
    {
        if (reference == null)
            return Nothing;

        return of(reference);
    }

    public Maybe<T> ifJust(Action<T> some)
    {
        if (hasValue)
            some(_value);

        return this;
    }

    public Maybe<T> ifNothing(Action nothing)
    {
        if (!hasValue)
            nothing();

        return this;
    }

    public TResult caseOf<TResult>(Func<T, TResult> some, Func<TResult> nothing)
    {
        if (hasValue)
            return some(_value);

        return nothing();
    }
}
