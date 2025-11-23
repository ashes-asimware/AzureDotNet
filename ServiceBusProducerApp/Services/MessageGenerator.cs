using Bogus;
using ServiceBusProducerApp.Models;

namespace ServiceBusProducerApp.Services
{
    public interface IMessageGenerator
    {
        List<ClassRoomMessage> GenerateClassRooms(int count, int minScore, int maxScore, int minStudents, int maxStudents);
    }

    public class MessageGenerator : IMessageGenerator
    {
        private readonly Faker<ClassRoomItem> _classRoomItemFaker;
        private readonly Faker<ClassRoomMessage> _classRoomFaker;

        public MessageGenerator()
        {
            // Configure fake data generator for classroom items (students)
            _classRoomItemFaker = new Faker<ClassRoomItem>()
                .CustomInstantiator(f => new ClassRoomItem(
                    StudentName: f.Name.FullName(),
                    StudentId: f.Random.Number(1000, 9999),
                    TeacherName: f.Name.FullName(),
                    TeacherId: f.Random.Number(100, 999),
                    Course: f.PickRandom("Mathematics", "Science", "English", "History", "Art", "Physical Education", "Music", "Computer Science"),
                    CourseId: f.Random.Number(1, 100),
                    Score: f.Random.Number(0, 100)
                ));

            // Configure fake data generator for classrooms
            _classRoomFaker = new Faker<ClassRoomMessage>()
                .CustomInstantiator(f => new ClassRoomMessage(
                    School: f.Company.CompanyName() + " School",
                    District: f.Address.City() + " District",
                    SchoolEmail: f.Internet.Email(),
                    ReportedOn: f.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now),
                    Status: f.PickRandom("Pending", "Processing", "Confirmed", "Completed"),
                    Items: new List<ClassRoomItem>()
                ));
        }

        public List<ClassRoomMessage> GenerateClassRooms(int count, int minScore, int maxScore, int minStudents, int maxStudents)
        {
            var classRooms = new List<ClassRoomMessage>();

            for (int i = 0; i < count; i++)
            {
                var classRoom = _classRoomFaker.Generate();
                
                // Generate random number of students
                var studentCount = new Random().Next(minStudents, maxStudents + 1);
                var students = _classRoomItemFaker.Generate(studentCount);

                // Adjust scores to be within bounds
                var adjustedStudents = students.Select(item =>
                {
                    var adjustedScore = item.Score;
                    if (adjustedScore < minScore)
                        adjustedScore = minScore;
                    else if (adjustedScore > maxScore)
                        adjustedScore = maxScore;

                    return item with { Score = adjustedScore };
                }).ToList();

                // Create new classroom with adjusted students
                classRoom = classRoom with { Items = adjustedStudents };
                classRooms.Add(classRoom);
            }

            return classRooms;
        }
    }
}
