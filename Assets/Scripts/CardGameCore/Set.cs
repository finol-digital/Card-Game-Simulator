using System;

public class Set : IEquatable<Set>
{
    public const string DefaultCode = "_CGSDEFAULT_";

    public static Set Default {
        get { return new Set(DefaultCode, DefaultCode); }
    }

    public string Code { get; private set; }

    public string Name { get; set; }

    public Set(string code, string name = null)
    {
        Code = !string.IsNullOrEmpty(code) ? code.Clone() as string : DefaultCode;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : Code.Clone() as string;
    }

    public bool Equals(Set other)
    {
        if (other == null)
            return false;
        
        return Code.Equals(other.Code);
    }

    public override string ToString()
    {
        if (Code.Equals(Name))
            return Code;
        return string.Format("{1} ({0})", Code, Name);
    }

}