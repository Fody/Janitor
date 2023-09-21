# <img src="/package_icon.png" height="30px"> Janitor.Fody

[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg)](https://gitter.im/Fody/Fody)
[![NuGet Status](https://img.shields.io/nuget/v/Janitor.Fody.svg)](https://www.nuget.org/packages/Janitor.Fody/)

Simplifies the implementation of [IDisposable](http://msdn.microsoft.com/en-us/library/system.idisposable.aspx).

**See [Milestones](../../milestones?state=closed) for release notes.**


### This is an add-in for [Fody](https://github.com/Fody/Home/)

**It is expected that all developers using Fody [become a Patron on OpenCollective](https://opencollective.com/fody/contribute/patron-3059). [See Licensing/Patron FAQ](https://github.com/Fody/Home/blob/master/pages/licensing-patron-faq.md) for more information.**


## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet installation

Install the [Janitor.Fody NuGet package](https://nuget.org/packages/Janitor.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package Janitor.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<Janitor/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <Janitor/>
</Weavers>
```


## What it does

 * Looks for all classes with a `Dispose` method.
 * Finds all instance fields that are `IDisposable` and cleans them up.
 * Adds an `int disposeSignaled` field that is `Interlocked.Exchange`ed inside `Dispose`.
 * Uses `disposeSignaled` to add an exit clause to `Dispose`.
 * Uses `disposeSignaled` to add a guard clause to all non-private instance methods. This will cause an `ObjectDisposedException` to be thrown if the class has been disposed.
 * Supports convention based overrides for custom disposing of managed and unmanaged resources.
 * Adds a finalizer when clean-up of unmanaged resources is required
 * Uses the `Dispose(isDisposing)` convention when clean-up of unmanaged resources is required


### Simple Case

All instance fields will be cleaned up in the `Dispose` method.


#### The Code

```cs
public class Sample : IDisposable
{
    MemoryStream stream;

    public Sample()
    {
        stream = new MemoryStream();
    }

    public void Method()
    {
        //Some code
    }

    public void Dispose()
    {
        //must be empty
    }
}
```


#### What gets compiled

```cs
public class Sample : IDisposable
{
    MemoryStream stream;
    int disposeSignaled;
    bool disposed;

    public Sample()
    {
        stream = new MemoryStream();
    }

    public void Method()
    {
        ThrowIfDisposed();
        //Some code
    }

    void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException("TemplateClass");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }
        var temp = Interlocked.Exchange<IDisposable>(ref stream, null);
        if (temp != null)
        {
            temp.Dispose();
        }
        disposed = true;
    }
}
```


### Custom managed handling

In some cases you may want to have custom code that cleans up your managed resources. If this is the case add a method `void DisposeManaged()`


#### Your Code

```cs
public class Sample : IDisposable
{
    MemoryStream stream;

    public Sample()
    {
        stream = new MemoryStream();
    }

    public void Method()
    {
        //Some code
    }

    public void Dispose()
    {
        //must be empty
    }

    void DisposeManaged()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }
}
```


#### What gets compiled

```cs
public class Sample : IDisposable
{
    MemoryStream stream;
    int disposeSignaled;
    bool disposed;

    public Sample()
    {
        stream = new MemoryStream();
    }

    void DisposeManaged()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }

    public void Method()
    {
        ThrowIfDisposed();
        //Some code
    }

    void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException("TemplateClass");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }
        DisposeManaged();
        disposed = true;
    }
}
```


### Custom unmanaged handling 

In some cases you may want to have custom code that cleans up your unmanaged resources. If this is the case add a method `void DisposeUnmanaged()`


#### The Code

```cs
public class Sample : IDisposable
{
    IntPtr handle;

    public Sample()
    {
        handle = new IntPtr();
    }

    public void Method()
    {
        //Some code
    }

    public void Dispose()
    {
        //must be empty
    }

    void DisposeUnmanaged()
    {
        CloseHandle(handle);
        handle = IntPtr.Zero;
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool CloseHandle(IntPtr hObject);
}
```


#### What gets compiled

```cs
public class Sample : IDisposable
{
    IntPtr handle;
    int disposeSignaled;
    bool disposed;

    public Sample()
    {
        handle = new IntPtr();
    }

    void DisposeUnmanaged()
    {
        CloseHandle(handle);
        handle = IntPtr.Zero;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern Boolean CloseHandle(IntPtr handle);

    public void Method()
    {
        ThrowIfDisposed();
        //Some code
    }

    void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException("TemplateClass");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }
        DisposeUnmanaged();
        GC.SuppressFinalize(this);
        disposed = true;
    }

    ~Sample()
    {
        Dispose();
    }
}
```


### Custom managed and unmanaged handling

Combining the above two scenarios will give you the following


#### The code

```cs
public class Sample : IDisposable
{
    MemoryStream stream;
    IntPtr handle;

    public Sample()
    {
        stream = new MemoryStream();
        handle = new IntPtr();
    }

    void DisposeUnmanaged()
    {
        CloseHandle(handle);
        handle = IntPtr.Zero;
    }

    void DisposeManaged()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }

    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool CloseHandle(IntPtr hObject);

    public void Method()
    {
        //Some code
    }

    public void Dispose()
    {
        //must be empty
    }
}
```


#### What gets compiled

```cs
public class Sample : IDisposable
{
    MemoryStream stream;
    IntPtr handle;
    int disposeSignaled;

    public Sample()
    {
        stream = new MemoryStream();
        handle = new IntPtr();
    }

    public void Method()
    {
        ThrowIfDisposed();
        //Some code
    }

    void DisposeUnmanaged()
    {
        CloseHandle(handle);
        handle = IntPtr.Zero;
    }

    void DisposeManaged()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern Boolean CloseHandle(IntPtr handle);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void ThrowIfDisposed()
    {
        if (disposeSignaled !=0)
        {
            throw new ObjectDisposedException("Sample");
        }
    }

    public void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
        {
            return;
        }
        if (disposing)
        {
            DisposeManaged();
        }
        DisposeUnmanaged();
    }

    ~Sample()
    {
        Dispose(false);
    }
}
```


## What's with the empty `Dispose()`

Notice that the `Dispose()` is empty in all of the above cases. This is because Janitor controls what goes in there. In fact if you put any code in there Janitor will throw an exception. If you want to control `IDisposable` for specific types use `[Janitor.SkipWeaving]` attribute applied to the type or `[Janitor.SkipWeavingNamespace("namespaceToSkip")]` to the assembly. Then Janitor wont touch it.


## Why not weave in `IDisposable`

So it is technically possible to flag a type, with an attribute or a custom interface, and inject the full implementation of `IDisposable`. This would mean the empty `Dispose` method would not be required. However, since Fody operates after a compile, it would mean you would not be able to use the type in question as if it was `IDisposable` when in the same assembly. You would also not be able to use it as `IDisposable` within the same solution since intellisense has no knowledge of the how Fody manipulates an assembly.


## What about base classes

Not currently supported.


## Icon

[Spray Bottle](https://thenounproject.com/noun/spray-bottle/#icon-No7154) designed by [Julieta Felix](https://thenounproject.com/julietafelix) from [The Noun Project](https://thenounproject.com).
