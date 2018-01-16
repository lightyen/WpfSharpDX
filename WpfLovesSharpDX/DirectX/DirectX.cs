using System;

namespace SharpDX
{
    public static class DirectX
    {
        private static Direct2D1.Factory d2dfactory;

        public static Direct2D1.Factory Factory2D
        {
            set { d2dfactory = value; }
            get { return d2dfactory; }
        }

        private static WIC.ImagingFactory imageFactory;

        public static WIC.ImagingFactory ImageFactory => imageFactory;

        private static DirectWrite.Factory writeFactory;

        public static DirectWrite.Factory WriteFactory => writeFactory;

        public static void CreateIndependentResource()
        {
            Factory2D = new Direct2D1.Factory();
            imageFactory = new WIC.ImagingFactory();
            writeFactory = new DirectWrite.Factory();
        }

        public static void ReleaseIndependentResource()
        {
            Utilities.Dispose(ref imageFactory);
            Utilities.Dispose(ref writeFactory);
            Utilities.Dispose(ref d2dfactory);
        }

        public static void EnumAdpter()
        {
            SharpDX.DXGI.Factory4 dxgiFactory = new SharpDX.DXGI.Factory4();
            foreach (var a in dxgiFactory.Adapters)
            {
                Console.WriteLine($"{a.Description.Description}");
            }
        }
    }
}
