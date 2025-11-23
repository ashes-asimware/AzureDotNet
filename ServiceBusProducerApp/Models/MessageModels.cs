using System.ComponentModel.DataAnnotations;

namespace ServiceBusProducerApp.Models
{
    public record class ClassRoomMessage
    (
        string School,
        string District,
        [Required]
        [EmailAddress]
        string SchoolEmail,
        DateTime ReportedOn,
        string Status,
        List<ClassRoomItem> Items
    );

    public record class ClassRoomItem
    (
        string StudentName,
        int StudentId,
        string TeacherName,
        int TeacherId,
        string Course,
        int CourseId,
        int Score
    );

    public record struct MessageGenerationRequest
    (
        int Count = 1,
        string? QueueName = null,
        string? TopicName = null,
        decimal MinAmount = 10,
        decimal MaxAmount = 1000,
        int MinItems = 1,
        int MaxItems = 10,
        string? Priority = null
    );

    public record struct MessageGenerationResponse
    (
        int MessagesSent,
        string Destination,
        List<string> MessageIds,
        string Status = "Success",
        string? ErrorMessage = null
    );
}
