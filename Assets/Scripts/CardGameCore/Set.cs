using System;

public class Set : IEquatable<Set>
{
    public const string DefaultCode = "_CGSDEFAULT_";
    public static readonly Set Default = new Set(DefaultCode, DefaultCode);

    public string Code { get; private set; }
    public string Name { get; set; }

    public Set(string code, string name = null)
    {
        Code = !string.IsNullOrEmpty(code) ? code.Clone() as string : DefaultCode;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : Code?.Clone() as string;
    }

    public bool Equals(Set other)
    {
        return other != null && Code.Equals(other.Code);
    }

    public override string ToString()
    {
        return Code.Equals(Name) ? Code : string.Format("{1} ({0})", Code, Name);
    }

}
