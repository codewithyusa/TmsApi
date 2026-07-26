namespace TmsApi.Infrastructure.Caching;

public static class CacheKeys
{
    public const string SchemaVersion = "v2";

    public static string AllCourses =>
        $"{SchemaVersion}:courses:all";

    public static string CourseById(int id) =>
        $"{SchemaVersion}:courses:{id}";
}