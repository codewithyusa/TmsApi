using TmsApi.Application.Grading;

namespace TmsApi.Tests;

public class GradingServiceTests
{
    [Fact]
    public void CalculateLetterGrade_HighScore_ReturnsDistinction()
    {
        // Arrange
        var service = new GradingService();

        // Act
        var result = service.CalculateLetterGrade(score: 85m, maxScore: 100m);

        // Assert
        Assert.Equal(GradeLevel.Distinction, result);
    }
}