using FluentValidation;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Validators;

public sealed class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.IpAddress).NotEmpty();
        RuleFor(x => x.FingerprintHash).NotEmpty();
    }
}
