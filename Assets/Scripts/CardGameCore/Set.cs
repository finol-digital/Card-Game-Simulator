using System;

public class Set : IEquatable<Set>
{
    public const string DefaultCode = "_CGSDEFAULT_";

    public string Code { get; private set; }

    public string Name { get; set; }

    public Set(string code, string name)
    {
        Code = !string.IsNullOrEmpty(code) ? code.Clone() as string : DefaultCode;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : DefaultCode;
    }

    public bool Equals(Set other)
    {
        return Code.Equals(other.Code);
    }

}