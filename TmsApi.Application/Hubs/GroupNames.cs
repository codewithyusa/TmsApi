namespace TmsApi.Application.Hubs;

public static class GroupNames
{
    public static string Student(string studentId)
        => $"student-{studentId}";

    public static string Course(string courseCode)
        => $"course-{courseCode}";
}