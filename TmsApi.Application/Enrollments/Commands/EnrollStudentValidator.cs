using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("StudentId must be greater than zero.");

        RuleFor(x => x.CourseCode)
            .NotEmpty()
            .WithMessage("CourseCode is required.")
            .Matches("^[A-Z]{2,3}-[0-9]{3}$")
            .WithMessage("Course code must follow the format XX-000 or XXX-000 (e.g., CS-101 or CSE-101).")
            .MaximumLength(50)
            .WithMessage("CourseCode cannot exceed 50 characters.");
    }
}