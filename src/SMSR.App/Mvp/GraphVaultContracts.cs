namespace SMSR.App.Mvp;

public sealed record GraphVaultConnect(string ProjectId, string VaultPath, bool AutoSync = false, bool Create = false, bool Confirm = false);
public sealed record GraphVaultBinding(string VaultPath, bool AutoSync);
public sealed record GraphVaultEntry(string Hash, string NodeId);
public sealed record GraphVaultManifest(string ProjectId, Dictionary<string, GraphVaultEntry> Files);
public sealed record GraphVaultResult(int Revision, int Written, int Unchanged, string[] Conflicts, int Retired);
public sealed record GraphVaultStatus(GraphVaultBinding? Binding, GraphVaultResult? LastSync, string? Error);
