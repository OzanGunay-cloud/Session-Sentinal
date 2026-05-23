using FluentValidation;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Validators;

public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TokenHash).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}
