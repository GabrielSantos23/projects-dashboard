using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ProjectDashboard.Shared.Models;

public class Project
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Path { get; set; } = string.Empty;
    
    public DateTime? LastCommit { get; set; }
    
    public string? LastCommitMessage { get; set; }
    
    public string? CurrentBranch { get; set; }
    
    public bool IsPinned { get; set; }
    
    public DateTime? ScannedAt { get; set; }

    
    public bool IsForked { get; set; }
    public string? RemoteUrl { get; set; }
    public string? UpstreamUrl { get; set; }

    
    public string Tags { get; set; } = "";          
    public string Notes { get; set; } = "";          
    public string Goals { get; set; } = "[]";        

    
    public string MetadataJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, string> Metadata
    {
        get => JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson) ?? new();
        set => MetadataJson = JsonSerializer.Serialize(value);
    }

    
    [NotMapped]
    public List<GoalItem> GoalItems
    {
        get
        {
            try { return JsonSerializer.Deserialize<List<GoalItem>>(Goals) ?? new(); }
            catch { return new(); }
        }
        set => Goals = JsonSerializer.Serialize(value);
    }

    
    [NotMapped]
    public string[] TagList => string.IsNullOrWhiteSpace(Tags)
        ? Array.Empty<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    

    [NotMapped]
    public string Status
    {
        get
        {
            if (LastCommit == null) return "Unknown";
            var days = (DateTime.UtcNow - LastCommit.Value).TotalDays;
            if (days <= 7) return "Active";
            if (days <= 30) return "Recent";
            if (days <= 90) return "Stalled";
            return "Archived";
        }
    }

    [NotMapped]
    public string TimeAgo
    {
        get
        {
            if (LastCommit == null) return "never";
            var span = DateTime.UtcNow - LastCommit.Value;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} week{((int)(span.TotalDays / 7) == 1 ? "" : "s")} ago";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)} month{((int)(span.TotalDays / 30) == 1 ? "" : "s")} ago";
            return $"{(int)(span.TotalDays / 365)} year{((int)(span.TotalDays / 365) == 1 ? "" : "s")} ago";
        }
    }

    [NotMapped]
    public string[] TechPills
    {
        get
        {
            var pills = new List<string>();
            var meta = Metadata;
            if (meta.TryGetValue("techs", out var techs) && !string.IsNullOrWhiteSpace(techs))
            {
                pills.AddRange(techs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else if (meta.TryGetValue("inferred_type", out var inferredType) && inferredType != "Unknown")
            {
                pills.Add(inferredType);
            }
            return pills.ToArray();
        }
    }

    
    [NotMapped]
    public int ContributorCount
    {
        get
        {
            var meta = Metadata;
            return meta.TryGetValue("contributors", out var c) && int.TryParse(c, out var n) ? n : 0;
        }
    }

    [NotMapped]
    public int TotalCommits
    {
        get
        {
            var meta = Metadata;
            return meta.TryGetValue("total_commits", out var c) && int.TryParse(c, out var n) ? n : 0;
        }
    }

    [NotMapped]
    public int BranchCount
    {
        get
        {
            var meta = Metadata;
            return meta.TryGetValue("branch_count", out var c) && int.TryParse(c, out var n) ? n : 0;
        }
    }

    [NotMapped]
    public int DocCount
    {
        get
        {
            var meta = Metadata;
            return meta.TryGetValue("doc_count", out var c) && int.TryParse(c, out var n) ? n : 0;
        }
    }

    [NotMapped]
    public string OwnershipLabel => IsForked ? "Fork" : "Owned";

    [NotMapped]
    public string Initial => !string.IsNullOrEmpty(Name) ? Name[0].ToString().ToUpper() : "?";
}

public class GoalItem
{
    public string Text { get; set; } = "";
    public bool Done { get; set; }
}
