﻿using System;
using System.Windows;
using System.Windows.Threading;

namespace Albedo.Utils
{
    public class DispatcherService
    {
        public static void Invoke(Action action) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
    }
}
