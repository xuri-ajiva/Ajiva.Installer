namespace Ajiva.Installer.ViewModels
{
    public struct AsValue<T>
    {
        public T FValue;

        public T Value
        {
            get => FValue;
            set => FValue = value;
        }

        public AsValue(T t)
        {
            FValue = t;
        }

        public static implicit operator AsValue<T>(T t) => new(t);
        public static implicit operator T(AsValue<T> t) => t.FValue;
    }
}
