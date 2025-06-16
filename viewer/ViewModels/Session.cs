using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using receiver;
using SkiaSharp;

namespace viewer.ViewModels;

public class Session
{
    private Task? receivingTask;
    private CancellationTokenSource tokenSrc = new CancellationTokenSource();
    private CancellationToken cancelToken;

    private Bitmap viewBitmap;
    private IPAddress? mcastIP = null;
    private ushort mcastPort = 0;

    public Session(ref Bitmap bitmap)
    {
        viewBitmap = bitmap;
    }

    public async void Start(IPAddress ip, ushort port)
    {
        tokenSrc = new CancellationTokenSource();
        cancelToken = tokenSrc.Token;

        if (mcastIP != null || mcastPort >= 1024)
        {
            byte firstByte = mcastIP.GetAddressBytes()[0];
            if (firstByte < 224 || firstByte > 239) return;
            
            mcastIP = ip;
            mcastPort = port;

            receivingTask = Task.Run(ReceiveBitmap, cancelToken);
            await receivingTask;
        }
        else return;
    }

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

            Receiver receiver = new Receiver(mcastIP, mcastPort);
            cancelToken.Register(receiver.Close);

            if (cancelToken.IsCancellationRequested)
            {
                receiver.Close();
                cancelToken.ThrowIfCancellationRequested();
            }

            int W = 0;
            int H = 0;
            ref ushort x = ref receiver.rectX;
            ref ushort y = ref receiver.rectY;
            ref byte[] pixelData = ref receiver.pixelData;
            ref byte p = ref receiver.pixelFormat;

            SKBitmap bitmap;
            SKData enc;
            MemoryStream ms;
            nint pixels;

            int iter;
            int i = 1;

            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    receiver.Close();
                    cancelToken.ThrowIfCancellationRequested();
                }

                receiver.ReceiveRectData();

                W = receiver.rectWidth * 10;
                H = receiver.rectHeight * 10;

                bitmap = new SKBitmap
                (
                    W, H,
                    SKColorType.Bgra8888,
                    SKAlphaType.Premul
                );

                pixels = bitmap.GetPixels();

                if (cancelToken.IsCancellationRequested)
                {
                    receiver.Close();
                    cancelToken.ThrowIfCancellationRequested();
                }

                receiver.ReceivePixels();

                SetPixels(p * (y * W + x), pixelData, pixels);

                iter = H * (10 - x / receiver.rectWidth) - y;

                while (iter-- > 1)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        receiver.Close();
                        cancelToken.ThrowIfCancellationRequested();
                    }

                    receiver.ReceiveRectData();

                    if (cancelToken.IsCancellationRequested)
                    {
                        receiver.Close();
                        cancelToken.ThrowIfCancellationRequested();
                    }

                    receiver.ReceivePixels();

                    SetPixels(p * (y * W + x), pixelData, pixels);
                }

                using (enc = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
                {

                    using (ms = new MemoryStream())
                    {
                        enc.SaveTo(ms);
                        ms.Position = 0;
                        viewBitmap = new Bitmap(ms);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void SetPixels(int offset, byte[] pixels, nint pixelsDest) =>
        Marshal.Copy(pixels, 0, pixelsDest + offset, pixels.Length);
}