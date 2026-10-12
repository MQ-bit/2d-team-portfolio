# Potion balance regression checks

Run from the repository root with .NET 9 or newer:

```powershell
dotnet run --project Assets/02.Scripts/Potion/Tests~/BalanceRuntime/BalanceRuntime.csproj
```

The harness compiles the production effect, receiver, status, timeline, and vaporization batch code and reads the real potion assets. It checks the 15 combination profiles, fractional remaining output, elemental multipliers, per-target caps, proportional damage, self damage, direct/residual/vaporization life steal, freeze rules, control refresh, and combination entry pulses.

Unity lifecycle, native physics, rendering, component lookup, and sound are stubbed. These checks do not replace a Unity play-mode check of scene contacts, reaction VFX, or timing at different frame rates. `Tests~` keeps the harness out of Unity's gameplay compilation.
