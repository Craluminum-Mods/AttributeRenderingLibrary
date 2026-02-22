namespace AttributeRenderingLibrary;

public class Variant(string key, string value)
{
    public string Key { get; protected set; } = key;
    public string Value { get; protected set; } = value;

    public static Variant? FromString(string keyVal)
    {
        string[] list = keyVal?.Split('-', 2);
        if (list is not { Length: 2 })
        {
            return null;
        }
        return new Variant(list[0], list[1]);
    }

    public override string ToString() => $"{Key}-{Value}";
}