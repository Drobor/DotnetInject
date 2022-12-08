namespace DotnetInject.Payload
{

    public interface IDotnetInjectEntryPoint
    {
        void Main();
    }

    public interface IDotnetInjectEntryPoint<T>
    {
        void Main(T arg);
    }
}