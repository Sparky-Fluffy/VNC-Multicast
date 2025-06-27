using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;
using receiver;
using SkiaSharp;

namespace viewer.ViewModels;

public class Session : ReactiveObject
{
    private Task? receivingTask;
    private CancellationTokenSource tokenSrc = new CancellationTokenSource();
    private CancellationToken cancelToken;

    private Bitmap viewBitmap;
    public Bitmap ViewBitmap
    {
        get => viewBitmap;
        set => this.RaiseAndSetIfChanged(ref viewBitmap, value);
    }

    private IPAddress? mcastIP = null;
    private ushort mcastPort = 0;
    private int mcastIfaceIndex = 0;

    public async void Start(string ip, ushort port, int ifaceIndex)
    {
        tokenSrc = new CancellationTokenSource();
        cancelToken = tokenSrc.Token;

        if (IPAddress.TryParse(ip, out mcastIP) || port >= 1024 || ifaceIndex >= 0)
        {
            byte firstByte = mcastIP.GetAddressBytes()[0];
            if (firstByte < 224 || firstByte > 239) return;

            mcastPort = port;
            ViewBitmap = null;

            receivingTask = Task.Run(ReceiveBitmap, cancelToken);
            await receivingTask;
        }
    }

    public void SetInterface(int ifaceIndex) => mcastIfaceIndex = ifaceIndex;

    public void Cancel()
    {
        tokenSrc.Cancel();
        tokenSrc.Dispose();

        mcastIP = null;
        mcastPort = 0;
    }

    private async Task ReceiveBitmap()
    {
        try
        {
            cancelToken.ThrowIfCancellationRequested();

            Receiver receiver = new Receiver(mcastIP, mcastPort, mcastIfaceIndex);

            if (cancelToken.IsCancellationRequested)
            {
                receiver.Close();
                cancelToken.ThrowIfCancellationRequested();
            }

            ref ushort W = ref receiver.Width;
            ref ushort H = ref receiver.Height;
            ref ushort w = ref receiver.rectWidth;
            ref ushort h = ref receiver.rectHeight;
            ref ushort x = ref receiver.rectX;
            ref ushort y = ref receiver.rectY;
            ref byte[] pixelData = ref receiver.data;
            ref byte p = ref receiver.pixelFormat;

            SKData enc;
            MemoryStream ms;

            if (cancelToken.IsCancellationRequested)
            {
                receiver.Close();
                cancelToken.ThrowIfCancellationRequested();
            }
            
            receiver.ReceiveRectData();

            SKBitmap bitmap = new SKBitmap
            (
                W, H,
                SKColorType.Bgra8888,
                SKAlphaType.Premul
            );

            nint pixels = bitmap.GetPixels();

            while (true)
            {
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            receiver.Close();
                            cancelToken.ThrowIfCancellationRequested();
                        }
                        receiver.ReceivePixel();
                        SetPixels(p * (y * W + i * W + x + j), pixelData, p, pixels);
                    }
                }

                using (enc = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
                {
                    using (ms = new MemoryStream())
                    {
                        enc.SaveTo(ms);
                        ms.Position = 0;
                        ViewBitmap = new Bitmap(ms);
                    }
                }

                receiver.ReceiveRectData();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SetPixels(int offset, byte[] pixels, int size, nint pixelsDest) =>
        Marshal.Copy(pixels, 1, pixelsDest + offset, size);
}