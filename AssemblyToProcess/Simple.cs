﻿using System;
using System.IO;
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local

public class Simple :
    IDisposable
{
    MemoryStream stream;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Simple()
    {
        stream = new MemoryStream();
    }

    public string PublicProperty { get; set; }
    internal string InternalProperty { get; set; }
    string PrivateProperty { get; set; }
    public static string StaticProperty { get; set; }
    protected string ProtectedProperty { get; set; }

    public void PublicMethod()
    {
    }

    internal void InternalMethod()
    {
    }

    void PrivateMethod()
    {
    }

    public static void StaticMethod()
    {
    }

    protected void ProtectedMethod()
    {
    }
}