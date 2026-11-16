using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

/// <summary>
/// Compiles HLSL source to DXBC (shader model 5.1) at runtime using d3dcompiler_47.dll,
/// which ships with every Windows 10/11 installation. SDL3's Direct3D 12 GPU backend accepts
/// DXBC 5.1 bytecode directly, so no offline shader toolchain is needed to run on Windows.
/// </summary>
internal static unsafe class D3DShaderCompiler
{
    private const uint D3DCOMPILE_DEBUG = 1u << 0;
    private const uint D3DCOMPILE_OPTIMIZATION_LEVEL3 = 1u << 15;

    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    public static byte[] Compile(string source, string sourceName, string entrypoint, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("HLSL shaders can only be compiled on Windows.");
        }

        return Cache.GetOrAdd($"{sourceName}|{entrypoint}|{target}", _ => CompileInternal(source, sourceName, entrypoint, target));
    }

    private static byte[] CompileInternal(string source, string sourceName, string entrypoint, string target)
    {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
        byte[] nameBytes = Encoding.ASCII.GetBytes(sourceName + "\0");
        byte[] entrypointBytes = Encoding.ASCII.GetBytes(entrypoint + "\0");
        byte[] targetBytes = Encoding.ASCII.GetBytes(target + "\0");

        uint flags = D3DCOMPILE_OPTIMIZATION_LEVEL3;
#if DEBUG
        flags |= D3DCOMPILE_DEBUG;
#endif

        void* code = null;
        void* errors = null;
        int hr;

        fixed (byte* sourcePtr = sourceBytes)
        fixed (byte* namePtr = nameBytes)
        fixed (byte* entrypointPtr = entrypointBytes)
        fixed (byte* targetPtr = targetBytes)
        {
            hr = D3DCompile(sourcePtr, (nuint)sourceBytes.Length, namePtr, null, null, entrypointPtr, targetPtr, flags, 0, &code, &errors);
        }

        try
        {
            string messages = errors != null ? BlobToString(errors) : null;

            if (hr < 0 || code == null)
            {
                throw new InvalidOperationException(
                    $"HLSL compilation of '{sourceName}' ({entrypoint}, {target}) failed with HRESULT 0x{hr:X8}:{Environment.NewLine}{messages}");
            }

            if (!string.IsNullOrWhiteSpace(messages))
            {
                Console.WriteLine($"[D3DShaderCompiler] Warnings for '{sourceName}':{Environment.NewLine}{messages}");
            }

            return BlobToArray(code);
        }
        finally
        {
            Release(code);
            Release(errors);
        }
    }

    // ID3DBlob vtable: 0 QueryInterface, 1 AddRef, 2 Release, 3 GetBufferPointer, 4 GetBufferSize.
    private static void* GetBufferPointer(void* blob) => ((delegate* unmanaged[Stdcall]<void*, void*>)(*(void***)blob)[3])(blob);

    private static nuint GetBufferSize(void* blob) => ((delegate* unmanaged[Stdcall]<void*, nuint>)(*(void***)blob)[4])(blob);

    private static void Release(void* blob)
    {
        if (blob != null)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)blob)[2])(blob);
        }
    }

    private static byte[] BlobToArray(void* blob)
    {
        var size = (int)GetBufferSize(blob);
        var result = new byte[size];
        new ReadOnlySpan<byte>(GetBufferPointer(blob), size).CopyTo(result);
        return result;
    }

    private static string BlobToString(void* blob)
    {
        var size = (int)GetBufferSize(blob);
        return size == 0 ? string.Empty : Encoding.UTF8.GetString((byte*)GetBufferPointer(blob), size).TrimEnd('\0');
    }

    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern int D3DCompile(
        byte* pSrcData,
        nuint srcDataSize,
        byte* pSourceName,
        void* pDefines,
        void* pInclude,
        byte* pEntrypoint,
        byte* pTarget,
        uint flags1,
        uint flags2,
        void** ppCode,
        void** ppErrorMsgs);
}
