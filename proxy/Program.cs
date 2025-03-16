namespace proxy;

class Program
{
    static void Main(string[] args)
    {
        Retranslator client = new Retranslator(new byte[] { 127, 0, 0, 1 },
                5900, Encodings.Raw);
        client.Connect();
    }
}
