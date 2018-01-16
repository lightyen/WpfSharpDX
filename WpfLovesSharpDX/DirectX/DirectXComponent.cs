using System;
using System.Windows;
using System.IO;
using Microsoft.Win32;

using SharpDX;
using SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using WIC = SharpDX.WIC;

using Win32Interoperation;
using Management;

namespace WpfDirectX {

    public abstract class DirectXComponent : Win32Control {

        private Device device;

        public Device Device => device;

        private DXGI.SwapChain swapChain;

        protected DXGI.SwapChain SwapChain => swapChain;

        private Texture2D backBuffer;

        protected Texture2D BackBuffer => backBuffer;

        //private D3D11.RenderTargetView renderTargetView;

        private double dpiScale;

        protected int SurfaceWidth { get; private set; }

        protected int SurfaceHeight { get; private set; }

        public double DpiScale => dpiScale;

        public event EventHandler D3DInitialized;

        public event EventHandler D3DSizeStartChange;

        public event EventHandler D3DSizeChanged;

        protected virtual void InternalInitialize() {
            dpiScale = GetDpiScale();
            SurfaceWidth = (int)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            SurfaceHeight = (int)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));

            var swapChainDescription = new DXGI.SwapChainDescription {
                OutputHandle = Hwnd,
                BufferCount = 2,
                Flags = DXGI.SwapChainFlags.AllowModeSwitch,
                IsWindowed = true,
                ModeDescription = new DXGI.ModeDescription(SurfaceWidth, SurfaceHeight, new DXGI.Rational(60, 1), DXGI.Format.B8G8R8A8_UNorm),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                SwapEffect = DXGI.SwapEffect.Discard,
                Usage = DXGI.Usage.RenderTargetOutput | DXGI.Usage.Shared
            };

            SharpDX.Direct3D.FeatureLevel[] featureLevels = null;

            if (VersionHelper.IsWindows10OrGreater()) {
                featureLevels = new SharpDX.Direct3D.FeatureLevel[]
                {
                    SharpDX.Direct3D.FeatureLevel.Level_12_1,
                    SharpDX.Direct3D.FeatureLevel.Level_12_0,
                    SharpDX.Direct3D.FeatureLevel.Level_11_1,
                    SharpDX.Direct3D.FeatureLevel.Level_11_0,
                    SharpDX.Direct3D.FeatureLevel.Level_10_1,
                    SharpDX.Direct3D.FeatureLevel.Level_10_0,
                    SharpDX.Direct3D.FeatureLevel.Level_9_3,
                    SharpDX.Direct3D.FeatureLevel.Level_9_2,
                    SharpDX.Direct3D.FeatureLevel.Level_9_1
                };
            } else if (VersionHelper.IsWindows7SP1OrGreater()) {
                featureLevels = new SharpDX.Direct3D.FeatureLevel[]
                {
                    SharpDX.Direct3D.FeatureLevel.Level_11_1,
                    SharpDX.Direct3D.FeatureLevel.Level_11_0,
                    SharpDX.Direct3D.FeatureLevel.Level_10_1,
                    SharpDX.Direct3D.FeatureLevel.Level_10_0,
                    SharpDX.Direct3D.FeatureLevel.Level_9_3,
                    SharpDX.Direct3D.FeatureLevel.Level_9_2,
                    SharpDX.Direct3D.FeatureLevel.Level_9_1
                };
            }

