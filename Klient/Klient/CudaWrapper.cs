using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

public static class CudaWrapper
{

    [DllImport("CudaRuntime.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void InvertImage(string inputPath, string outputPath);

    public static string ProcessImage(string inputPath, string operation)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"[CUDA] Wejściowy plik nie istnieje: {inputPath}");
            return null;
        }

        string outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + $"_{operation}" + Path.GetExtension(inputPath)
        );

        try
        {
            Console.WriteLine($"[CUDA] Wywoływanie InvertImage: {inputPath} -> {outputPath}");
            InvertImage(inputPath, outputPath);
            Console.WriteLine($"[CUDA] Zakończono zapis: {outputPath}");
        }
        catch (DllNotFoundException)
        {
            Console.WriteLine("[CUDA] Nie znaleziono CudaRuntime.dll!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CUDA] Błąd podczas przetwarzania: {ex.Message}");
        }

        return outputPath;
    }
}
