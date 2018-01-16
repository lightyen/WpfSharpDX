using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Threading;
using System.Timers;
using SharpDX;
using D2D = SharpDX.Direct2D1;
using WpfDirectX;

namespace WpfLovesSharpDX {
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window {

        MyDirect2DComponent D2DComponent;

        public MainWindow() {
            
            D2DComponent = new MyDirect2DComponent();
            this.DataContext = D2DComponent;
            InitializeComponent();
        }

        private async void ToggleButton_Checked(object sender, RoutedEventArgs e) {
            D2DComponent.Running = true;
            await D2DComponent.Run();
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e) {
            D2DComponent.Running = false;
        }
    }

    public class MyDirect2DComponent : Direct2DComponent {

        List<D2D.Ellipse> points;
        D2D.SolidColorBrush redBrush;
        Random random;

        public bool Running {
            get; set;
        }

        public MyDirect2DComponent() {
            
            random = new Random();
            points = new List<D2D.Ellipse>();

            this.Unloaded += (a, b) => {
                Running = false;
            };
        }

        public Task Run() {

            var task = Task.Run(() => {

                redBrush = new D2D.SolidColorBrush(RenderTarget, new Color(255.0f, 0.0f, 0.0f));
                
                while (Running) {
                    points.Clear();
                    for(int i = 0; i < 500; i++) {
                        points.Add(new D2D.Ellipse(new Vector2(random.NextFloat(0, (float)(ActualWidth * DpiScale)), random.NextFloat(0, (float)(ActualHeight * DpiScale))), 3, 3));
                    }

                    this.Draw();
                    Thread.Sleep(16);
                }
            });

            return task;
        }

        protected override void Render() {

            RenderTarget.Clear(new Color(15, 87, 83));

            foreach (var p in points) {
                RenderTarget.FillEllipse(p, redBrush);
            }
        }
    }
}
