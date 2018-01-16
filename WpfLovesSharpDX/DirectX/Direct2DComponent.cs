using System;
using SharpDX;
using D2D = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;

namespace WpfDirectX
{
    public class Direct2DComponent : DirectXComponent
    {
        private D2D.RenderTarget renderTarget;

        //private D2D.Bitmap1 bitmap;

        protected D2D.RenderTarget RenderTarget => renderTarget;

        //private D2D.Device d2dDevice;

        //private D2D.DeviceContext deviceContext;

        public Direct2DComponent()
        {
            Loaded += (o, e) =>
            {
                Draw();
            };
        }

        protected sealed override void InternalInitialize()
        {
            base.InternalInitialize();
            CreateResource();
        }

        protected sealed override void InternalUninitialize()
        {
            ReleaseResource();   
            base.InternalUninitialize();
        }

        protected virtual void CreateResource()
        {
            //***
            using (var surface = BackBuffer.QueryInterface<DXGI.Surface>())
            {

                renderTarget = new D2D.RenderTarget(DirectX.Factory2D, surface, new D2D.RenderTargetProperties()
                {
                    PixelFormat = new D2D.PixelFormat(DXGI.Format.Unknown, D2D.AlphaMode.Premultiplied),

                });

            }
            renderTarget.AntialiasMode = D2D.AntialiasMode.PerPrimitive;
            
            /***/

            /***
            using (var surface = BackBuffer.QueryInterface<DXGI.Surface>())
            {
                deviceContext = new D2D.DeviceContext(surface, new SharpDX.Direct2D1.CreationProperties()
                {
                    DebugLevel = SharpDX.Direct2D1.DebugLevel.Information,
                    Options = SharpDX.Direct2D1.DeviceContextOptions.None,
                    ThreadingMode = SharpDX.Direct2D1.ThreadingMode.SingleThreaded
                }
                );
                bitmap = new SharpDX.Direct2D1.Bitmap1(deviceContext, surface, new SharpDX.Direct2D1.BitmapProperties1()
                {
                    BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                    PixelFormat = new SharpDX.Direct2D1.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore),
                    DpiX = (float)Dpi,
                    DpiY = (float)Dpi
                }
                );
                deviceContext.Target = bitmap;
            }
            
            deviceContext.AntialiasMode = D2D.AntialiasMode.PerPrimitive;
            /***/
        }

        protected virtual void ReleaseResource()
        {
            //Utilities.Dispose(ref bitmap);
            //Utilities.Dispose(ref deviceContext);
            Utilities.Dispose(ref renderTarget);
        }

        protected override void BeforeResize()
        {
            ReleaseResource();
            base.BeforeResize();
        }

        protected override void AfterResize()
        {
            base.AfterResize();
            CreateResource();
        }

        protected sealed override void BeginRender()
        {
            base.BeginRender();
            RenderTarget.BeginDraw();
        }

        protected sealed override void EndRender()
        {
            RenderTarget.EndDraw();
            base.EndRender();
        }

        protected override void Render()
        {
            RenderTarget.Clear(Color.Black);
        }

        public virtual void Draw()
        {
            try
            {
                BeginRender();
                Render();
                EndRender();
            }
            catch (SharpDXException e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
        }
    }
}
