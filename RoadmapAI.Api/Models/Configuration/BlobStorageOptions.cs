namespace RoadmapAI.Api.Models.Configuration;

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "course-materials";
}
