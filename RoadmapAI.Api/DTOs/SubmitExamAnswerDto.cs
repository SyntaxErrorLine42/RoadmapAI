namespace RoadmapAI.Api.DTOs;

public class SubmitExamAnswerDto
{
    public Guid QuestionId { get; set; }
    public string SelectedOption { get; set; } = string.Empty;
}
