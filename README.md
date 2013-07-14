## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Icon](https://raw.github.com/Fody/Janitor/master/Icons/package_icon.png)

Simplifies the implementation of [IDisposable](http://msdn.microsoft.com/en-us/library/system.idisposable.aspx).

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)

## Nuget package http://nuget.org/packages/Janitor.Fody 

## What it does 

 * Looks for all classes with a `Dispose` method.
 * Finds all instance fields that are `IDisposable` and cleans them up.
 * Adds a `isDisposed` field that is set inside `Dispose`.
 * Uses `isDisposed` to add an exit clause to `Dispose`.
 * Uses `isDisposed` to add to all instance methods a guard clause. This will cause an `ObjectDisposedException` to be thrown if the class has been disposed.
 * Supports convention based overrides for custom disposing of managed and unmanaged resources.
 * Adds a finalizer when cleanup of unmanaged resources is required
 * Uses the `Dispose(isDisposing)` convention when cleanup of unmanaged resources is required

### Simple Case

All instance fields will be cleaned up in the `Dispose` method.

#### Your Code

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
    
#### What gets compiled

    public class Sample : IDisposable
    {
        MemoryStream stream;
        bool isDisposed;

        public Sample()
        {
            stream = new MemoryStream();
        }

        public void Method()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Sample");
            }
            //Some code
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
    
### Custom managed handling 

In some cases you may want to have custom code that cleans up your managed resources. If this is the case add a method `void DisposeManaged()`

#### Your Code

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

#### What gets compiled

    public class Sample : IDisposable
    {
        MemoryStream stream;
        bool isDisposed;

        public Sample()
        {
            stream = new MemoryStream();
        }

        public void Method()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Sample");
            }
            //Some code
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            DisposeManaged();
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

### Custom unmanaged handling 

In some cases you may want to have custom code that cleans up your unmanaged resources. If this is the case add a method `void Disposeunmanaged()`

#### Your Code

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

#### What gets compiled

    public class Sample : IDisposable
    {
        bool isDisposed;
        IntPtr handle;

        public Sample()
        {
            handle = new IntPtr();
        }

        public void Method()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Sample");
            }
            //Some code
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            DisposeUnmanaged();
            GC.SuppressFinalize(this);
        }

        void DisposeUnmanaged()
        {
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        static extern Boolean CloseHandle(IntPtr handle);

        ~Sample()
        {
            Dispose();
        }
    }
    
### Custom managed and unmanaged handling 

Combining the above two scenarios will give you the following

#### Your code

    public class Sample : IDisposable
    {
        MemoryStream stream;
        IntPtr handle;

        public Sample()
        {
            stream = new MemoryStream();
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

    }

#### What gets compiled

    public class Sample : IDisposable
    {
        MemoryStream stream;
        bool isDisposed;
        IntPtr handle;

        public Sample()
        {
            stream = new MemoryStream();
            handle = new IntPtr();
        }

        public void Method()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Sample");
            }
            //Some code
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            if (disposing)
            {
                DisposeManaged();
            }
            DisposeUnmanaged();
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
        static extern Boolean CloseHandle(IntPtr handle);

        ~Sample()
        {
            Dispose(false);
        }
    }

## What's with the empty `Dispose()`

You will notice that the `Dispose()` is empty in all of the above cases. This is because Janitor controls what goes in there. In fact if you put any code in there Janitor will throw an exception. If you want to control `IDisposable` for specific types you can use `[Janitor.SkipWeaving]` attribute applied to the type. Then Janitor wont touch it.

## Why not weave in `IDisposable`

So it is technically possible to flag a type, with an attribute or a custom interface, and inject the full implementation of `IDisposable`. This would mean the empty `Dispose` method would not be required. However, since Fody operates after a compile, it would mean you would not be able to use the type in question as if it was `IDisposable` when in the same assembly. You would also not be able to use it as `IDisposable` within the same solution since intellisense has no knowledge of the how Fody manipulates an assembly.

## What about base classes

Not currently supported.

## Icon

<a href="http://thenounproject.com/noun/spray-bottle/#icon-No7154" target="_blank">Spray Bottle</a> designed by <a href="http://thenounproject.com/julietafelix" target="_blank">Julieta Felix</a> from The Noun Project