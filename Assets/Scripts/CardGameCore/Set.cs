using System;
using System.Collections.Generic;

public class Set
{
    public string Code { get; set; }

    public string Name { get; set; }

    public Set(string code, string name)
    {
        Code = code.Clone() as string;
        Name = name.Clone() as string;
    }
}