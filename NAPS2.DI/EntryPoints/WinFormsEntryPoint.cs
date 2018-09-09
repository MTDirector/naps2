﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAPS2.DI.Modules;
using NAPS2.Operation;
using NAPS2.Util;
using NAPS2.WinForms;
using NAPS2.Worker;
using Ninject;

namespace NAPS2.DI.EntryPoints
{
    /// <summary>
    /// The entry point logic for NAPS2.exe, the NAPS2 GUI.
    /// </summary>
    public static class WinFormsEntryPoint
    {
        public static void Run(string[] args)
        {
            // Initialize Ninject (the DI framework)
            var kernel = new StandardKernel(new CommonModule(), new WinFormsModule());

            // Parse the command-line arguments and see if we're doing something other than displaying the main form
            var lifecycle = kernel.Get<Lifecycle>();
            lifecycle.ParseArgs(args);
            lifecycle.ExitIfRedundant();

            // Start a pending worker process
            WorkerManager.Init();

            // Set up basic application configuration
            kernel.Get<CultureInitializer>().InitCulture(Thread.CurrentThread);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += UnhandledException;

            // Show the main form
            var formFactory = kernel.Get<IFormFactory>();
            Application.Run(formFactory.Create<FDesktop>());

            // Cancel and then wait for any pending operations so they can safely clean up
            var operationProgress = kernel.Get<IOperationProgress>();
            operationProgress.ActiveOperations.ForEach(op => op.Cancel());
            Task.WaitAll(operationProgress.ActiveOperations.Select(op => op.Success).ToArray<Task>());
        }

        private static void UnhandledException(object sender, ThreadExceptionEventArgs threadExceptionEventArgs)
        {
            Log.FatalException("An error occurred that caused the application to close.", threadExceptionEventArgs.Exception);
        }
    }
}
