﻿using System;
using System.Threading.Tasks;

public class WithTask : IDisposable
{
    public TaskCompletionSource<int> taskCompletionSource;
    public Task<int> taskField;

    public WithTask()
    {
        taskCompletionSource = new TaskCompletionSource<int>();
        taskField = taskCompletionSource.Task;
    }

    public void Dispose()
    {
    }
}