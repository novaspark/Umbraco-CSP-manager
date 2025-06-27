namespace Umbraco.Community.CSPManager.Models;
using System;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

[TableName((nameof(ScriptItem)))]
[PrimaryKey(nameof(Id), AutoIncrement = false)]
public class ScriptItem
{
	[PrimaryKeyColumn(AutoIncrement = false)]
	public Guid Id { get; set; }

	public string? Src { get; set; }

	public string? Description { get; set; }

	public string? FileLocation { get; set; }

	public int? Position { get; set; }

	public string? Hash { get; set; }

	public bool? SynchroniseOnStartup { get; set; }

	public DateTime? LastUpdated { get; set; }
}

public class UpdateModel
{
	public Guid Id { get; set; }
	public string? Description { get; set; }

	public bool? SynchroniseOnStartup { get; set; }
}
