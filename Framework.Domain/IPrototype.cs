namespace Framework.Domain;

public interface IPrototype<out T>
{
    T Clone();
}