            try {
                Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport, featureLevels,
                    swapChainDescription,
                    out device, out swapChain);
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }

            using (var factory = swapChain.GetParent<DXGI.Factory>()) {
                factory.MakeWindowAssociation(Hwnd, DXGI.WindowAssociationFlags.IgnoreAll);
            }

            backBuffer = swapChain.GetBackBuffer<Texture2D>(0);

            Console.WriteLine(SupportLevel);

            //backBuffer = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(swapChain, 0);
            //renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
        }

        protected virtual void InternalUninitialize() {
            //Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref backBuffer);
            Utilities.Dispose(ref swapChain);

            // This is a workaround for an issue in SharpDx3.0.2 (https://github.com/sharpdx/SharpDX/issues/731)
            // Will need to be removed when fixed in next SharpDx release
            //((IUnknown)device).Release();

            Utilities.Dispose(ref device);
            //GC.Collect(2, GCCollectionMode.Forced);
        }

        protected override sealed void Initialize() {
            InternalInitialize();
            D3DInitialized?.Invoke(this, new EventArgs());
        }

        protected override sealed void Uninitialize() {
            InternalUninitialize();
        }

        protected override void Resized() {
            D3DSizeStartChange?.Invoke(this, new EventArgs());
            BeforeResize();
            try {
                swapChain.ResizeBuffers(0, 0, 0, DXGI.Format.Unknown, DXGI.SwapChainFlags.None);
            } catch (SharpDXException e) {
                MessageBox.Show(e.ToString());
            }
            AfterResize();
            D3DSizeChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void BeforeResize() {
            //Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref backBuffer);
        }

        protected virtual void AfterResize() {
            backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            //renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
        }

        protected virtual void BeginRender() {
            device.ImmediateContext.Rasterizer.SetViewport(0, 0, (float)ActualWidth, (float)ActualHeight);
            //device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);
        }

        protected virtual void EndRender() {
            swapChain.Present(1, DXGI.PresentFlags.None);
        }

        protected abstract void Render();

        public void SaveImage() {
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                DefaultExt = ".png",
                Filter = "PNG|*.png|JPEG|*.jpg",
                FileName = Application.Current.FindResource("Undefined") as string,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (saveFileDialog.ShowDialog() == true) {
                SaveImage(saveFileDialog.FileName);
            }
        }

        public void SaveImage(string filename) {
            Guid format;

            string ext = Path.GetExtension(filename);

            switch (ext) {
                case ".png":
                    format = WIC.ContainerFormatGuids.Png;
                    break;
                case ".jpg":
                case ".jpeg":
                    format = WIC.ContainerFormatGuids.Jpeg;
                    break;
                case ".tiff":
                    format = WIC.ContainerFormatGuids.Tiff;
                    break;
                default:
                    format = WIC.ContainerFormatGuids.Png;
                    break;
            }

            //SaveWICTextureToFile(Device.ImmediateContext, BackBuffer, ref format, filename);
            SaveWICTextureToFileFix(Device.ImmediateContext, BackBuffer, ref format, filename);
        }

        public string SupportLevel {
            get {
                switch (device.FeatureLevel) {
                    case SharpDX.Direct3D.FeatureLevel.Level_12_1:
                        return "DirectX 12.1";
                    case SharpDX.Direct3D.FeatureLevel.Level_12_0:
                        return "DirectX 12.0";
                    case SharpDX.Direct3D.FeatureLevel.Level_11_1:
                        return "DirectX 11.1";
                    case SharpDX.Direct3D.FeatureLevel.Level_11_0:
                        return "DirectX 11.0";
                    case SharpDX.Direct3D.FeatureLevel.Level_10_1:
                        return "DirectX 11.1";
                    case SharpDX.Direct3D.FeatureLevel.Level_10_0:
                        return "DirectX 10.0";
                    case SharpDX.Direct3D.FeatureLevel.Level_9_3:
                        return "DirectX 9.3";
                    case SharpDX.Direct3D.FeatureLevel.Level_9_2:
                        return "DirectX 9.2";
                    case SharpDX.Direct3D.FeatureLevel.Level_9_1:
                        return "DirectX 9.1";
                    default:
                        return "DirectX Not Support";
                }
            }
        }

        private void SaveWICTextureToFile(
                                       DeviceContext context,
                                       Texture2D source,
                                       ref Guid guidContainerFormat,
                                       string fileName) {
            Texture2D staging = CaptureTexture(context, source, out Texture2DDescription desc);

            if (staging == null) return;

            FileStream fs = new FileStream(fileName, FileMode.Create);
            WIC.BitmapEncoder encoder = new WIC.BitmapEncoder(DirectX.ImageFactory, guidContainerFormat);
            encoder.Initialize(fs);

            WIC.BitmapFrameEncode frameEncode = new WIC.BitmapFrameEncode(encoder);
            //IPropertyBag2

            frameEncode.Initialize();
            frameEncode.SetSize(desc.Width, desc.Height);
            frameEncode.SetResolution(72.0, 72.0);
            Guid pfGuid;
            Guid targetGuid;

            //bool sRGB = false;
            switch (desc.Format) {
                case DXGI.Format.R32G32B32A32_Float: pfGuid = WIC.PixelFormat.Format128bppRGBAFloat; break;
                case DXGI.Format.R16G16B16A16_Float: pfGuid = WIC.PixelFormat.Format64bppRGBAHalf; break;
                case DXGI.Format.R16G16B16A16_UNorm: pfGuid = WIC.PixelFormat.Format64bppRGBA; break;
                case DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm: pfGuid = WIC.PixelFormat.Format32bppRGBA1010102XR; break; // DXGI 1.1
                case DXGI.Format.R10G10B10A2_UNorm: pfGuid = WIC.PixelFormat.Format32bppRGBA1010102; break;
                case DXGI.Format.B5G5R5A1_UNorm: pfGuid = WIC.PixelFormat.Format16bppBGRA5551; break;
                case DXGI.Format.B5G6R5_UNorm: pfGuid = WIC.PixelFormat.Format16bppBGR565; break;
                case DXGI.Format.R32_Float: pfGuid = WIC.PixelFormat.Format32bppGrayFloat; break;
                case DXGI.Format.R16_Float: pfGuid = WIC.PixelFormat.Format16bppGrayHalf; break;
                case DXGI.Format.R16_UNorm: pfGuid = WIC.PixelFormat.Format16bppGray; break;
                case DXGI.Format.R8_UNorm: pfGuid = WIC.PixelFormat.Format8bppGray; break;
                case DXGI.Format.A8_UNorm: pfGuid = WIC.PixelFormat.Format8bppAlpha; break;

                case DXGI.Format.R8G8B8A8_UNorm:
                    pfGuid = WIC.PixelFormat.Format32bppRGBA;
                    break;

                case DXGI.Format.R8G8B8A8_UNorm_SRgb:
                    pfGuid = WIC.PixelFormat.Format32bppRGBA;
                    //sRGB = true;
                    break;

                case DXGI.Format.B8G8R8A8_UNorm: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGRA;
                    break;

                case DXGI.Format.B8G8R8A8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGRA;
                    //sRGB = true;
                    break;

                case DXGI.Format.B8G8R8X8_UNorm: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGR;
                    break;

                case DXGI.Format.B8G8R8X8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGR;
                    //sRGB = true;
                    break;

                default:
                    return;
            }

            switch (desc.Format) {
                case DXGI.Format.R32G32B32A32_Float:
                case DXGI.Format.R16G16B16A16_Float:
                    targetGuid = WIC.PixelFormat.Format24bppBGR;
                    break;
                case DXGI.Format.R16G16B16A16_UNorm: targetGuid = WIC.PixelFormat.Format48bppBGR; break;
                case DXGI.Format.B5G5R5A1_UNorm: targetGuid = WIC.PixelFormat.Format16bppBGR555; break;
                case DXGI.Format.B5G6R5_UNorm: targetGuid = WIC.PixelFormat.Format16bppBGR565; break;

                case DXGI.Format.R32_Float:
                case DXGI.Format.R16_Float:
                case DXGI.Format.R16_UNorm:
                case DXGI.Format.R8_UNorm:
                case DXGI.Format.A8_UNorm:
                    targetGuid = WIC.PixelFormat.Format8bppGray;
                    break;

                default:
                    targetGuid = WIC.PixelFormat.Format24bppBGR;
                    break;
            }

            frameEncode.SetPixelFormat(ref targetGuid);

            #region Write

            DataBox db = context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None, out DataStream stream);

            if (pfGuid != targetGuid) {
                WIC.FormatConverter formatCoverter = new WIC.FormatConverter(DirectX.ImageFactory);

                if (formatCoverter.CanConvert(pfGuid, targetGuid)) {
                    WIC.Bitmap src = new WIC.Bitmap(DirectX.ImageFactory, desc.Width, desc.Height, pfGuid,
                        new DataRectangle(db.DataPointer, db.RowPitch));

                    formatCoverter.Initialize(src, targetGuid, SharpDX.WIC.BitmapDitherType.None, null, 0, SharpDX.WIC.BitmapPaletteType.Custom);

                    frameEncode.WriteSource(formatCoverter, new Rectangle(0, 0, desc.Width, desc.Height));
                }

            } else {
                frameEncode.WritePixels(desc.Height, new DataRectangle(db.DataPointer, db.RowPitch));
            }

            context.UnmapSubresource(staging, 0);

            frameEncode.Commit();
            encoder.Commit();

            #endregion

            frameEncode.Dispose();
            encoder.Dispose();

            fs.Close();

        }

        private Result SaveWICTextureToFileFix(
                                       DeviceContext context,
                                       Texture2D source,
                                       ref Guid guidContainerFormat,
                                       string fileName) {
            if (fileName == null)
                return Result.InvalidArg;

            Result res = CaptureTextureFix(context, source, out Texture2DDescription desc, out Texture2D staging);
            if (res.Failure) return res;

            Guid pfGuid;
            //bool sRGB = false;
            Guid targetGuid;

            switch (desc.Format) {
                case DXGI.Format.R32G32B32A32_Float: pfGuid = WIC.PixelFormat.Format128bppRGBAFloat; break;
                case DXGI.Format.R16G16B16A16_Float: pfGuid = WIC.PixelFormat.Format64bppRGBAHalf; break;
                case DXGI.Format.R16G16B16A16_UNorm: pfGuid = WIC.PixelFormat.Format64bppRGBA; break;
                case DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm: pfGuid = WIC.PixelFormat.Format32bppRGBA1010102XR; break; // DXGI 1.1
                case DXGI.Format.R10G10B10A2_UNorm: pfGuid = WIC.PixelFormat.Format32bppRGBA1010102; break;
                case DXGI.Format.B5G5R5A1_UNorm: pfGuid = WIC.PixelFormat.Format16bppBGRA5551; break;
                case DXGI.Format.B5G6R5_UNorm: pfGuid = WIC.PixelFormat.Format16bppBGR565; break;
                case DXGI.Format.R32_Float: pfGuid = WIC.PixelFormat.Format32bppGrayFloat; break;
                case DXGI.Format.R16_Float: pfGuid = WIC.PixelFormat.Format16bppGrayHalf; break;
                case DXGI.Format.R16_UNorm: pfGuid = WIC.PixelFormat.Format16bppGray; break;
                case DXGI.Format.R8_UNorm: pfGuid = WIC.PixelFormat.Format8bppGray; break;
                case DXGI.Format.A8_UNorm: pfGuid = WIC.PixelFormat.Format8bppAlpha; break;

                case DXGI.Format.R8G8B8A8_UNorm:
                    pfGuid = WIC.PixelFormat.Format32bppRGBA;
                    break;

                case DXGI.Format.R8G8B8A8_UNorm_SRgb:
                    pfGuid = WIC.PixelFormat.Format32bppRGBA;
                    //sRGB = true;
                    break;

                case DXGI.Format.B8G8R8A8_UNorm: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGRA;
                    break;

                case DXGI.Format.B8G8R8A8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGRA;
                    //sRGB = true;
                    break;

                case DXGI.Format.B8G8R8X8_UNorm: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGR;
                    break;

                case DXGI.Format.B8G8R8X8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = WIC.PixelFormat.Format32bppBGR;
                    //sRGB = true;
                    break;

                default:
                    return Result.GetResultFromWin32Error(unchecked((int)0x80070032));
            }

            // Create file
            FileStream fs = new FileStream(fileName, FileMode.Create);
            WIC.BitmapEncoder encoder = new WIC.BitmapEncoder(DirectX.ImageFactory, guidContainerFormat);
            encoder.Initialize(fs);


            WIC.BitmapFrameEncode frameEncode = new WIC.BitmapFrameEncode(encoder);
            frameEncode.Initialize();
            frameEncode.SetSize(desc.Width, desc.Height);
            frameEncode.SetResolution(72.0, 72.0);


            switch (desc.Format) {
                case DXGI.Format.R32G32B32A32_Float:
                case DXGI.Format.R16G16B16A16_Float:
                    targetGuid = WIC.PixelFormat.Format24bppBGR;
                    break;
                case DXGI.Format.R16G16B16A16_UNorm: targetGuid = WIC.PixelFormat.Format48bppBGR; break;
                case DXGI.Format.B5G5R5A1_UNorm: targetGuid = WIC.PixelFormat.Format16bppBGR555; break;
                case DXGI.Format.B5G6R5_UNorm: targetGuid = WIC.PixelFormat.Format16bppBGR565; break;

                case DXGI.Format.R32_Float:
                case DXGI.Format.R16_Float:
                case DXGI.Format.R16_UNorm:
                case DXGI.Format.R8_UNorm:
                case DXGI.Format.A8_UNorm:
                    targetGuid = WIC.PixelFormat.Format8bppGray;
                    break;

                default:
                    targetGuid = WIC.PixelFormat.Format24bppBGR;
                    break;
            }

            frameEncode.SetPixelFormat(ref targetGuid);

            #region Write

            DataBox db = context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None, out DataStream stream);

            if (pfGuid != targetGuid) {
                WIC.FormatConverter formatCoverter = new WIC.FormatConverter(DirectX.ImageFactory);

                if (formatCoverter.CanConvert(pfGuid, targetGuid)) {
                    WIC.Bitmap src = new WIC.Bitmap(DirectX.ImageFactory, desc.Width, desc.Height, pfGuid,
                        new DataRectangle(db.DataPointer, db.RowPitch));

                    formatCoverter.Initialize(src, targetGuid, SharpDX.WIC.BitmapDitherType.None, null, 0, SharpDX.WIC.BitmapPaletteType.Custom);

                    frameEncode.WriteSource(formatCoverter, new Rectangle(0, 0, desc.Width, desc.Height));
                }

            } else {
                frameEncode.WritePixels(desc.Height, new DataRectangle(db.DataPointer, db.RowPitch));
            }

            context.UnmapSubresource(staging, 0);

            frameEncode.Commit();
            encoder.Commit();

            #endregion

            frameEncode.Dispose();
            encoder.Dispose();

            fs.Close();

            return Result.Ok;
        }

        private Texture2D CaptureTexture(DeviceContext deviceContext,
            Texture2D source, out Texture2DDescription desc) {
            Device d3dDevice = deviceContext.Device;
            // debug: i got it!
            //D3D11.Texture2D texture = source.QueryInterface<D3D11.Texture2D>();
            Texture2D staging = null;

            desc = source.Description;

            if (desc.SampleDescription.Count > 1) {
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                Texture2D temp = new Texture2D(d3dDevice, desc);

                DXGI.Format fmt = EnsureNotTypeless(desc.Format);

                FormatSupport support = d3dDevice.CheckFormatSupport(fmt);

                if ((support & FormatSupport.MultisampleResolve) == 0) return null;

                for (int item = 0; item < desc.ArraySize; ++item) {
                    for (int level = 0; level < desc.MipLevels; ++level) {
                        int index = Resource.CalculateSubResourceIndex(level, item, desc.MipLevels);
                        deviceContext.ResolveSubresource(temp, index, source, index, fmt);
                    }
                }

                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                staging = new Texture2D(d3dDevice, desc);
                deviceContext.CopyResource(temp, staging);
            } else if (desc.Usage == ResourceUsage.Staging &&
                      desc.CpuAccessFlags == CpuAccessFlags.Read) {
                staging = source;
            } else {
                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                staging = new Texture2D(d3dDevice, desc);
                deviceContext.CopyResource(source, staging);
            }

            return staging;
        }

        private Result CaptureTextureFix(DeviceContext deviceContext,
            Texture2D source,
            out Texture2DDescription desc,
            out Texture2D staging) {
            desc = new Texture2DDescription();
            staging = null;

            if (deviceContext == null || source == null)
                return Result.InvalidArg;

            ResourceDimension resType = source.Dimension;

            if (resType != ResourceDimension.Texture2D) {
                //string message = SharpDX.Diagnostics.ErrorManager.GetErrorMessage(0);
                //return Result.GetResultFromWin32Error(ERROR_NOT_SUPPORTED)
            }

            desc = source.Description;

            Device d3dDevice = deviceContext.Device;
            //Texture2D staging = null;

            if (desc.SampleDescription.Count > 1) {
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                Texture2D temp;

                try {
                    temp = new Texture2D(d3dDevice, desc);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }

                DXGI.Format fmt = EnsureNotTypeless(desc.Format);

                FormatSupport support = FormatSupport.None;
                try {
                    support = d3dDevice.CheckFormatSupport(fmt);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }

                if ((support & FormatSupport.MultisampleResolve) == 0)
                    return Result.Fail;

                for (int item = 0; item < desc.ArraySize; ++item) {
                    for (int level = 0; level < desc.MipLevels; ++level) {
                        int index = Resource.CalculateSubResourceIndex(level, item, desc.MipLevels);
                        deviceContext.ResolveSubresource(temp, index, source, index, fmt);
                    }
                }

                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                try {
                    staging = new Texture2D(d3dDevice, desc);
                    deviceContext.CopyResource(temp, staging);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }
            } else if (desc.Usage == ResourceUsage.Staging && desc.CpuAccessFlags == CpuAccessFlags.Read) {
                staging = source;
            } else {
                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                try {
                    staging = new Texture2D(d3dDevice, desc);
                    deviceContext.CopyResource(source, staging);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }
            }

            return Result.Ok;
        }


        private DXGI.Format EnsureNotTypeless(DXGI.Format format) {
            switch (format) {
                case DXGI.Format.R32G32B32A32_Typeless: return DXGI.Format.R32G32B32A32_Float;
                case DXGI.Format.R32G32B32_Typeless: return DXGI.Format.R32G32B32_Float;
                case DXGI.Format.R16G16B16A16_Typeless: return DXGI.Format.R16G16B16A16_UNorm;
                case DXGI.Format.R32G32_Typeless: return DXGI.Format.R32G32_Float;
                case DXGI.Format.R10G10B10A2_Typeless: return DXGI.Format.R10G10B10A2_UNorm;
                case DXGI.Format.R8G8B8A8_Typeless: return DXGI.Format.R8G8B8A8_UNorm;
                case DXGI.Format.R16G16_Typeless: return DXGI.Format.R16G16_UNorm;
                case DXGI.Format.R32_Typeless: return DXGI.Format.R32_Float;
                case DXGI.Format.R8G8_Typeless: return DXGI.Format.R8G8_UNorm;
                case DXGI.Format.R16_Typeless: return DXGI.Format.R16_UNorm;
                case DXGI.Format.R8_Typeless: return DXGI.Format.R8_UNorm;
                case DXGI.Format.BC1_Typeless: return DXGI.Format.BC1_UNorm;
                case DXGI.Format.BC2_Typeless: return DXGI.Format.BC2_UNorm;
                case DXGI.Format.BC3_Typeless: return DXGI.Format.BC3_UNorm;
                case DXGI.Format.BC4_Typeless: return DXGI.Format.BC4_UNorm;
                case DXGI.Format.BC5_Typeless: return DXGI.Format.BC5_UNorm;
                case DXGI.Format.B8G8R8A8_Typeless: return DXGI.Format.B8G8R8A8_UNorm;
                case DXGI.Format.B8G8R8X8_Typeless: return DXGI.Format.B8G8R8X8_UNorm;
                case DXGI.Format.BC7_Typeless: return DXGI.Format.BC7_UNorm;
                default: return format;
            }
        }

        private double GetDpiScale() {
            PresentationSource source = PresentationSource.FromVisual(this);
            return source.CompositionTarget.TransformToDevice.M11;
        }

        public double Dpi {
            get {
                return 96.0 * GetDpiScale();
            }
        }

        internal class HResults {
            // ReSharper disable InconsistentNaming
            public const int D2DERR_RECREATE_TARGET = unchecked((int)0x8899000C);
            public const int DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005);
            // ReSharper restore InconsistentNaming
        }


    }
}
