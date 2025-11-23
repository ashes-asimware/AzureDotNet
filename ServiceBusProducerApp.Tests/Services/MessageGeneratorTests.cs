using Xunit;
using FluentAssertions;
using ServiceBusProducerApp.Services;

namespace ServiceBusProducerApp.Tests.Services
{
    public class MessageGeneratorTests
    {
        private readonly MessageGenerator _generator;

        public MessageGeneratorTests()
        {
            _generator = new MessageGenerator();
        }

        [Fact]
        public void GenerateClassRooms_WithValidParameters_ReturnsCorrectCount()
        {
            // Arrange
            var count = 5;
            var minScore = 60;
            var maxScore = 100;
            var minStudents = 10;
            var maxStudents = 30;

            // Act
            var result = _generator.GenerateClassRooms(count, minScore, maxScore, minStudents, maxStudents);

            // Assert
            result.Should().HaveCount(count);
        }

        [Fact]
        public void GenerateClassRooms_GeneratesMessagesWithStudents()
        {
            // Arrange
            var count = 3;
            var minScore = 0;
            var maxScore = 100;
            var minStudents = 5;
            var maxStudents = 10;

            // Act
            var result = _generator.GenerateClassRooms(count, minScore, maxScore, minStudents, maxStudents);

            // Assert
            result.Should().AllSatisfy(classRoom =>
            {
                classRoom.Items.Should().NotBeEmpty();
                classRoom.Items.Count.Should().BeInRange(minStudents, maxStudents);
            });
        }

        [Fact]
        public void GenerateClassRooms_EnforcesScoreBounds()
        {
            // Arrange
            var count = 10;
            var minScore = 70;
            var maxScore = 90;
            var minStudents = 5;
            var maxStudents = 10;

            // Act
            var result = _generator.GenerateClassRooms(count, minScore, maxScore, minStudents, maxStudents);

            // Assert
            result.Should().AllSatisfy(classRoom =>
            {
                classRoom.Items.Should().AllSatisfy(student =>
                {
                    student.Score.Should().BeInRange(minScore, maxScore);
                });
            });
        }

        [Fact]
        public void GenerateClassRooms_GeneratesUniqueSchools()
        {
            // Arrange
            var count = 20;

            // Act
            var result = _generator.GenerateClassRooms(count, 0, 100, 1, 10);

            // Assert
            var schoolNames = result.Select(c => c.School).ToList();
            schoolNames.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void GenerateClassRooms_PopulatesAllRequiredFields()
        {
            // Arrange
            var count = 1;

            // Act
            var result = _generator.GenerateClassRooms(count, 0, 100, 1, 5);
            var classRoom = result.First();

            // Assert
            classRoom.School.Should().NotBeNullOrEmpty();
            classRoom.District.Should().NotBeNullOrEmpty();
            classRoom.SchoolEmail.Should().NotBeNullOrEmpty();
            classRoom.ReportedOn.Should().BeCloseTo(DateTime.Now, TimeSpan.FromDays(30));
            classRoom.Status.Should().NotBeNullOrEmpty();
            classRoom.Items.Should().NotBeEmpty();
        }

        [Fact]
        public void GenerateClassRooms_PopulatesStudentFields()
        {
            // Arrange
            var count = 1;

            // Act
            var result = _generator.GenerateClassRooms(count, 0, 100, 1, 5);
            var student = result.First().Items.First();

            // Assert
            student.StudentName.Should().NotBeNullOrEmpty();
            student.StudentId.Should().BeGreaterThan(0);
            student.TeacherName.Should().NotBeNullOrEmpty();
            student.TeacherId.Should().BeGreaterThan(0);
            student.Course.Should().NotBeNullOrEmpty();
            student.CourseId.Should().BeGreaterThan(0);
            student.Score.Should().BeInRange(0, 100);
        }

        [Theory]
        [InlineData(1, 0, 100, 1, 50)]
        [InlineData(10, 50, 75, 5, 15)]
        [InlineData(100, 0, 50, 1, 3)]
        public void GenerateClassRooms_WithVariousParameters_GeneratesValidData(
            int count, int minScore, int maxScore, int minStudents, int maxStudents)
        {
            // Act
            var result = _generator.GenerateClassRooms(count, minScore, maxScore, minStudents, maxStudents);

            // Assert
            result.Should().HaveCount(count);
            result.Should().AllSatisfy(classRoom =>
            {
                classRoom.Items.Count.Should().BeInRange(minStudents, maxStudents);
                classRoom.Items.Should().AllSatisfy(student =>
                {
                    student.Score.Should().BeInRange(minScore, maxScore);
                });
            });
        }
    }
}
