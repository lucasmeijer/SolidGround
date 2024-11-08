namespace SolidGroundClient;

public class SolidGroundVariable<T>(string name, T defaultValue) : SolidGroundVariable(name, defaultValue ?? throw new ArgumentNullException(nameof(defaultValue)))
{
    public T Value => (T)ActualValue; 
}