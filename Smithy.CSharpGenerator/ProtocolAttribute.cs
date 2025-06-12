using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ProtocolAttribute : Attribute
{
    public string Name { get; }
    public ProtocolAttribute(string name) => Name = name;
}
