using System.ComponentModel.DataAnnotations;

namespace ServiceBusConsumerApp.Models
{
    /// <summary>
    /// Example message model for demonstration purposes.
    /// Replace with your actual message structure.
    /// </summary>
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
}
