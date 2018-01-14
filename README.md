[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg?style=flat&max-age=86400)](https://gitter.im/Fody/Fody)
[![NuGet Status](http://img.shields.io/nuget/v/Janitor.Fody.svg?style=flat&max-age=86400)](https://www.nuget.org/packages/Janitor.Fody/)


## This is an add-in for [Fody](https://github.com/Fody/Fody/)

![Icon](https://raw.githubusercontent.com/Fody/Janitor/master/package_icon.png)
![Icon](https://raw.githubusercontent.com/Fody/Janitor/master/Janitor.jpg)

Simplifies the implementation of [IDisposable](http://msdn.microsoft.com/en-us/library/system.idisposable.aspx).

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)


## Usage

See also [Fody usage](https://github.com/Fody/Fody#usage).


### NuGet installation

Install the [Janitor.Fody NuGet package](https://nuget.org/packages/Janitor.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```
PM> Install-Package Janitor.Fody
PM> Update-Package Fody
```

The `Update-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<Janitor/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <Janitor/>
</Weavers>
```


## What it does

 * Looks for all classes with a `Dispose` method.
 * Finds all instance fields that are `IDisposable` and cleans them up.
 * Adds a `volatile int disposeSignaled` field that is `Interlocked.Exchange`ed inside `Dispose`.
 * Uses `disposeSignaled` to add an exit clause to `Dispose`.
 * Uses `disposeSignaled` to add a guard clause to all non-private instance methods. This will cause an `ObjectDisposedException` to be thrown if the class has been disposed.
 * Supports convention based overrides for custom disposing of managed and unmanaged resources.
 * Adds a finalizer when clean-up of unmanaged resources is required
 * Uses the `Dispose(isDisposing)` convention when clean-up of unmanaged resources is required


### Simple Case

All instance fields will be cleaned up in the `Dispose` method.


#### Your Code

```
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

```
public class Sample : IDisposable
{
    MemoryStream stream;
    volatile int disposeSignaled;
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

```
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

```
public class Sample : IDisposable
{
    MemoryStream stream;
    volatile int disposeSignaled;
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


#### Your Code

```
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

```
public class Sample : IDisposable
{
    IntPtr handle;
    volatile int disposeSignaled;
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


#### Your code

```
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

```
public class Sample : IDisposable
{
    MemoryStream stream;
    IntPtr handle;
    volatile int disposeSignaled;

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

You will notice that the `Dispose()` is empty in all of the above cases. This is because Janitor controls what goes in there. In fact if you put any code in there Janitor will throw an exception. If you want to control `IDisposable` for specific types use `[Janitor.SkipWeaving]` attribute applied to the type or `[Janitor.SkipWeavingNamespace("namespaceToSkip"]` to the assembly. Then Janitor wont touch it.


## Why not weave in `IDisposable`

So it is technically possible to flag a type, with an attribute or a custom interface, and inject the full implementation of `IDisposable`. This would mean the empty `Dispose` method would not be required. However, since Fody operates after a compile, it would mean you would not be able to use the type in question as if it was `IDisposable` when in the same assembly. You would also not be able to use it as `IDisposable` within the same solution since intellisense has no knowledge of the how Fody manipulates an assembly.


## What about base classes

Not currently supported.


## Icon

<a href="http://thenounproject.com/noun/spray-bottle/#icon-No7154" target="_blank">Spray Bottle</a> designed by <a href="http://thenounproject.com/julietafelix" target="_blank">Julieta Felix</a> from The Noun Project.