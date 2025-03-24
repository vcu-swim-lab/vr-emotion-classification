using System;
using System.Runtime.InteropServices;

public static class Mobilenet
{
    const string dll = "Mobilenet-v2-DLL.dll";

    [DllImport(dll)]
    public static extern int load_model(string model);

    [DllImport(dll)]
    public static extern long run_inference(byte[] image, int[] output, int length);

    [DllImport(dll)]
    public static extern void cleanup();
}
