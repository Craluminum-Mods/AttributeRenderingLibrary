namespace AttributeRenderingLibrary;

public class Variant(string key, string value)
{
    public string Key { get; protected set; } = key;
    public string Value { get; protected set; } = value;

    public static Variant? FromString(string keyVal)
    {
        string[] list = keyVal?.Split('-', 2);
        return list switch
        {
            { Length: 2 } => new Variant(list[0], list[1]),
            _ => null
        };
    }

    public override string ToString() => $"{Key}-{Value}";
}