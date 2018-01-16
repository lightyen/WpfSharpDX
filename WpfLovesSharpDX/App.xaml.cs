using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SharpDX;

namespace WpfApp2 {
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            DirectX.CreateIndependentResource();
        }

        protected override void OnExit(ExitEventArgs e) {
            DirectX.ReleaseIndependentResource();
            base.OnExit(e);
        }
    }
}
