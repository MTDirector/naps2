using Xunit;

namespace NAPS2.App.Tests;

public class WinFormsAppTests
{
    [Fact]
    public void CreatesWindow()
    {
        var process = AppTestHelper.StartGuiProcess("NAPS2.exe");
        try
        {
            AppTestHelper.WaitForVisibleWindow(process);
            Assert.Equal("Not Another PDF Scanner 2", process.MainWindowTitle);
            Assert.True(process.CloseMainWindow());
            Assert.True(process.WaitForExit(1000));
        }
        finally
        {
            AppTestHelper.Cleanup(process);
        }
    }
    
}