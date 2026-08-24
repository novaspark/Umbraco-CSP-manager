namespace Umbraco.Community.CSPManager;

public sealed class CspManagerOptions
{
	public bool DisableBackOfficeHeader { get; set; } = false;

	/// <summary>
	/// The Subresource Integrity hash algorithm used when generating script hashes.
	/// One of "sha256", "sha384" or "sha512". Defaults to "sha384".
	/// </summary>
	public string ScriptHashAlgorithm { get; set; } = "sha384";
}