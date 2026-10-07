# Compile the actual shader functions with D3D11's compiler, using minimal URP
# transform/texture declarations. This catches HLSL errors, not Unity rendering setup.
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class PotionShaderCompiler
{
    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
    private static extern int D3DCompile(byte[] source, UIntPtr length, string name,
        IntPtr defines, IntPtr include, string entry, string target, uint flags,
        uint effectFlags, out IntPtr code, out IntPtr errors);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr BufferPointer(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate UIntPtr BufferSize(IntPtr self);
    public static void Compile(string source, string name, string entry, string target)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        IntPtr code, errors;
        int result = D3DCompile(bytes, (UIntPtr)bytes.Length, name, IntPtr.Zero,
            IntPtr.Zero, entry, target, 0, 0, out code, out errors);
        string message = "";
        try
        {
            if (errors != IntPtr.Zero)
            {
                IntPtr table = Marshal.ReadIntPtr(errors);
                var pointer = Marshal.GetDelegateForFunctionPointer<BufferPointer>(Marshal.ReadIntPtr(table, 3 * IntPtr.Size));
                var size = Marshal.GetDelegateForFunctionPointer<BufferSize>(Marshal.ReadIntPtr(table, 4 * IntPtr.Size));
                message = Marshal.PtrToStringAnsi(pointer(errors), checked((int)size(errors).ToUInt64()));
            }
            if (result < 0) throw new Exception(name + " " + entry + ": " + message);
        }
        finally
        {
            if (code != IntPtr.Zero) Marshal.Release(code);
            if (errors != IntPtr.Zero) Marshal.Release(errors);
        }
    }
}
'@
$shaderDirectory = Join-Path $PSScriptRoot '../../../09.Resources/Resources'
$prefix = @'
#define half float
#define half3 float3
#define half4 float4
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#define TEXTURE2D(name) Texture2D name;
#define SAMPLER(name) SamplerState name;
#define SAMPLE_TEXTURE2D(tex,samplerName,uv) tex.Sample(samplerName,uv)
float4 TransformObjectToHClip(float3 p) { return float4(p, 1); }
float3 TransformObjectToWorld(float3 p) { return p; }
'@
foreach ($name in @('PotionAreaVisual', 'PotionFireVortex', 'PotionFrostCrystals', 'PotionHealingSpring', 'PotionPurifyingLife')) {
    $source = Get-Content -LiteralPath (Join-Path $shaderDirectory "$name.shader") -Raw
    $body = [regex]::Match($source, 'HLSLINCLUDE(.*?)ENDHLSL', 'Singleline').Groups[1].Value
    $body = [regex]::Replace($body, '#include "([^"]+)"', {
        param($match)
        $includePath = $match.Groups[1].Value
        if ($includePath.StartsWith('Packages/')) { return '' }
        return Get-Content -LiteralPath (Join-Path $shaderDirectory $includePath) -Raw
    })
    [PotionShaderCompiler]::Compile($prefix + "`n" + $body, $name, 'Vert', 'vs_4_0')
    [PotionShaderCompiler]::Compile($prefix + "`n" + $body, $name, 'Frag', 'ps_4_0')
    Write-Output "PASS: $name vertex + fragment"
}
