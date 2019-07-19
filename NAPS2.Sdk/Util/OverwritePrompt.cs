﻿using System;
using System.Windows.Forms;

namespace NAPS2.Util
{
    /// <summary>
    /// A base class for objects that can prompt the user to overwrite an existing file.
    ///
    /// Implementors: WinFormsOverwritePrompt, ConsoleOverwritePrompt
    /// </summary>
    public abstract class OverwritePrompt
    {
        private static OverwritePrompt _default = new StubOverwritePrompt();

        public static OverwritePrompt Default
        {
            get
            {
                TestingContext.NoStaticDefaults();
                return _default;
            }
            set => _default = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Asks the user if they would like to overwrite the specified file.
        ///
        /// If DialogResult.Cancel is specified, the current operation should be cancelled even if there are other files to write.
        /// </summary>
        /// <param name="path">The path of the file to overwrite.</param>
        /// <returns>Yes, No, or Cancel.</returns>
        public abstract DialogResult ConfirmOverwrite(string path);
    }
}