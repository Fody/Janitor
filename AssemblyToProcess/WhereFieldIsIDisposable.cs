﻿using System;

public class WhereFieldIsIDisposable :
    IDisposable
{
    public IDisposable Field = new Disposable();

    public void Dispose()
    {
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}