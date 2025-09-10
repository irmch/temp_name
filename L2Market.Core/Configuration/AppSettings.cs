using System;

namespace L2Market.Core.Configuration
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppSettings
    {
        public NamedPipeSettings NamedPipe { get; set; } = new();
        public InjectionSettings Injection { get; set; } = new();
        public UISettings UI { get; set; } = new();
    }

    /// <summary>
    /// NamedPipe specific settings
    /// </summary>
    public class NamedPipeSettings
    {
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxRetries { get; set; } = 3;
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan ServerShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// DLL injection specific settings
    /// </summary>
    public class InjectionSettings
    {
        public TimeSpan WorkflowTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan ProcessSearchTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public string DefaultProcessName { get; set; } = "l2.exe";
        public string DefaultDllPath { get; set; } = "";
    }

    /// <summary>
    /// UI specific settings
    /// </summary>
    public class UISettings
    {
        public bool AutoScroll { get; set; } = true;
        public int MaxLogLines { get; set; } = 1000;
        public bool ShowTimestamps { get; set; } = true;
        public string Theme { get; set; } = "Light";
        public bool MinimizeToTray { get; set; } = false;
    }
}
