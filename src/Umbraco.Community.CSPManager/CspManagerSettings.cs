using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Community.CSPManager;

public class CspManagerSettings
{
	public string SiteUrl { get; set; } = string.Empty;
	public string HashAlgorithm { get; set; } = "sha256";
}